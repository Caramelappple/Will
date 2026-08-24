namespace _Scripts.LSO.UI.Feedback
{
    /// <summary>
    /// 플레이어의 행동이 거부된 이유.
    /// UI가 무엇을 흔들지 골라내는 용도라 게임 규칙만큼 촘촘할 필요는 없다.
    ///
    /// 값은 뒤에만 추가할 것. 중간에 끼워 넣으면 씬에 저장된 필터가 어긋난다.
    /// </summary>
    public enum LSO_RejectReason
    {
        Unknown,

        /// <summary>카드 코스트가 모자람.</summary>
        NotEnoughCost,

        /// <summary>행동력이 모자람.</summary>
        NotEnoughActionPoint,

        /// <summary>놓을 수 없는 칸.</summary>
        InvalidTile,

        /// <summary>내 턴이 아님.</summary>
        NotYourTurn,

        /// <summary>아직 잠긴 기능.</summary>
        Locked
    }
}
