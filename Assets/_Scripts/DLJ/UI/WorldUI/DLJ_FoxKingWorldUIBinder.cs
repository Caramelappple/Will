using UnityEngine;

namespace _Scripts.DLJ.UI.WorldUI
{
    /// <summary>
    /// 여우왕의 수탈 자원과 탐욕 마일스톤을 공통 기물 UI로 전달한다.
    /// 자원이 줄면 감소량을 잠깐 보여준 뒤 최신 보유량으로 복귀한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DLJ_FoxKingWorldUIBinder : MonoBehaviour
    {
        [SerializeField] private global::DLJ_FoxKingBoss foxKing;
        [SerializeField] private DLJ_WorldUIController worldUI;

        [Header("수탈 자원")]
        [SerializeField] private DLJ_WorldUISlotId resourceSlot = DLJ_WorldUISlotId.Resource;
        [SerializeField] private Sprite resourceIcon;
        [SerializeField] private Color resourceTint = Color.white;
        [SerializeField] private Color spendTint = new(1f, 0.55f, 0.25f, 1f);
        [SerializeField, Min(0.05f)] private float spendFeedbackDuration = 0.8f;

        [Header("탐욕 마일스톤")]
        [SerializeField] private bool showGreedMilestones = true;
        [SerializeField] private DLJ_WorldUISlotId greedSlot = DLJ_WorldUISlotId.Greed;
        [SerializeField] private Sprite greedIcon;
        [SerializeField] private Color achievedTint = new(1f, 0.85f, 0.25f, 1f);
        [SerializeField] private Color pendingTint = new(0.18f, 0.18f, 0.18f, 1f);

        private int _lastResourceValue;
        private int _accumulatedSpend;
        private float _spendWindowEnd;
        private bool _reportedNullMilestone;

        private void Awake()
        {
            ResolveReferences();

            if (foxKing == null)
                Debug.LogWarning($"{name}: FoxKing World UI에 연결할 DLJ_FoxKingBoss가 없습니다.", this);
            if (worldUI == null)
                Debug.LogWarning($"{name}: FoxKing World UI Controller가 없습니다.", this);
            if (showGreedMilestones && greedIcon == null)
                Debug.LogWarning($"{name}: 탐욕 마일스톤 아이콘이 없어 상태 슬롯을 숨깁니다.", this);
        }

        private void OnEnable()
        {
            if (foxKing == null || worldUI == null) return;

            _lastResourceValue = foxKing.StolenResources;
            _accumulatedSpend = 0;
            _spendWindowEnd = 0f;
            foxKing.OnStolenResourcesChanged += HandleResourceChanged;
            foxKing.OnGreedChanged += HandleGreedChanged;

            RefreshResource(foxKing.StolenResources);
            RefreshGreed(foxKing.Greed);
        }

        private void OnDisable()
        {
            if (foxKing == null) return;

            foxKing.OnStolenResourcesChanged -= HandleResourceChanged;
            foxKing.OnGreedChanged -= HandleGreedChanged;
        }

        [ContextMenu("Refresh FoxKing World UI")]
        public void RefreshAll()
        {
            ResolveReferences();
            if (foxKing == null || worldUI == null) return;

            _lastResourceValue = foxKing.StolenResources;
            RefreshResource(foxKing.StolenResources);
            RefreshGreed(foxKing.Greed);
        }

        private void HandleResourceChanged(int current)
        {
            int spent = Mathf.Max(0, _lastResourceValue - current);
            bool gained = current > _lastResourceValue;
            _lastResourceValue = current;

            // 임시 표시가 끝날 때 복귀할 최신 값을 먼저 저장한다.
            RefreshResource(current);

            if (spent > 0)
            {
                float now = Time.unscaledTime;
                _accumulatedSpend = now <= _spendWindowEnd
                    ? _accumulatedSpend + spent
                    : spent;
                _spendWindowEnd = now + spendFeedbackDuration;

                worldUI.ShowTemporary(
                    resourceSlot,
                    DLJ_WorldUIData.TextValue($"-{_accumulatedSpend}", resourceIcon, spendTint),
                    spendFeedbackDuration);
            }
            else if (gained)
            {
                _accumulatedSpend = 0;
                _spendWindowEnd = 0f;
            }
        }

        private void HandleGreedChanged(int current)
        {
            RefreshGreed(current);
        }

        private void RefreshResource(int current)
        {
            worldUI.Set(
                resourceSlot,
                DLJ_WorldUIData.TextValue(current.ToString(), resourceIcon, resourceTint));
        }

        private void RefreshGreed(int current)
        {
            if (!showGreedMilestones || greedIcon == null || foxKing.GreedMilestones.Count == 0)
            {
                worldUI.Hide(greedSlot);
                return;
            }

            int achieved = 0;
            int capacity = 0;
            for (int i = 0; i < foxKing.GreedMilestones.Count; i++)
            {
                global::DLJ_GreedMilestone milestone = foxKing.GreedMilestones[i];
                if (milestone == null)
                {
                    if (!_reportedNullMilestone)
                    {
                        _reportedNullMilestone = true;
                        Debug.LogWarning($"{name}: 비어 있는 탐욕 마일스톤을 건너뜁니다.", foxKing);
                    }

                    continue;
                }

                capacity++;
                if (current >= milestone.threshold)
                    achieved++;
            }

            if (capacity == 0)
            {
                worldUI.Hide(greedSlot);
                return;
            }

            worldUI.Set(
                greedSlot,
                DLJ_WorldUIData.Stacks(
                    achieved,
                    greedIcon,
                    achievedTint,
                    capacity: capacity,
                    inactiveTint: pendingTint));
        }

        private void ResolveReferences()
        {
            if (foxKing == null)
                foxKing = GetComponentInParent<global::DLJ_FoxKingBoss>();
            if (foxKing == null)
                foxKing = GetComponentInChildren<global::DLJ_FoxKingBoss>(true);

            if (worldUI == null)
                worldUI = GetComponentInChildren<DLJ_WorldUIController>(true);
            if (worldUI == null)
                worldUI = GetComponentInParent<DLJ_WorldUIController>();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (spendFeedbackDuration < 0.05f)
                spendFeedbackDuration = 0.05f;

            ResolveReferences();
        }
#endif
    }
}
