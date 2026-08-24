using System;
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;

namespace _Scripts.LSO.Deck
{
    /// <summary>
    /// 편집 중인 덱. 켜진 도감 칸 번호를 들고 있는다.
    ///
    /// 카드 목록이 아니라 칸 번호인 이유는 화면 때문이다.
    /// 곰 셋 중 둘을 골랐을 때 목록이 [곰, 곰]이면 어느 칸에 체크를 그릴지 알 수 없다.
    ///
    /// 확정하는 순간에만 ToCards()로 펼친다. 그때부터는 LSO_RunDeck이 정본을 맡는다.
    /// </summary>
    public sealed class LSO_DeckDraft
    {
        private readonly LSO_CardPalette _palette;
        private readonly LSO_DeckRulesSO _rules;

        // 도감 순서를 유지한다. 아래 덱 칸이 이 순서로 그려지므로,
        // 하나를 취소해도 나머지가 자리를 옮기지 않는다.
        private readonly List<int> _selected = new();

        public LSO_DeckDraft(LSO_CardPalette palette, LSO_DeckRulesSO rules)
        {
            _palette = palette;
            _rules = rules;
        }

        public int Count => _selected.Count;

        public int MaxCards => _rules != null ? _rules.MaxCards : 8;

        public int MinCards => _rules != null ? _rules.MinCards : 1;

        public bool IsFull => Count >= MaxCards;

        /// <summary>고른 칸 번호. 도감 순서대로 정렬돼 있다.</summary>
        public IReadOnlyList<int> SelectedSlots => _selected;

        /// <summary>무엇이든 바뀌었을 때. 화면을 다시 그리는 용도다.</summary>
        public event Action OnChanged;

        /// <summary>
        /// 아래 셋은 연출용이다. OnChanged가 "다시 그려라"라면 이쪽은 "무슨 일이 있었나"다.
        ///
        /// 다시 그리기와 연출을 나눈 이유는, Redraw가 목록 전체를 훑는 반면
        /// 연출은 방금 눌린 칸 하나에만 걸려야 하기 때문이다.
        /// </summary>
        public event Action<int> OnSlotAdded;

        public event Action<int> OnSlotRemoved;

        /// <summary>거절됐을 때. 흔들림이나 빨간 반짝임을 여기에 건다.</summary>
        public event Action<int, LSO_DeckValidation> OnRejected;

        public bool IsSelected(int slot) => _selected.Contains(slot);

        /// <summary>
        /// 켜져 있으면 끄고, 꺼져 있으면 켠다.
        ///
        /// 끄는 것은 언제나 성공한다. 8장을 채운 뒤에도 취소는 되어야 하기 때문이다.
        /// </summary>
        public LSO_DeckValidation Toggle(int slot)
        {
            if (_palette == null || !_palette.IsValidSlot(slot))
                return Reject(slot, LSO_DeckRejectReason.InvalidSlot);

            int index = _selected.IndexOf(slot);
            if (index >= 0)
            {
                _selected.RemoveAt(index);

                // 연출을 먼저 알린다. OnChanged가 화면을 다시 그리면서
                // 방금 눌린 칸의 참조가 갈릴 수 있기 때문이다.
                OnSlotRemoved?.Invoke(slot);
                OnChanged?.Invoke();

                return LSO_DeckValidation.Ok();
            }

            if (IsFull)
                return Reject(slot, LSO_DeckRejectReason.DeckFull, MaxCards);

            InsertSorted(slot);

            OnSlotAdded?.Invoke(slot);
            OnChanged?.Invoke();

            return LSO_DeckValidation.Ok();
        }

        private LSO_DeckValidation Reject(int slot, LSO_DeckRejectReason reason, int value = 0)
        {
            LSO_DeckValidation result = LSO_DeckValidation.Fail(reason, value);

            OnRejected?.Invoke(slot, result);

            return result;
        }

        public void Clear()
        {
            if (_selected.Count == 0) return;

            _selected.Clear();
            OnChanged?.Invoke();
        }

        /// <summary>이어하기처럼 이미 정해진 덱에서 시작할 때 쓴다.</summary>
        public void SelectAll(IEnumerable<int> slots)
        {
            _selected.Clear();

            if (slots != null)
            {
                foreach (int slot in slots)
                {
                    if (_palette == null || !_palette.IsValidSlot(slot)) continue;
                    if (_selected.Contains(slot)) continue;
                    if (IsFull) break;

                    InsertSorted(slot);
                }
            }

            OnChanged?.Invoke();
        }

        public LSO_DeckValidation ValidateForConfirm()
        {
            if (Count < MinCards)
                return LSO_DeckValidation.Fail(LSO_DeckRejectReason.TooFewCards, MinCards);

            return LSO_DeckValidation.Ok();
        }

        /// <summary>고른 칸을 카드 목록으로 펼친다. 같은 카드가 여러 번 들어갈 수 있다.</summary>
        public List<LSO_CardSO> ToCards()
        {
            var cards = new List<LSO_CardSO>(_selected.Count);

            for (int i = 0; i < _selected.Count; i++)
            {
                LSO_CardSO card = _palette?[_selected[i]];
                if (card == null) continue;

                cards.Add(card);
            }

            return cards;
        }

        private void InsertSorted(int slot)
        {
            int index = _selected.BinarySearch(slot);
            if (index < 0) index = ~index;

            _selected.Insert(index, slot);
        }
    }
}
