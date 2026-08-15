using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 덱을 세이브 형태(id 목록)와 게임 형태(카드 목록) 사이에서 옮긴다.
    ///
    /// 덱의 정본은 KTH_DeckDataPersistent.savedInventory 하나뿐이므로
    /// 읽기도 쓰기도 그쪽 공개 API만 쓴다.
    /// </summary>
    public sealed class LDY_DeckSaveGateway
    {
        private readonly LDY_CardCatalogSO _catalog;

        public LDY_DeckSaveGateway(LDY_CardCatalogSO catalog)
        {
            _catalog = catalog;
        }

        public void Capture(LDY_RunSaveData data)
        {
            data.deckCardIds.Clear();

            KTH_DeckDataPersistent holder = KTH_DeckDataPersistent.Instance;
            if (holder == null)
            {
                Debug.LogWarning("[LDY_DeckSaveGateway] KTH_DeckDataPersistent가 없어 덱을 읽지 못했습니다.");
                return;
            }

            foreach (LSO_CardSO card in holder.SavedInventory)
            {
                if (card == null) continue;

                data.deckCardIds.Add(card.Id);
            }
        }

        public void Restore(LDY_RunSaveData data)
        {
            KTH_DeckDataPersistent holder = KTH_DeckDataPersistent.Instance;
            if (holder == null)
            {
                Debug.LogWarning("[LDY_DeckSaveGateway] KTH_DeckDataPersistent가 없어 덱을 되돌리지 못했습니다.");
                return;
            }

            if (_catalog == null)
            {
                Debug.LogError("[LDY_DeckSaveGateway] 카드 카탈로그가 없어 id를 카드로 되돌릴 수 없습니다.");
                return;
            }

            var deck = new List<LSO_CardSO>();

            foreach (string id in data.deckCardIds)
            {
                LSO_CardSO card = _catalog.Find(id);

                // 카탈로그에 없는 id는 되돌릴 방법이 없다.
                // 조용히 넘기면 덱이 소리 없이 줄어들므로 알린다.
                if (card == null)
                {
                    Debug.LogWarning($"[LDY_DeckSaveGateway] 카드 id '{id}' 를 카탈로그에서 찾지 못했습니다.");
                    continue;
                }

                deck.Add(card);
            }

            holder.SaveInventory(deck);
        }
    }
}
