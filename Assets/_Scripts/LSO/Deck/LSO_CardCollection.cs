using System;
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Manager;

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

        public event Action<LSO_CardSO, int> CollectionChanged;

        public LSO_CardCollection(DeckCardsSaveData[] savedItems)
        {
            if (savedItems == null)
                return;

            foreach (DeckCardsSaveData item in savedItems)
                AddItem(item.cardId, item.amount, false);
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

        public DeckCardsSaveData[] ToSaveData()
        {
            DeckCardsSaveData[] result = new DeckCardsSaveData[_items.Count];
            int index = 0;

            foreach (KeyValuePair<LSO_CardSO, int> item in _items)
                result[index++] = new DeckCardsSaveData(item.Key, item.Value);

            return result;
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
