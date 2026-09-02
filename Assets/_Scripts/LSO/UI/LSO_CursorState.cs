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
        Interactable,

        /// <summary>
        /// 커서를 아예 감춘다. 그림이 없는 것이 아니라 화면에서 사라진다.
        ///
        /// 컷신이나 연출처럼 조작할 것이 없는 동안 쓴다.
        /// 맨 뒤에 있으므로 우선순위가 가장 높다 — 감추기로 했으면
        /// 그 밑에 무엇이 요청 중이든 보이지 않는 것이 맞다.
        /// </summary>
        Hidden
    }
}
