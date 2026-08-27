namespace _Scripts.LSO.UI.Input
{
    /// <summary>
    /// 호버 이펙트를 사용할 클래스에 붙이는 인터페이스
    /// </summary>
    public interface LSO_IHoverEffect
    {
        /// <summary>
        /// 마우스가 올라갈때
        /// </summary>
        void OnHoverEnter();
        /// <summary>
        /// 마우스가 내려갈때
        /// </summary>
        void OnHoverExit();
    }
}
