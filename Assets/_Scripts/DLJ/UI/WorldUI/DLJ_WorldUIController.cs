using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.DLJ.UI.WorldUI
{
    /// <summary>
    /// 기물 위 UI의 공통 진입점.
    /// 효과 코드는 슬롯 ID와 데이터만 넘기고, 실제 UI 구조는 프리팹이 결정한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DLJ_WorldUIController : MonoBehaviour
    {
        [Tooltip("자식의 DLJ_WorldUISlot을 Awake에서 자동 등록한다.")]
        [SerializeField] private bool autoDiscoverChildSlots = true;

        [Tooltip("자식이 아닌 슬롯까지 함께 사용할 때만 넣는다.")]
        [SerializeField] private List<DLJ_WorldUISlot> additionalSlots = new();

        private readonly Dictionary<DLJ_WorldUISlotId, DLJ_WorldUISlot> _slotsById = new();
        private readonly HashSet<DLJ_WorldUISlotId> _reportedMissingIds = new();

        private void Awake()
        {
            RebuildLookup(true);
        }

        public bool Set(DLJ_WorldUISlotId id, DLJ_WorldUIData data)
        {
            if (TryGetSlot(id, out DLJ_WorldUISlot slot))
            {
                slot.SetPersistent(data);
                return true;
            }

            ReportMissingSlot(id);
            return false;
        }

        /// <summary>
        /// 일회성 데이터를 잠깐 표시한 뒤 그 전에 Set한 지속 데이터로 복귀한다.
        /// 투자 비용, 피해량처럼 순간적으로 보여줄 값에 사용한다.
        /// </summary>
        public bool ShowTemporary(DLJ_WorldUISlotId id, DLJ_WorldUIData data, float duration)
        {
            if (TryGetSlot(id, out DLJ_WorldUISlot slot))
            {
                slot.ShowTemporary(data, duration);
                return true;
            }

            ReportMissingSlot(id);
            return false;
        }

        public bool Hide(DLJ_WorldUISlotId id)
        {
            if (TryGetSlot(id, out DLJ_WorldUISlot slot))
            {
                slot.Hide();
                return true;
            }

            ReportMissingSlot(id);
            return false;
        }

        public void HideAll()
        {
            if (_slotsById.Count == 0)
                RebuildLookup(false);

            foreach (DLJ_WorldUISlot slot in _slotsById.Values)
                slot.Hide();
        }

        [ContextMenu("Refresh Slot Cache")]
        public void RefreshSlotCache()
        {
            _reportedMissingIds.Clear();
            RebuildLookup(true);
        }

        public bool TryGetSlot(DLJ_WorldUISlotId id, out DLJ_WorldUISlot slot)
        {
            if (_slotsById.Count == 0)
                RebuildLookup(false);

            if (id != DLJ_WorldUISlotId.None)
                return _slotsById.TryGetValue(id, out slot);

            slot = null;
            return false;
        }

        private void RebuildLookup(bool reportInvalidEntries)
        {
            _slotsById.Clear();

            if (autoDiscoverChildSlots)
            {
                DLJ_WorldUISlot[] childSlots = GetComponentsInChildren<DLJ_WorldUISlot>(true);
                foreach (DLJ_WorldUISlot childSlot in childSlots)
                {
                    if (BelongsToThisController(childSlot))
                        RegisterSlot(childSlot, reportInvalidEntries);
                }
            }

            foreach (DLJ_WorldUISlot additionalSlot in additionalSlots)
                RegisterSlot(additionalSlot, reportInvalidEntries);
        }

        private void RegisterSlot(DLJ_WorldUISlot slot, bool reportInvalidEntry)
        {
            if (slot == null || slot.Id == DLJ_WorldUISlotId.None)
            {
                if (reportInvalidEntry)
                    Debug.LogWarning($"{name}: ID가 지정되지 않은 World UI 슬롯이 있습니다.", slot != null ? slot : this);
                return;
            }

            if (_slotsById.TryGetValue(slot.Id, out DLJ_WorldUISlot registered))
            {
                if (registered == slot) return;

                if (reportInvalidEntry)
                    Debug.LogWarning($"{name}: World UI 슬롯 ID '{slot.Id}'가 중복됩니다.", slot);
                return;
            }

            _slotsById.Add(slot.Id, slot);
        }

        private bool BelongsToThisController(DLJ_WorldUISlot slot)
        {
            Transform current = slot.transform;
            while (current != null)
            {
                if (current.TryGetComponent(out DLJ_WorldUIController owner))
                    return owner == this;

                current = current.parent;
            }

            return false;
        }

        private void ReportMissingSlot(DLJ_WorldUISlotId id)
        {
            if (!_reportedMissingIds.Add(id)) return;

            Debug.LogWarning($"{name}: World UI 슬롯 '{id}'을 찾지 못했습니다.", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildLookup(false);
        }
#endif
    }
}
