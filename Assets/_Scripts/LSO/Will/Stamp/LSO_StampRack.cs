using System;
using System.Collections.Generic;
using _Scripts.LSO.Reward;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.LSO.Will.Stamp
{
    /// <summary>
    /// 보유한 도장을 양초 옆에 원형으로 늘어놓고, 지금 든 도장을 정한다.
    ///
    /// 고른 도장이 무엇인지는 여기 하나만 안다.
    /// 슬롯들은 상태를 갖지 않고 랙이 시키는 대로 보여주기만 한다 —
    /// 두 곳이 같은 값을 들면 어긋났을 때 어느 쪽이 맞는지 정할 방법이 없다.
    ///
    /// 손패에 찍는 것은 여기서 하지 않는다. 이 랙은 "지금 무엇을 들고 있나"까지다.
    /// 찍는 쪽은 Selected를 읽고, 찍은 뒤에 Deselect를 부를지는 스스로 정한다.
    ///
    /// 씬 배선: 양초 옆 빈 오브젝트에 붙이고 Slot Prefab과 Center를 연결할 것.
    /// </summary>
    public class LSO_StampRack : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("복제할 도장 슬롯 원본.")]
        [SerializeField] private LSO_StampSlot slotPrefab;

        [Tooltip("원의 중심. 비워두면 이 오브젝트 자신을 쓴다.")]
        [SerializeField] private Transform center;

        [Header("원형 배치")]
        [Tooltip("중심에서 도장까지의 거리.")]
        [SerializeField, Min(0f)] private float radius = 0.5f;

        [Tooltip("첫 도장이 놓일 각도(도). 0이면 기준 자리의 로컬 +X 방향이다.")]
        [SerializeField] private float startAngle;

        [Tooltip("도장이 늘어설 전체 각도(도). 360이면 한 바퀴를 채운다.")]
        [SerializeField, Range(1f, 360f)] private float sweepAngle = 360f;

        [Tooltip("원이 놓일 평면. 켜면 바닥에(XZ), 끄면 세워서(XY) 배치한다.")]
        [SerializeField] private bool layFlat = true;

        [Header("숫자키")]
        [Tooltip("켜면 1~5 로도 고를 수 있다. 얻은 순서와 같은 번호다.")]
        [SerializeField] private bool useNumberKeys = true;

        [Tooltip("숫자키로 고를 수 있는 최대 개수. 기획서 기준 5개.")]
        [SerializeField, Range(1, 9)] private int numberKeyCount = 5;

        [Header("반응")]
        [Tooltip("든 도장이 바뀔 때마다. 아무것도 안 들었으면 None이 온다.\n" +
                 "'BACKSPACE 시 취소' 안내를 여기서 켜고 끄면 된다.")]
        [SerializeField] private LSO_WillTypeEvent onSelectionChanged;

        private readonly List<LSO_StampSlot> _slots = new();

        /// <summary>지금 든 도장. 아무것도 안 들었으면 None.</summary>
        public LSO_WillType Selected { get; private set; } = LSO_WillType.None;

        /// <summary>도장을 들고 있는지. 보드 클릭을 막을 때 본다.</summary>
        public bool HasSelection => Selected != LSO_WillType.None;

        /// <summary>든 도장이 바뀌었을 때. 코드로 구독하는 쪽이 쓴다.</summary>
        public event Action<LSO_WillType> SelectionChanged;

        private Transform Center => center != null ? center : transform;

        private void Awake()
        {
            if (slotPrefab == null)
                Debug.LogError($"{name}: Slot Prefab이 없어 도장을 놓을 수 없습니다.", this);
        }

        private void OnEnable()
        {
            LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

            if (library != null) library.OnItemLibraryUpdated += Rebuild;

            Rebuild();
        }

        private void OnDisable()
        {
            LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

            if (library != null) library.OnItemLibraryUpdated -= Rebuild;

            // 꺼질 때 든 것을 놓는다. 든 채로 굳으면 다시 켰을 때
            // 아무것도 안 보이는데 클릭이 도장 찍기로 먹는다.
            Deselect();
        }

        /// <summary>
        /// 보유 목록대로 슬롯을 다시 만든다.
        ///
        /// 보상으로 유언을 받으면 라이브러리가 알려주므로 그때 다시 부른다.
        /// 순서는 재고 목록 순서를 그대로 따른다 — 그것이 곧 얻은 순서이고, 숫자키 번호다.
        /// </summary>
        public void Rebuild()
        {
            ClearSlots();

            if (slotPrefab == null) return;

            List<LSO_WillType> wills = CollectWills();

            for (int i = 0; i < wills.Count; i++)
            {
                LSO_StampSlot slot = Instantiate(slotPrefab, Center);

                slot.transform.localPosition = PositionOf(i, wills.Count);
                slot.transform.localRotation = Quaternion.identity;

                slot.Bind(wills[i], HandleSlotClicked);

                _slots.Add(slot);
            }

            // 들고 있던 도장이 목록에서 사라졌을 수 있다.
            if (HasSelection && IndexOf(Selected) < 0)
                Deselect();
            else
                RefreshMarks();
        }

        /// <summary>
        /// 보유한 유언을 얻은 순서대로 모은다.
        ///
        /// 재고는 중복을 허용하므로 같은 유언이 여러 번 들어 있을 수 있다.
        /// 도장은 종류마다 하나면 되니 처음 나온 것만 남긴다.
        /// </summary>
        private static List<LSO_WillType> CollectWills()
        {
            var result = new List<LSO_WillType>();

            LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

            if (library == null) return result;

            foreach (DLJ_WillDataSO will in library.UnlockedWills)
            {
                if (will == null) continue;

                LSO_WillType type = will.WillType;

                if (type == LSO_WillType.None) continue;
                if (result.Contains(type)) continue;

                result.Add(type);
            }

            return result;
        }

        /// <summary>
        /// 원 위의 자리. 첫 칸이 Start Angle에 놓이고 시계 반대 방향으로 퍼진다.
        ///
        /// 한 바퀴(360)일 때는 마지막 칸이 첫 칸과 겹치지 않게 개수로 나누고,
        /// 부채꼴일 때는 양 끝에 도장이 오도록 개수-1로 나눈다.
        /// </summary>
        private Vector3 PositionOf(int index, int total)
        {
            if (total <= 1) return Vector3.zero;

            bool fullCircle = Mathf.Approximately(sweepAngle, 360f);

            float step = fullCircle ? sweepAngle / total : sweepAngle / (total - 1);

            float deg = startAngle + step * index;
            float rad = deg * Mathf.Deg2Rad;

            float x = Mathf.Cos(rad) * radius;
            float y = Mathf.Sin(rad) * radius;

            return layFlat ? new Vector3(x, 0f, y) : new Vector3(x, y, 0f);
        }

        private void HandleSlotClicked(LSO_StampSlot slot)
        {
            if (slot == null) return;

            // 든 것을 다시 누르면 놓는다. 도장을 놓는 방법이 BACKSPACE 하나뿐이면
            // 그 키를 모르는 사람은 빠져나올 길이 없다.
            if (Selected == slot.Will)
            {
                Deselect();
                return;
            }

            Select(slot.Will);
        }

        /// <summary>도장을 든다. 보유하지 않은 유언이면 아무 일도 하지 않는다.</summary>
        public void Select(LSO_WillType will)
        {
            if (will == LSO_WillType.None)
            {
                Deselect();
                return;
            }

            if (IndexOf(will) < 0)
            {
                Debug.LogWarning($"{name}: {will} 도장을 갖고 있지 않습니다.", this);
                return;
            }

            if (Selected == will) return;

            Selected = will;

            RefreshMarks();
            Raise();
        }

        /// <summary>든 도장을 놓는다. BACKSPACE가 이것을 부른다.</summary>
        public void Deselect()
        {
            if (!HasSelection) return;

            Selected = LSO_WillType.None;

            RefreshMarks();
            Raise();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // 놓는 것을 먼저 본다. 같은 프레임에 둘 다 눌렸다면 놓는 쪽이 이겨야
            // "취소했는데 다시 들렸다"가 생기지 않는다.
            if (Keyboard.current.backspaceKey.wasPressedThisFrame)
            {
                Deselect();
                return;
            }

            if (!useNumberKeys) return;

            for (int i = 0; i < numberKeyCount && i < _slots.Count; i++)
            {
                // digit1Key 부터 차례로. 숫자는 얻은 순서와 같다.
                Key key = Key.Digit1 + i;

                if (!Keyboard.current[key].wasPressedThisFrame) continue;

                HandleSlotClicked(_slots[i]);
                return;
            }
        }

        private int IndexOf(LSO_WillType will)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null && _slots[i].Will == will) return i;
            }

            return -1;
        }

        private void RefreshMarks()
        {
            foreach (LSO_StampSlot slot in _slots)
            {
                if (slot == null) continue;

                slot.SetSelected(HasSelection && slot.Will == Selected);
            }
        }

        private void Raise()
        {
            SelectionChanged?.Invoke(Selected);
            onSelectionChanged?.Invoke(Selected);
        }

        private void ClearSlots()
        {
            foreach (LSO_StampSlot slot in _slots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }

            _slots.Clear();
        }
    }
}
