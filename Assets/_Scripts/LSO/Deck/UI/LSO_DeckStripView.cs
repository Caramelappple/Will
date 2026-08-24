using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LSO.Deck.UI
{
    /// <summary>
    /// 아래쪽 "고른 카드" 줄. 칸을 직접 배치하지 않고 여기서 만들어 깐다.
    ///
    /// 칸 수를 손으로 맞출 필요가 없어서 최대 장수가 바뀌어도 숫자 하나만 고치면 된다.
    /// 빈 칸까지 미리 깔아두는 이유는 둘이다.
    /// 몇 장 더 넣을 수 있는지가 숫자 없이도 읽히고,
    /// 줄 길이가 변하지 않아 취소할 때 나머지 카드가 자리를 옮기지 않는다.
    ///
    /// 씬 배선: HorizontalLayoutGroup을 붙인 오브젝트를 layoutGroup에 넣을 것.
    ///          칸은 그 아래에 생긴다.
    /// </summary>
    public class LSO_DeckStripView : MonoBehaviour
    {
        [Header("칸")]
        [Tooltip("칸이 생길 곳. 붙어 있는 HorizontalLayoutGroup이 배치를 맡는다.")]
        [SerializeField] private HorizontalLayoutGroup layoutGroup;

        [SerializeField] private LSO_DeckSlotView slotPrefab;

        [Tooltip("깔아둘 칸 수. 덱 규칙의 최대 장수와 같게 둘 것.")]
        [SerializeField, Min(1)] private int slotCount = 8;

        [Header("표시")]
        [Tooltip("'3 / 8' 표시. 비워둬도 된다.")]
        [SerializeField] private TextMeshProUGUI countText;

        private readonly List<LSO_DeckSlotView> _slots = new();

        private bool _warnedCountMismatch;

        public event Action<int> OnSlotClicked;

        private void Awake()
        {
            BuildSlots();
        }

        private void OnDestroy()
        {
            ClearSlots();
        }

        public void Refresh(LSO_DeckDraft draft, LSO_CardPalette palette)
        {
            if (draft == null) return;

            BuildSlots();
            WarnIfCountMismatch(draft);

            IReadOnlyList<int> selected = draft.SelectedSlots;

            for (int i = 0; i < _slots.Count; i++)
            {
                LSO_DeckSlotView view = _slots[i];
                if (view == null) continue;

                if (i < selected.Count)
                {
                    int paletteSlot = selected[i];
                    view.SetCard(paletteSlot, palette?[paletteSlot]);
                }
                else
                {
                    view.SetEmpty();
                }
            }

            if (countText != null)
                countText.text = $"카드를 선택하세요 {draft.Count} / {draft.MaxCards}";
        }

        /// <summary>이미 깔려 있으면 아무 일도 하지 않는다. Awake와 Refresh 양쪽에서 불러도 안전하다.</summary>
        private void BuildSlots()
        {
            if (_slots.Count > 0) return;

            if (layoutGroup == null || slotPrefab == null)
            {
                Debug.LogError($"{name}: 덱 칸을 만들 수 없습니다. Layout Group과 칸 프리팹 연결을 확인하세요.", this);
                return;
            }

            for (int i = 0; i < slotCount; i++)
            {
                LSO_DeckSlotView view = Instantiate(slotPrefab, layoutGroup.transform);
                view.OnClicked += HandleClicked;

                _slots.Add(view);
            }
        }

        private void ClearSlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                LSO_DeckSlotView view = _slots[i];
                if (view == null) continue;

                view.OnClicked -= HandleClicked;
            }

            _slots.Clear();
        }

        // 칸 수와 최대 장수가 어긋나면 8장을 골라도 마지막 칸이 안 보이거나 빈 칸이 남는다.
        // 한 번만 알린다. 매 프레임 콘솔을 채우면 오히려 못 보고 지나친다.
        private void WarnIfCountMismatch(LSO_DeckDraft draft)
        {
            if (_warnedCountMismatch) return;
            if (_slots.Count >= draft.MaxCards) return;

            _warnedCountMismatch = true;

            Debug.LogWarning(
                $"{name}: 덱 칸이 {_slots.Count}개인데 최대 장수는 {draft.MaxCards}장입니다. " +
                $"Slot Count를 {draft.MaxCards}로 맞추세요.", this);
        }

        private void HandleClicked(int slot)
        {
            OnSlotClicked?.Invoke(slot);
        }
    }
}
