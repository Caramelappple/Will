using System.Collections;
using System.Collections.Generic;
using _Scripts.LDY.AI;
using UnityEngine;

namespace _Scripts.LDY
{
    // 씬 배선: BoardManager/MoveSystem/AttackSystem/ActionPointManager를 연결하고,
    // 적 턴이 시작될 때 RunEnemyTurn()을 호출할 것.
    // 행동력이 남아있는 한 같은 적도 여러 번 행동할 수 있어서, 행동력이 0이 되거나
    // 아무도 더 할 행동이 없을 때까지 적 목록을 반복해서 훑는다.
    //
    // 무엇을 할지는 LDY_EnemyBrain이 정하고, 그 결정을 LDY_ActionExecutor가 기존 이동/공격 시스템으로 실행한다.
    // 이 클래스는 턴 진행과 그 둘의 연결만 담당한다.
    public class LDY_EnemyAI : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private LDY_MoveSystem moveSystem;
        [SerializeField] private LDY_AttackSystem attackSystem;
        [SerializeField] private LDY_ActionPointManager actionPoints;
        [Tooltip("적 행동 사이 대기 시간. 판단이 맞는지 확인되면 0으로 내려도 된다.")]
        [SerializeField] private float actionDelay = 0.5f;

        [Header("판단")]
        [Tooltip("비워두면 기본 4종(공격 우선 / 처치 보너스 / 전선 / 접근)을 쓴다.")]
        [SerializeReference] private List<LDY_IActionScorer> scorers = new();
        [Tooltip("기물별 전용 scorer 매핑. 비워도 공용 scorer만으로 동작한다.")]
        [SerializeField] private LDY_ScorerRegistry scorerRegistry;
        [Tooltip("후보별 점수 내역을 콘솔에 남긴다. 조용히 무시된 행동은 이 값과 무관하게 항상 경고로 남는다.")]
        [SerializeField] private bool enableDecisionLog;

        private LDY_EnemyBrain _brain;
        private LDY_ActionExecutor _executor;

        private void Awake()
        {
            // ActionPointManager도 필수다. 없으면 행동력이 줄지 않아 턴 루프의 종료 조건이 사라지고,
            // 공격 실행 여부를 관측할 수단도 없어진다. 관측이 안 되는 채로 계속 도는 건 의미가 없으므로 진행하지 않는다.
            if (board == null || moveSystem == null || attackSystem == null || actionPoints == null)
            {
                Debug.LogError($"{name}: Board / MoveSystem / AttackSystem / ActionPointManager를 모두 연결해야 한다.", this);
                return;
            }

            _brain = new LDY_EnemyBrain(moveSystem, attackSystem, ResolveScorers(), scorerRegistry);
            _executor = new LDY_ActionExecutor(moveSystem, attackSystem, actionPoints);
        }

        public void RunEnemyTurn()
        {
            StartCoroutine(RunEnemyTurnCoroutine());
        }

        // 턴 매니저처럼 완료 시점을 알아야 하는 외부 코드는 이 코루틴을 직접 yield하면 된다.
        public IEnumerator RunEnemyTurnCoroutine()
        {
            Debug.Log("[LDY_EnemyAI] 적 턴 시작");

            if (_brain == null)
            {
                Debug.LogError($"{name}: 배선이 없어 적 턴을 진행할 수 없다.", this);
                yield break;
            }

            // actionPoints는 Awake에서 필수로 확인했다. null 허용 분기를 남겨두면
            // 그 경우에만 종료 조건이 통째로 사라지는 함정이 된다.
            while (actionPoints.HasActionPoints)
            {
                var enemies = board.GetAllByTeam(LDY_Team.Enemy);
                bool actedThisPass = false;

                foreach (var enemy in enemies)
                {
                    if (!actionPoints.HasActionPoints) break;
                    if (enemy == null || !CanAct(enemy)) continue;

                    yield return new WaitForSeconds(actionDelay);

                    // 대기하는 동안 다른 공격으로 이 기물이 죽었거나 행동력이 소진됐을 수 있으니 다시 확인한다.
                    if (enemy == null) continue;
                    if (!actionPoints.HasActionPoints) break;
                    if (!CanAct(enemy)) continue;

                    // 대기를 골랐거나 실행이 거부되면 보드가 그대로다.
                    // 여기서 actedThisPass를 올리면 같은 상태로 무한히 반복하게 된다.
                    if (ActEnemy(enemy))
                        actedThisPass = true;
                }

                if (!actedThisPass) break; // 아무도 더 할 행동이 없으면 종료 (무한 루프 방지)
            }

            Debug.Log("[LDY_EnemyAI] 적 턴 종료");
        }

        private bool CanAct(LDY_Animal enemy)
        {
            return attackSystem.GetAttackTargets(enemy).Count > 0 || moveSystem.GetMovableTiles(enemy).Count > 0;
        }

        /// <summary>보드에 실제로 변화가 생겼으면 true.</summary>
        private bool ActEnemy(LDY_Animal enemy)
        {
            // 판단 직전마다 확인한다. 턴 시작에만 반영하면 적이 움직이는 도중 체크박스를 켰을 때
            // 행동 로그는 바로 나오는데 점수 내역만 다음 턴까지 안 나온다.
            ApplyLoggerSetting();

            LDY_EnemyAction action = _brain.Decide(enemy, board);
            LDY_ActionOutcome outcome = _executor.Execute(enemy, action);

            LogAction(enemy, action, outcome);

            // 확인된 실행만 "행동했다"로 친다. 관측이 불확실한 결과를 true로 치면
            // 보드가 그대로인데 패스가 계속 이어질 수 있다.
            return outcome == LDY_ActionOutcome.Executed;
        }

        // 결정과 실행 결과를 한 줄에 같이 남긴다.
        // 조용히 무시된 행동은 토글과 무관하게 항상 알린다 — 놓치면 "결정이 틀린 것"과 구분할 수 없다.
        private void LogAction(LDY_Animal enemy, in LDY_EnemyAction action, LDY_ActionOutcome outcome)
        {
            if (outcome == LDY_ActionOutcome.Rejected)
            {
                Debug.LogWarning($"[LDY_EnemyAI] {enemy.name}: {action} → 실행 거부됨 (시스템이 조용히 무시함)", enemy);
                return;
            }

            // ActionPointManager를 필수로 확인하므로 여기까지 올 수 없다. 오면 배선 전제가 깨진 것이다.
            if (outcome == LDY_ActionOutcome.Unverified)
            {
                Debug.LogError($"[LDY_EnemyAI] {enemy.name}: {action} → 실행 여부를 확인할 수 없다. 배선을 확인할 것.", enemy);
                return;
            }

            if (!enableDecisionLog) return;

            Debug.Log($"[LDY_EnemyAI] {enemy.name}: {action} → {Describe(outcome)}", enemy);
        }

        private static string Describe(LDY_ActionOutcome outcome)
        {
            switch (outcome)
            {
                case LDY_ActionOutcome.Executed: return "실행됨";
                case LDY_ActionOutcome.Waited: return "대기";
                default: return outcome.ToString();
            }
        }

        // 재생 중에 체크박스를 껐다 켜도 다음 턴부터 반영된다.
        private void ApplyLoggerSetting()
        {
            bool alreadyOn = _brain.Logger != null;
            if (enableDecisionLog == alreadyOn) return;

            _brain.Logger = enableDecisionLog ? new LDY_ConsoleDecisionLogger() : null;
        }

        private IReadOnlyList<LDY_IActionScorer> ResolveScorers()
        {
            if (scorers != null && scorers.Count > 0)
                return scorers;

            // 전부 비어 있으면 모든 후보가 0점이 되어 항상 대기를 고른다. 기본값을 반드시 채워 넣는다.
            return new LDY_IActionScorer[]
            {
                new LDY_AttackPriorityScorer(),
                new LDY_KillBonusScorer(),
                new LDY_FrontlineScorer(),
                new LDY_PositioningScorer(),
            };
        }
    }
}
