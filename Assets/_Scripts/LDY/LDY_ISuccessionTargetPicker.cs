using System.Collections.Generic;

namespace _Scripts.LDY
{
    /// <summary>
    /// 적 팀이 계승 대상을 자동으로 고를 때 쓰는 기준. 스테이지별로 갈아끼울 수 있도록 분리해 둔다.
    /// 유효 대상인지는 여기서 판단하지 않는다 — 그 판정은 DLJ_SuccessionSystem이 계속 맡는다.
    /// </summary>
    public interface LDY_ISuccessionTargetPicker
    {
        /// <summary>고를 대상이 없으면 null.</summary>
        LDY_Animal Pick(LDY_Animal dying, IReadOnlyList<LDY_Animal> candidates);
    }
}
