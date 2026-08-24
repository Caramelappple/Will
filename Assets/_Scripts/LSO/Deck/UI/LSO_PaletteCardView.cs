using System;
using _Scripts.LSO.Deck.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LSO.Deck.UI
{
    /// <summary>
    /// 도감 칸 하나. 누르면 자기 번호를 알린다.
    ///
    /// 이 칸은 자기가 선택됐는지 스스로 정하지 않는다. 눌렸다고 알리기만 하고,
    /// 실제로 켜졌는지는 덱 쪽 판정을 거친 뒤 SetSelected로 되돌아온다.
    /// 8장이 차서 거절될 수 있기 때문에 스스로 켜면 화면과 데이터가 어긋난다.
    ///
    /// 드래그를 받지 않는다. ScrollRect 안에서 드래그를 가로채면 스크롤이 막힌다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LSO_PaletteCardView : MonoBehaviour
    {
        [SerializeField] private Button button;

        [Tooltip("카드 일러스트.")]
        [SerializeField] private Image cardImage;

        [Tooltip("고른 칸에 켜지는 표시. 체크나 테두리.")]
        [SerializeField] private GameObject selectedMark;

        [Header("고를 수 없을 때")]
        [Tooltip("덱이 다 차서 더 못 넣을 때 덧씌울 것. 어두운 판 같은 것. 비워둬도 된다.")]
        [SerializeField] private GameObject blockedMark;

        [Tooltip("덱이 다 찼을 때 카드를 죽일 색. 흰색이면 원래 색 그대로다.")]
        [SerializeField] private Color blockedTint = new(0.45f, 0.45f, 0.45f, 1f);

        [Header("선택")]
        [Tooltip("비워두면 표시하지 않는다.")]
        [SerializeField] private TextMeshProUGUI cardName;

        [SerializeField] private TextMeshProUGUI cost;

        /// <summary>도감 목록에서의 자리. 덱은 이 번호로 켜고 끈다.</summary>
        public int Slot { get; private set; } = -1;

        public LSO_CardSO Card { get; private set; }

        public event Action<int> OnClicked;

        private bool _selected;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>어떤 카드의 몇 번 칸인지 정한다. 목록을 채울 때 부른다.</summary>
        public void Bind(int slot, LSO_CardSO card)
        {
            Slot = slot;
            Card = card;

            if (cardImage != null)
                cardImage.sprite = card != null ? card.Image : null;

            SetText(cardName, card != null ? card.AnimalName : string.Empty);
            SetText(cost, card != null ? card.Cost.ToString() : string.Empty);

            SetSelected(false);
            SetBlocked(false);
        }

        /// <summary>
        /// 실제로 값이 바뀔 때만 발행한다.
        ///
        /// Refresh는 목록 전체를 훑으므로 클릭 한 번에 모든 칸이 이 메서드를 지나간다.
        /// 바뀌지 않은 칸까지 연출이 걸리면 화면 전체가 매번 들썩인다.
        /// </summary>
        public event Action<bool> OnSelectionChanged;

        public void SetSelected(bool selected)
        {
            bool changed = _selected != selected;
            _selected = selected;

            if (selectedMark != null)
                selectedMark.SetActive(selected);

            if (changed)
                OnSelectionChanged?.Invoke(selected);
        }

        /// <summary>
        /// 덱이 다 차서 더 못 고르는 상태를 표시한다.
        ///
        /// 버튼은 살려둔다. interactable을 끄면 Unity가 클릭을 아예 넘기지 않아
        /// 거절 자체가 일어나지 않고, 왜 안 되는지 알려줄 기회도 사라진다.
        /// 누를 수는 있되 들어가지 않고 안내가 뜨는 쪽이 낫다.
        /// </summary>
        public void SetBlocked(bool blocked)
        {
            if (blockedMark != null)
                blockedMark.SetActive(blocked);

            if (cardImage != null)
                cardImage.color = blocked ? blockedTint : Color.white;
        }

        /// <summary>
        /// 눌렸지만 덱에 못 들어갔을 때. 흔들림이나 빨간 반짝임을 여기에 건다.
        /// 거절 사유가 함께 오므로 이유에 따라 다른 연출을 걸 수도 있다.
        /// </summary>
        public event Action<LSO_DeckValidation> OnRejected;

        public void PlayReject(LSO_DeckValidation result)
        {
            OnRejected?.Invoke(result);
        }

        private void HandleClick()
        {
            if (Slot < 0) return;

            OnClicked?.Invoke(Slot);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target == null) return;
            if (target.text == value) return;

            target.text = value;
        }
    }
}
