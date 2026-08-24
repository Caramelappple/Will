using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;

namespace _Scripts.LSO.Deck
{
    /// <summary>
    /// 고를 수 있는 카드 칸 목록.
    ///
    /// 보유한 카드 한 장이 칸 하나다. 같은 카드를 셋 갖고 있으면 칸도 셋이다.
    /// 종류별로 접지 않는 이유는 조작이 토글이기 때문이다.
    /// 칸마다 켜짐/꺼짐 두 상태뿐이라, 접어버리면 같은 카드를 두 장 넣을 방법이 없다.
    ///
    /// MonoBehaviour가 아니다. 씬 없이 만들 수 있어야 덱 규칙을 따로 시험해볼 수 있다.
    /// </summary>
    public sealed class LSO_CardPalette
    {
        private readonly List<LSO_CardSO> _slots = new();

        public int Count => _slots.Count;

        public LSO_CardSO this[int slot] => IsValidSlot(slot) ? _slots[slot] : null;

        public bool IsValidSlot(int slot) => slot >= 0 && slot < _slots.Count;

        /// <summary>
        /// 보유 목록으로 칸을 만든다. null은 걸러낸다.
        ///
        /// 넘긴 순서가 곧 칸 번호다. 이 순서가 바뀌면 이미 고른 칸이 엉뚱한 카드를 가리키므로,
        /// 덱을 짜는 동안에는 원본 목록을 건드리지 않아야 한다.
        /// </summary>
        public static LSO_CardPalette From(IEnumerable<LSO_CardSO> cards)
        {
            var palette = new LSO_CardPalette();
            if (cards == null) return palette;

            foreach (LSO_CardSO card in cards)
            {
                if (card == null) continue;

                palette._slots.Add(card);
            }

            return palette;
        }
    }
}
