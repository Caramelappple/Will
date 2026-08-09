using System;

namespace _Scripts.LSO.Manager
{
    /// <summary>
    /// 보유 카드 한 종류의 저장 형태.
    /// 카드 에셋 참조가 아니라 id 문자열로 적는다. 참조는 JSON으로 나가지 않기 때문이다.
    /// 문자열을 실제 카드로 되돌리는 일은 LSO_CardRegistry가 맡는다.
    /// </summary>
    [Serializable]
    public struct DeckCardsSaveData
    {
        public string cardId;
        public int amount;

        public DeckCardsSaveData(string cardId, int amount)
        {
            this.cardId = cardId;
            this.amount = amount;
        }
    }

    [Serializable]
    public struct GameSaveData
    {
        public int stage;
        public int maxCost;
        public DeckCardsSaveData[] inventoryItems;

        public static GameSaveData CreateDefault()
        {
            return new GameSaveData
            {
                stage = 0,
                maxCost = 5,
                inventoryItems = Array.Empty<DeckCardsSaveData>(),
            };
        }
    }
}
