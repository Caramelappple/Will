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
    }
}
