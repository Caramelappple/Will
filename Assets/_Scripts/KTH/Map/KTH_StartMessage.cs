using DG.Tweening;
using TMPro;
using UnityEngine;

public class KTH_StartMessage : MonoBehaviour
{
    [Header("페이드 대상 (이미지+텍스트를 함께 관리)")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("비활성화할 캔버스")]
    [SerializeField] private GameObject targetCanvas;

    [Header("스테이지 정보 텍스트 (선택, 없으면 무시)")]
    [SerializeField] private TMP_Text stageText;
    [Tooltip("{0} = 챕터, {1} = 스테이지 번호, {2} = 랜덤 문구")]
    [SerializeField] private string stageTextFormat = "STAGE {0}-{1}\n{2}";

    [Header("랜덤 문구 소스 (선택, 없으면 무시)")]
    [SerializeField] private KTH_RandomText randomText;

    [Header("페이드 설정")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private float holdDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("시작 지연")]
    [Tooltip("씬 진입 연출(카메라 iris 등)과 겹치지 않도록 재생을 늦추는 시간(초)")]
    [SerializeField] private float startDelay = 0f;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        UpdateStageText();
        PlayFadeInOut();
    }

    private void UpdateStageText()
    {
        if (stageText == null) return;

        int chapter = LDY_MapManager.Instance != null ? LDY_MapManager.Instance.CurrentChapter : 0;
        int stage = LDY_MapManager.Instance != null ? LDY_MapManager.Instance.CurrentStage : 0;
        string picked = randomText != null ? randomText.GetRandomText() : string.Empty;

        stageText.text = string.Format(stageTextFormat, chapter, stage, picked);
    }

    private void PlayFadeInOut()
    {
        canvasGroup.DOKill();
        canvasGroup.alpha = 0f;

        if (targetCanvas != null)
            targetCanvas.SetActive(true);

        Sequence seq = DOTween.Sequence();
        seq.SetDelay(startDelay);
        seq.Append(canvasGroup.DOFade(maxAlpha, fadeInDuration).SetEase(Ease.OutQuad));
        seq.AppendInterval(holdDuration);
        seq.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            if (targetCanvas != null)
                targetCanvas.SetActive(false);
        });
    }

    public void PlayStartMessage()
    {
        UpdateStageText();
        PlayFadeInOut();
    }

    private void OnDestroy()
    {
        canvasGroup.DOKill();
    }
}