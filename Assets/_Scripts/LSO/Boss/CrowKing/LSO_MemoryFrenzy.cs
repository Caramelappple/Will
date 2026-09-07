using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Animal.Data;
using UnityEngine;
using _Scripts.LSO.Interfaces;

namespace _Scripts.LSO.Boss.CrowKing
{
    /// <summary>
    /// 기억 폭주. 2페이즈에 활성화된다.
    ///
    /// 활성화된 뒤로 총 공격력이 6씩 갱신될 때마다(6, 12, 18 …) 1회분이 충전되고,
    /// 그다음 공격 한 번이 2연타가 된다. 쓰고 나면 다음 갱신까지 다시 잠잠해진다.
    /// 한 번에 여러 구간을 뛰어넘어도 충전은 1회다.
    ///
    /// 충전 판정과 소비를 다른 훅에 나눠 둔 이유:
    ///   ModifyAttackCount는 "몇 번 때릴까"를 묻는 질의다. 여기서 충전을 깎으면
    ///   나중에 AI가 미리 계산해보려고 한 번 더 부르는 순간 충전이 사라진다.
    ///   소비는 실제로 때렸다는 알림(OnAttack)에서 한다.
    /// </summary>
    public sealed class LSO_MemoryFrenzy : LSO_IAbility, LSO_IAbilityCountModifier, IOnAnimalAttack, LSO_IPhaseAware,
        LSO_IAbilityInitializable
    {
        // 공격력이 이 값의 배수를 새로 넘길 때마다 충전된다.
        private const int FrenzyStep = 6;

        /// <summary>충전됐을 때 늘어나는 타격 수. 1이면 2연타.</summary>
        private const int BonusStrikes = 1;

        private bool _unlocked;

        // 마지막으로 확인한 구간. 공격력 6이면 1, 12면 2.
        // 2페이즈 진입 시점의 구간으로 시작한다.
        private int _lastChargedStep;

        private bool _charged;

        /// <summary>
        /// 여기서 하는 일은 진단뿐이다. 실제 동작에 필요한 self는 훅이 매번 넘겨준다.
        ///
        /// 이 특성은 LSO_BossPhase가 OnPhaseChanged를 불러줘야만 해금된다.
        /// 그 컴포넌트가 없으면 _unlocked가 영영 false로 남아 아무 일도 일어나지 않는데,
        /// 컴파일도 통과하고 로그도 없어서 "붙였는데 왜 2연타가 안 나오지"로 시간을 버리게 된다.
        /// </summary>
        public void Initialize(LSO_AbilityContext context)
        {
            LDY_Animal owner = context?.Owner;

            if (owner == null)
            {
                Debug.LogError("LSO_MemoryFrenzy: 주인을 몰라 페이즈 해금을 확인할 수 없습니다.");
                return;
            }

            if (owner.GetComponent<LSO_BossPhase>() == null)
                Debug.LogError(
                    $"{owner.name}: LSO_BossPhase가 없어 기억 폭주가 해금되지 않습니다. " +
                    $"보스 오브젝트에 LSO_BossPhase를 붙이세요.", owner);
        }

        /// <summary>
        /// 기억 폭주는 2페이즈에 활성화된다. 그전에는 아무 일도 하지 않는다.
        ///
        /// == 2 가 아니라 >= 2 로 보는 이유는 큰 피해 한 방에 페이즈를 건너뛸 수 있어서다.
        /// </summary>
        public void OnPhaseChanged(LDY_Animal self, int phase)
        {
            if (phase < 2) return;
            if (_unlocked) return;

            _unlocked = true;

            // 진입 시점의 공격력을 기준선으로 잡는다.
            // 이미 6의 배수를 넘겨 있어도 충전하지 않고, 여기서부터 새로 센다.
            //
            // 첫 공격 때 잡으면 그 사이에 오른 공격력이 기준선에 묻혀버린다.
            _lastChargedStep = self != null ? self.GetAtk() / FrenzyStep : 0;
        }

        /// <summary>
        /// 충전돼 있으면 한 번 더 때린다.
        ///
        /// 받은 값에 더하는 이유는 누적 질의이기 때문이다.
        /// 2를 그대로 돌려주면 다른 특성이 올려놓은 횟수를 덮어쓴다.
        /// </summary>
        public int ModifyAttackCount(LDY_Animal self, LDY_Animal target, int count)
        {
            if (!_unlocked) return count;

            RefreshCharge(self);

            return _charged ? count + BonusStrikes : count;
        }

        /// <summary>
        /// 실제로 때렸으니 충전을 쓴다.
        /// 2연타면 이 알림이 두 번 오는데, 첫 타격에서 이미 꺼지므로 나머지는 그냥 지나간다.
        /// </summary>
        public void OnAttack(LSO_AnimalSO animal)
        {
            _charged = false;
        }

        /// <summary>
        /// 공격력이 새 구간에 들어갔으면 충전한다.
        ///
        /// 구간 번호로 비교하기 때문에 한 번에 두 칸 이상 뛰어넘어도 충전은 1회다.
        /// 같은 구간에서 여러 번 불려도 결과가 바뀌지 않아 질의 도중에 불러도 안전하다.
        /// </summary>
        private void RefreshCharge(LDY_Animal self)
        {
            if (self == null) return;

            int step = self.GetAtk() / FrenzyStep;
            if (step <= _lastChargedStep) return;

            _lastChargedStep = step;
            _charged = true;
        }
    }
}
