using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 스테이지를 클리어하고 맵으로 돌아왔을 때 한 번 떴다가 스스로 사라지는 텍스트.
///
/// 창이 아니라서 열림/닫힘 상태를 들고 있지 않다. Show()를 부르면 페이드 인 → 잠깐 머무름 →
/// 페이드 아웃까지 알아서 끝나므로 닫아줄 쪽이 필요 없고, 그래서 LSO_IPanel 계약도 쓰지 않는다.
/// 연출 방식(CanvasGroup 페이드 + 살짝 확대)만 LSO_FadePanel과 같게 맞췄다.
///
/// 맵 클릭을 가리면 안 되므로 raycast는 항상 꺼둔다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class LDY_ClearBanner : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TextMeshProUGUI messageText;
    [Tooltip("폰트와 색을 맞추기 위한 맵 테마. 비워두면 텍스트에 설정된 값을 그대로 쓴다.")]
    [SerializeField] private LDY_MapTheme theme;

    [Header("시간")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    [Tooltip("다 뜬 상태로 머무는 시간")]
    [SerializeField, Min(0f)] private float holdDuration = 1.1f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;

    [Header("크기")]
    [Tooltip("나타나기 시작할 때의 배율. 1이면 크기 변화 없이 페이드만 한다.")]
    [SerializeField, Range(0.5f, 1.5f)] private float startScale = 0.92f;

    [Header("색")]
    [Tooltip("켜면 테마의 골드(클리어 강조색), 끄면 본문 색을 쓴다.")]
    [SerializeField] private bool useAccentColor = true;

    private CanvasGroup canvasGroup;
    private RectTransform rect;
    private Vector3 baseScale = Vector3.one;
    private Sequence sequence;

    private void Awake()
    {
        EnsureInitialized();

        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 메시지를 띄운다. 재생 중에 또 부르면 앞선 연출을 끊고 처음부터 다시 시작한다.
    /// </summary>
    public void Show(string message)
    {
        // 씬에 꺼둔 채로 저장했더라도 이때 깨어난다.
        gameObject.SetActive(true);
        EnsureInitialized();

        if (messageText != null) messageText.text = message;
        else Debug.LogWarning("[LDY_ClearBanner] Message Text가 연결되지 않아 글자 없이 재생합니다.", this);

        KillSequence();

        canvasGroup.alpha = 0f;
        rect.localScale = baseScale * startScale;

        sequence = DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutCubic))
            .Join(rect.DOScale(baseScale, fadeInDuration).SetEase(Ease.OutCubic))
            .AppendInterval(holdDuration)
            .Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad))
            // 맵이 멈춰 있어도 보여야 하는 안내라 timeScale을 타지 않는다.
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() => sequence = null);
    }

    /// <summary>
    /// Awake보다 Show가 먼저 올 수 있어서(꺼둔 오브젝트를 켜면서 부르는 경우) 양쪽에서 부른다.
    /// </summary>
    private void EnsureInitialized()
    {
        if (canvasGroup != null) return;

        canvasGroup = GetComponent<CanvasGroup>();
        rect = (RectTransform)transform;
        baseScale = rect.localScale;

        // 배너는 보여주기만 하는 물건이다. 맵 노드 클릭을 가로채지 않게 막아둔다.
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (theme == null || messageText == null) return;

        if (theme.headerFont != null) messageText.font = theme.headerFont;

        messageText.color = useAccentColor ? theme.gold : theme.textOnDark;
        messageText.raycastTarget = false;
    }

    private void KillSequence()
    {
        if (sequence == null) return;

        sequence.Kill();
        sequence = null;
    }

    private void OnDisable()
    {
        // 다른 코드가 강제로 꺼버린 경우. 반쯤 페이드된 채로 굳지 않게 정리한다.
        KillSequence();

        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }
}
