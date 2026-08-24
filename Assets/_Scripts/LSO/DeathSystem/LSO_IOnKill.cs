using _Scripts.LDY;

namespace _Scripts.LSO.DeathSystem
{
    /// <summary>
    /// 자기가 다른 기물을 직접 처치한 순간 호출된다.
    ///
    /// LSO_IOnDeath의 대칭이다 — 그쪽은 죽는 본인이 듣고, 이쪽은 죽인 쪽이 듣는다.
    /// 예전에는 죽는 쪽만 알림을 받아서, 까마귀왕의 포식처럼 "내가 죽였을 때"가 조건인 특성을
    /// 표현할 방법이 아예 없었다.
    ///
    /// victim은 아직 파괴 전이라 스탯과 동물 데이터를 읽을 수 있다.
    /// 다만 이미 격자에서는 빠진 뒤이므로 보드 조회로는 찾을 수 없다.
    ///
    /// 유언보다 먼저 불린다. 계승처럼 죽은 기물의 스탯을 옮기는 유언이 있어서,
    /// 나중에 부르면 원본이 아닌 값을 읽게 된다.
    /// </summary>
    public interface LSO_IOnKill
    {
        /// <param name="self">처치한 기물. 이 특성의 소유자다.</param>
        /// <param name="victim">처치당한 기물. 파괴 직전이라 아직 참조할 수 있다.</param>
        void OnKill(LDY_Animal self, LDY_Animal victim);
    }
}
