using System;
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using _Scripts.LSO.UI.Panel;
public enum KTH_Axis3D
{
    X,
    Y,
    Z
}

[RequireComponent(typeof(Collider))]
public class KTH_HandCard : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Card Visual")]
    [SerializeField] private SpriteRenderer outlineImage;
    [SerializeField] private TextMeshPro cost;

    [Header("Select Animation Settings")]
    [SerializeField] private float selectScale = 1.2f;
    [Tooltip("선택됐을 때 카드가 어느 축으로 이동할지")]
    [SerializeField] private KTH_Axis3D selectMoveAxis = KTH_Axis3D.Y;
    [Tooltip("선택됐을 때 위 축 방향으로 얼마나 이동할지")]
    [SerializeField] private float selectMoveAmount = 0.2f;
    [SerializeField] private float selectDuration = 0.12f;

    [Header("Hover Settings")]
    [SerializeField] private float hoverEnterDelay = 0.01f;
    [SerializeField] private float hoverExitDelay = 0.08f;
    [SerializeField] private float infoPanelHoverDelay = 0.5f;

    [Header("Draw Animation Settings")]
    [SerializeField] private float drawStartScale = 0.3f;
    [SerializeField] private float drawDipDistance = 0.5f;
    [SerializeField] private float drawHookDistance = 0.2f;

    [Header("Double Click / Move Down Settings")]
    [Tooltip("더블클릭 시 '선택되지 않은' 나머지 카드가 어느 축으로 내려갈지")]
    [SerializeField] private KTH_Axis3D moveDownAxis = KTH_Axis3D.Y;
    [Tooltip("더블클릭 시 나머지 카드가 얼마나 내려갈지")]
    [SerializeField] private float moveDownAmount = 1.0f;
    [SerializeField] private float moveDownDuration = 0.2f;
    [SerializeField] private Ease moveDownEase = Ease.OutCubic;

    private LSO_WillPanel willPanel;
    private LSO_CardSO cardData;

    private bool isSelected;
    private bool isConfirmed;
    private bool isPlacementMode;
    private bool isPointerOver;
    private bool isMovedDown;

    private Vector3 originalLocalPos;
    private Vector3 originalLocalRot;

    private Tween hoverEnterTween;
    private Tween hoverExitTween;
    private Tween infoPanelTween;

    private KTH_CardSorting cardSorting;
    private Collider cardCollider;

    private static KTH_HandCard currentSelectedCard;

    // 더블클릭 시 "나머지 카드"를 내려보내기 위해 현재 존재하는 모든 핸드카드를 추적
    private static readonly List<KTH_HandCard> allHandCards = new List<KTH_HandCard>();

    public LSO_CardSO CardData => cardData;
    public bool IsSelected => isSelected;
    public bool IsConfirmed => isConfirmed;
    public bool IsPlacementMode => isPlacementMode;
    public bool IsMovedDown => isMovedDown;
    public float SelectScale => selectScale;

    public static bool HasConfirmedSelection =>
        currentSelectedCard != null &&
        currentSelectedCard.isConfirmed;

    public event Action<KTH_HandCard> OnCardClicked;

    /// <summary>
    /// 카드가 더블클릭됐을 때 발생하는 static 이벤트.
    /// 파라미터로 더블클릭된 카드(KTH_HandCard)가 전달됨.
    /// 외부 오브젝트는 OnEnable/OnDisable에서 구독/해제하면 됨.
    /// </summary>
    public static event Action<KTH_HandCard> OnCardDoubleClicked;

    private void Awake()
    {
        cardSorting = GetComponent<KTH_CardSorting>();
        cardCollider = GetComponent<Collider>();

        if (!allHandCards.Contains(this))
        {
            allHandCards.Add(this);
        }
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

        // 이름 / 아이콘 / 공격력은 LSO_RewardPieceCard 계열 컴포넌트가 그린다.
        // (이 카드 프리팹에 같이 붙어있는 걸 전제로 함)

        if (cost != null)
            cost.text = $"{cardData.Animal.cost}";

        if (outlineImage != null)
            outlineImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// KTH_HandCardLayout 등 외부에서 이 카드를 시각적으로 맨 앞에 오게 하고 싶을 때 호출.
    /// (구 UI 버전의 transform.SetAsLastSibling() 대체)
    /// </summary>
    public void BringToFront()
    {
        cardSorting?.BringToFront();
    }

    // ============================================================
    // Hover
    // ============================================================

    public void OnPointerEnter(PointerEventData eventData)
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
            return;

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
            return;

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

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;

        KillHoverEnterTween();
        KillInfoPanelTween();

        if (isConfirmed)
            return;

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

    // ============================================================
    // Click
    // ============================================================

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }

        // ==================================================
        // 더블클릭 처리 (Unity가 판정한 clickCount 사용)
        // 더블클릭이면 기존 단일클릭(선택/확정) 로직을 타지 않고 별도 처리 후 종료.
        // ==================================================
        if (eventData.clickCount >= 2)
        {
            HandleDoubleClick();
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

    // ============================================================
    // Double Click
    // ============================================================

    /// <summary>
    /// 더블클릭된 카드(this)를 제외한 나머지 모든 핸드카드를 아래로 내리고,
    /// OnCardDoubleClicked 이벤트를 발생시켜 구독중인 오브젝트가 반응하도록 함.
    /// </summary>
    private void HandleDoubleClick()
    {
        for (int i = 0; i < allHandCards.Count; i++)
        {
            KTH_HandCard card = allHandCards[i];

            if (card == null ||
                card == this)
            {
                continue;
            }

            card.PlayMoveDownAnimation();
        }

        OnCardDoubleClicked?.Invoke(this);
    }

    /// <summary>
    /// 현재 내려가 있는 모든 카드를 원래 위치로 복구.
    /// (더블클릭 상태를 해제하고 싶을 때 외부에서 호출)
    /// </summary>
    public static void RestoreAllCards()
    {
        for (int i = 0; i < allHandCards.Count; i++)
        {
            allHandCards[i]?.PlayMoveUpAnimation();
        }
    }

    /// <summary>
    /// 이 카드를 moveDownAxis 방향으로 moveDownAmount 만큼 내리는 애니메이션.
    /// </summary>
    public void PlayMoveDownAnimation()
    {
        if (isMovedDown)
            return;

        isMovedDown = true;

        Vector3 targetPos = originalLocalPos;

        switch (moveDownAxis)
        {
            case KTH_Axis3D.X:
                targetPos.x -= moveDownAmount;
                break;

            case KTH_Axis3D.Y:
                targetPos.y -= moveDownAmount;
                break;

            case KTH_Axis3D.Z:
                targetPos.z -= moveDownAmount;
                break;
        }

        transform.DOKill();

        transform
            .DOLocalMove(
                targetPos,
                moveDownDuration
            )
            .SetEase(moveDownEase)
            .SetTarget(transform);
    }

    /// <summary>
    /// 내려갔던 카드를 원래 위치(originalLocalPos)로 복구.
    /// </summary>
    public void PlayMoveUpAnimation()
    {
        if (!isMovedDown)
            return;

        isMovedDown = false;

        transform.DOKill();

        transform
            .DOLocalMove(
                originalLocalPos,
                moveDownDuration
            )
            .SetEase(moveDownEase)
            .SetTarget(transform);
    }

    // ============================================================
    // Selection
    // ============================================================

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
            return;

        hoverEnterTween.Kill();
        hoverEnterTween = null;
    }

    private void KillHoverExitTween()
    {
        if (hoverExitTween == null)
            return;

        hoverExitTween.Kill();
        hoverExitTween = null;
    }

    private void KillInfoPanelTween()
    {
        if (infoPanelTween == null)
            return;

        infoPanelTween.Kill();
        infoPanelTween = null;
    }

    public void SetSelected(bool value)
    {
        if (isSelected == value)
            return;

        isSelected = value;

        transform.DOKill();

        if (outlineImage != null)
        {
            outlineImage.gameObject.SetActive(
                isSelected
            );
        }

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
            return;

        Vector3 targetPos =
            originalLocalPos;

        switch (selectMoveAxis)
        {
            case KTH_Axis3D.X:
                targetPos.x += selectMoveAmount;
                break;

            case KTH_Axis3D.Y:
                targetPos.y += selectMoveAmount;
                break;

            case KTH_Axis3D.Z:
                targetPos.z += selectMoveAmount;
                break;
        }

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
                    originalLocalRot,
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

    // ============================================================
    // Spawn
    // ============================================================

    public void SetSpawnPosition(Vector3 worldPos)
    {
        transform.position = worldPos;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    // ============================================================
    // Pool Reset
    // ============================================================

    public void ResetForPool()
    {
        transform.DOKill(true);

        KillHoverEnterTween();
        KillHoverExitTween();
        KillInfoPanelTween();

        isSelected = false;
        isConfirmed = false;
        isPlacementMode = false;
        isPointerOver = false;
        isMovedDown = false;

        if (currentSelectedCard == this)
        {
            currentSelectedCard = null;
        }

        if (outlineImage != null)
        {
            outlineImage.gameObject.SetActive(false);
        }

        enabled = true;

        if (cardSorting == null)
        {
            cardSorting = GetComponent<KTH_CardSorting>();
        }

        if (cardSorting != null)
        {
            cardSorting.enabled = true;
        }

        if (cardCollider == null)
        {
            cardCollider = GetComponent<Collider>();
        }

        if (cardCollider != null)
        {
            cardCollider.enabled = true;
        }

        ResetRendererAlpha(outlineImage);

        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;

        cardData = null;
        willPanel = null;
    }

    private static void ResetRendererAlpha(
        SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Color color = renderer.color;
        color.a = 1f;
        renderer.color = color;
    }

    // ============================================================
    // Original Transform
    // ============================================================

    public void SetOriginalTransformSilently(
        Vector3 pos,
        Vector3 rot)
    {
        originalLocalPos = pos;
        originalLocalRot = rot;
    }

    public void UpdateOriginalTransform(
        Vector3 pos,
        Vector3 rot)
    {
        originalLocalPos = pos;
        originalLocalRot = rot;
    }

    // ============================================================
    // Rearrange
    // ============================================================

    public void MoveToHandPositionWithDelay(
        Vector3 targetPos,
        Vector3 targetRot,
        float duration,
        float delay,
        Ease ease)
    {
        if (isSelected)
            return;

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
                    targetRot,
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

    // ============================================================
    // Draw Animation
    // ============================================================

    public void PlayDrawAnimation(
        Vector3 targetLocalPos,
        Vector3 targetLocalRot,
        float duration = 0.4f)
    {
        originalLocalPos = targetLocalPos;
        originalLocalRot = targetLocalRot;

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
                    targetLocalRot,
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

    // ============================================================
    // Consume / Discard
    // ============================================================

    public void ConsumeAndRearrange(
        KTH_DiscardCardUI discardPile = null,
        Action onComplete = null)
    {
        // 선택 상태 해제
        CancelSelectionState();

        // 현재 카드의 기존 애니메이션 제거
        transform.DOKill(true);

        // ==================================================
        // 손패에서 제거
        // ==================================================

        if (KTH_HandCardLayout.Instance != null)
        {
            KTH_HandCardLayout.Instance.RemoveCard(
                this
            );
        }

        // ==================================================
        // 디스카드 더미가 없는 경우
        // ==================================================

        if (discardPile == null ||
            discardPile.DiscardCardTransform == null)
        {
            ReleaseOrDestroy();

            // 카드 사용 완료만 알림
            onComplete?.Invoke();

            return;
        }

        // ==================================================
        // 디스카드 애니메이션 찾기
        // ==================================================

        KTH_DiscardAnimation discardAnimation =
            discardPile.GetComponent<KTH_DiscardAnimation>();

        if (discardAnimation == null)
        {
            discardAnimation =
                FindAnyObjectByType<KTH_DiscardAnimation>();
        }

        // ==================================================
        // 디스카드 애니메이션이 없는 경우
        // ==================================================

        if (discardAnimation == null)
        {
            discardPile.AddToDiscardPile(
                cardData
            );

            ReleaseOrDestroy();

            onComplete?.Invoke();

            return;
        }

        // ==================================================
        // 디스카드 애니메이션
        // ==================================================

        discardAnimation.Play(
            this,
            discardPile,
            cardData,
            onComplete
        );
    }

    // ============================================================
    // Pool / Destroy
    // ============================================================

    private void ReleaseOrDestroy()
    {
        if (KTH_HandCardPool.Instance != null)
        {
            KTH_HandCardPool.Instance.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
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

        allHandCards.Remove(this);
    }

    public static void DeselectCurrent()
    {
        if (currentSelectedCard == null)
            return;

        KTH_HandCard card =
            currentSelectedCard;

        card.CancelSelectionState();

        if (KTH_InfoPanel.Instance != null)
        {
            KTH_InfoPanel.Instance.CancleInfoPanl();
        }
    }
}
