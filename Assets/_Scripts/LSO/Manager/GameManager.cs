using System;
using _Scripts.LDY;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.DeathSystem;
using _Scripts.LSO.Deck;
using _Scripts.LSO.Manager;
using UnityEngine;

namespace _Scripts.LSO
{
    public class GameManager : MonoSingleton<GameManager>
    {
        [Tooltip("비워두면 자식 오브젝트에서 찾고, 그래도 없으면 직접 추가한다.")]
        [SerializeField] private GameEventDispatcher eventDispatcher;

        public GameSaveData SaveData { get; private set; }
        /// <summary>플레이어가 보유한 카드(세이브 대상). 전투용 더미는 LSO_Deck이 따로 만든다.</summary>
        public LSO_CardCollection Cards { get; private set; }
        public GameEventDispatcher EventDispatcher => eventDispatcher;
        
        public LDY_TurnManager TurnManager { get; private set; }
        public event Action<LDY_TurnManager> TurnManagerChanged;

        /// <summary>현재 전투의 보드. 씬마다 새로 생기므로 보드가 스스로 등록한다.</summary>
        public LDY_BoardManager Board { get; private set; }

        /// <summary>기물을 죽이는 창구. 특성이 스스로 죽거나 남을 죽일 때 쓴다.</summary>
        public LSO_IDeathService DeathService { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            DontDestroyOnLoad(gameObject);

            SaveData = GameSaveData.CreateDefault();
            Cards = new LSO_CardCollection(SaveData.inventoryItems);

            if (eventDispatcher == null)
                eventDispatcher = GetComponentInChildren<GameEventDispatcher>(true);
            if (eventDispatcher == null)
                eventDispatcher = gameObject.AddComponent<GameEventDispatcher>();
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
            if (board == null) return;

            Board = board;
        }

        public void UnregisterBoard(LDY_BoardManager board)
        {
            if (Board != board) return;

            Board = null;
        }

        public void RegisterDeathService(LSO_IDeathService service)
        {
            if (service == null) return;

            DeathService = service;
        }

        public void UnregisterDeathService(LSO_IDeathService service)
        {
            if (!ReferenceEquals(DeathService, service)) return;

            DeathService = null;
        }
    }
}
