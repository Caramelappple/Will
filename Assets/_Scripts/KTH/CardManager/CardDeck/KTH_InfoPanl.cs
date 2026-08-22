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

        if (infoPanlRect == null)
            infoPanlRect = infoPanl.GetComponent<RectTransform>();

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

        // 정보패널을 먼저 닫는다 (보드 칸을 클릭해야 하므로 패널이 화면을 가리면 안 됨)
        currentSequence?.Kill();
        infoPanl.SetActive(false);

        // 선택 상태를 풀어서 카드가 손패 자리로 다시 내려가게 함 (아직 소환 확정 아님, 배치 대기 중)
        if (cardToRemove != null)
            cardToRemove.SetSelected(false);

        currentCard = null;
        cardData = null;

        // 보드 칸을 클릭할 때까지 대기 → 클릭하면 그 자리에 소환됨
        bool started = cardPlacer.BeginPlacement(
            cardToPlace,
            team,
            onPlaced: animal =>
            {
                if (animal != null && cardToRemove != null)
                {
                    // 소환 완료 시점에만 카드 제거 + 재정렬 + 버린카드 더미로 날아가는 연출
                    cardToRemove.ConsumeAndRearrange(discardPile);
                }
            },
            onCancelled: () =>
            {
                // 우클릭 등으로 취소하면 카드는 이미 손패 자리로 돌아가 있으므로 추가 처리 불필요
            });

        if (!started)
        {
            Debug.LogWarning("[KTH_InfoPanl] 소환 시작 실패 (턴이 아니거나 코스트 부족).");
        }
    }
}