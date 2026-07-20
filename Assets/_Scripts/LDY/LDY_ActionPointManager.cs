using UnityEngine;

namespace _Scripts.LDY
{
    // 턴마다 공유되는 행동력 풀. 이동/공격 1회당 1씩 소모하며, 0이 되면 턴이 끝난다.
    // 기물별 회수 제한은 없다 — 같은 기물도 행동력이 남아있으면 계속 행동할 수 있다.
    // 씬 배선: TurnManager/MoveSystem/AttackSystem/EnemyAI가 전부 같은 인스턴스를 참조해야 한다.
    public class LDY_ActionPointManager : MonoBehaviour
    {
        [SerializeField] private int maxActionPoints = 5;

        public int Max => maxActionPoints;
        public int Current { get; private set; }
        public bool HasActionPoints => Current > 0;

        public event System.Action<int, int> OnActionPointsChanged;

        private void Awake()
        {
            Current = maxActionPoints;
        }

        public void ResetPoints()
        {
            Current = maxActionPoints;
            OnActionPointsChanged?.Invoke(Current, maxActionPoints);
        }

        public bool TryConsume(int amount = 1)
        {
            if (Current < amount) return false;
            Current -= amount;
            OnActionPointsChanged?.Invoke(Current, maxActionPoints);
            return true;
        }
    }
}
