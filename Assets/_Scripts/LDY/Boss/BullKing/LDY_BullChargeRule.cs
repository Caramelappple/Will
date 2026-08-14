using System;
using UnityEngine;

namespace _Scripts.LDY.Boss.BullKing
{
    /// <summary>
    /// 페이즈 하나에서 쓰는 돌진 수치 묶음.
    ///
    /// 페이즈마다 값만 다르고 규칙은 같으므로, 특성에 상수로 박지 않고 이렇게 한 벌씩 들려준다.
    /// 3페이즈가 생기면 LDY_BullKingBoss에 한 벌을 더 얹기만 하면 되고 특성은 손대지 않는다.
    /// </summary>
    [Serializable]
    public sealed class LDY_BullChargeRule
    {
        [Tooltip("한 번의 돌진으로 달릴 수 있는 최대 칸 수.")]
        [Min(1)] public int chargeRange = 4;

        [Tooltip("충돌한 기물이 받는 피해. 연쇄로 밀려나는 기물도 같은 값을 받는다.")]
        [Min(0)] public int collisionDamage = 3;

        [Tooltip("더 밀려날 곳이 없어 벽에 부딪힌 기물이 추가로 받는 피해.")]
        [Min(0)] public int wallDamage = 2;

        [Tooltip("한 번의 돌진으로 밀어낼 수 있는 기물 수. 이 수를 넘는 기물은 밀리지 않는다.")]
        [Min(1)] public int maxChainPush = 3;
    }
}
