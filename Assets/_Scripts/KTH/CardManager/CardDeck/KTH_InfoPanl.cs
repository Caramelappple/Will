using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KTH_InfoPanl : MonoBehaviour
{
    public static KTH_InfoPanl Instance { get; private set; }

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

    public LSO_CardSO CardData => cardData;

    private void Awake()
    {
        Instance = this;

        originalPos = infoPanlRect.anchoredPosition;
        infoPanl.SetActive(false);
    }

    public void StartInfoPanl(LSO_CardSO data, KTH_HandCard card = null)
    {
        cardData = data;
        currentCard = card;
        SetPanl(data);

        PlayOpenAnimation();
    }

    private void PlayOpenAnimation()
    {
        currentSequence?.Kill();

        infoPanl.SetActive(true);

        infoPanlRect.anchoredPosition = originalPos - new Vector2(0f, moveUpDistance);
        infoPanlRect.localScale = Vector3.zero;
        infoPanlRect.localEulerAngles = new Vector3(0f, rotateAngle, 0f);

        CanvasGroup cg = infoPanl.GetComponent<CanvasGroup>();
        if (cg == null) cg = infoPanl.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        currentSequence = DOTween.Sequence();
        currentSequence.Append(infoPanlRect.DOAnchorPos(originalPos, animDuration).SetEase(animEase));
        currentSequence.Join(
            DOVirtual.Float(rotateAngle, 0f, animDuration, y =>
            {
                infoPanlRect.localEulerAngles = new Vector3(0f, y, 0f);
            }).SetEase(Ease.OutQuad)
        );
        currentSequence.Join(infoPanlRect.DOScale(1f, animDuration).SetEase(animEase));
        currentSequence.Join(cg.DOFade(1f, animDuration * 0.7f));
    }

    private void SetPanl(LSO_CardSO data)
    {
        if (!data.IsValid)
        {
            Debug.LogWarning("[KTH_InfoPanl] 유효하지 않은 카드 데이터입니다.");
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
        willExplanationText.text = abilityTypes.Count > 0
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

    public void CancleInfoPanl()
    {
        currentSequence?.Kill();

        infoPanl.SetActive(false);

        if (currentCard != null)
        {
            currentCard.SetSelected(false);
            currentCard = null;
        }
    }

    /// <summary>
    /// 정보패널의 소환(셀렉트) 버튼을 눌렀을 때 호출됨.
    /// 실제로 배치 모드가 시작될 때만 핸드 컨테이너가 내려감.
    /// </summary>
    public void SelectInfoPanl()
    {
        if (cardData == null || !cardData.IsValid)
        {
            Debug.LogWarning("[KTH_InfoPanl] 유효하지 않은 카드라 소환할 수 없습니다.");
            return;
        }

        if (cardPlacer == null)
        {
            Debug.LogWarning("[KTH_InfoPanl] cardPlacer가 연결되어 있지 않습니다.");
            return;
        }

        LSO_CardSO cardToPlace = cardData;
        KTH_HandCard cardToRemove = currentCard;

        currentSequence?.Kill();
        infoPanl.SetActive(false);

        if (cardToRemove != null)
            cardToRemove.SetSelected(false);

        currentCard = null;
        cardData = null;

        bool started = cardPlacer.BeginPlacement(
            cardToPlace,
            team,
            onPlaced: animal =>
            {
                // 배치 완료 → 핸드 컨테이너 원위치로 복귀
                if (KTH_HandCardLayout.Instance != null)
                    KTH_HandCardLayout.Instance.MoveUpFromPlacement();

                if (animal != null && cardToRemove != null)
                {
                    cardToRemove.ConsumeAndRearrange(discardPile);
                }
            },
            onCancelled: () =>
            {
                // 배치 취소 → 핸드 컨테이너 원위치로 복귀
                if (KTH_HandCardLayout.Instance != null)
                    KTH_HandCardLayout.Instance.MoveUpFromPlacement();
            });

        if (started)
        {
            // 배치 모드 시작 성공 → 이때만 핸드 컨테이너를 아래로 내림
            if (KTH_HandCardLayout.Instance != null)
                KTH_HandCardLayout.Instance.MoveDownForPlacement();
        }
        else
        {
            Debug.LogWarning("[KTH_InfoPanl] 소환 시작 실패 (턴이 아니거나 코스트 부족).");
        }
    }
}