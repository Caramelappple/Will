using System;
using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KTH_InfoPanelController : MonoBehaviour
{
    [Header("UI 연결")]
    public RectTransform panelRoot;
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text descText;
    public TMP_Text costText;
    public Button placeButton;
    public Button closeButton;

    [Header("버린 카드 UI")]
    public KTH_DiscardCardUI discardCardUI;

    [Header("바깥 클릭 감지")]
    public Button outsideClickCatcher;

    [Header("패널 클릭 방지")]
    public Image panelClickBlocker;

    [Header("패널 연출 설정")]
    public float animDuration = 0.4f;
    public float startYOffset = -200f;
    public float startYRotation = -180f;

    [Header("카드 버리기 연출")]
    public float flyDuration = 0.5f;
    public float rotateAngle = -360f;
    public Vector3 endScale =
        new Vector3(
            0.15f,
            0.15f,
            0.15f
        );

    private Vector2 _originalAnchoredPos;

    private Action _onCancel;

    private KTH_HandCardView _currentCardView;
    private LSO_CardSO _currentData;

    // 패널이 열리고 있는 중인지
    private bool _isOpening;

    // 패널이 닫히는 중인지
    private bool _isClosing;

    // 현재 패널이 완전히 열린 상태인지
    private bool _isOpen;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
            closeButton.onClick.AddListener(Hide);
        }

        if (panelRoot != null)
        {
            _originalAnchoredPos =
                panelRoot.anchoredPosition;
        }

        RestoreReferences();
        SetupPanelClickBlocker();
        EnsureOutsideClickCatcher();

        HideImmediate();
    }

    private void RestoreReferences()
    {
        if (discardCardUI == null)
        {
            discardCardUI =
                FindFirstObjectByType<KTH_DiscardCardUI>(
                    FindObjectsInactive.Include
                );
        }

        if (panelClickBlocker == null &&
            panelRoot != null)
        {
            Transform blocker =
                panelRoot.Find(
                    "PanelClickBlocker"
                );

            if (blocker != null)
            {
                panelClickBlocker =
                    blocker.GetComponent<Image>();
            }
        }
    }

    private void EnsureOutsideClickCatcher()
    {
        if (panelRoot == null)
            return;

        // 최상위 Canvas를 찾아서 그 바로 아래에 catcher를 둔다.
        // (체스판 스크립트는 건드리지 않고, 이 catcher가 화면 전체를 UI 레이어에서 덮도록만 한다.)
        Canvas rootCanvas =
            panelRoot.GetComponentInParent<Canvas>()?.rootCanvas;

        Transform blockerParent =
            rootCanvas != null
                ? rootCanvas.transform
                : panelRoot.parent;

        if (blockerParent == null)
            return;

        if (outsideClickCatcher == null)
        {
            Transform existing =
                blockerParent.Find("OutsideClickCatcher");

            if (existing != null)
                outsideClickCatcher = existing.GetComponent<Button>();
        }

        if (outsideClickCatcher != null)
            return;

        GameObject catcher =
            new GameObject(
                "OutsideClickCatcher",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        RectTransform rect = catcher.GetComponent<RectTransform>();
        rect.SetParent(blockerParent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = catcher.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        outsideClickCatcher = catcher.GetComponent<Button>();

        ColorBlock colors = outsideClickCatcher.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = Color.clear;
        colors.pressedColor = Color.clear;
        colors.selectedColor = Color.clear;
        colors.disabledColor = Color.clear;
        outsideClickCatcher.colors = colors;

        outsideClickCatcher.onClick.AddListener(HandleOutsideClick);
    }

    private void SetupPanelClickBlocker()
    {
        if (panelClickBlocker == null)
            return;

        panelClickBlocker.raycastTarget = true;
    }

    private void HandleOutsideClick()
    {
        // 열리는 중에는 절대로 취소하지 않는다.
        if (_isOpening)
            return;

        // 닫히는 중에도 입력 무시
        if (_isClosing)
            return;

        // 완전히 열린 상태에서만 취소
        if (!_isOpen)
            return;

        Hide();
    }

    public void Show(
        KTH_HandCardView cardView,
        bool showPlaceButton,
        Action onPlace,
        Action onCancel = null)
    {
        if (cardView == null ||
            cardView.Data == null)
        {
            return;
        }

        _currentCardView = cardView;

        ShowInternal(
            cardView.Data,
            showPlaceButton,
            onPlace,
            onCancel
        );
    }

    public void Show(
        LSO_CardSO data,
        bool showPlaceButton,
        Action onPlace,
        Action onCancel = null)
    {
        if (data == null)
            return;

        _currentCardView = null;

        ShowInternal(
            data,
            showPlaceButton,
            onPlace,
            onCancel
        );
    }

    private void ShowInternal(
        LSO_CardSO data,
        bool showPlaceButton,
        Action onPlace,
        Action onCancel)
    {
        if (data == null)
            return;

        // ----------------------------------------------------------------
        // [수정] 이미 다른 카드에 대해 패널이 열려 있는 상태에서
        // 새 카드를 선택하면, 기존 카드의 취소 콜백(_onCancel)을
        // 먼저 실행해줘야 한다.
        // 이 콜백은 보통 이전에 선택된 핸드카드를
        // SetSelected(false) / ResetSelectionOffset() 등으로
        // 원래 상태로 복구시키는 역할을 한다.
        // 이걸 호출하지 않고 그냥 _onCancel을 덮어쓰면
        // 이전 카드가 선택된 채로(내려간 상태 / sortingOrder 변경 상태 등)
        // 방치되어 다음 로직(핸드 재배치 등)에서 사라지거나
        // 이상하게 보이는 문제가 발생한다.
        // ----------------------------------------------------------------
        if (_onCancel != null &&
            _currentCardView != null)
        {
            Action previousCancel = _onCancel;
            _onCancel = null;
            previousCancel.Invoke();
        }

        // 기존 애니메이션 완전히 제거
        if (panelRoot != null)
        {
            panelRoot.DOKill();
        }

        _currentData = data;
        _onCancel = onCancel;

        _isOpening = true;
        _isClosing = false;
        _isOpen = false;

        if (iconImage != null)
            iconImage.sprite = data.Image;

        if (nameText != null)
            nameText.text = data.AnimalName;

        if (descText != null)
            descText.text = data.Description;

        if (costText != null)
            costText.text =
                $"Cost {data.Cost}";

        SetupPlaceButton(
            showPlaceButton,
            onPlace
        );

        // 중요:
        // 패널이 올라오는 동안 바깥 클릭을 완전히 막는다.
        if (outsideClickCatcher != null)
        {
            outsideClickCatcher.gameObject
                .SetActive(true);

            outsideClickCatcher.interactable =
                false;

            outsideClickCatcher.transform
                .SetAsFirstSibling();
        }

        if (panelClickBlocker != null)
        {
            panelClickBlocker.gameObject
                .SetActive(true);

            panelClickBlocker.raycastTarget =
                true;
        }

        panelRoot.gameObject.SetActive(true);

        panelRoot.anchoredPosition =
            _originalAnchoredPos +
            new Vector2(
                0f,
                startYOffset
            );

        panelRoot.localRotation =
            Quaternion.Euler(
                0f,
                startYRotation,
                0f
            );

        Sequence sequence =
            DOTween.Sequence();

        sequence
            .SetTarget(panelRoot)
            .SetLink(panelRoot.gameObject);

        sequence
            .Join(
                panelRoot
                    .DOAnchorPos(
                        _originalAnchoredPos,
                        animDuration
                    )
                    .SetEase(Ease.OutBack)
            )
            .Join(
                panelRoot
                    .DOLocalRotate(
                        Vector3.zero,
                        animDuration,
                        RotateMode.FastBeyond360
                    )
                    .SetEase(Ease.OutCubic)
            )
            .OnComplete(() =>
            {
                // 이제 완전히 열린 상태
                _isOpening = false;
                _isClosing = false;
                _isOpen = true;

                // 이제부터 바깥 클릭 취소 허용
                if (outsideClickCatcher != null)
                {
                    outsideClickCatcher.interactable =
                        true;
                }
            });
    }

    private void SetupPlaceButton(
        bool showPlaceButton,
        Action onPlace)
    {
        if (placeButton == null)
            return;

        placeButton.gameObject.SetActive(
            showPlaceButton
        );

        placeButton.onClick.RemoveAllListeners();

        if (!showPlaceButton ||
            onPlace == null)
        {
            return;
        }

        placeButton.onClick.AddListener(() =>
        {
            // 연타 방지
            if (_isOpening ||
                _isClosing ||
                !_isOpen)
            {
                return;
            }

            KTH_HandCardView targetCard =
                _currentCardView;

            LSO_CardSO targetData =
                _currentData;

            // [수정] Place 버튼으로 정상적으로 카드를 사용/배치하는
            // 경로이므로, 여기서는 _onCancel을 호출하지 않고
            // 그냥 비워서 HideWithAnim() 내부에서 다시
            // 호출되지 않도록 한다. (중복 실행 방지)
            _onCancel = null;

            HideWithAnim();

            if (targetCard != null)
            {
                PlayDiscardDirectly(
                    targetCard,
                    targetData,
                    onPlace
                );
            }
            else
            {
                onPlace?.Invoke();

                if (discardCardUI != null &&
                    targetData != null)
                {
                    discardCardUI
                        .AddToDiscardPile(
                            targetData
                        );
                }
            }
        });
    }

    private void PlayDiscardDirectly(
    KTH_HandCardView cardView,
    LSO_CardSO cardData,
    Action onComplete)
    {
        if (discardCardUI == null)
        {
            discardCardUI =
                FindFirstObjectByType<KTH_DiscardCardUI>(
                    FindObjectsInactive.Include
                );
        }

        // 버린 카드 UI가 없으면
        // 바로 다음 단계로 진행
        if (discardCardUI == null ||
            discardCardUI.DiscardCardTransform == null)
        {
            if (cardData != null &&
                discardCardUI != null)
            {
                discardCardUI.AddToDiscardPile(cardData);
            }

            // ⭐ 카드 비활성화하지 않음
            // ⭐ enabled = false 하지 않음
            //
            // 실제 삭제는 DeckManager의
            // FinalizeCardPlacement()에서 한다.
            onComplete?.Invoke();

            return;
        }

        RectTransform cardRect =
            cardView.GetComponent<RectTransform>();

        RectTransform targetRect =
            discardCardUI.DiscardCardTransform;

        cardRect.DOKill();

        // 버리는 연출 중에는 다른 UI보다 위에 표시
        cardRect.SetAsLastSibling();

        RectTransform parentRect =
            cardRect.parent as RectTransform;

        Canvas parentCanvas =
            cardRect.GetComponentInParent<Canvas>();

        Camera uiCamera =
            parentCanvas != null &&
            parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? parentCanvas.worldCamera
                : null;

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                targetRect.position
            );

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPosition,
                uiCamera,
                out Vector2 targetPosition
            );

        Sequence sequence =
            DOTween.Sequence();

        sequence
            .Join(
                cardRect
                    .DOAnchorPos(
                        targetPosition,
                        flyDuration
                    )
                    .SetEase(Ease.InQuad)
            )
            .Join(
                cardRect
                    .DOLocalRotate(
                        new Vector3(
                            0f,
                            0f,
                            rotateAngle
                        ),
                        flyDuration,
                        RotateMode.FastBeyond360
                    )
                    .SetEase(Ease.InOutQuad)
            )
            .Join(
                cardRect
                    .DOScale(
                        endScale,
                        flyDuration
                    )
                    .SetEase(Ease.InBack)
            )
            .OnComplete(() =>
            {
                // 버린 카드 데이터 등록
                if (cardData != null)
                {
                    discardCardUI.AddToDiscardPile(
                        cardData
                    );
                }

                // ⭐ 여기서 절대 SetActive(false) 하지 않는다.
                // ⭐ 여기서 절대 Destroy() 하지 않는다.
                //
                // 연출이 끝났으면 실제 배치 로직으로 넘긴다.
                onComplete?.Invoke();
            });
    }

    public void HideWithAnim()
    {
        // 이미 닫히는 중이면 무시
        if (_isClosing)
            return;

        // 아직 열리는 중이면 닫지 않는다.
        if (_isOpening)
            return;

        if (!_isOpen)
            return;

        _isClosing = true;
        _isOpen = false;

        // 바깥 클릭을 즉시 차단
        if (outsideClickCatcher != null)
        {
            outsideClickCatcher.interactable =
                false;
        }

        Action cancel =
            _onCancel;

        _currentCardView = null;
        _currentData = null;
        _onCancel = null;

        cancel?.Invoke();

        if (panelRoot == null)
            return;

        panelRoot.DOKill();

        Vector2 targetPosition =
            _originalAnchoredPos +
            new Vector2(
                0f,
                startYOffset
            );

        Vector3 targetRotation =
            new Vector3(
                0f,
                startYRotation,
                0f
            );

        Sequence sequence =
            DOTween.Sequence();

        sequence
            .SetTarget(panelRoot)
            .SetLink(panelRoot.gameObject);

        sequence
            .Join(
                panelRoot
                    .DOAnchorPos(
                        targetPosition,
                        animDuration
                    )
                    .SetEase(Ease.InBack)
            )
            .Join(
                panelRoot
                    .DOLocalRotate(
                        targetRotation,
                        animDuration,
                        RotateMode.FastBeyond360
                    )
                    .SetEase(Ease.InCubic)
            )
            .OnComplete(() =>
            {
                _isClosing = false;

                if (panelRoot != null)
                {
                    panelRoot.gameObject
                        .SetActive(false);
                }

                if (panelClickBlocker != null)
                {
                    panelClickBlocker.gameObject
                        .SetActive(false);
                }

                if (outsideClickCatcher != null)
                {
                    outsideClickCatcher.gameObject
                        .SetActive(false);

                    outsideClickCatcher.interactable =
                        false;
                }
            });
    }

    public void Hide()
    {
        // 열리는 중에는 절대 닫지 않는다.
        if (_isOpening)
            return;

        // 닫히는 중이면 무시
        if (_isClosing)
            return;

        // 이미 닫혀 있으면 무시
        if (!_isOpen)
            return;

        _isOpen = false;
        _isClosing = true;

        if (outsideClickCatcher != null)
        {
            outsideClickCatcher.interactable =
                false;

            outsideClickCatcher.gameObject
                .SetActive(false);
        }

        Action cancel =
            _onCancel;

        _currentCardView = null;
        _currentData = null;
        _onCancel = null;

        cancel?.Invoke();

        if (panelRoot != null)
        {
            panelRoot.DOKill();

            panelRoot.gameObject
                .SetActive(false);
        }

        if (panelClickBlocker != null)
        {
            panelClickBlocker.gameObject
                .SetActive(false);
        }

        _isClosing = false;
    }

    private void HideImmediate()
    {
        if (panelRoot != null)
        {
            panelRoot.DOKill();

            panelRoot.anchoredPosition =
                _originalAnchoredPos;

            panelRoot.localRotation =
                Quaternion.identity;

            panelRoot.gameObject
                .SetActive(false);
        }

        if (outsideClickCatcher != null)
        {
            outsideClickCatcher.gameObject
                .SetActive(false);

            outsideClickCatcher.interactable =
                false;
        }

        if (panelClickBlocker != null)
        {
            panelClickBlocker.gameObject
                .SetActive(false);
        }

        _currentCardView = null;
        _currentData = null;
        _onCancel = null;

        _isOpening = false;
        _isClosing = false;
        _isOpen = false;
    }
}