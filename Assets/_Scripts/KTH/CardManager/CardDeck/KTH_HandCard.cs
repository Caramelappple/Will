using System;
using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    [Header("Draw Animation Settings")]
    [SerializeField] private float drawStartScale = 0.3f;
    [SerializeField] private float drawDipDistance = 60f;
    [SerializeField] private float drawHookDistance = 25f;

    [Header("Discard Animation Settings")]
    [SerializeField] private float discardDuration = 0.35f;

    private LSO_CardSO cardData;

    private bool isSelected;
    private bool isConfirmed;

    private Vector3 originalLocalPos;
    private float originalZRotation;

    private Tween hoverEnterTween;
    private Tween hoverExitTween;

    private static KTH_HandCard currentSelectedCard;

    public LSO_CardSO CardData => cardData;
    public bool IsSelected => isSelected;
    public bool IsConfirmed => isConfirmed;
    public float SelectScale => selectScale;

    public static bool HasConfirmedSelection =>
        currentSelectedCard != null;

    public event Action<KTH_HandCard> OnCardClicked;

    // =========================================================
    // Setup
    // =========================================================

    public void Setup(LSO_CardSO data)
    {
        cardData = data;
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

    // =========================================================
    // Hover
    // =========================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        KillHoverExitTween();

        if (currentSelectedCard != null)
        {
            return;
        }

        if (cardData == null)
        {
            return;
        }

        if (isSelected)
        {
            return;
        }

        KillHoverEnterTween();

        hoverEnterTween = DOVirtual.DelayedCall(
            hoverEnterDelay,
            HandleHoverEnter
        );
    }

    private void HandleHoverEnter()
    {
        hoverEnterTween = null;

        if (currentSelectedCard != null)
        {
            return;
        }

        if (cardData == null)
        {
            return;
        }

        if (isSelected)
        {
            return;
        }

        // 카드 선택 연출
        SetSelected(true);

        // 한 번의 호버로
        // 인포 패널 표시 + 배치 시작
        if (KTH_InfoPanel.Instance != null)
        {
            KTH_InfoPanel.Instance.StartHoverPlacement(
                cardData,
                this
            );
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        KillHoverEnterTween();

        if (currentSelectedCard != null)
        {
            return;
        }

        if (isConfirmed)
        {
            return;
        }

        KillHoverExitTween();

        hoverExitTween = DOVirtual.DelayedCall(
            hoverExitDelay,
            HandleHoverExit
        );
    }

    private void HandleHoverExit()
    {
        hoverExitTween = null;

        if (currentSelectedCard != null)
        {
            return;
        }

        if (isConfirmed)
        {
            return;
        }

        if (KTH_InfoPanel.Instance != null)
        {
            KTH_InfoPanel.Instance.CancelHoverSelection(this);
        }

        if (isSelected)
        {
            CancelSelectionState();
        }
    }

    // =========================================================
    // Click
    // =========================================================

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        KillHoverEnterTween();
        KillHoverExitTween();

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
        }
        else
        {
            isConfirmed = true;
            currentSelectedCard = this;

            SetSelected(true);

            if (KTH_InfoPanel.Instance != null)
            {
                KTH_InfoPanel.Instance.StartInfoPanl(
                    cardData,
                    this
                );
            }
        }

        OnCardClicked?.Invoke(this);
    }

    // =========================================================
    // Complete Selection State Reset
    // =========================================================

    /// <summary>
    /// 선택과 관련된 모든 상태를 완전히 초기화한다.
    /// SetSelected(false)와 달리 클릭 확정 상태와
    /// static currentSelectedCard까지 함께 제거한다.
    /// </summary>
    public void CancelSelectionState()
    {
        isConfirmed = false;

        if (currentSelectedCard == this)
        {
            currentSelectedCard = null;
        }

        KillHoverEnterTween();
        KillHoverExitTween();

        SetSelected(false);
    }

    // =========================================================
    // Tween
    // =========================================================

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

    // =========================================================
    // Select
    // =========================================================

    public void SetSelected(bool value)
    {
        if (isSelected == value)
        {
            return;
        }

        isSelected = value;

        transform.DOKill();

        outlineImage.gameObject.SetActive(isSelected);

        if (isSelected)
        {
            PlaySelectAnimation();
        }
        else
        {
            PlayDeselectAnimation();
        }

        if (KTH_HandCardLayout.Instance != null)
        {
            KTH_HandCardLayout.Instance.OnCardSelectionChanged(
                this,
                isSelected
            );
        }
    }

    private void PlaySelectAnimation()
    {
        Vector3 targetPos = originalLocalPos;
        targetPos.y += selectMoveY;

        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(transform);

        sequence.Join(
            transform
                .DOLocalMove(targetPos, selectDuration)
                .SetEase(Ease.OutBack, 0.7f)
        );

        sequence.Join(
            transform
                .DOScale(
                    Vector3.one * selectScale,
                    selectDuration
                )
                .SetEase(Ease.OutBack, 0.7f)
        );

        sequence.Join(
            transform
                .DOLocalRotate(
                    Vector3.zero,
                    selectDuration
                )
                .SetEase(Ease.OutBack, 0.7f)
        );
    }

    private void PlayDeselectAnimation()
    {
        Sequence sequence = DOTween.Sequence();
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

    // =========================================================
    // Spawn
    // =========================================================

    public void SetSpawnPosition(Vector3 worldPos)
    {
        transform.position = worldPos;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    // =========================================================
    // Layout
    // =========================================================

    public void UpdateOriginalTransform(
        Vector3 pos,
        float zRot)
    {
        originalLocalPos = pos;
        originalZRotation = zRot;

        if (!isSelected)
        {
            return;
        }

        Vector3 selectedPos = originalLocalPos;
        selectedPos.y += selectMoveY;

        transform.DOKill();

        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(transform);

        sequence.Join(
            transform
                .DOLocalMove(selectedPos, 0.15f)
                .SetEase(Ease.OutCubic)
        );

        sequence.Join(
            transform
                .DOLocalRotate(
                    Vector3.zero,
                    0.15f
                )
                .SetEase(Ease.OutCubic)
        );
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

        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(transform);

        sequence.Join(
            transform
                .DOLocalMove(targetPos, duration)
                .SetDelay(delay)
                .SetEase(ease)
        );

        sequence.Join(
            transform
                .DOLocalRotate(
                    new Vector3(0f, 0f, targetRotZ),
                    duration
                )
                .SetDelay(delay)
                .SetEase(ease)
        );

        sequence.Join(
            transform
                .DOScale(Vector3.one, duration)
                .SetDelay(delay)
                .SetEase(ease)
        );
    }

    // =========================================================
    // Draw
    // =========================================================

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

        Vector3 startPos = transform.localPosition;

        Vector3 midPos = Vector3.Lerp(
            startPos,
            targetLocalPos,
            0.5f
        );

        midPos.y -= drawDipDistance;

        Vector3 preTargetPos = targetLocalPos;
        preTargetPos.y -= drawHookDistance;

        preTargetPos.x -=
            (targetLocalPos.x - startPos.x) * 0.08f;

        Vector3[] path =
        {
            startPos,
            midPos,
            preTargetPos,
            targetLocalPos
        };

        Sequence sequence = DOTween.Sequence();
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

    // =========================================================
    // Consume
    // =========================================================

    public void ConsumeAndRearrange(
        KTH_DiscardCardUI discardPile = null)
    {
        CancelSelectionState();

        transform.DOKill(true);

        if (KTH_HandCardLayout.Instance != null)
        {
            KTH_HandCardLayout.Instance.RemoveCard(this);
        }

        if (discardPile != null &&
            discardPile.DiscardCardTransform != null)
        {
            PlayDiscardAnimation(discardPile);
        }
        else
        {
            Debug.LogWarning(
                $"[KTH_HandCard] discardPile이 비어있어 " +
                $"'{(cardData != null ? cardData.name : "Unknown")}' " +
                $"카드가 버린 카드 더미에 기록되지 않고 파괴됩니다!"
            );

            Destroy(gameObject);
        }
    }

    // =========================================================
    // Discard
    // =========================================================

    private void PlayDiscardAnimation(
        KTH_DiscardCardUI discardPile)
    {
        CanvasGroup canvasGroup =
            GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;

        Transform discardParent =
            discardPile.DiscardCardTransform.parent;

        transform.SetParent(
            discardParent,
            true
        );

        Vector3 targetLocalPos =
            discardPile
                .DiscardCardTransform
                .localPosition;

        float randomTilt =
            UnityEngine.Random.Range(
                -30f,
                30f
            );

        Sequence sequence = DOTween.Sequence();

        sequence.Join(
            transform
                .DOLocalMove(
                    targetLocalPos,
                    discardDuration
                )
                .SetEase(Ease.InQuad)
        );

        sequence.Join(
            transform
                .DOScale(
                    Vector3.zero,
                    discardDuration
                )
                .SetEase(Ease.InQuad)
        );

        sequence.Join(
            transform
                .DOLocalRotate(
                    new Vector3(
                        0f,
                        0f,
                        randomTilt
                    ),
                    discardDuration,
                    RotateMode.FastBeyond360
                )
                .SetEase(Ease.OutQuad)
        );

        sequence.Join(
            canvasGroup
                .DOFade(
                    0f,
                    discardDuration * 0.85f
                )
                .SetEase(Ease.InQuad)
        );

        sequence.OnComplete(() =>
        {
            discardPile.AddToDiscardPile(cardData);
            Destroy(gameObject);
        });
    }

    // =========================================================
    // Destroy
    // =========================================================

    private void OnDestroy()
    {
        KillHoverEnterTween();
        KillHoverExitTween();

        transform.DOKill();

        if (currentSelectedCard == this)
        {
            currentSelectedCard = null;
        }
    }

    // =========================================================
    // Static Deselect
    // =========================================================

    public static void DeselectCurrent()
    {
        if (currentSelectedCard == null)
        {
            return;
        }

        KTH_HandCard card = currentSelectedCard;

        card.CancelSelectionState();

        if (KTH_InfoPanel.Instance != null)
        {
            KTH_InfoPanel.Instance.CancleInfoPanl();
        }
    }
}