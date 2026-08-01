using System;
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;

namespace _Scripts.LSO.Deck
{
    /// <summary>
    /// 전투 한 판 동안만 존재하는 런타임 카드 더미.
    /// 뽑을 더미 / 손패 / 버린 더미를 관리한다.
    /// 플레이어가 "보유한" 카드 목록은 LSO_CardCollection이 담당하며, 이 클래스는 세이브 대상이 아니다.
    /// MonoBehaviour가 아니므로 로직만 따로 테스트할 수 있다.
    /// </summary>
    public sealed class LSO_Deck
    {
        private readonly List<LSO_CardSO> _drawPile = new();
        private readonly List<LSO_CardSO> _hand = new();
        private readonly List<LSO_CardSO> _discardPile = new();
        private readonly Random _random;

        /// <summary>더미 구성이 바뀔 때마다 발생한다. UI 갱신용.</summary>
        public event Action OnDeckChanged;

        public IReadOnlyList<LSO_CardSO> Hand => _hand;
        public IReadOnlyList<LSO_CardSO> DrawPile => _drawPile;
        public IReadOnlyList<LSO_CardSO> DiscardPile => _discardPile;

        public int DrawPileCount => _drawPile.Count;
        public int HandCount => _hand.Count;
        public int DiscardPileCount => _discardPile.Count;

        /// <summary>뽑을 더미와 버린 더미를 합친 수. 0이면 더 이상 뽑을 수 없다.</summary>
        public int RemainingCardCount => _drawPile.Count + _discardPile.Count;

        public LSO_Deck(IEnumerable<LSO_CardSO> cards, int? randomSeed = null)
        {
            _random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();

            if (cards == null) return;

            foreach (LSO_CardSO card in cards)
            {
                if (card != null && card.IsValid)
                    _drawPile.Add(card);
            }
        }

        /// <summary>뽑을 더미를 섞는다.</summary>
        public void Shuffle()
        {
            ShuffleInPlace(_drawPile);
            OnDeckChanged?.Invoke();
        }

        /// <summary>
        /// 한 장 뽑아 손패에 넣고 그 카드를 돌려준다.
        /// 뽑을 더미가 비면 버린 더미를 섞어 되채운다. 그래도 없으면 null.
        /// </summary>
        public LSO_CardSO Draw()
        {
            if (_drawPile.Count == 0)
                RefillFromDiscard();

            if (_drawPile.Count == 0)
                return null;

            int lastIndex = _drawPile.Count - 1;
            LSO_CardSO card = _drawPile[lastIndex];
            _drawPile.RemoveAt(lastIndex);
            _hand.Add(card);

            OnDeckChanged?.Invoke();
            return card;
        }

        /// <summary>여러 장 뽑는다. 실제로 뽑은 수를 돌려준다(더미가 모자라면 그만큼만).</summary>
        public int DrawMany(int count)
        {
            int drawn = 0;

            for (int i = 0; i < count; i++)
            {
                if (_drawPile.Count == 0)
                    RefillFromDiscard();

                if (_drawPile.Count == 0)
                    break;

                int lastIndex = _drawPile.Count - 1;
                _hand.Add(_drawPile[lastIndex]);
                _drawPile.RemoveAt(lastIndex);
                drawn++;
            }

            if (drawn > 0)
                OnDeckChanged?.Invoke();

            return drawn;
        }

        /// <summary>손패의 카드를 버린 더미로 보낸다. 카드를 실제로 냈을 때 호출.</summary>
        public bool Discard(LSO_CardSO card)
        {
            if (card == null || !_hand.Remove(card))
                return false;

            _discardPile.Add(card);
            OnDeckChanged?.Invoke();
            return true;
        }

        /// <summary>손패 전체를 버린다. 턴 종료 처리용.</summary>
        public void DiscardHand()
        {
            if (_hand.Count == 0) return;

            _discardPile.AddRange(_hand);
            _hand.Clear();
            OnDeckChanged?.Invoke();
        }

        /// <summary>버린 더미를 섞어 뽑을 더미로 되돌린다.</summary>
        private void RefillFromDiscard()
        {
            if (_discardPile.Count == 0) return;

            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            ShuffleInPlace(_drawPile);
        }

        /// <summary>
        /// 카드 셔플
        /// </summary>
        /// <param name="cards">섞을 덱</param>
        private void ShuffleInPlace(List<LSO_CardSO> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }
    }
}
