using System;
using _Scripts.LDY;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.DeathSystem;
using UnityEngine;

namespace _Scripts.LSO.Manager
{
    /// <summary>
    /// 씬을 넘나드는 공용 창구를 찾아주는 등록소.
    ///
    /// 여기서 하는 일은 "지금 쓸 수 있는 매니저가 무엇인지"를 알려주는 것뿐이다.
    /// 실제 일은 등록된 쪽이 한다. 게임 규칙이나 데이터를 여기에 두지 말 것.
    ///
    /// 세이브·덱·카드 목록은 LDY_SaveService 계열이 맡는다.
    /// </summary>
    public class GameManager : MonoSingleton<GameManager>
    {
        [Tooltip("비워두면 자식 오브젝트에서 찾고, 그래도 없으면 직접 추가한다.")]
        [SerializeField] private GameEventDispatcher eventDispatcher;

        /// <summary>
        /// 턴·사망 같은 전역 이벤트 통로.
        ///
        /// Awake가 아니라 여기서 준비하는 이유는 MonoSingleton.Instance가
        /// Awake가 아직 돌지 않은 인스턴스도 돌려주기 때문이다.
        /// 조립을 Awake에 두면 그 사이에 읽는 쪽이 null을 받아 이벤트가 조용히 안 걸린다.
        ///
        /// 따라서 GameEventDispatcher는 자기 Awake/OnEnable에서 이 프로퍼티를 읽으면 안 된다.
        /// 아직 필드에 대입되기 전이라 무한 재귀가 된다.
        /// </summary>
        public GameEventDispatcher EventDispatcher
        {
            get
            {
                if (eventDispatcher != null) return eventDispatcher;

                GameEventDispatcher found = GetComponentInChildren<GameEventDispatcher>(true);

                eventDispatcher = found != null
                    ? found
                    : gameObject.AddComponent<GameEventDispatcher>();

                return eventDispatcher;
            }
        }

        public LDY_TurnManager TurnManager { get; private set; }
        public event Action<LDY_TurnManager> TurnManagerChanged;

        /// <summary>현재 전투의 보드. 씬마다 새로 생기므로 보드가 스스로 등록한다.</summary>
        public LDY_BoardManager Board { get; private set; }

        /// <summary>
        /// 보드가 등록되거나 풀렸을 때. 사라지면 null이 온다.
        ///
        /// 보드를 기다리는 쪽이 "아직 없으면 다음 프레임에 다시 본다"를 각자 구현하지 않게 하려고 둔다.
        /// 구독 시점에 이미 보드가 있을 수 있으므로, 붙인 직후 Board를 한 번 직접 확인할 것.
        /// </summary>
        public event Action<LDY_BoardManager> BoardChanged;

        /// <summary>기물을 죽이는 창구. 특성이 스스로 죽거나 남을 죽일 때 쓴다.</summary>
        public LSO_IDeathService DeathService { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            DontDestroyOnLoad(gameObject);
        }

        public void RegisterTurnManager(LDY_TurnManager turnManager)
        {
            if (turnManager == null || TurnManager == turnManager) return;

            TurnManager = turnManager;
            TurnManagerChanged?.Invoke(TurnManager);
        }

        public void UnregisterTurnManager(LDY_TurnManager turnManager)
        {
            if (TurnManager != turnManager) return;

            TurnManager = null;
            TurnManagerChanged?.Invoke(null);
        }

        public void RegisterBoard(LDY_BoardManager board)
        {
            if (board == null || Board == board) return;

            Board = board;
            BoardChanged?.Invoke(Board);
        }

        // 비교에 ==를 쓰는 것은 LDY_BoardManager가 UnityEngine.Object이기 때문이다.
        // 파괴된 보드는 ==에서 null로 취급되므로, 이미 사라진 보드가 뒤늦게 해제를 걸어도 걸러진다.
        public void UnregisterBoard(LDY_BoardManager board)
        {
            if (Board != board) return;

            Board = null;
            BoardChanged?.Invoke(null);
        }

        public void RegisterDeathService(LSO_IDeathService service)
        {
            if (service == null) return;

            DeathService = service;
        }

        // 이쪽은 순수 인터페이스라 Unity의 == 규칙이 없다.
        // 위와 다른 방식인 것은 실수가 아니므로 통일하지 말 것.
        public void UnregisterDeathService(LSO_IDeathService service)
        {
            if (!ReferenceEquals(DeathService, service)) return;

            DeathService = null;
        }
    }
}
