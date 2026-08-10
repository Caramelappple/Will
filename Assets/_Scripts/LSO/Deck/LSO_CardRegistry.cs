using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

namespace _Scripts.LSO.Deck
{
    /// <summary>
    /// id 문자열로 카드 에셋을 찾는다.
    /// 세이브에는 id만 적히므로, 불러올 때 이 표를 거쳐야 실제 카드로 되돌아온다.
    /// 카드 목록은 GameManager가 인스펙터에서 넘겨준다.
    /// </summary>
    public sealed class LSO_CardRegistry
    {
        private readonly Dictionary<string, LSO_CardSO> _byId = new();

        public int Count => _byId.Count;

        public LSO_CardRegistry(IEnumerable<LSO_CardSO> cards)
        {
            if (cards == null) return;

            foreach (LSO_CardSO card in cards)
            {
                if (card == null) continue;

                // id가 겹치면 어느 쪽을 써야 할지 알 수 없다. 조용히 덮어쓰면 나중에 원인을 못 찾으므로 에러로 알린다.
                if (_byId.TryGetValue(card.Id, out LSO_CardSO existing))
                {
                    Debug.LogError(
                        $"LSO_CardRegistry: id '{card.Id}'가 중복입니다. ({existing.name} / {card.name})", card);
                    continue;
                }

                _byId.Add(card.Id, card);
            }
        }

        /// <summary>id에 해당하는 카드. 없으면 null.</summary>
        public LSO_CardSO Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            return _byId.TryGetValue(id, out LSO_CardSO card) ? card : null;
        }

        public bool Contains(string id)
        {
            return !string.IsNullOrEmpty(id) && _byId.ContainsKey(id);
        }
    }
}
