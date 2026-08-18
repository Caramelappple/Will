namespace _Scripts.LDY
{
    public enum LDY_RangeType
    {
        Melee,
        Ranged,
        Jump,
        None,

        // 값이 에셋에 int로 저장되므로 새 항목은 반드시 이 아래에만 붙일 것.
        // 중간에 끼우면 기존 에셋의 사거리가 통째로 다른 것을 가리킨다.

        /// <summary>룩처럼 상하좌우 1칸. Melee와 달리 대각선은 닿지 않는다.</summary>
        MeleeOrthogonal,
    }
}
