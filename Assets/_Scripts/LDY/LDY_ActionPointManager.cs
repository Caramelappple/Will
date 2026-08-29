using UnityEngine;

namespace _Scripts.LDY
{
    // 턴마다 공유되는 행동력 풀. 이동/공격 1회당 1씩 소모하며, 0이 되면 턴이 끝난다.
    // 기물별 회수 제한은 없다 — 같은 기물도 행동력이 남아있으면 계속 행동할 수 있다.
    // 씬 배선: TurnManager/MoveSystem/AttackSystem/EnemyAI가 전부 같은 인스턴스를 참조해야 한다.
    public class LDY_ActionPointManager : MonoBehaviour
    {
        public static LDY_ActionPointManager instance;
        
        [Tooltip("턴마다 채워지는 기본 행동력. 화면에 Current/Max로 표시되는 그 값이다.")]
        [SerializeField] private int maxActionPoints = 5;

        [Tooltip("추가로 얻어서 쌓아둘 수 있는 상한. 유언·아이템으로 받는 여분이 여기까지만 들어온다.\n" +
                 "Max보다 작게 두면 Max를 상한으로 친다(추가 획득이 아예 막힌다).")]
        [SerializeField] private int addMaxActionPoints = 10;

        private const int minActionPoints = 0;

        private int _current;

        public int Max => maxActionPoints;
        public int Min => minActionPoints;

        /// <summary>
        /// 추가 획득까지 포함한 절대 상한.
        ///
        /// Max와 나눠 둔 이유는 둘이 다른 질문에 답하기 때문이다.
        /// Max는 "턴마다 얼마나 채워지나", AddMax는 "얼마까지 들고 있을 수 있나"다.
        /// 화면에는 Max만 나오므로 여분을 들고 있으면 6/5처럼 최대치를 넘겨 보인다.
        ///
        /// 인스펙터에서 AddMax를 Max보다 작게 적어두는 실수를 대비해 큰 쪽을 쓴다.
        /// 그대로 두면 리셋으로 채운 값이 곧바로 상한을 넘겨 다음 대입에서 깎인다.
        /// </summary>
        public int AddMax => Mathf.Max(addMaxActionPoints, maxActionPoints);

        /// <summary>지금 추가로 더 받을 수 있는 양.</summary>
        public int Headroom => Mathf.Max(0, AddMax - Current);

        public int Current
        {
            get => _current;
            private set
            {
                // 상한도 여기서 막는다. 늘리는 경로가 늘어날 때마다 각자 클램프하게 두면
                // 하나만 빠뜨려도 AddMax를 넘긴 값이 조용히 들어온다.
                int clamped = Mathf.Clamp(value, minActionPoints, AddMax);
                if (clamped == _current) return;   // 값이 안 바뀌면 이벤트도 안 쏜다

                _current = clamped;

                // 표시는 그대로 Current/Max다. AddMax는 화면에 내보내지 않는다.
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
        /// 남은 값과 비교하지 않고 그냥 대입한다. AddActionPoints(계약 유언 등)로 받은 여분은
        /// AddMax까지 쌓일 수 있는데, 큰 쪽을 남기면 그 초과분이 매 턴 리셋을 통과해
        /// 전투가 끝날 때까지 눌러앉는다.
        /// 풀은 플레이어와 적이 함께 쓰므로 적 턴까지 그 이득을 물려받게 된다.
        ///
        /// 즉 여분은 "이번 턴 안에 쓰라"는 것이지 저축이 아니다.
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

        /// <summary>차감하지 않고 추가로 받을 수 있는지만 본다.</summary>
        public bool CanAdd(int amount = 1)
        {
            return amount > 0 && Headroom > 0;
        }

        /// <summary>
        /// 추가 행동력을 받는다. AddMax를 넘는 만큼은 버린다.
        ///
        /// 넘친다고 통째로 거절하지 않는다. 상한 직전에서 3을 주는 효과가 아무것도 안 주는 것보다
        /// 1이라도 주는 편이 플레이어가 납득하기 쉽고, 유언·환급이 언제 터질지 고를 수 없기 때문이다.
        ///
        /// 실제로 늘어난 양을 돌려준다. 부르는 쪽(계약 유언, 코스트 환급)이
        /// "몇 개가 버려졌는지" 알아야 연출이나 로그를 맞출 수 있다.
        /// </summary>
        /// <returns>실제로 늘어난 양. 상한에 닿아 있으면 0.</returns>
        public int AddActionPoints(int amount = 1)
        {
            if (amount <= 0) return 0;

            int gained = Mathf.Min(amount, Headroom);
            if (gained <= 0) return 0;

            Current += gained;
            return gained;
        }

        /// <summary>
        /// 턴마다 채워지는 기본량을 올린다. 화면의 최대치가 함께 올라간다.
        ///
        /// AddMax는 건드리지 않는다. Max가 AddMax를 넘어서면 AddMax 프로퍼티가
        /// 큰 쪽을 쓰므로 상한이 저절로 따라 올라간다.
        /// </summary>
        public void MaxUpActionPoints(int amount = 1)
        {
            if (amount <= 0) return;
            maxActionPoints += amount;
            ResetPoints();
        }

        /// <summary>
        /// 추가 획득 상한만 바꾼다. 현재 값은 건드리지 않되, 새 상한을 넘겨 있으면 깎는다.
        /// </summary>
        public void SetAddMax(int value)
        {
            if (value <= 0) return;

            addMaxActionPoints = value;

            // 상한이 내려갔는데 이미 그보다 많이 들고 있으면 다음 대입까지 초과분이 남는다.
            // 한 번 다시 대입해 세터의 클램프를 태운다.
            Current = _current;
        }
    }
}
