namespace _Scripts.LSO.HealthSystem.Data
{
    /// <summary>
    /// 피해가 어디서 왔는지 구분한다.
    /// "근접 공격을 받으면 반격", "저주 피해 무효" 같은 특성이 이 값으로 판단한다.
    /// 값을 명시하지 않고 만든 DamageData는 Unknown이 되므로, 기존 호출부는 그대로 동작한다.
    /// </summary>
    public enum LSO_DamageSource
    {
        Unknown = 0,
        Melee,
        Ranged,
        Jump,
        Curse,
        Rage,
        Ability,

        /// <summary>
        /// 상어왕의 영역 공격(사냥터 개장).
        ///
        /// 특성으로 막을 수 없는 피해다. 옹골참으로 버티거나 허약으로 흘려낼 수 없다.
        /// 막을 수 있는지를 판단하는 곳은 LSO_DamageRules 하나이므로, 특성 쪽에서
        /// 이 값을 직접 보지 말 것.
        ///
        /// Ability 에서 떼어낸 이유는 흡혈·가시 같은 보통의 특성 피해와 같은 취급을
        /// 받으면 안 되기 때문이다. 그쪽은 막혀도 된다.
        /// </summary>
        SharkKingZone,
    }
}
