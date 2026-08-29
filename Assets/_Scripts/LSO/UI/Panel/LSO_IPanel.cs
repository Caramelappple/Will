namespace _Scripts.LSO.UI.Panel
{
    /// <summary>
    /// 열고 닫을 수 있는 창.
    /// 버튼은 "어떻게" 열리는지 몰라야 한다. 즉시 켜지든 페이드로 나타나든 이 계약만 지키면 된다.
    /// </summary>
    public interface LSO_IPanel
    {
        /// <summary>지금 열려 있는지.</summary>
        bool IsOpen { get; }

        void Open();

        void Close();
    }
}
