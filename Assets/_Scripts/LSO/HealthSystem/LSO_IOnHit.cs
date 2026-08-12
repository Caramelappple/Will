using _Scripts.LDY;
using _Scripts.LSO.HealthSystem.Data;

namespace _Scripts.LSO.HealthSystem
{
    /// <summary>
    /// 자기 자신이 피해를 맞았을 때 호출된다. 반격처럼 "맞은 뒤 반응"하는 특성이 구현한다.
    /// 피해량을 바꾸려는 목적이면 이게 아니라 LSO_IDamageModifier를 쓸 것.
    /// </summary>
    public interface LSO_IOnHit
    {
        void OnHit(LDY_Animal self, DamageData data);
    }
}
