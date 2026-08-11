using System;
using System.Collections.Generic;
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

        /// <summary>id로 카드를 찾는 표. 세이브를 되돌릴 때 필요하다.</summary>
        public LSO_CardRegistry CardRegistry { get; private set; }

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

            CardRegistry = new LSO_CardRegistry(cardCatalog);

            // 여기서 ApplySave를 부르면 안 된다. 아직 세이브를 읽기 전이라 기본값이 들어오는데,
            // 그러면 덱빌드 씬에서 이미 확정해둔 덱까지 함께 지워진다.
            // (GameManager는 전투 씬에서 처음 생길 수도 있다.)
            // 새 게임을 시작하는 쪽이 ApplySave(GameSaveData.CreateDefault())를 명시적으로 부를 것.
            SaveData = GameSaveData.CreateDefault();

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
            RestoreDeck(data.inventoryItems);
            SaveDataChanged?.Invoke(SaveData);
        }

        /// <summary>
        /// 저장 직전에 호출한다. 보유 카드는 런타임 목록이 원본이므로 여기서 최신 값을 담아 내보낸다.
        /// </summary>
        public GameSaveData CaptureSave()
        {
            GameSaveData data = SaveData;
            data.inventoryItems = CaptureDeck();

            return data;
        }

        /// <summary>
        /// 세이브에 적힌 id를 실제 카드로 되돌려 덱에 넣는다.
        /// 게임이 실제로 읽는 덱은 KTH_DeckDataPersistent 하나뿐이므로 그쪽을 직접 채운다.
        /// </summary>
        private void RestoreDeck(DeckCardsSaveData[] items)
        {
            KTH_DeckDataPersistent holder = KTH_DeckDataPersistent.Instance;
            if (holder == null)
            {
                Debug.LogWarning("GameManager: KTH_DeckDataPersistent가 없어 덱을 복원하지 못했습니다.");
                return;
            }

            var deck = new List<LSO_CardSO>();

            if (items != null)
            {
                foreach (DeckCardsSaveData item in items)
                {
                    LSO_CardSO card = CardRegistry.Find(item.cardId);

                    // 카탈로그에 없는 id는 되돌릴 방법이 없다. 조용히 넘기면 덱이 소리 없이 줄어드므로 알린다.
                    if (card == null)
                    {
                        Debug.LogWarning($"GameManager: 세이브의 카드 id '{item.cardId}'를 cardCatalog에서 찾지 못했습니다.", this);
                        continue;
                    }

                    for (int i = 0; i < item.amount; i++)
                        deck.Add(card);
                }
            }

            holder.SaveInventory(deck);
        }

        /// <summary>덱은 같은 카드가 여러 장 들어가는 목록이므로, 저장할 때 id+수량으로 접는다.</summary>
        private DeckCardsSaveData[] CaptureDeck()
        {
            KTH_DeckDataPersistent holder = KTH_DeckDataPersistent.Instance;

            // 덱을 읽을 수 없으면 마지막으로 반영된 값을 그대로 둔다. 빈 배열로 덮으면 세이브가 덱을 잃는다.
            if (holder == null)
                return SaveData.inventoryItems ?? Array.Empty<DeckCardsSaveData>();

            var amounts = new Dictionary<string, int>();
            foreach (LSO_CardSO card in holder.savedInventory)
            {
                if (card == null) continue;

                amounts.TryGetValue(card.ID.ToString(), out int count);
                amounts[card.ID.ToString()] = count + 1;
            }

            var result = new DeckCardsSaveData[amounts.Count];
            int index = 0;
            foreach (KeyValuePair<string, int> pair in amounts)
                result[index++] = new DeckCardsSaveData(pair.Key, pair.Value);

            return result;
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
