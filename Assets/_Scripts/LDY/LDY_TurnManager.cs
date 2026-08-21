using System.Collections;
using _Scripts.LSO.Manager;
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

        /// <summary>
        /// 이동이나 공격 연출이 하나라도 재생 중인지.
        ///
        /// 턴 전환이 연출 도중에 끼어들지 않게 막는 것이 원래 용도였는데,
        /// "지금 화면에서 뭔가 움직이는 중인가"를 묻는 자리가 여기밖에 없어서 밖으로 열어둔다.
        /// 승리 판정 뒤 씬을 넘기기 전에 기다리는 쪽(KTH_GameEndManager 등)도 이걸 보면 된다.
        ///
        /// 공격의 복귀 애니메이션은 데미지가 들어간 뒤에도 이어지므로,
        /// "마지막 적이 죽었다"와 "연출이 끝났다"는 서로 다른 시점이다. 넘기기 전에 이 값을 볼 것.
        /// </summary>
        public bool IsAnimating()
        {
            return (moveSystem != null && moveSystem.IsBusy) || (attackSystem != null && attackSystem.IsBusy);
        }

        public void EndPlayerTurn()
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
