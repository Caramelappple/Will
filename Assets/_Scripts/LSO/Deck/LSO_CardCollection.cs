using System;
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Manager;
using UnityEngine;

namespace _Scripts.LSO.Deck
{
    /// <summary>
    /// 플레이어가 보유한 카드와 그 수량. 세이브/로드 대상이다.
    /// 전투 중의 뽑을 더미/손패/버린 더미는 LSO_Deck이 따로 담당한다.
    /// (이전 이름: DeckModule)
    /// </summary>
    public sealed class LSO_CardCollection
    {
        private readonly Dictionary<LSO_CardSO, int> _items = new Dictionary<LSO_CardSO, int>();

        /// <summary>카드 한 종류의 수량이 바뀔 때. (카드, 바뀐 뒤 수량)</summary>
        public event Action<LSO_CardSO, int> CollectionChanged;

        /// <summary>세이브를 불러와 목록 전체가 갈렸을 때. UI는 이걸 받으면 통째로 다시 그린다.</summary>
        public event Action Reloaded;

        public LSO_CardCollection()
        {
        }

        public LSO_CardCollection(DeckCardsSaveData[] savedItems, LSO_CardRegistry registry)
        {
            Load(savedItems, registry);
        }

        /// <summary>
        /// 세이브 내용으로 목록을 갈아끼운다. 기존 내용은 버린다.
        /// 레지스트리에 없는 id는 건너뛴다. 카드 하나가 사라졌다고 세이브 전체를 못 읽는 편이 더 나쁘다.
        /// </summary>
        public void Load(DeckCardsSaveData[] savedItems, LSO_CardRegistry registry)
        {
            _items.Clear();

            if (savedItems != null && registry != null)
            {
                foreach (DeckCardsSaveData item in savedItems)
                {
                    LSO_CardSO card = registry.Find(item.cardId);

                    if (card == null)
                    {
                        Debug.LogWarning($"LSO_CardCollection: '{item.cardId}' 카드를 찾지 못해 건너뜁니다.");
                        continue;
                    }

                    AddItem(card, item.amount, false);
                }
            }

            Reloaded?.Invoke();
        }

        public int GetItemAmount(LSO_CardSO itemId)
        {
            return itemId && _items.TryGetValue(itemId, out int amount)
                ? amount
                : 0;
        }

        public void AddItem(LSO_CardSO itemId, int amount)
        {
            AddItem(itemId, amount, true);
        }

        public bool TryRemoveItem(LSO_CardSO itemId, int amount)
        {
            if (!itemId || amount <= 0 || GetItemAmount(itemId) < amount)
                return false;

            _items[itemId] -= amount;
            if (_items[itemId] <= 0)
                _items.Remove(itemId);

            CollectionChanged?.Invoke(itemId, GetItemAmount(itemId));
            return true;
        }

        /// <summary>보유 수량만큼 카드를 펼친 목록. 이걸로 전투용 LSO_Deck을 만든다.</summary>
        public List<LSO_CardSO> ToCardList()
        {
            List<LSO_CardSO> result = new List<LSO_CardSO>();

            foreach (KeyValuePair<LSO_CardSO, int> item in _items)
            {
                for (int i = 0; i < item.Value; i++)
                    result.Add(item.Key);
            }

            return result;
        }

        /// <summary>현재 보유 목록을 저장 형태로 변환한다. 카드 참조가 아니라 id 문자열로 나간다.</summary>
        public DeckCardsSaveData[] ToSaveData()
        {
            List<DeckCardsSaveData> result = new List<DeckCardsSaveData>(_items.Count);

            foreach (KeyValuePair<LSO_CardSO, int> item in _items)
            {
                if (item.Key == null) continue;

                result.Add(new DeckCardsSaveData(item.Key.Id, item.Value));
            }

            return result.ToArray();
        }

        public void Clear()
        {
            LSO_CardSO[] itemIds = new LSO_CardSO[_items.Count];
            _items.Keys.CopyTo(itemIds, 0);

            foreach (LSO_CardSO itemId in itemIds)
                TryRemoveItem(itemId, GetItemAmount(itemId));
        }

        private void AddItem(LSO_CardSO itemId, int amount, bool notify)
        {
            if (!itemId || amount <= 0)
                return;

            _items.TryGetValue(itemId, out int currentAmount);
            _items[itemId] = currentAmount + amount;

            if (notify)
                CollectionChanged?.Invoke(itemId, _items[itemId]);
        }
    }
}
