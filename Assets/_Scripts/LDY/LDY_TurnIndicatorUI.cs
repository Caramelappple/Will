using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LDY
{
    // 씬 배선: TurnManager/ActionPointManager와 표시할 UI Text를 연결할 것.
    public class LDY_TurnIndicatorUI : MonoBehaviour
    {
        [SerializeField] private LDY_TurnManager turnManager;
        [SerializeField] private LDY_ActionPointManager actionPoints;
        [SerializeField] private Text label;

        private void OnEnable()
        {
            if (turnManager != null)
                turnManager.OnTurnChanged += HandleTurnChanged;
            if (actionPoints != null)
                actionPoints.OnActionPointsChanged += HandleActionPointsChanged;
        }

        private void OnDisable()
        {
            if (turnManager != null)
                turnManager.OnTurnChanged -= HandleTurnChanged;
            if (actionPoints != null)
                actionPoints.OnActionPointsChanged -= HandleActionPointsChanged;
        }

        private void Start()
        {
            if (turnManager != null)
                HandleTurnChanged(turnManager.CurrentTurn);
        }

        private void HandleTurnChanged(LDY_Team team)
        {
            _currentTeam = team;
            Refresh();
        }

        private void HandleActionPointsChanged(int current, int max)
        {
            Refresh();
        }

        private LDY_Team _currentTeam;

        private void Refresh()
        {
            if (label == null) return;

            string teamText = _currentTeam == LDY_Team.Player ? "Player Turn" : "Enemy Turn";
            string apText = actionPoints != null ? $"  AP {actionPoints.Current}/{actionPoints.Max}" : "";
            label.text = teamText + apText;
            label.color = _currentTeam == LDY_Team.Player ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.35f, 0.35f);
        }
    }
}
