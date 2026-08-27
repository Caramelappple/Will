using System;
using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using _Scripts.LSO.UI.Panel;

public class KTH_HandCard : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Card UI")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Image outlineImage;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI cost;
    [SerializeField] private TextMeshProUGUI power;

    [Header("Select Animation Settings")]
    [SerializeField] private float selectScale = 1.2f;
    [SerializeField] private float selectMoveY = 25f;
    [SerializeField] private float selectDuration = 0.22f;

    [Header("Hover Settings")]
    [SerializeField] private float hoverEnterDelay = 0.03f;
    [SerializeField] private float hoverExitDelay = 0.12f;
    [SerializeField] private float infoPanelHoverDelay = 0.5f;

    [Header("Draw Animation Settings")]
    [SerializeField] private float drawStartScale = 0.3f;
    [SerializeField] private float drawDipDistance = 60f;
    [SerializeField] private float drawHookDistance = 25f;

    private LSO_WillPanel willPanel;
    private LSO_CardSO cardData;

    private bool isSelected;
    private bool isConfirmed;
    private bool isPlacementMode;
    private bool isPointerOver;

    private Vector3 originalLocalPos;
    private float originalZRotation;

    private Tween hoverEnterTween;
    private Tween hoverExitTween;
    private Tween infoPanelTween;

    private KTH_CardSorting cardSorting;

    private static KTH_HandCard currentSelectedCard;

    public LSO_CardSO CardData => cardData;
    public bool IsSelected => isSelected;
    public bool IsConfirmed => isConfirmed;
    public bool IsPlacementMode => isPlacementMode;
    public float SelectScale => selectScale;

    public static bool HasConfirmedSelection =>
        currentSelectedCard != null &&
        currentSelectedCard.isConfirmed;

    public event Action<KTH_HandCard> OnCardClicked;

    private void Awake()
    {
        cardSorting = GetComponent<KTH_CardSorting>();
    }

    public void Setup(
        LSO_CardSO data,
        LSO_WillPanel panel)
    {
        cardData = data;
        willPanel = panel;

        SettingUi();
    }

    private void Start()
    {
        if (cardData != null)
        {
            SettingUi();
        }
    }

    public void SettingUi()
    {
        if (cardData == null)
        {
            return;
        }

        cardImage.sprite = cardData.Image;
        title.text = cardData.Animal.animalName;
        cost.text = $"{cardData.Animal.cost}";
        power.text = $"{cardData.Animal.damage}";

        outlineImage.gameObject.SetActive(false);
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        isPointerOver = true;

        KillHoverExitTween();

        if (willPanel != null &&
            willPanel.IsSelecting)
        {
            return;
        }

        if (currentSelectedCard != null &&
            currentSelectedCard != this &&
            currentSelectedCard.isConfirmed)
        {
            return;
        }

        if (cardData == null ||
            isSelected)
        {
            return;
        }

        KillHoverEnterTween();

        hoverEnterTween =
            DOVirtual.DelayedCall(
                hoverEnterDelay,
                HandleHoverEnter
            );
    }

    private void HandleHoverEnter()
    {
        hoverEnterTween = null;

        if (!isPointerOver)
        {
            return;
        }

        if (willPanel != null &&
            willPanel.IsSelecting)
        {
            return;
        }

        if (currentSelectedCard != null &&
            currentSelectedCard != this &&
            currentSelectedCard.isConfirmed)
        {
            return;
        }

        if (cardData == null ||
            isSelected)
        {
            return;
        }

        SetSelected(true);

        StartInfoPanelDelay();
    }

    private void StartInfoPanelDelay()
    {
        KillInfoPanelTween();

        if (isConfirmed)
        {
            return;
        }

        infoPanelTween =
            DOVirtual.DelayedCall(
                infoPanelHoverDelay,
                ShowHoverInfo
            );
    }

    private void ShowHoverInfo()
    {
        infoPanelTween = null;

        if (!isPointerOver ||
            !isSelected ||
            isConfirmed ||
            cardData == null)
        {
            return;
        }

        if (KTH_InfoPanel.Instance != null)
        {
            KTH_InfoPanel.Instance.StartHoverInfo(
                cardData,
                this
            );
        }
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        isPointerOver = false;

        KillHoverEnterTween();
        KillInfoPanelTween();

        if (isConfirmed)
        {
            return;
        }

        KillHoverExitTween();

        hoverExitTween =
            DOVirtual.DelayedCall(
                hoverExitDelay,
                HandleHoverExit
            );
    }

    private void HandleHoverExit()
    {
        hoverExitTween = null;

        if (isPointerOver ||
            isConfirmed)
        {
            return;
        }

        if (KTH_InfoPanel.Instance != null)
        {
            KTH_InfoPanel.Instance.CancelHoverSelection(
                this
            );
        }

        if (isSelected)
        {
            CancelSelectionState();
        }
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (eventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }

        if (willPanel != null &&
            willPanel.IsSelecting)
        {
            return;
        }

        KillHoverEnterTween();
        KillHoverExitTween();
        KillInfoPanelTween();

        if (currentSelectedCard != null &&
            currentSelectedCard != this)
        {
            currentSelectedCard.CancelSelectionState();
        }

        if (isConfirmed)
        {
            CancelSelectionState();

            if (KTH_InfoPanel.Instance != null)
            {
                KTH_InfoPanel.Instance.CancleInfoPanl();
            }

            OnCardClicked?.Invoke(this);
            return;
        }

        isConfirmed = true;
        isPlacementMode = true;

        currentSelectedCard = this;

        cardSorting?.BringToFront();

        SetSelected(true);

        KTH_HandCardLayout.Instance?.EnterPlacementMode(
            this
        );

        transform.SetAsLastSibling();

        if (KTH_InfoPanel.Instance != null)
        {
            if (KTH_InfoPanel.Instance.CurrentCard != this)
            {
                KTH_InfoPanel.Instance.StartInfoPanl(
                    cardData,
                    this
                );
            }

            KTH_InfoPanel.Instance.SelectInfoPanl();
        }

        OnCardClicked?.Invoke(this);
    }

    public void CancelSelectionState()
    {
        bool wasPlacementMode = isPlacementMode;

        isConfirmed = false;
        isPlacementMode = false;

        if (currentSelectedCard == this)
        {
            currentSelectedCard = null;
        }

        KillHoverEnterTween();
        KillHoverExitTween();
        KillInfoPanelTween();

        SetSelected(false);

        if (wasPlacementMode)
        {
            KTH_HandCardLayout.Instance?.ExitPlacementMode();
        }
    }

    private void KillHoverEnterTween()
    {
        if (hoverEnterTween == null)
        {
            return;
        }

        hoverEnterTween.Kill();
        hoverEnterTween = null;
    }

    private void KillHoverExitTween()
    {
        if (hoverExitTween == null)
        {
            return;
        }

        hoverExitTween.Kill();
        hoverExitTween = null;
    }

    private void KillInfoPanelTween()
    {
        if (infoPanelTween == null)
        {
            return;
        }

        infoPanelTween.Kill();
        infoPanelTween = null;
    }

    public void SetSelected(bool value)
    {
        if (isSelected == value)
        {
            return;
        }

        isSelected = value;

        transform.DOKill();

        outlineImage.gameObject.SetActive(
            isSelected
        );

        if (isSelected)
        {
            cardSorting?.BringToFront();
            PlaySelectAnimation();
        }
        else
        {
            PlayDeselectAnimation();
            cardSorting?.RestoreSorting();
        }

        if (KTH_HandCardLayout.Instance != null)
        {
            KTH_HandCardLayout.Instance
                .OnCardSelectionChanged(
                    this,
                    isSelected
                );
        }
    }

    private void PlaySelectAnimation()
    {
        if (isPlacementMode)
        {
            return;
        }

        Vector3 targetPos =
            originalLocalPos;

        targetPos.y += selectMoveY;

        Sequence sequence =
            DOTween.Sequence();

        sequence.SetTarget(transform);

        sequence.Join(
            transform
                .DOLocalMove(
                    targetPos,
                    selectDuration
                )
                .SetEase(
                    Ease.OutBack,
                    0.7f
                )
        );

        sequence.Join(
            transform
                .DOScale(
                    Vector3.one * selectScale,
                    selectDuration
                )
                .SetEase(
                    Ease.OutBack,
                    0.7f
                )
        );
    }

    private void PlayDeselectAnimation()
    {
        Sequence sequence =
            DOTween.Sequence();

        sequence.SetTarget(transform);

        sequence.Join(
            transform
                .DOLocalMove(
                    originalLocalPos,
                    selectDuration
                )
                .SetEase(Ease.OutCubic)
        );

        sequence.Join(
            transform
                .DOLocalRotate(
                    new Vector3(
                        0f,
                        0f,
                        originalZRotation
                    ),
                    selectDuration
                )
                .SetEase(Ease.OutCubic)
        );

        sequence.Join(
            transform
                .DOScale(
                    Vector3.one,
                    selectDuration
                )
                .SetEase(Ease.OutCubic)
        );
    }

    public void SetSpawnPosition(
        Vector3 worldPos)
    {
        transform.position = worldPos;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    public void SetOriginalTransformSilently(
        Vector3 pos,
        float zRot)
    {
        originalLocalPos = pos;
        originalZRotation = zRot;
    }

    public void UpdateOriginalTransform(
        Vector3 pos,
        float zRot)
    {
        originalLocalPos = pos;
        originalZRotation = zRot;
    }

    public void MoveToHandPositionWithDelay(
        Vector3 targetPos,
        float targetRotZ,
        float duration,
        float delay,
        Ease ease)
    {
        if (isSelected)
        {
            return;
        }

        transform.DOKill();

        Sequence sequence =
            DOTween.Sequence();

        sequence.SetTarget(transform);

        sequence.Join(
            transform
                .DOLocalMove(
                    targetPos,
                    duration
                )
                .SetDelay(delay)
                .SetEase(ease)
        );

        sequence.Join(
            transform
                .DOLocalRotate(
                    new Vector3(
                        0f,
                        0f,
                        targetRotZ
                    ),
                    duration
                )
                .SetDelay(delay)
                .SetEase(ease)
        );

        sequence.Join(
            transform
                .DOScale(
                    Vector3.one,
                    duration
                )
                .SetDelay(delay)
                .SetEase(ease)
        );
    }

    public void PlayDrawAnimation(
        Vector3 targetLocalPos,
        float targetZRotation,
        float duration = 0.4f)
    {
        originalLocalPos = targetLocalPos;
        originalZRotation = targetZRotation;

        transform.DOKill();

        transform.localScale =
            Vector3.one * drawStartScale;

        Vector3 startPos =
            transform.localPosition;

        Vector3 midPos =
            Vector3.Lerp(
                startPos,
                targetLocalPos,
                0.5f
            );

        midPos.y -= drawDipDistance;

        Vector3 preTargetPos =
            targetLocalPos;

        preTargetPos.y -= drawHookDistance;

        preTargetPos.x -=
            (targetLocalPos.x - startPos.x) *
            0.08f;

        Vector3[] path =
        {
            startPos,
            midPos,
            preTargetPos,
            targetLocalPos
        };

        Sequence sequence =
            DOTween.Sequence();

        sequence.SetTarget(transform);

        sequence.Join(
            transform
                .DOLocalPath(
                    path,
                    duration,
                    PathType.CatmullRom
                )
                .SetEase(Ease.InOutSine)
        );

        sequence.Join(
            transform
                .DOLocalRotate(
                    new Vector3(
                        0f,
                        0f,
                        targetZRotation
                    ),
                    duration
                )
                .SetEase(Ease.OutCubic)
        );

        sequence.Join(
            transform
                .DOScale(
                    Vector3.one,
                    duration
                )
                .SetEase(Ease.OutBack)
        );
    }

    public void ConsumeAndRearrange(
        KTH_DiscardCardUI discardPile = null)
    {
        CancelSelectionState();

        transform.DOKill(true);

        if (KTH_HandCardLayout.Instance != null)
        {
            KTH_HandCardLayout.Instance.RemoveCard(
                this
            );
        }

        if (discardPile == null ||
            discardPile.DiscardCardTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        KTH_DiscardAnimation discardAnimation =
            discardPile.GetComponent<KTH_DiscardAnimation>();

        if (discardAnimation == null)
        {
            discardAnimation =
                FindAnyObjectByType<KTH_DiscardAnimation>();
        }

        if (discardAnimation == null)
        {
            discardPile.AddToDiscardPile(
                cardData
            );

            Destroy(gameObject);
            return;
        }

        discardAnimation.Play(
            this,
            discardPile,
            cardData
        );
    }

    private void OnDestroy()
    {
        KillHoverEnterTween();
        KillHoverExitTween();
        KillInfoPanelTween();

        transform.DOKill();

        if (currentSelectedCard == this)
        {
            currentSelectedCard = null;
        }
    }

    public static void DeselectCurrent()
    {
        if (currentSelectedCard == null)
        {
            return;
        }

        KTH_HandCard card =
            currentSelectedCard;

        card.CancelSelectionState();

        if (KTH_InfoPanel.Instance != null)
        {
            KTH_InfoPanel.Instance.CancleInfoPanl();
        }
    }
}