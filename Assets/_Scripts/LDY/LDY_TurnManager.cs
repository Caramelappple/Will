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

        // 행동력이 0이 됐다고 턴을 자동으로 넘기지 않는다.
        //
        // 기획서: "코스트를 모두 사용한 경우에도 직접 버튼을 눌러 턴을 종료한다."
        //
        // 자동으로 넘기면 마지막 행동의 연출이 끝나기도 전에 화면이 적 턴으로 바뀌어
        // 방금 무슨 일이 일어났는지 확인할 틈이 없다. 남은 기물의 배치를 다시 보거나
        // 기물 정보를 열어보는 것도 못 한다.
        //
        // 그래서 턴을 넘기는 경로는 턴 종료 버튼 하나뿐이다.
        // 예전에는 이 Update가 대신 막아주던 조건들(적 턴인지, 연출 중인지)이 있었으므로
        // 그 검사는 EndPlayerTurn 안으로 옮겼다.

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

        /// <summary>
        /// 지금 턴 종료 버튼을 눌러도 되는지. 버튼을 회색으로 만들 때 쓴다.
        ///
        /// 못 누르는 이유를 화면에 보여주지 않으면 "버튼이 안 먹는다"로 보인다.
        /// </summary>
        public bool CanEndPlayerTurn()
        {
            if (_isProcessingTurn) return false;
            if (CurrentTurn != LDY_Team.Player) return false;
            if (IsAnimating()) return false;

            return true;
        }

        /// <summary>
        /// 플레이어 턴을 끝내고 적 턴으로 넘긴다. 턴 종료 버튼이 부른다.
        ///
        /// 검사를 여기서 한다. 예전에는 Update가 자동으로 넘기면서 같은 검사를 했고
        /// 버튼은 그냥 통과했는데, 그러면 적 턴 중에 버튼을 또 누르거나
        /// 공격 연출 도중에 눌렀을 때 턴이 겹쳐 진행된다.
        ///
        /// 반환값을 두지 않는다. 턴 종료 버튼이 씬에서 UnityEvent로 연결돼 있는데,
        /// UnityEvent는 void 메서드만 목록에 올리므로 반환형을 바꾸면 그 연결이 끊긴다.
        /// 눌러도 되는지는 CanEndPlayerTurn으로 미리 물어보면 된다.
        /// </summary>
        public void EndPlayerTurn()
        {
            if (!CanEndPlayerTurn()) return;

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
