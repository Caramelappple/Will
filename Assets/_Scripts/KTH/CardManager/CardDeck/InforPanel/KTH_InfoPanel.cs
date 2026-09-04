using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;

// =========================================================
// Composition Root: 인포 패널 기능을 조립하는 오케스트레이터.
//
// 애니메이션(KTH_PanelAnimationSettings.cs) / 타이핑 연출(KTH_TypewriterEffect.cs) /
// 카메라 줌(KTH_CameraZoomSettings.cs) / 어빌리티 설명(KTH_AbilityExplanationEntry.cs) /
// 카드 배치(KTH_CardPlacementController.cs)는 전부 같은 폴더의 별도 파일에서
// 인터페이스로 분리되어 있다. 이 클래스는 그것들을 인터페이스로만 참조해서
// 연결하고(DIP), 스탯/설명을 화면에 표시하는 "뷰" 역할은 직접 겸한다
// (뷰는 필드 몇 개를 채우는 게 전부라 별도 컴포넌트로 뺄 실익이 적다).
//
// 인스펙터에 이미 연결해둔 값을 잃지 않도록 필드 이름은 기존과 동일하게 유지했다.
//
// 기본 세팅: infoPanlRect / cardPlacer 를 인스펙터에서 비워둬도
// Awake에서 스스로 찾아 채운다.
// =========================================================
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

    [Header("클릭 시 가까이 보기 (기획: 인포 창을 누르면 가까이 가져온다)")]
    [SerializeField] private Vector2 closeUpAnchoredPos = new Vector2(0f, 0f);
    [SerializeField] private float closeUpScale = 1.3f;
    [SerializeField] private float closeUpDuration = 0.3f;
    [SerializeField] private Ease closeUpEase = Ease.OutQuad;

    [Header("벅샷 룰렛 스타일 - 낚아채기 전 움츠림(anticipation)")]
    [SerializeField] private float anticipationDistance = 12f;
    [SerializeField] private float anticipationDuration = 0.08f;

    [Header("벅샷 룰렛 스타일 - 정착할 때 손맛(오버슈트)")]
    [SerializeField] private float overshootAmount = 1.3f;

    [Header("벅샷 룰렛 스타일 - 들고 보는 동안 손떨림(idle sway)")]
    [SerializeField] private float swayAngle = 1.5f;
    [SerializeField] private float swayPositionAmount = 4f;
    [SerializeField] private float swayDuration = 1.4f;

    [Header("만년필 업데이트 연출 (기획: 만년필로 인포 창을 업데이트)")]
    [SerializeField] private float secondsPerChar = 0.03f;
    [SerializeField] private float penZoomHoldDuration = 0.3f;

    [Header("카메라 확대 연출 (기획: 양피지에 내용이 적힐 때 카메라 확대)")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float zoomedOrthoSize = 3f;
    [SerializeField] private float zoomedFieldOfView = 40f;
    [SerializeField] private float cameraZoomDuration = 0.4f;
    [SerializeField] private Ease cameraZoomEase = Ease.OutQuad;

    [Header("유언(어빌리티) 설명 - 비워두면 enum 이름을 그대로 표시")]
    [SerializeField] private KTH_AbilityExplanationEntry[] abilityExplanations;

    private IInfoPanelAnimator animator;
    private ITypewriterEffect traitTypewriter;
    private ITypewriterEffect willTypewriter;
    private ICameraZoomService cameraZoomService;
    private IAbilityExplanationProvider abilityExplanationProvider;
    private ICardPlacementController cardPlacementController;

    private Sequence penWriteSequence;
    private LSO_CardSO cardData;
    private KTH_HandCard currentCard;
    private bool isPenModeActive;

    public LSO_CardSO CardData => cardData;
    public KTH_HandCard CurrentCard => currentCard;
    public bool IsPenModeActive => isPenModeActive;

    private void Awake()
    {
        Instance = this;
        EnsureDefaultSetup();
        ComposeDependencies();
        infoPanl.SetActive(false);
    }

    // 기본 세팅: 인스펙터에서 안 채운 참조 중, 로컬에서 바로 구할 수 있는 것만
    // 스스로 채운다. FindObjectOfType처럼 씬 전체를 뒤지는 탐색은 아무리
    // Awake 1회뿐이라도 쓰지 않는다 — 대신 인스펙터에 꼭 연결해야 하고,
    // 안 되어 있으면 바로 알아챌 수 있도록 경고만 남긴다.
    private void EnsureDefaultSetup()
    {
        if (infoPanlRect == null && infoPanl != null)
        {
            infoPanlRect = infoPanl.GetComponent<RectTransform>();
        }
        if (cardPlacer == null)
        {
            Debug.LogWarning(
                "[KTH_InfoPanel] cardPlacer가 인스펙터에 연결되어 있지 않습니다. 직접 연결해주세요."
            );
        }
        EnsureClickHandling();
    }

    // 기본 세팅: "인포 창을 누르면 가까이 가져온다"를 쓰기 위해 Button이나
    // EventTrigger를 인스펙터에서 손수 연결할 필요가 없도록, infoPanl에
    // PointerClick을 받는 EventTrigger를 스스로 붙이고 OnClickInfoPanel()을
    // 등록해둔다. infoPanl에 Raycast Target이 켜진 Graphic(보통 배경 Image)이
    // 있어야 클릭이 감지된다.
    private void EnsureClickHandling()
    {
        if (infoPanl == null)
        {
            return;
        }
        if (infoPanl.GetComponent<Graphic>() == null)
        {
            Debug.LogWarning(
                "[KTH_InfoPanel] infoPanl에 클릭을 받을 Graphic(Image 등, Raycast Target 켜짐)이 " +
                "없어서 '가까이 보기' 클릭이 감지되지 않습니다. 배경 Image를 추가해주세요."
            );
        }
        EventTrigger trigger = infoPanl.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = infoPanl.AddComponent<EventTrigger>();
        }
        bool alreadyWired = trigger.triggers.Exists(
            entry => entry.eventID == EventTriggerType.PointerClick
        );
        if (alreadyWired)
        {
            return;
        }
        var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        clickEntry.callback.AddListener(_ => OnClickInfoPanel());
        trigger.triggers.Add(clickEntry);
    }

    private void ComposeDependencies()
    {
        CanvasGroup canvasGroup = infoPanl.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = infoPanl.AddComponent<CanvasGroup>();
        }

        var animationSettings = new KTH_PanelAnimationSettings
        {
            animDuration = animDuration,
            moveUpDistance = moveUpDistance,
            rotateAngle = rotateAngle,
            animEase = animEase,
            closeUpAnchoredPos = closeUpAnchoredPos,
            closeUpScale = closeUpScale,
            closeUpDuration = closeUpDuration,
            closeUpEase = closeUpEase,
            anticipationDistance = anticipationDistance,
            anticipationDuration = anticipationDuration,
            overshootAmount = overshootAmount,
            swayAngle = swayAngle,
            swayPositionAmount = swayPositionAmount,
            swayDuration = swayDuration
        };
        var zoomSettings = new KTH_CameraZoomSettings
        {
            zoomedOrthoSize = zoomedOrthoSize,
            zoomedFieldOfView = zoomedFieldOfView,
            duration = cameraZoomDuration,
            ease = cameraZoomEase
        };

        animator = new KTH_InfoPanelAnimator(infoPanlRect, canvasGroup, animationSettings);
        traitTypewriter = new KTH_TypewriterEffect();
        willTypewriter = new KTH_TypewriterEffect();
        cameraZoomService = new KTH_CameraZoomService(targetCamera, zoomSettings);
        abilityExplanationProvider = new KTH_AbilityExplanationProvider(abilityExplanations);
        cardPlacementController = new KTH_CardPlacementController(cardPlacer);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        penWriteSequence?.Kill();
        animator?.Dispose();
        traitTypewriter?.Dispose();
        willTypewriter?.Dispose();
        cameraZoomService?.Dispose();
    }

    // =========================================================
    // 일반 인포 패널 열기
    // =========================================================
    public void StartInfoPanl(
        LSO_CardSO data,
        KTH_HandCard card = null)
    {
        if (data == null || !data.IsValid)
        {
            return;
        }
        cardData = data;
        currentCard = card;
        ShowStats(data);
        string traitText = data.Description ?? string.Empty;
        string willText = ResolveWillText(data);
        // 기획: 만년필을 누르고 기물을 선택하면 양피지에 내용이 한 글자씩 적힌다
        if (isPenModeActive)
        {
            PlayPenWriteAnimation(traitText, willText);
        }
        else
        {
            StopPenWriteAnimation();
            ShowDescriptionInstant(traitText, willText);
        }
        animator.Open(infoPanl);
    }

    // =========================================================
    // 호버 시작
    //
    // 인포 패널만 표시한다.
    // 기물 배치는 시작하지 않는다.
    // =========================================================
    public void StartHoverInfo(
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
        cardData = data;
        currentCard = card;
        ShowStats(data);
        StopPenWriteAnimation();
        ShowDescriptionInstant(data.Description ?? string.Empty, ResolveWillText(data));
        animator.Open(infoPanl);
    }

    // =========================================================
    // 클릭 후 실제 배치 시작
    // =========================================================
    public void SelectInfoPanl()
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
        KTH_HandCard cardToRemove = currentCard;
        bool started = cardPlacementController.TryBeginPlacement(
            cardData,
            team,
            onPlacedSuccessfully: () =>
            {
                if (cardToRemove == null)
                {
                    return;
                }
                // 현재 패널이 이 카드를 보고 있을 때만 초기화
                if (currentCard == cardToRemove)
                {
                    currentCard = null;
                    cardData = null;
                }
                // 패널 닫기
                if (infoPanl.activeSelf)
                {
                    ClosePanel();
                }
                // 사용한 카드 제거
                cardToRemove.ConsumeAndRearrange(discardPile);
            },
            onPlacementFailed: null,
            // 우클릭 시 배치만 취소. 카드 선택과 인포 패널은 그대로 유지
            onCancelled: null
        );
        if (!started)
        {
            Debug.LogWarning(
                "[KTH_InfoPanel] 소환 시작 실패 (턴이 아니거나 코스트 부족)."
            );
        }
    }

    // =========================================================
    // 호버 이탈
    // =========================================================
    public void CancelHoverSelection(KTH_HandCard card)
    {
        if (card == null)
        {
            return;
        }
        // 현재 패널이 이 카드가 아니면 무시
        if (currentCard != card)
        {
            return;
        }
        // 클릭 확정 상태면 호버 이탈로 취소하지 않음
        if (card.IsConfirmed)
        {
            return;
        }
        KTH_HandCardLayout.Instance?.MoveUpFromPlacement();
        currentCard = null;
        cardData = null;
        if (infoPanl.activeSelf && !animator.IsClosing)
        {
            ClosePanel();
        }
    }

    // =========================================================
    // 패널 닫기
    // =========================================================
    public void CancleInfoPanl()
    {
        if (animator.IsClosing)
        {
            return;
        }
        cardPlacementController.CancelPlacement();
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
        ClosePanel();
    }

    // =========================================================
    // 만년필 모드 (기획: 만년필로 인포 창을 업데이트 할 수 있다)
    //
    // 만년필 버튼 등 외부 UI에서 호출하여 On/Off 한다.
    // 만년필 모드가 켜진 상태에서 기물을 선택(StartInfoPanl)하면
    // 양피지 내용이 한 글자씩 타이핑되며 카메라가 확대된다.
    // =========================================================
    public void SetPenModeActive(bool active)
    {
        isPenModeActive = active;
    }

    public void TogglePenMode()
    {
        isPenModeActive = !isPenModeActive;
    }

    // =========================================================
    // 인포 창 클릭 (기획: 인포 창을 누르면 인포 창을 가까이 가져온다)
    //
    // EnsureClickHandling()이 Awake에서 infoPanl에 EventTrigger를 자동으로
    // 붙이고 이 메서드를 PointerClick에 등록해두므로, 인스펙터에서 따로
    // Button/EventTrigger를 연결할 필요는 없다.
    // =========================================================
    public void OnClickInfoPanel()
    {
        animator.ToggleCloseUp();
        // 확대돼서 화면을 덮고 있는 동안은 그 뒤 보드 칸이 같이 클릭되어
        // 배치가 일어나지 않도록 막고, 원위치로 돌아가면 다시 풀어준다.
        cardPlacementController.SetBoardBlocked(animator.IsBroughtCloser);
    }

    // =========================================================
    // 뷰 - 스탯/설명 표시 (인포패널이 인포뷰 역할도 겸함)
    // =========================================================
    private void ShowStats(LSO_CardSO data)
    {
        // 연산량 최적화: 문자열 보간($"") 대신 ToString() 사용 (GC 할당 소폭 감소)
        icon.sprite = data.Image;
        titleText.text = data.AnimalName;
        attackStateText.text = data.Damage.ToString();
        hpStateText.text = data.MaxHealth.ToString();
        costStateText.text = data.Cost.ToString();
        moveStateText.text = data.Animal.MoveRange.ToString();
        attackTypeStateText.text = data.Range.ToString();
    }

    private void ShowDescriptionInstant(string traitText, string willText)
    {
        TraitExplanationText.text = traitText ?? string.Empty;
        willExplanationText.text = willText ?? string.Empty;
    }

    private void ClearDescriptionText()
    {
        TraitExplanationText.text = string.Empty;
        willExplanationText.text = string.Empty;
    }

    // =========================================================
    // 내부 헬퍼
    // =========================================================
    private string ResolveWillText(LSO_CardSO data)
    {
        var abilityTypes = data.AbilityTypes;
        return abilityTypes.Count > 0
            ? abilityExplanationProvider.GetExplanation(abilityTypes[0])
            : string.Empty;
    }

    private void PlayPenWriteAnimation(string traitText, string willText)
    {
        StopPenWriteAnimation();
        ClearDescriptionText();
        // 이건 UI(RectTransform)라서 카메라 확대(ortho size / FOV)는 캔버스가
        // Screen Space - Overlay면 화면에 아무 영향을 못 준다. 그래서 실제로
        // 눈에 보이는 "확대" 연출은 패널 자체를 당겨오는 애니메이터가 담당하고,
        // 카메라 줌은 3D 보드까지 함께 확대하고 싶을 때를 위해 같이 걸어둔다.
        animator.BringCloser();
        // 만년필로 쓰는 동안도 확대된 상태이므로 뒤 보드가 같이 클릭되지 않게 막는다.
        cardPlacementController.SetBoardBlocked(true);
        cameraZoomService.ZoomIn();

        Tween traitTween = traitTypewriter.Play(TraitExplanationText, traitText, secondsPerChar);
        Tween willTween = string.IsNullOrEmpty(willText)
            ? null
            : willTypewriter.Play(willExplanationText, willText, secondsPerChar);

        penWriteSequence = DOTween.Sequence();
        if (traitTween != null)
        {
            penWriteSequence.Join(traitTween);
        }
        if (willTween != null)
        {
            penWriteSequence.Join(willTween);
        }
        penWriteSequence.AppendInterval(penZoomHoldDuration);
        penWriteSequence.OnComplete(() =>
        {
            animator.PutDown();
            cardPlacementController.SetBoardBlocked(false);
            cameraZoomService.ZoomOut();
        });
    }

    private void StopPenWriteAnimation()
    {
        penWriteSequence?.Kill();
        traitTypewriter.Stop();
        willTypewriter.Stop();
    }

    private void ClosePanel()
    {
        StopPenWriteAnimation();
        cameraZoomService.ZoomOut();
        // 확대된 채로 패널이 닫히는 경우(우클릭 취소 등)에도 보드 막힘이
        // 풀리지 않고 남는 일이 없도록 닫을 때 항상 같이 풀어준다.
        cardPlacementController.SetBoardBlocked(false);
        animator.Close(infoPanl);
    }
}
