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

        // ESC는 넣지 않는다. LDY_EscapeKeyHandler가 이미 ESC로
        // 배치 취소 · 창 닫기 · 설정 열기를 순서대로 처리하고 있어서,
        // 여기서도 받으면 카메라가 돌아가면서 설정 창이 함께 열린다.
        //
        // ESC로도 돌아가게 하려면 그 핸들러의 우선순위 목록에
        // ReturnToDefault를 넣는 편이 낫다.
    }
}
