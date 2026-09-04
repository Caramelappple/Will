using _Scripts.LSO.HealthSystem.Data;

namespace _Scripts.LSO.HealthSystem
{
    public interface LSO_IDamageModifier
    {
        /// <summary>
        /// 도는 차례. 작을수록 먼저다.
        ///
        /// 숫자를 직접 적지 말고 <see cref="LSO_DamagePriority"/> 의 이름을 쓸 것.
        /// 거기에 전체 차례와 왜 그 순서여야 하는지가 적혀 있다.
        /// </summary>
        public int Priority { get; }

        public int ModifyIncomingDamage(DamageableResources target, DamageData data, int damage);
    }
}
