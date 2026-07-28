using System;
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Manager;

namespace _Scripts.LSO
{
    public sealed class DeckModule
    {
        private readonly Dictionary<LSO_CardSO, int> _items = new Dictionary<LSO_CardSO, int>();

        public event Action<LSO_CardSO, int> DeckChanged;

        public DeckModule(DeckCardsSaveData[] savedItems)
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

            DeckChanged?.Invoke(itemId, GetItemAmount(itemId));
            return true;
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
                DeckChanged?.Invoke(itemId, _items[itemId]);
        }
    }
}
