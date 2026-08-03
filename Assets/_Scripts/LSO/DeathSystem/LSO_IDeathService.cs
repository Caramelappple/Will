using _Scripts.LDY;

namespace _Scripts.LSO.DeathSystem
{
    /// <summary>
    /// 기물을 죽이는 단 하나의 창구.
    /// 공격으로 죽든 특성으로 죽든 이 경로를 타야 보드 제거·이벤트·유언이 빠짐없이 처리된다.
    /// </summary>
    public interface LSO_IDeathService
    {
        /// <param name="victim">죽는 기물.</param>
        /// <param name="killer">처치한 기물. 자연사·특성 자멸 등은 null.</param>
        void Kill(LDY_Animal victim, LDY_Animal killer);
    }
}
