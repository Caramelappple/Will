using _Scripts.LDY;

namespace _Scripts.LSO.DeathSystem
{
    /// <summary>
    /// 자기 자신이 죽는 순간 호출된다. 소유 기물이 직접 물어보므로 전역 등록이 필요 없다.
    /// 오브젝트가 파괴되기 전에 불리므로 이 시점에는 아직 self를 참조할 수 있다.
    /// </summary>
    public interface LSO_IOnDeath
    {
        void OnDeath(LDY_Animal self, LDY_Animal killer);
    }
}
