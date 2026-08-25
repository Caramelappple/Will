using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KTH_InfoPanel : MonoBehaviour
{
    public static KTH_InfoPanel Instance { get; private set; }

    [SerializeField] private GameObject infoPanl;
    [SerializeField] private RectTransform infoPanlRect;

    [Header("소환 연결")]
    [SerializeField] private LDY_CardPlacer cardPlacer;
    [SerializeField] private LDY_Team team = LDY_Team.Player;

    [Header("버린 카드 더미")]
    [SerializeField] private KTH_DiscardCardUI discardPile;

    [Header("아이콘 이미지")]
    [SerializeField] private Image icon;

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI willExplanationText;
    [SerializeField] private TextMeshProUGUI TraitExplanationText;

    [Header("스텟 텍스트")]
    [SerializeField] private TextMeshProUGUI attackStateText;
    [SerializeField] private TextMeshProUGUI hpStateText;
    [SerializeField] private TextMeshProUGUI costStateText;
    [SerializeField] private TextMeshProUGUI moveStateText;
    [SerializeField] private TextMeshProUGUI attackTypeStateText;

    [Header("애니메이션 설정")]
    [SerializeField] private float animDuration = 0.4f;
    [SerializeField] private float moveUpDistance = 100f;
    [SerializeField] private float rotateAngle = 360f;
    [SerializeField] private Ease animEase = Ease.OutBack;

    private LSO_CardSO cardData;
    private KTH_HandCard currentCard;

    private Vector2 originalPos;
    private Sequence currentSequence;
    private CanvasGroup canvasGroup;

    private bool isClosing;

    public LSO_CardSO CardData => cardData;
    public KTH_HandCard CurrentCard => currentCard;

    private void Awake()
    {
        Instance = this;

        originalPos = infoPanlRect.anchoredPosition;

        canvasGroup = infoPanl.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = infoPanl.AddComponent<CanvasGroup>();
        }

        infoPanl.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        currentSequence?.Kill();
    }

    // =========================================================
    // 일반 인포 패널 열기
    // 클릭 선택 등에서 사용
    // =========================================================

    public void StartInfoPanl(
        LSO_CardSO data,
        KTH_HandCard card = null)
    {
        if (data == null || !data.IsValid)
        {
            return;
        }

        if (isClosing)
        {
            currentSequence?.Kill();
            isClosing = false;
        }

        cardData = data;
        currentCard = card;

        SetPanl(data);
        PlayOpenAnimation();
    }

    // =========================================================
    // 호버 시작
    // 인포 패널 표시 + 즉시 배치 시작
    // =========================================================

    public void StartHoverPlacement(
        LSO_CardSO data,
        KTH_HandCard card)
    {
        if (data == null || !data.IsValid)
        {
            return;
        }

        if (card == null)
        {
            return;
        }

        if (cardPlacer == null)
        {
            Debug.LogWarning(
                "[KTH_InfoPanel] cardPlacer가 연결되어 있지 않습니다."
            );
            return;
        }

        // 다른 카드 배치가 남아 있다면 먼저 취소
        if (cardPlacer.IsPlacing)
        {
            cardPlacer.CancelPlacement();
        }

        if (isClosing)
        {
            currentSequence?.Kill();
            isClosing = false;
        }

        cardData = data;
        currentCard = card;

        SetPanl(data);
        PlayOpenAnimation();

        BeginCurrentCardPlacement();
    }

    // =========================================================
    // 배치 시작
    // =========================================================

    public void SelectInfoPanl()
    {
        BeginCurrentCardPlacement();
    }

    public void StartPlacementFromHover()
    {
        BeginCurrentCardPlacement();
    }

    private void BeginCurrentCardPlacement()
    {
        if (cardData == null || !cardData.IsValid)
        {
            return;
        }

        if (currentCard == null)
        {
            return;
        }

        if (cardPlacer == null)
        {
            Debug.LogWarning(
                "[KTH_InfoPanel] cardPlacer가 연결되어 있지 않습니다."
            );
            return;
        }

        // 이미 배치 중이면 중복 시작하지 않음
        if (cardPlacer.IsPlacing)
        {
            return;
        }

        LSO_CardSO cardToPlace = cardData;
        KTH_HandCard cardToRemove = currentCard;

        bool started = cardPlacer.BeginPlacement(
            cardToPlace,
            team,

            onPlaced: animal =>
            {
                if (KTH_HandCardLayout.Instance != null)
                {
                    KTH_HandCardLayout.Instance.MoveUpFromPlacement();
                }

                // 배치 실패
                if (animal == null)
                {
                    return;
                }

                // 다른 상태로 넘어간 경우 잘못된 카드 제거 방지
                if (cardToRemove == null)
                {
                    return;
                }

                // 현재 패널이 이 카드를 보고 있는 경우에만 초기화
                if (currentCard == cardToRemove)
                {
                    currentCard = null;
                    cardData = null;
                }

                // 패널 닫기
                if (infoPanl.activeSelf)
                {
                    PlayCloseAnimation();
                }

                // 사용한 카드 제거
                cardToRemove.ConsumeAndRearrange(discardPile);
            },

            onCancelled: () =>
            {
                if (KTH_HandCardLayout.Instance != null)
                {
                    KTH_HandCardLayout.Instance.MoveUpFromPlacement();
                }

                // 우클릭은 배치만 취소
                // 카드 호버 상태는 유지한다.
            }
        );

        if (!started)
        {
            Debug.LogWarning(
                "[KTH_InfoPanel] 소환 시작 실패 (턴이 아니거나 코스트 부족)."
            );

            return;
        }

        if (KTH_HandCardLayout.Instance != null)
        {
            KTH_HandCardLayout.Instance.MoveDownForPlacement();
        }
    }

    // =========================================================
    // 호버 이탈 취소
    // =========================================================

    public void CancelHoverSelection(KTH_HandCard card)
    {
        if (card == null)
        {
            return;
        }

        // 현재 이 카드가 아니면 건드리지 않음
        if (currentCard != card)
        {
            return;
        }

        if (cardPlacer != null && cardPlacer.IsPlacing)
        {
            cardPlacer.CancelPlacement();
        }

        if (KTH_HandCardLayout.Instance != null)
        {
            KTH_HandCardLayout.Instance.MoveUpFromPlacement();
        }

        card.CancelSelectionState();

        currentCard = null;
        cardData = null;

        if (infoPanl.activeSelf && !isClosing)
        {
            PlayCloseAnimation();
        }
    }

    // =========================================================
    // 패널 닫기
    // =========================================================

    public void CancleInfoPanl()
    {
        if (isClosing)
        {
            return;
        }

        if (cardPlacer != null && cardPlacer.IsPlacing)
        {
            cardPlacer.CancelPlacement();
        }

        if (currentCard != null)
        {
            currentCard.CancelSelectionState();
        }

        currentCard = null;
        cardData = null;

        if (!infoPanl.activeSelf)
        {
            return;
        }

        PlayCloseAnimation();
    }

    // =========================================================
    // Animation
    // =========================================================

    private void PlayOpenAnimation()
    {
        currentSequence?.Kill();

        infoPanl.SetActive(true);

        infoPanlRect.anchoredPosition =
            originalPos - new Vector2(0f, moveUpDistance);

        infoPanlRect.localScale = Vector3.zero;

        infoPanlRect.localEulerAngles =
            new Vector3(0f, rotateAngle, 0f);

        canvasGroup.alpha = 0f;

        currentSequence = DOTween.Sequence();

        currentSequence.Append(
            infoPanlRect
                .DOAnchorPos(originalPos, animDuration)
                .SetEase(animEase)
        );

        currentSequence.Join(
            DOVirtual
                .Float(
                    rotateAngle,
                    0f,
                    animDuration,
                    y =>
                    {
                        infoPanlRect.localEulerAngles =
                            new Vector3(0f, y, 0f);
                    }
                )
                .SetEase(Ease.OutQuad)
        );

        currentSequence.Join(
            infoPanlRect
                .DOScale(1f, animDuration)
                .SetEase(animEase)
        );

        currentSequence.Join(
            canvasGroup
                .DOFade(1f, animDuration * 0.7f)
        );
    }

    private void PlayCloseAnimation()
    {
        currentSequence?.Kill();

        isClosing = true;

        currentSequence = DOTween.Sequence();

        currentSequence.Join(
            infoPanlRect
                .DOAnchorPos(
                    originalPos - new Vector2(0f, moveUpDistance),
                    animDuration
                )
                .SetEase(Ease.InBack)
        );

        currentSequence.Join(
            infoPanlRect
                .DOScale(0f, animDuration)
                .SetEase(Ease.InBack)
        );

        currentSequence.Join(
            DOVirtual
                .Float(
                    infoPanlRect.localEulerAngles.y,
                    rotateAngle,
                    animDuration,
                    y =>
                    {
                        infoPanlRect.localEulerAngles =
                            new Vector3(0f, y, 0f);
                    }
                )
                .SetEase(Ease.InQuad)
        );

        currentSequence.Join(
            canvasGroup
                .DOFade(0f, animDuration * 0.7f)
        );

        currentSequence.OnComplete(() =>
        {
            infoPanl.SetActive(false);

            infoPanlRect.anchoredPosition = originalPos;
            infoPanlRect.localScale = Vector3.one;
            infoPanlRect.localEulerAngles = Vector3.zero;

            canvasGroup.alpha = 1f;
            isClosing = false;
        });
    }

    // =========================================================
    // UI
    // =========================================================

    private void SetPanl(LSO_CardSO data)
    {
        if (data == null || !data.IsValid)
        {
            return;
        }

        icon.sprite = data.Image;
        titleText.text = data.AnimalName;

        attackStateText.text = $"{data.Damage}";
        hpStateText.text = $"{data.MaxHealth}";
        costStateText.text = $"{data.Cost}";
        moveStateText.text = $"{data.Animal.MoveRange}";
        attackTypeStateText.text = $"{data.Range}";

        TraitExplanationText.text = data.Description;

        var abilityTypes = data.AbilityTypes;

        willExplanationText.text =
            abilityTypes.Count > 0
                ? GetAbilityExplanation(abilityTypes[0])
                : string.Empty;
    }

    private string GetAbilityExplanation(LSO_AbilityType type)
    {
        return type switch
        {
            _ => type.ToString()
        };
    }
}