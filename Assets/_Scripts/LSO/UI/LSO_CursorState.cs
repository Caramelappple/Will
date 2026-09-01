namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 커서 모양.
    ///
    /// 값은 뒤에만 추가할 것. 중간에 끼워 넣으면 씬에 저장된 값이 어긋난다.
    /// </summary>
    public enum LSO_CursorState
    {
        /// <summary>아무것도 안 가리키고 있을 때.</summary>
        Default,

        /// <summary>가리키고는 있지만 지금은 누를 수 없을 때. 적 턴, 연출 중 등.</summary>
        Blocked,

        /// <summary>누를 수 있는 것 위에 있을 때.</summary>
        Interactable
    }
}
