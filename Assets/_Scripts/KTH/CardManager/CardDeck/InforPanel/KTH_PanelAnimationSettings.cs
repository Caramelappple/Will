using System;
using DG.Tweening;
using UnityEngine;

// =========================================================
// 기본 세팅: 인스펙터에서 값을 안 건드려도 합리적인 기본값으로 동작한다.
//
// 지금 단계는 "하나씩" 진행 중 - 클릭하면 가까이 당겨서(줌인) 보여주는
// 기본 동작만 깔끔하게 다시 만든 상태다. 벅샷 룰렛 스타일(anticipation/
// overshoot/idle sway)은 이 기본 줌인이 마음에 든 다음 다시 하나씩 얹는다.
// =========================================================
[Serializable]
public struct KTH_PanelAnimationSettings
{
    public float animDuration;
    public float moveUpDistance;
    public float rotateAngle;
    public Ease animEase;

    [Header("가까이 보기 (기획: 인포 창을 누르면 가까이 가져온다)")]
    public Vector2 closeUpAnchoredPos;
    public float closeUpScale;
    public float closeUpDuration;
    public Ease closeUpEase;

    // 아래 필드들은 나중에 벅샷 룰렛 스타일을 다시 얹을 때 쓸 값이라
    // 인스펙터 세팅이 날아가지 않도록 구조체에는 그대로 남겨둔다.
    // 지금 애니메이터 코드에서는 사용하지 않는다.
    [Header("(다음 단계) 낚아채기 전 움츠림(anticipation)")]
    public float anticipationDistance;
    public float anticipationDuration;

    [Header("(다음 단계) 정착할 때 손맛(오버슈트)")]
    public float overshootAmount;

    [Header("(다음 단계) 들고 보는 동안 손떨림(idle sway)")]
    public float swayAngle;
    public float swayPositionAmount;
    public float swayDuration;

    public static KTH_PanelAnimationSettings Default => new KTH_PanelAnimationSettings
    {
        animDuration = 0.4f,
        moveUpDistance = 100f,
        rotateAngle = 360f,
        animEase = Ease.OutBack,
        closeUpAnchoredPos = Vector2.zero,
        closeUpScale = 1.3f,
        closeUpDuration = 0.3f,
        closeUpEase = Ease.OutQuad,
        anticipationDistance = 12f,
        anticipationDuration = 0.08f,
        overshootAmount = 1.3f,
        swayAngle = 1.5f,
        swayPositionAmount = 4f,
        swayDuration = 1.4f
    };
}

// =========================================================
// SRP: 인포 패널의 열기 / 닫기 / "가까이 보기" 애니메이션만 담당한다.
// UI 데이터, 카메라, 카드 배치 로직은 알지 못한다.
// MonoBehaviour가 아닌 순수 C# 클래스라 GameObject/컴포넌트 오버헤드가 없다.
// =========================================================
public interface IInfoPanelAnimator : IDisposable
{
    bool IsOpen { get; }
    bool IsClosing { get; }
    bool IsBroughtCloser { get; }
    void Open(GameObject panelObject, Action onOpened = null);
    void Close(GameObject panelObject, Action onClosed = null);
    void ToggleCloseUp();

    // 이건 UI(RectTransform) 애니메이션이라 카메라 설정과 무관하게 항상 동작한다.
    // 클릭 토글과 별개로, 만년필 타이핑 같은 다른 이벤트에서도
    // "가까이 당겨오기 / 내려놓기"를 직접 호출할 수 있도록 공개한다.
    void BringCloser();
    void PutDown();
}

public sealed class KTH_InfoPanelAnimator : IInfoPanelAnimator
{
    private readonly RectTransform rect;
    private readonly CanvasGroup canvasGroup;
    private readonly KTH_PanelAnimationSettings settings;
    private readonly Vector2 originalPos;
    private readonly int originalSiblingIndex;

    private Sequence mainSequence;
    private Sequence closeUpSequence;
    private bool isBroughtCloser;

    public bool IsOpen { get; private set; }
    public bool IsClosing { get; private set; }
    public bool IsBroughtCloser => isBroughtCloser;

    public KTH_InfoPanelAnimator(
        RectTransform rect,
        CanvasGroup canvasGroup,
        KTH_PanelAnimationSettings settings)
    {
        this.rect = rect;
        this.canvasGroup = canvasGroup;
        this.settings = settings;
        originalPos = rect.anchoredPosition;
        // 확대(가까이 보기)됐을 때 다른 UI에 가리지 않도록 맨 위로 올렸다가,
        // 원위치로 돌아가면 원래 그리기 순서로 복원하기 위해 기억해둔다.
        originalSiblingIndex = rect.GetSiblingIndex();
    }

    public void Open(GameObject panelObject, Action onOpened = null)
    {
        mainSequence?.Kill();
        closeUpSequence?.Kill();
        isBroughtCloser = false;
        IsClosing = false;

        panelObject.SetActive(true);
        rect.anchoredPosition =
            originalPos - new Vector2(0f, settings.moveUpDistance);
        rect.localScale = Vector3.zero;
        rect.localEulerAngles = new Vector3(0f, settings.rotateAngle, 0f);
        canvasGroup.alpha = 0f;

        mainSequence = DOTween.Sequence();
        mainSequence.Append(
            rect.DOAnchorPos(originalPos, settings.animDuration)
                .SetEase(settings.animEase)
        );
        mainSequence.Join(
            DOVirtual
                .Float(
                    settings.rotateAngle,
                    0f,
                    settings.animDuration,
                    y => rect.localEulerAngles = new Vector3(0f, y, 0f)
                )
                .SetEase(Ease.OutQuad)
        );
        mainSequence.Join(
            rect.DOScale(1f, settings.animDuration).SetEase(settings.animEase)
        );
        mainSequence.Join(
            canvasGroup.DOFade(1f, settings.animDuration * 0.7f)
        );
        mainSequence.OnComplete(() =>
        {
            IsOpen = true;
            onOpened?.Invoke();
        });
    }

    public void Close(GameObject panelObject, Action onClosed = null)
    {
        if (IsClosing)
        {
            return;
        }
        mainSequence?.Kill();
        closeUpSequence?.Kill();
        isBroughtCloser = false;
        IsClosing = true;
        IsOpen = false;

        mainSequence = DOTween.Sequence();
        mainSequence.Join(
            rect.DOAnchorPos(
                    originalPos - new Vector2(0f, settings.moveUpDistance),
                    settings.animDuration
                )
                .SetEase(Ease.InBack)
        );
        mainSequence.Join(
            rect.DOScale(0f, settings.animDuration).SetEase(Ease.InBack)
        );
        mainSequence.Join(
            DOVirtual
                .Float(
                    rect.localEulerAngles.y,
                    settings.rotateAngle,
                    settings.animDuration,
                    y => rect.localEulerAngles = new Vector3(0f, y, 0f)
                )
                .SetEase(Ease.InQuad)
        );
        mainSequence.Join(
            canvasGroup.DOFade(0f, settings.animDuration * 0.7f)
        );
        mainSequence.OnComplete(() =>
        {
            panelObject.SetActive(false);
            rect.anchoredPosition = originalPos;
            rect.localScale = Vector3.one;
            rect.localEulerAngles = Vector3.zero;
            canvasGroup.alpha = 1f;
            IsClosing = false;
            onClosed?.Invoke();
        });
    }

    // =========================================================
    // 기획: 인포 창을 누르면 인포 창을 가까이 가져온다 (다시 누르면 원위치)
    //
    // 지금은 가장 기본적인 형태: 목표 위치/스케일로 부드럽게 줌인/줌아웃만 한다.
    // 벅샷 룰렛 스타일의 움츠림/오버슈트/손떨림은 이 동작이 확정되면 다음
    // 단계에서 하나씩 얹는다.
    // =========================================================
    public void ToggleCloseUp()
    {
        if (!IsOpen || IsClosing)
        {
            return;
        }
        if (isBroughtCloser)
        {
            PutDown();
        }
        else
        {
            BringCloser();
        }
    }

    // 이건 UI(RectTransform) 애니메이션이라 카메라 확대/축소 설정과 무관하게
    // 항상 화면에 보인다. 클릭 토글뿐 아니라 만년필 타이핑 같은 다른 이벤트에서도
    // 그대로 재사용한다 (예: 카메라 줌이 안 먹는 Screen Space - Overlay 캔버스여도 동작).
    public void BringCloser()
    {
        if (!IsOpen || IsClosing)
        {
            return;
        }
        isBroughtCloser = true;
        closeUpSequence?.Kill();
        // 확대되는 순간 다른 UI(카드, 다른 패널 등)에 가려지지 않도록 맨 위로.
        rect.SetAsLastSibling();

        closeUpSequence = DOTween.Sequence();
        closeUpSequence.Join(
            rect.DOAnchorPos(settings.closeUpAnchoredPos, settings.closeUpDuration)
                .SetEase(settings.closeUpEase)
        );
        closeUpSequence.Join(
            rect.DOScale(settings.closeUpScale, settings.closeUpDuration)
                .SetEase(settings.closeUpEase)
        );
    }

    public void PutDown()
    {
        if (!IsOpen)
        {
            return;
        }
        isBroughtCloser = false;
        closeUpSequence?.Kill();

        closeUpSequence = DOTween.Sequence();
        closeUpSequence.Join(
            rect.DOAnchorPos(originalPos, settings.closeUpDuration)
                .SetEase(settings.closeUpEase)
        );
        closeUpSequence.Join(
            rect.DOScale(1f, settings.closeUpDuration).SetEase(settings.closeUpEase)
        );
        // 원래 자리로 돌아가는 트윈이 끝나면 그리기 순서도 원래대로 되돌린다.
        // (트윈 도중에 먼저 되돌리면 애니메이션 중에 다시 다른 UI에 가릴 수 있다)
        closeUpSequence.OnComplete(() => rect.SetSiblingIndex(originalSiblingIndex));
    }

    public void Dispose()
    {
        mainSequence?.Kill();
        closeUpSequence?.Kill();
    }
}
