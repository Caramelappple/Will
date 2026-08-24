using System;
using _Scripts.LSO.Deck.Data;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LSO.Deck.UI
{
    /// <summary>
    /// 고른 카드가 놓이는 칸 하나. 비어 있을 수도 있다.
    ///
    /// 자기가 어느 도감 칸에서 왔는지 들고 있다가, 눌리면 그 번호를 그대로 알린다.
    /// 도감에서 눌렀을 때와 같은 Toggle을 타야 취소가 한 경로로 모인다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LSO_DeckSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;

        [Tooltip("카드 일러스트. 빈 칸일 때는 꺼진다.")]
        [SerializeField] private Image cardImage;

        [Tooltip("빈 칸에 보여줄 표시. 비워둬도 된다.")]
        [SerializeField] private GameObject emptyMark;

        /// <summary>이 칸에 놓인 카드의 도감 번호. 비어 있으면 -1.</summary>
        public int Slot { get; private set; } = -1;

        public bool IsEmpty => Slot < 0;

        public event Action<int> OnClicked;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            SetEmpty();
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

        public void SetCard(int slot, LSO_CardSO card)
        {
            Slot = slot;

            if (cardImage != null)
            {
                cardImage.sprite = card != null ? card.Image : null;
                cardImage.enabled = card != null;
            }

            if (emptyMark != null)
                emptyMark.SetActive(false);

            if (button != null)
                button.interactable = true;
        }

        public void SetEmpty()
        {
            Slot = -1;

            if (cardImage != null)
            {
                cardImage.sprite = null;
                cardImage.enabled = false;
            }

            if (emptyMark != null)
                emptyMark.SetActive(true);

            // 빈 칸을 눌러도 할 일이 없다. 눌리는 느낌만 주면 오조작으로 읽힌다.
            if (button != null)
                button.interactable = false;
        }

        private void HandleClick()
        {
            if (IsEmpty) return;

            OnClicked?.Invoke(Slot);
        }
    }
}
