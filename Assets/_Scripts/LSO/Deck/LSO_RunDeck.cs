using System.Collections.Generic;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.Deck.Data;

namespace _Scripts.LSO.Deck
{
    /// <summary>
    /// 이번 런에서 쓸 덱. 씬을 넘어가며 살아남는다.
    ///
    /// 덱을 읽는 곳이 전부 여기 하나만 보게 하는 것이 목적이다.
    /// 전투(카드 드로우)와 세이브가 같은 목록을 보면 둘이 어긋날 자리가 없어진다.
    ///
    /// 같은 카드가 여러 번 들어간다. 수량으로 접지 않는 이유는
    /// 세이브(LDY_RunSaveData.deckCardIds)가 이미 같은 형태이고,
    /// 드로우는 어차피 한 장씩 꺼내기 때문이다.
    /// </summary>
    public class LSO_RunDeck : MonoSingleton<LSO_RunDeck>
    {
        private readonly List<LSO_CardSO> _cards = new();

        public IReadOnlyList<LSO_CardSO> Cards => _cards;

        public int Count => _cards.Count;

        public bool HasDeck => _cards.Count > 0;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            DontDestroyOnLoad(gameObject);
        }

        /// <summary>덱 구성 화면에서 확정할 때 부른다.</summary>
        public void Commit(IEnumerable<LSO_CardSO> cards)
        {
            _cards.Clear();

            if (cards == null) return;

            foreach (LSO_CardSO card in cards)
            {
                if (card == null) continue;

                _cards.Add(card);
            }
        }

        /// <summary>세이브에서 되돌릴 때 부른다. Commit과 하는 일은 같지만 부르는 쪽이 다르다.</summary>
        public void Restore(IEnumerable<LSO_CardSO> cards) => Commit(cards);

        public void Clear() => _cards.Clear();
    }
}
