using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.Deck.UI
{
    /// <summary>
    /// 위쪽 도감 목록. 스크롤 Content에 카드 칸을 채운다.
    ///
    /// 고른 카드도 목록에서 빼지 않고 체크 표시만 켠다.
    /// 빼버리면 스크롤이 튀고, 방금 누른 자리에 다른 카드가 올라와서 취소하기가 어려워진다.
    ///
    /// 씬 배선: Content에 GridLayoutGroup과 ContentSizeFitter(Vertical: Preferred)를 붙이고
    ///          GridLayoutGroup의 Constraint를 Fixed Column Count로 둘 것.
    ///          Flexible로 두면 가로로 늘어나 세로로 쌓이지 않는다.
    /// </summary>
    public class LSO_PaletteScrollView : MonoBehaviour
    {
        [SerializeField] private LSO_PaletteCardView cardPrefab;

        [Tooltip("ScrollRect의 Content. 카드가 이 아래에 생긴다.")]
        [SerializeField] private Transform content;

        private readonly List<LSO_PaletteCardView> _views = new();

        public event Action<int> OnSlotClicked;

        /// <summary>도감 칸을 처음부터 다시 만든다. 보상으로 카드가 늘었을 때만 부르면 된다.</summary>
        public void Build(LSO_CardPalette palette)
        {
            ClearViews();

            // 무엇이 빠졌는지 하나씩 짚어준다.
            // 한 줄로 묶어 알리면 셋 중 어느 것이 문제인지 인스펙터를 다 뒤져야 한다.
            if (cardPrefab == null)
            {
                Debug.LogError($"{name}: Card Prefab이 비어 있습니다.", this);
                return;
            }

            if (content == null)
            {
                Debug.LogError($"{name}: Content가 비어 있습니다. ScrollRect의 Content를 넣으세요.", this);
                return;
            }

            if (palette == null || palette.Count == 0)
            {
                Debug.LogWarning(
                    $"{name}: 고를 수 있는 카드가 없습니다. " +
                    $"ItemLibraryManager의 Unlocked Pieces에 카드가 들어 있는지 확인하세요.", this);
                return;
            }

            for (int slot = 0; slot < palette.Count; slot++)
            {
                LSO_PaletteCardView view = Instantiate(cardPrefab, content);
                view.Bind(slot, palette[slot]);
                view.OnClicked += HandleClicked;

                _views.Add(view);
            }
        }

        /// <summary>
        /// 특정 칸의 뷰를 찾는다. 연출을 그 칸 하나에만 걸 때 쓴다.
        /// 없으면 null.
        /// </summary>
        public LSO_PaletteCardView Find(int slot)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                LSO_PaletteCardView view = _views[i];
                if (view != null && view.Slot == slot) return view;
            }

            return null;
        }

        /// <summary>체크 표시와 누를 수 있는지를 지금 덱 상태에 맞춘다.</summary>
        public void Refresh(LSO_DeckDraft draft)
        {
            if (draft == null) return;

            for (int i = 0; i < _views.Count; i++)
            {
                LSO_PaletteCardView view = _views[i];
                if (view == null) continue;

                bool selected = draft.IsSelected(view.Slot);

                view.SetSelected(selected);

                // 이미 고른 칸은 덱이 차 있어도 멀쩡해야 한다. 그것마저 막으면 취소할 방법이 없다.
                view.SetBlocked(!selected && draft.IsFull);
            }
        }

        private void OnDestroy()
        {
            ClearViews();
        }

        private void ClearViews()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                LSO_PaletteCardView view = _views[i];
                if (view == null) continue;

                view.OnClicked -= HandleClicked;
                Destroy(view.gameObject);
            }

            _views.Clear();
        }

        private void HandleClicked(int slot)
        {
            OnSlotClicked?.Invoke(slot);
        }
    }
}
