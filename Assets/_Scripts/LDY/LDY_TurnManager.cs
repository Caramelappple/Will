using System.Collections;
using _Scripts.LSO;
using UnityEngine;

namespace _Scripts.LDY
{
    public class LDY_TurnManager : MonoBehaviour
    {
        [SerializeField] private LDY_EnemyAI enemyAI;
        [SerializeField] private LDY_MoveSystem moveSystem;
        [SerializeField] private LDY_AttackSystem attackSystem;
        [SerializeField] private LDY_ActionPointManager actionPoints;

        public LDY_Team CurrentTurn { get; private set; } = LDY_Team.Player;
        public LDY_ActionPointManager ActionPoints => actionPoints;
        public event System.Action<LDY_Team> OnTurnChanged;

        private bool _isProcessingTurn;
        
        private void Awake()
        {
            GameManager.Instance.RegisterTurnManager(this);
        }

        private void OnDestroy()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.UnregisterTurnManager(this);
        }

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
            
            while (IsAnimating())
                yield return null;

            CurrentTurn = LDY_Team.Player;
            actionPoints.ResetPoints();
            OnTurnChanged?.Invoke(CurrentTurn);
            _isProcessingTurn = false;
        }
    }
}
