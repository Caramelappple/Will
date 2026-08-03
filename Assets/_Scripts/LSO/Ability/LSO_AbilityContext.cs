using _Scripts.LDY;
using _Scripts.LSO.DeathSystem;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 특성이 자기 소유자 바깥의 정보(보드, 이벤트)에 닿기 위한 통로.
    /// Board와 Events는 호출 시점에 조회하므로, 특성이 만들어지는 순서가 매니저보다 빨라도 안전하다.
    /// </summary>
    public class LSO_AbilityContext
    {
        /// <summary>이 특성을 들고 있는 기물.</summary>
        public LDY_Animal Owner { get; }

        public LSO_AbilityContext(LDY_Animal owner)
        {
            Owner = owner;
        }

        /// <summary>격자 조회용. 매니저가 아직 없으면 null.</summary>
        public LDY_BoardManager Board =>
            GameManager.HasInstance ? GameManager.Instance.Board : null;

        /// <summary>턴/사망 이벤트 구독용. 매니저가 아직 없으면 null.</summary>
        public GameEventDispatcher Events =>
            GameManager.HasInstance ? GameManager.Instance.EventDispatcher : null;

        /// <summary>기물을 죽일 때 쓴다. 직접 Destroy하지 말 것.</summary>
        public LSO_IDeathService Deaths =>
            GameManager.HasInstance ? GameManager.Instance.DeathService : null;
    }
}
