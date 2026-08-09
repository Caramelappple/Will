using _Scripts.LSO.Deck.Data;
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

    [Header("바깥 클릭 감지")]
    public Button outsideClickCatcher;

    [Header("연출 설정 (DOTween)")]
    public float animDuration = 0.4f;     // 연출 시간
    public float startYOffset = -200f;   // 시작 Y 오프셋
    public float startYRotation = -180f; // 시작 Y축 회전 각도

    private Vector2 _originalAnchoredPos;
    private Action _onCancel;

    private KTH_HandCardView _currentCardView;
    private LSO_CardSO _currentData;

    private void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Hide);

        if (panelRoot)
        {
            _originalAnchoredPos = panelRoot.anchoredPosition;
        }

        EnsureOutsideClickCatcher();
        HideImmediate();
    }

    private void EnsureOutsideClickCatcher()
    {
        if (outsideClickCatcher != null || panelRoot == null) return;

        var catcherGO = new GameObject("OutsideClickCatcher", typeof(RectTransform), typeof(Image), typeof(Button));
        var catcherRect = (RectTransform)catcherGO.transform;

        catcherRect.SetParent(panelRoot.parent, false);
        catcherRect.SetSiblingIndex(panelRoot.GetSiblingIndex());

        catcherRect.anchorMin = Vector2.zero;
        catcherRect.anchorMax = Vector2.one;
        catcherRect.offsetMin = Vector2.zero;
        catcherRect.offsetMax = Vector2.zero;

        var image = catcherGO.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        outsideClickCatcher = catcherGO.GetComponent<Button>();
        var colors = outsideClickCatcher.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = Color.clear;
        colors.pressedColor = Color.clear;
        colors.selectedColor = Color.clear;
        colors.disabledColor = Color.clear;
        outsideClickCatcher.colors = colors;

        outsideClickCatcher.onClick.AddListener(HandleOutsideClick);
    }

    private void HandleOutsideClick()
    {
        Hide();
    }

    public void Show(KTH_HandCardView cardView, bool showPlaceButton, Action onPlace, Action onCancel = null)
    {
        if (cardView == null || cardView.Data == null) return;

        if (panelRoot != null && panelRoot.gameObject.activeSelf && _currentCardView == cardView)
        {
            return;
        }

        _currentCardView = cardView;
        ShowInternal(cardView.Data, showPlaceButton, onPlace, onCancel);
    }

    public void Show(LSO_CardSO data, bool showPlaceButton, Action onPlace, Action onCancel = null)
    {
        if (data == null) return;

        if (panelRoot != null && panelRoot.gameObject.activeSelf && _currentCardView == null && _currentData == data)
        {
            return;
        }

        _currentCardView = null;
        ShowInternal(data, showPlaceButton, onPlace, onCancel);
    }

    private void ShowInternal(LSO_CardSO data, bool showPlaceButton, Action onPlace, Action onCancel)
    {
        _onCancel = null;
        if (panelRoot != null) panelRoot.DOKill();

        _currentData = data;
        _onCancel = onCancel;

        if (iconImage) iconImage.sprite = data.Image;
        if (nameText) nameText.text = data.AnimalName;
        if (descText) descText.text = data.Description;
        if (costText) costText.text = $"Cost {data.Cost}";

        if (placeButton)
        {
            placeButton.gameObject.SetActive(showPlaceButton);
            placeButton.onClick.RemoveAllListeners();
            if (showPlaceButton && onPlace != null)
            {
                placeButton.onClick.AddListener(() =>
                {
                    onPlace();
                    HideWithAnim();
                });
            }
        }

        if (!panelRoot) return;

        if (outsideClickCatcher != null)
        {
            outsideClickCatcher.gameObject.SetActive(true);
            outsideClickCatcher.transform.SetSiblingIndex(panelRoot.GetSiblingIndex());
        }

        panelRoot.gameObject.SetActive(true);
        panelRoot.anchoredPosition = _originalAnchoredPos + new Vector2(0f, startYOffset);
        panelRoot.localRotation = Quaternion.Euler(0f, startYRotation, 0f);
        panelRoot.localScale = Vector3.one * 0.3f;

        Sequence showSequence = DOTween.Sequence();
        showSequence.SetTarget(panelRoot);
        showSequence.SetLink(panelRoot.gameObject);

        showSequence.Join(panelRoot.DOAnchorPos(_originalAnchoredPos, animDuration).SetEase(Ease.OutBack))
                    .Join(panelRoot.DOLocalRotate(Vector3.zero, animDuration, RotateMode.FastBeyond360).SetEase(Ease.OutCubic))
                    .Join(panelRoot.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack));
    }

    public void HideWithAnim()
    {
        Action tempCancel = _onCancel;
        _currentCardView = null;
        _currentData = null;
        _onCancel = null;

        tempCancel?.Invoke();

        if (outsideClickCatcher) outsideClickCatcher.gameObject.SetActive(false);

        if (!panelRoot) return;

        panelRoot.DOKill();

        Vector2 targetPos = _originalAnchoredPos + new Vector2(0f, startYOffset);
        Vector3 targetRotation = new Vector3(0f, startYRotation, 0f);

        Sequence hideSequence = DOTween.Sequence();
        hideSequence.SetTarget(panelRoot);
        hideSequence.SetLink(panelRoot.gameObject);

        hideSequence.Join(panelRoot.DOAnchorPos(targetPos, animDuration).SetEase(Ease.InBack))
                    .Join(panelRoot.DOLocalRotate(targetRotation, animDuration, RotateMode.FastBeyond360).SetEase(Ease.InCubic))
                    .Join(panelRoot.DOScale(Vector3.one * 0.3f, animDuration).SetEase(Ease.InBack))
                    .OnComplete(() =>
                    {
                        panelRoot.gameObject.SetActive(false);
                    });
    }

    public void Hide()
    {
        Action tempCancel = _onCancel;
        _currentCardView = null;
        _currentData = null;
        _onCancel = null;

        // 매니저에 전달된 onCancel 콜백 실행 및 안전 복원
        tempCancel?.Invoke();

        var deckManager = FindFirstObjectByType<KTH_DeckManager>();
        if (deckManager != null)
        {
            deckManager.DeselectAllCards();
        }

        if (outsideClickCatcher) outsideClickCatcher.gameObject.SetActive(false);

        if (!panelRoot) return;

        panelRoot.DOKill();
        panelRoot.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
        {
            panelRoot.gameObject.SetActive(false);
        });
    }

    private void HideImmediate()
    {
        _currentCardView = null;
        _currentData = null;
        _onCancel = null;

        if (outsideClickCatcher) outsideClickCatcher.gameObject.SetActive(false);

        if (panelRoot)
        {
            panelRoot.DOKill();
            panelRoot.gameObject.SetActive(false);
        }
    }
}