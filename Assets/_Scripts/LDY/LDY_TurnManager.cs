using System.Collections;
using UnityEngine;

namespace _Scripts.LDY
{
    // 씬 배선: EnemyAI/MoveSystem/AttackSystem/ActionPointManager를 연결할 것.
    // 팀이 공유하는 행동력(ActionPointManager)이 0이 되고, 진행 중인 연출까지 끝나야 자동으로 턴이 넘어간다.
    // (연출이 끝나기 전에 턴이 넘어가면, 아직 죽지 않은 걸로 보이는 대상을 적이 동시에 노려 충돌이 날 수 있다.)
    public class LDY_TurnManager : MonoBehaviour
    {
        [SerializeField] private LDY_EnemyAI enemyAI;
        [SerializeField] private LDY_MoveSystem moveSystem;
        [SerializeField] private LDY_AttackSystem attackSystem;
        [SerializeField] private LDY_ActionPointManager actionPoints;

        public LDY_Team CurrentTurn { get; private set; } = LDY_Team.Player;
        public event System.Action<LDY_Team> OnTurnChanged;

        private bool _isProcessingTurn;

        private void Start()
        {
            actionPoints.ResetPoints();
            OnTurnChanged?.Invoke(CurrentTurn);
        }

        private void Update()
        {
            if (_isProcessingTurn || CurrentTurn != LDY_Team.Player) return;
            if (IsAnimating()) return;
            if (actionPoints.HasActionPoints) return;

            EndPlayerTurn();
        }

        private bool IsAnimating()
        {
            return (moveSystem != null && moveSystem.IsBusy) || (attackSystem != null && attackSystem.IsBusy);
        }

        private void EndPlayerTurn()
        {
            _isProcessingTurn = true;
            CurrentTurn = LDY_Team.Enemy;
            actionPoints.ResetPoints();
            OnTurnChanged?.Invoke(CurrentTurn);
            StartCoroutine(RunEnemyTurnRoutine());
        }

        private IEnumerator RunEnemyTurnRoutine()
        {
            yield return StartCoroutine(enemyAI.RunEnemyTurnCoroutine());

            // 적 턴 마지막 행동의 연출이 아직 재생 중일 수 있으니 끝날 때까지 기다린다.
            while (IsAnimating())
                yield return null;

            CurrentTurn = LDY_Team.Player;
            actionPoints.ResetPoints();
            OnTurnChanged?.Invoke(CurrentTurn);
            _isProcessingTurn = false;
        }
    }
}
