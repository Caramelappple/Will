using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Scripts.LDY
{
    // 씬 배선: BoardManager/MoveSystem/AttackSystem/ActionPointManager를 연결하고,
    // 적 턴이 시작될 때 RunEnemyTurn()을 호출할 것.
    // 행동력이 남아있는 한 같은 적도 여러 번 행동할 수 있어서, 행동력이 0이 되거나
    // 아무도 더 할 행동이 없을 때까지 적 목록을 반복해서 훑는다.
    public class LDY_EnemyAI : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private LDY_MoveSystem moveSystem;
        [SerializeField] private LDY_AttackSystem attackSystem;
        [SerializeField] private LDY_ActionPointManager actionPoints;
        [SerializeField] private float actionDelay = 0.5f;

        public void RunEnemyTurn()
        {
            StartCoroutine(RunEnemyTurnCoroutine());
        }

        // 턴 매니저처럼 완료 시점을 알아야 하는 외부 코드는 이 코루틴을 직접 yield하면 된다.
        public IEnumerator RunEnemyTurnCoroutine()
        {
            Debug.Log("[LDY_EnemyAI] 적 턴 시작");

            while (actionPoints == null || actionPoints.HasActionPoints)
            {
                var enemies = board.GetAllByTeam(LDY_Team.Enemy);
                bool actedThisPass = false;

                foreach (var enemy in enemies)
                {
                    if (actionPoints != null && !actionPoints.HasActionPoints) break;
                    if (enemy == null || !CanAct(enemy)) continue;

                    yield return new WaitForSeconds(actionDelay);

                    // 대기하는 동안 다른 공격으로 이 기물이 죽었거나 행동력이 소진됐을 수 있으니 다시 확인한다.
                    if (enemy == null) continue;
                    if (actionPoints != null && !actionPoints.HasActionPoints) break;
                    if (!CanAct(enemy)) continue;

                    ActEnemy(enemy);
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

        private void ActEnemy(LDY_Animal enemy)
        {
            var targets = attackSystem.GetAttackTargets(enemy);
            if (targets.Count > 0)
            {
                var target = ChooseTarget(targets);
                if (target != null)
                {
                    Debug.Log($"[LDY_EnemyAI] {enemy.name} 이(가) {target.name} 공격");
                    attackSystem.Attack(enemy, target);
                    return;
                }
            }

            Debug.Log($"[LDY_EnemyAI] {enemy.name} 이동 시도");
            MoveTowardNearest(enemy);
        }

        // 1순위: |ATK - HP|가 작은 적. 2순위(동점): z좌표(격자에서 적 진영에 가까운 축)가 큰 적.
        // pos.y는 시각용 높이값이라 우선순위 판정에 쓰지 않는다.
        public LDY_Animal ChooseTarget(List<LDY_Animal> targets)
        {
            if (targets == null || targets.Count == 0) return null;

            return targets
                .OrderBy(t => Mathf.Abs(t.GetAtk() - t.health.GetValue()))
                .ThenByDescending(t => t.pos.z)
                .First();
        }

        public void MoveTowardNearest(LDY_Animal enemy)
        {
            var players = board.GetAllByTeam(LDY_Team.Player);
            if (players.Count == 0) return;

            var movable = moveSystem.GetMovableTiles(enemy);
            if (movable.Count == 0) return;

            var nearestPlayer = players.OrderBy(p => Manhattan(enemy.pos, p.pos)).First();
            var best = movable.OrderBy(tile => Manhattan(tile, nearestPlayer.pos)).First();

            moveSystem.MoveTo(enemy, best);
        }

        // 거리 계산은 x/z 평면만 사용한다 (y는 시각용 높이값이라 무시).
        private static int Manhattan(Vector3Int a, Vector3Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
        }
    }
}
