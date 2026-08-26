using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 노드 하나를 나타내는 UI 버튼. 사각 패널은 없고 아이콘(별) + 뒤에서 번지는 글로우 + 현재 위치를
// 감싸는 링(테두리 빛)만 보여줌. Background는 화면에는 안 보이고 클릭 판정 영역으로만 쓰임
// 색은 타입이 아니라 "상태"로만 결정 (팔레트 3톤 유지: 잠김=무채색, 진행중=밝은 중립색+골드 링, 클리어=골드)
public class LDY_MapNodeView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Button button;
    [SerializeField] private Image glowImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image borderImage; // 현재 위치를 감싸는 골드 링 (겉테두리 빛)
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cornerStarImage;

    [Header("Type Icons (모양으로만 구분, 색은 사용하지 않음)")]
    [SerializeField] private Sprite battleIcon;
    [SerializeField] private Sprite bossIcon;

    [Header("State Icons (클리어/현재 위치일 때 iconImage 스프라이트를 덮어씀)")]
    [SerializeField] private Sprite successSprite; // 클리어 상태일 때 iconImage에 표시
    [SerializeField] private Sprite currentSprite;  // 현재 위치일 때 iconImage에 표시

    [Header("아이콘 크기 (부모 대비 채우는 비율)")]
    [Range(0.5f, 1f)][SerializeField] private float iconFillRatio = 0.9f;

    [Header("클리어 연출 (방금 클리어한 노드에만 한 번 재생)")]
    [SerializeField, Min(0.02f)] private float clearPopDuration = 0.24f;
    [Tooltip("팝 하는 순간 아이콘이 잠깐 커지는 배율. 원래 크기 대비 값이다.")]
    [SerializeField, Range(1f, 2f)] private float clearPopScale = 1.35f;

    /// <summary>
    /// 진입 판정을 통과한 노드 클릭을 알린다. 클릭 연출(링 등)을 붙이기 위한 신호일 뿐이라
    /// 여기에 아무도 붙어있지 않아도 맵 동작은 그대로다.
    ///
    /// static인 이유: 노드 뷰는 맵이 갱신될 때마다 통째로 다시 만들어지므로(LDY_MapUIController.RebuildMap)
    /// 인스턴스 이벤트로 두면 연출 쪽이 매번 다시 구독해야 한다.
    /// 구독하는 쪽은 반드시 OnDisable/OnDestroy에서 해제할 것. static 이벤트는 씬을 넘겨도 살아남는다.
    ///
    /// System.Action을 풀네임으로 쓴 이유: 이 파일은 UnityEngine을 열어두고 Random.Range를 쓰는데,
    /// using System;을 얹으면 System.Random과 겹쳐 CS0104가 난다.
    /// </summary>
    public static event System.Action<LDY_MapNodeView> NodeSelected;

    public RectTransform RectTransform => (RectTransform)transform;
    public int NodeIndex { get; private set; }

    private LDY_MapManager manager;
    private LDY_MapNode nodeData;
    private LDY_MapTheme theme;
    private LDY_MapUIController uiController;

    private bool isPulsing;
    private bool isRingActive;
    private float phaseOffset;
    private Color pulseBaseColor;

    // 프리팹 루트가 이미 1.2배로 저장돼 있어서 연출이 끝날 때 절대값 1로 되돌리면 크기가 어긋난다.
    // 배치가 끝난 시점의 아이콘 스케일을 기억해두고 항상 그 값을 기준으로 키웠다 되돌린다.
    private Vector3 iconBaseScale = Vector3.one;
    private Sequence clearPopSequence;

    public void Initialize(LDY_MapManager manager, LDY_MapNode node, int index, LDY_MapTheme theme, LDY_MapUIController uiController)
    {
        this.manager = manager;
        this.theme = theme;
        this.uiController = uiController;
        nodeData = node;
        NodeIndex = index;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        RectTransform.anchoredPosition = node.position;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClick);

        // 별을 크게: 아이콘이 부모 박스를 거의 꽉 채우도록 인셋을 넓힘
        if (iconImage != null)
        {
            RectTransform iconRt = iconImage.rectTransform;
            float margin = (1f - iconFillRatio) * 0.5f;
            iconRt.anchorMin = new Vector2(margin, margin);
            iconRt.anchorMax = new Vector2(1f - margin, 1f - margin);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            iconBaseScale = iconRt.localScale;
        }

        // Background는 투명한 클릭 판정 영역으로만 사용 (사각 패널 없음)
        if (backgroundImage != null) backgroundImage.color = new Color(0f, 0f, 0f, 0f);
        if (cornerStarImage != null) cornerStarImage.gameObject.SetActive(false);

        if (borderImage != null && borderImage.sprite == null)
            borderImage.sprite = LDY_ProceduralSprite.Ring;

        if (glowImage != null && glowImage.sprite == null)
            glowImage.sprite = LDY_ProceduralSprite.SoftGlow;

        Refresh();
    }

    public void Refresh()
    {
        if (nodeData == null || theme == null) return;

        bool isCurrent = nodeData.isUnlocked && !nodeData.isCleared;
        bool glowActive = nodeData.isCleared || isCurrent;
        bool ringActive = isCurrent; // "겉테두리가 빛나는" 표시는 지금 갈 수 있는 노드에만

        // [수정] 현재 노드가 이미 클리어된 노드라면 현재 위치 표시를 하지 않습니다.
        // 아직 클리어되지 않고 해금된 노드 중에서만 판단합니다.
        bool playerHere = !nodeData.isCleared &&
                          ((manager != null && manager.CurrentNodeIndex == NodeIndex) ||
                           (uiController != null && uiController.IsPlayerAt(NodeIndex)));

        // iconImage 스프라이트를 상태에 따라 결정 (SetActive 대신 교체)
        // 우선순위: 클리어 > 현재 위치 > 기본 타입 아이콘
        if (iconImage != null)
        {
            if (nodeData.isCleared)
                iconImage.sprite = successSprite;
            else if (playerHere)
                iconImage.sprite = currentSprite;
            else
                iconImage.sprite = GetIcon(nodeData.type);
        }

        Color iconColor;
        float iconAlpha;

        if (nodeData.isCleared || isCurrent)
        {
            iconColor = Color.white;
            iconAlpha = 1f;
        }
        else
        {
            iconColor = theme.textLocked;
            iconAlpha = 1f;
        }

        if (iconImage != null)
            iconImage.color = new Color(iconColor.r, iconColor.g, iconColor.b, iconAlpha);

        if (borderImage != null)
            borderImage.gameObject.SetActive(ringActive);

        SetPulse(glowActive, ringActive, playerHere);

        button.interactable = nodeData.isUnlocked;
    }

    /// <summary>
    /// 이 노드가 선택됐다고 알린다. 링 연출이 여기에 붙는다.
    /// 매니저가 타이밍을 정해 LDY_MapUIController를 거쳐 불러준다.
    /// </summary>
    public void PlaySelectRing()
    {
        NodeSelected?.Invoke(this);
    }

    /// <summary>
    /// 클리어 표시(X)가 찍히는 순간을 한 번 보여준다.
    ///
    /// Refresh()가 이미 스프라이트를 X로 바꿔둔 뒤에 불린다. 여기서는 그 X를 투명한 상태에서
    /// 살짝 크게 띄웠다가 제자리로 되돌리기만 한다. 예전에 클리어해둔 노드는 이 메서드를 부르지
    /// 않으므로 맵에 다시 들어와도 연출 없이 곧바로 X가 보인다.
    /// </summary>
    public void PlayClearPop()
    {
        if (iconImage == null) return;

        KillClearPop();

        RectTransform iconRt = iconImage.rectTransform;

        // Refresh()가 클리어/현재 노드의 아이콘을 항상 불투명하게 그리므로 도착 알파는 1로 고정한다.
        // 지금 색에서 읽어오면 연출이 끊겼다 다시 시작될 때 0에서 0으로 페이드해 X가 안 보인다.
        Color iconColor = iconImage.color;
        iconImage.color = new Color(iconColor.r, iconColor.g, iconColor.b, 0f);
        iconRt.localScale = iconBaseScale;

        float half = clearPopDuration * 0.5f;

        clearPopSequence = DOTween.Sequence()
            .Append(iconRt.DOScale(iconBaseScale * clearPopScale, half).SetEase(Ease.OutCubic))
            .Join(iconImage.DOFade(1f, half).SetEase(Ease.OutCubic))
            .Append(iconRt.DOScale(iconBaseScale, half).SetEase(Ease.InOutCubic))
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() => clearPopSequence = null);
    }

    private void KillClearPop()
    {
        if (clearPopSequence == null) return;

        clearPopSequence.Kill();
        clearPopSequence = null;

        // 중간에 끊겼을 때 반쯤 커지거나 반쯤 투명한 채로 굳지 않게 되돌린다.
        if (iconImage != null)
        {
            iconImage.rectTransform.localScale = iconBaseScale;

            Color iconColor = iconImage.color;
            iconImage.color = new Color(iconColor.r, iconColor.g, iconColor.b, 1f);
        }
    }

    private void OnDisable()
    {
        KillClearPop();
    }

    private void SetPulse(bool glowActive, bool ringActive, bool playerHere)
    {
        isPulsing = glowActive || ringActive;
        isRingActive = ringActive;

        if (isPulsing)
        {
            float intensity = theme.glowHdrIntensity;
            Color baseColor = playerHere ? theme.playerGlow : theme.GetTypeGlowColor(nodeData.type);
            pulseBaseColor = new Color(baseColor.r * intensity, baseColor.g * intensity, baseColor.b * intensity, 1f);
        }
        else
        {
            if (glowImage != null) glowImage.color = new Color(0f, 0f, 0f, 0f);
        }
    }

    private void Update()
    {
        if (!isPulsing || theme == null) return;

        float wave = (Mathf.Sin(Time.time * theme.glowPulseSpeed + phaseOffset) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(theme.glowMinAlpha, theme.glowMaxAlpha, wave);

        if (glowImage != null)
        {
            Color gc = pulseBaseColor;
            gc.a = alpha;
            glowImage.color = gc;
        }

        if (isRingActive && borderImage != null)
        {
            Color rc = pulseBaseColor;
            rc.a = alpha;
            borderImage.color = rc;
        }
    }

    private void HandleClick()
    {
        // 클릭 시 곧바로 이동하고자 하는 노드의 ScreenUV 기준 전환 이벤트 실행
        //
        // 연출과 "현재 위치" 표시는 여기서 건드리지 않는다. 클릭이 받아들여졌는지, 토큰이
        // 언제 도착하는지는 매니저만 알기 때문이다. 매니저가 OnNodeSelected로 알려주면
        // LDY_MapUIController가 받아서 이 뷰의 PlaySelectRing()을 부른다.
        if (manager != null)
            manager.OnNodeClicked(NodeIndex, GetScreenUV());
    }

    // 씬 전환 연출의 중심점으로 사용하기 위한 노드의 화면 비율 좌표(0~1) 계산
    private Vector2 GetScreenUV()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return new Vector2(0.5f, 0.5f);

        RectTransform canvasRect = (RectTransform)canvas.transform;
        Vector3 localPos = canvasRect.InverseTransformPoint(RectTransform.position);
        Rect rect = canvasRect.rect;

        return new Vector2(
            (localPos.x - rect.xMin) / rect.width,
            (localPos.y - rect.yMin) / rect.height);
    }

    private Sprite GetIcon(LDY_NodeType type)
    {
        switch (type)
        {
            case LDY_NodeType.Battle:
                return battleIcon;
            case LDY_NodeType.Boss:
                return bossIcon;
            default:
                // type이 예상을 벗어났을 때도 기본 아이콘(예: battleIcon)을 넘겨주거나 디버그 로그 출력
                Debug.LogWarning($"[LDY_MapNodeView] 정의되지 않은 노드 타입입니다: {type}");
                return battleIcon;
        }
    }
}