using UnityEngine;

namespace _Scripts.LDY
{
    // 턴마다 공유되는 행동력 풀. 이동/공격 1회당 1씩 소모하며, 0이 되면 턴이 끝난다.
    // 기물별 회수 제한은 없다 — 같은 기물도 행동력이 남아있으면 계속 행동할 수 있다.
    // 씬 배선: TurnManager/MoveSystem/AttackSystem/EnemyAI가 전부 같은 인스턴스를 참조해야 한다.
    public class LDY_ActionPointManager : MonoBehaviour
    {
        public static LDY_ActionPointManager instance;
        
        [SerializeField] private int maxActionPoints = 5;
        private const int minActionPoints = 0;
        
        private int _current;
    
        public int Max => maxActionPoints;
        public int Min => minActionPoints;
        
        public int Current
        {
            get => _current;
            private set
            {
                int clamped = Mathf.Max(value, minActionPoints);
                if (clamped == _current) return;   // 값이 안 바뀌면 이벤트도 안 쏜다

                _current = clamped;
                OnActionPointsChanged?.Invoke(_current, maxActionPoints);
            }
        }

        public bool HasActionPoints => Current > 0;
        
        //첫번째가 현재, 두번째가 최대
        public event System.Action<int, int> OnActionPointsChanged;

        private void Awake()
        {
            // 중복을 파괴하지 않고 경고만 남긴다. 이 컴포넌트는 여러 시스템이 인스펙터로 직접 참조하므로
            // 여기서 없애면 그 참조들이 한꺼번에 끊어져 원인 파악이 더 어려워진다.
            //
            // 대신 조용히 두면 안 된다. instance 폴백을 쓰는 쪽(LDY_CardPlacer, DLJ_CostRefund)만
            // 다른 풀에서 값을 빼게 되어, 소환·환급만 어긋나는 형태로 드러나기 때문이다.
            if (instance == null)
                instance = this;
            else if (instance != this)
                Debug.LogError(
                    $"{name}: 씬에 LDY_ActionPointManager가 둘 이상 있습니다(기존: {instance.name}). " +
                    "instance 폴백을 쓰는 쪽이 어느 풀을 쓸지 보장되지 않으므로 하나만 남기세요.", this);

            Current = maxActionPoints;
        }

        private void OnDestroy()
        {
            // 자기가 대표일 때만 비운다. 중복 인스턴스가 사라지면서 대표를 지워버리지 않게 한다.
            if (instance == this)
                instance = null;
        }

        /// <summary>
        /// 턴이 바뀔 때 행동력을 최대치로 되돌린다.
        ///
        /// 남은 값과 비교하지 않고 그냥 대입한다. Current 세터는 하한만 클램프하므로
        /// AddActionPoints(계약 유언 등)로 최대치를 넘긴 값이 들어올 수 있는데,
        /// 큰 쪽을 남기면 그 초과분이 매 턴 리셋을 통과해 전투가 끝날 때까지 눌러앉는다.
        /// 풀은 플레이어와 적이 함께 쓰므로 적 턴까지 그 이득을 물려받게 된다.
        /// </summary>
        public void ResetPoints()
        {
            Current = maxActionPoints;
        }

        /// <summary>
        /// 스테이지마다 다른 행동력을 적용할 때 쓴다. 현재 값도 함께 채워서,
        /// 턴 매니저가 이미 초기화를 마친 뒤에 호출돼도 옛 최대치가 남지 않게 한다.
        /// </summary>
        public void SetMax(int value)
        {
            if (value <= 0) return;

            maxActionPoints = value;
            ResetPoints();
        }

        /// <summary>차감하지 않고 되는지만 본다. UI가 미리 회색 처리할 때 쓴다.</summary>
        public bool CanAfford(int amount = 1)
        {
            return amount >= 0 && Current >= amount;
        }

        public bool TryConsume(int amount = 1)
        {
            // 딱 맞을 때(Current == amount)도 써야 한다.
            // <= 로 두면 마지막 1을 못 쓰고 "부족하다"가 뜬다.
            if (!CanAfford(amount)) return false;

            Current -= amount;
            return true;
        }

        public void AddActionPoints(int amount = 1)
        {
            if (amount <= 0) return;
            Current += amount;
        }

        public void MaxUpActionPoints(int amount = 1)
        {
            if (amount <= 0) return;
            maxActionPoints += amount;
            ResetPoints();
        }
    }
}
