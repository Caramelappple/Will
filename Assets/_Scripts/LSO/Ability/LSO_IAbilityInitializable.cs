namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 소유자나 보드 정보가 필요한 특성이 구현한다.
    /// 필요 없는 특성은 구현하지 않으면 되므로, 모든 특성이 컨텍스트를 떠안지 않는다.
    /// </summary>
    public interface LSO_IAbilityInitializable
    {
        /// <summary>특성이 만들어진 직후 한 번 호출된다.</summary>
        void Initialize(LSO_AbilityContext context);
    }
}
