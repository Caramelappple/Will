using _Scripts.LDY;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 한 번의 공격 행동에서 실제로 몇 번 때릴지 바꾼다.
    ///
    /// IStatModifier와 같은 누적 질의 방식이다. 특성이 "한 번 더 때려라"라고 명령하지 않고
    /// 공격 시스템이 먼저 물어본다. 명령형으로 만들면 연출과 행동력 회계가 특성마다 흩어진다.
    ///
    /// 여러 번 때려도 행동력은 한 번만 소모된다. 2회 공격은 두 번의 행동이 아니라
    /// 한 번의 행동이 강해진 것이기 때문이다.
    /// </summary>
    public interface LSO_IAbilityCountModifier
    {
        /// <param name="self">공격하는 기물.</param>
        /// <param name="target">맞는 기물. 특정 상대에게만 다단이 되는 특성을 위해 함께 넘긴다.</param>
        /// <param name="count">여기까지 누적된 타격 횟수. 기본은 1이다.</param>
        int ModifyAttackCount(LDY_Animal self, LDY_Animal target, int count);
    }
}
