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
        Will        = 1 << 6,   // 촛불 숫자키/휠 + 유언 붙이기
        InfoPanel   = 1 << 7,
        Reward      = 1 << 8,
        All         = ~0
    }
}
