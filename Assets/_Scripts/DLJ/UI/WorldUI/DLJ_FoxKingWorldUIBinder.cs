using UnityEngine;

namespace _Scripts.DLJ.UI.WorldUI
{
    /// <summary>여우왕의 효과 발생량만 순간 팝업에 전달한다. 초기값이나 보유량은 표시하지 않는다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DLJ_PieceFeedback))]
    public sealed class DLJ_FoxKingWorldUIBinder : MonoBehaviour
    {
        [SerializeField] private global::DLJ_FoxKingBoss foxKing;
        [SerializeField] private DLJ_PieceFeedback feedback;

        [Header("수탈 자원 효과")]
        [SerializeField] private Sprite resourceIcon;
        [SerializeField] private Color resourceTint = Color.white;
        [SerializeField] private Color spendTint = new(1f, 0.55f, 0.25f, 1f);
        [SerializeField, Min(0.05f)] private float spendFeedbackDuration = 0.8f;

        [Header("탐욕 단계 달성 효과")]
        [SerializeField] private bool showGreedMilestones = true;
        [SerializeField] private Sprite greedIcon;
        [SerializeField] private Color achievedTint = new(1f, 0.85f, 0.25f, 1f);

        private int _lastResourceValue;
        private int _lastAchievedMilestones;
        private int _pendingGain;
        private int _pendingSpend;
        private int _pendingMilestones;

        private void OnEnable()
        {
            ResolveReferences();
            if (foxKing == null || feedback == null)
            {
                Debug.LogWarning($"{name}: FoxKing과 PieceFeedback 연결이 필요합니다.", this);
                return;
            }

            RefreshAll();
            foxKing.OnStolenResourcesChanged += HandleResourceChanged;
            foxKing.OnGreedChanged += HandleGreedChanged;
        }

        private void OnDisable()
        {
            if (foxKing != null)
            {
                foxKing.OnStolenResourcesChanged -= HandleResourceChanged;
                foxKing.OnGreedChanged -= HandleGreedChanged;
            }
            ClearPending();
        }

        /// <summary>표시 없이 현재 값을 기준점으로 잡는다. 초기화/재활성화가 효과로 오인되지 않는다.</summary>
        [ContextMenu("Refresh FoxKing World UI")]
        public void RefreshAll()
        {
            ResolveReferences();
            ClearPending();
            if (foxKing == null) return;
            _lastResourceValue = foxKing.StolenResources;
            _lastAchievedMilestones = CountAchievedMilestones(foxKing.Greed);
        }

        private void HandleResourceChanged(int current)
        {
            int change = current - _lastResourceValue;
            _lastResourceValue = current;
            if (change > 0) _pendingGain += change;
            else if (change < 0) _pendingSpend -= change;
        }

        private void HandleGreedChanged(int current)
        {
            int achieved = CountAchievedMilestones(current);
            if (showGreedMilestones)
                _pendingMilestones += Mathf.Max(0, achieved - _lastAchievedMilestones);
            _lastAchievedMilestones = achieved;
        }

        private void LateUpdate()
        {
            if (feedback == null) return;

            // 같은 프레임의 이중 투자 비용은 합산하고 획득/소비는 서로 상쇄하지 않는다.
            if (_pendingGain > 0)
                feedback.ShowText(resourceIcon, FormatDelta("RES", _pendingGain, resourceIcon),
                    resourceTint, spendFeedbackDuration);
            if (_pendingSpend > 0)
                feedback.ShowText(resourceIcon, FormatDelta("RES", -_pendingSpend, resourceIcon),
                    spendTint, spendFeedbackDuration);
            if (_pendingMilestones > 0)
                feedback.ShowText(greedIcon, FormatDelta("GREED", _pendingMilestones, greedIcon),
                    achievedTint, spendFeedbackDuration);
            ClearPending();
        }

        private int CountAchievedMilestones(int greed)
        {
            int count = 0;
            foreach (global::DLJ_GreedMilestone milestone in foxKing.GreedMilestones)
                if (milestone != null && greed >= milestone.threshold) count++;
            return count;
        }

        private static string FormatDelta(string label, int value, Sprite icon)
        {
            string number = value > 0 ? $"+{value}" : value.ToString();
            return icon != null ? number : $"{label} {number}";
        }

        private void ClearPending()
        {
            _pendingGain = 0;
            _pendingSpend = 0;
            _pendingMilestones = 0;
        }

        private void ResolveReferences()
        {
            if (foxKing == null) foxKing = GetComponent<global::DLJ_FoxKingBoss>();
            if (feedback == null) feedback = GetComponent<DLJ_PieceFeedback>();
        }
    }
}
