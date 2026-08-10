using System;
using _Scripts.LDY;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.DeathSystem;
using _Scripts.LSO.Deck;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Manager;
using UnityEngine;

namespace _Scripts.LSO
{
    public class GameManager : MonoSingleton<GameManager>
    {
        [Tooltip("비워두면 자식 오브젝트에서 찾고, 그래도 없으면 직접 추가한다.")]
        [SerializeField] private GameEventDispatcher eventDispatcher;

        [Tooltip("게임에 존재하는 모든 카드. 세이브에 적힌 id를 실제 카드로 되돌릴 때 쓴다.\n" +
                 "여기 없는 카드는 세이브에서 복원되지 않는다.")]
        [SerializeField] private LSO_CardSO[] cardCatalog;

        public GameSaveData SaveData { get; private set; }
        
        /// <summary>세이브가 통째로 반영됐을 때. 스테이지 UI 등이 다시 그리는 신호로 쓴다.</summary>
        public event Action<GameSaveData> SaveDataChanged;

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

            ApplySave(GameSaveData.CreateDefault());

            if (eventDispatcher == null)
                eventDispatcher = GetComponentInChildren<GameEventDispatcher>(true);
            if (eventDispatcher == null)
                eventDispatcher = gameObject.AddComponent<GameEventDispatcher>();
        }

        /// <summary>
        /// 불러온 세이브를 현재 상태로 반영한다. 새 게임 시작과 세이브 로드 양쪽에서 같은 경로를 쓴다.
        /// </summary>
        public void ApplySave(GameSaveData data)
        {
            SaveData = data;
        }

        /// <summary>
        /// 저장 직전에 호출한다. 보유 카드는 런타임 목록이 원본이므로 여기서 최신 값을 담아 내보낸다.
        /// </summary>
        public GameSaveData CaptureSave()
        {
            GameSaveData data = SaveData;

            return data;
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
