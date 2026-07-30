using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class KTH_InfoPanelController : MonoBehaviour
{
    [Header("UI 연결 (RectTransform 필수)")]
    public RectTransform panelRoot;
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text descText;
    public TMP_Text costText;
    public Button placeButton;
    public Button closeButton;

    [Header("연출 설정 (DOTween)")]
    public float animDuration = 0.5f;     // 연출 시간
    public float startYOffset = -200f;   // 시작 Y 오프셋 (현재 위치보다 아래)
    public float startYRotation = -180f; // 시작 Y축 회전 각도 (-180도 뒤집힌 상태)

    private Vector2 _originalAnchoredPos; // UI 전용 AnchoredPosition 저장용

    private void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Hide);

        if (panelRoot)
        {
            _originalAnchoredPos = panelRoot.anchoredPosition;
        }

        HideImmediate();
    }

    /// <summary>
    /// 카드 정보를 UI에 표시하며 Y축으로 회전하면서 올라오는 연출 실행
    /// </summary>
    public void Show(KTH_CardData data, bool showPlaceButton, Action onPlace)
    {
        // 1. 데이터 세팅
        if (iconImage) iconImage.sprite = data.icon;
        if (nameText) nameText.text = data.cardName;
        if (descText) descText.text = data.description;
        if (costText) costText.text = $"Cost {data.cost}";

        if (placeButton)
        {
            placeButton.gameObject.SetActive(showPlaceButton);
            placeButton.onClick.RemoveAllListeners();
            if (showPlaceButton && onPlace != null)
                placeButton.onClick.AddListener(() => onPlace());
        }

        if (!panelRoot) return;

        // 2. DOTween 연출 실행
        panelRoot.gameObject.SetActive(true);
        panelRoot.DOKill();

        // 초기 상태 설정
        // - 위치: 아래쪽 (startYOffset)
        // - 회전: Y축으로 startYRotation (-180도)
        // - 크기: 약간 작게 (0.5배)
        panelRoot.anchoredPosition = _originalAnchoredPos + new Vector2(0f, startYOffset);
        panelRoot.localRotation = Quaternion.Euler(0f, startYRotation, 0f);
        panelRoot.localScale = Vector3.one * 0.5f;

        // Sequence 생성
        Sequence showSequence = DOTween.Sequence();

        showSequence.Join(panelRoot.DOAnchorPos(_originalAnchoredPos, animDuration).SetEase(Ease.OutBack))
                    // Y축(0, 0, 0)을 향해 빙글 돌아 정면 바라보기
                    .Join(panelRoot.DOLocalRotate(Vector3.zero, animDuration, RotateMode.Fast).SetEase(Ease.OutCubic))
                    .Join(panelRoot.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack));
    }

    /// <summary>
    /// 창을 닫을 때 애니메이션
    /// </summary>
    public void Hide()
    {
        if (!panelRoot) return;

        panelRoot.DOKill();
        panelRoot.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
        {
            panelRoot.gameObject.SetActive(false);
        });
    }

    private void HideImmediate()
    {
        if (panelRoot)
        {
            panelRoot.DOKill();
            panelRoot.gameObject.SetActive(false);
        }
    }
}