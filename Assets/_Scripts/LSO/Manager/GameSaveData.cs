using System;
using _Scripts.LSO.Deck.Data;

namespace _Scripts.LSO.Manager
{
    [Serializable]
    public struct DeckCardsSaveData
    {
        public LSO_CardSO cardId;
        public int amount;

        public DeckCardsSaveData(LSO_CardSO itemId, int amount)
        {
            this.cardId = itemId;
            this.amount = amount;
        }
    }
    
    public struct InventoryItemSaveData
    {
        public LSO_ItemSO itemId;
        public int amount;

        public InventoryItemSaveData(LSO_ItemSO itemId, int amount)
        {
            this.itemId = itemId;
            this.amount = amount;
        }
    }

    [Serializable]
    public struct GameSaveData
    {
        public int stage;
        public DeckCardsSaveData[] inventoryItems;
        
        public static GameSaveData CreateDefault()
        {
            return new GameSaveData
            {
               stage = 0,
               inventoryItems = Array.Empty<DeckCardsSaveData>(),
            };
        }
    }
}