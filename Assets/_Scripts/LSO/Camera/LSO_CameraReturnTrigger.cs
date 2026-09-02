namespace _Scripts.LSO.Camera
{
    /// <summary>
    /// 기본 샷으로 돌아가게 만드는 조작.
    ///
    /// 값은 뒤에만 추가할 것. 중간에 끼워 넣으면 씬에 저장된 목록이 어긋난다.
    /// </summary>
    public enum LSO_CameraReturnTrigger
    {
        /// <summary>화면 아무 데나 좌클릭.</summary>
        LeftClickAnywhere,

        /// <summary>화면 아무 데나 우클릭.</summary>
        RightClickAnywhere

        // ESC는 아직 넣지 않았다. 프로젝트에 ESC를 보는 곳이 하나도 없어서
        // 지금 넣으면 여기가 유일한 주인이 된다. 나중에 ESC 처리가 다시 생기면
        // 두 곳이 같은 키를 나눠 갖게 되므로, 그때 어디서 받을지 먼저 정할 것.
    }
}
