using System;

namespace _Scripts.LSO.Tutorial
{
    [Flags]
    public enum LSO_TutorialAction
    {
        None        = 0,
        CardSelect  = 1 << 0,
        Place       = 1 << 1,
        PieceSelect = 1 << 2,
        Move        = 1 << 3,
        Attack      = 1 << 4,
        EndTurn     = 1 << 5,
        WillPaint   = 1 << 6,   // 촛불을 눌러 선택한 카드에 유언 붙이기
        InfoPanel   = 1 << 7,
        Reward      = 1 << 8,
        WillSelect  = 1 << 9,   // 숫자키/휠로 촛불의 유언 바꾸기
        All         = ~0
    }
}
