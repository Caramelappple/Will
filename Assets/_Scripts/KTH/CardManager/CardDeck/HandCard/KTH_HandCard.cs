using System;
using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using _Scripts.LSO.UI.Panel;

public enum KTH_Axis3D
{
    X,
    Y,
    Z
}

// 이 클래스는 손패 카드 하나의 "얼굴"이다. 실제 동작은 대부분 아래 컨트롤러들에 위임한다.
//   - KTH_HandCardHoverController     : 마우스 오버 진입/이탈, 정보 패널 호버
//   - KTH_HandCardSelectionController : 선택/확정(배치 모드) 상태 기계 + 선택 연출
//   - KTH_HandCardDoubleClickController : 더블클릭 시 나머지 카드 내리기
//   - KTH_HandCardMotionAnimator      : 스폰 위치, 드로우 연출, 재정렬 이동
//   - KTH_HandCardDiscardHandler      : 카드 소모(버림/반납) 흐름
// 이 클래스 자체는 Collider/EventSystem 인터페이스가 필요해서 MonoBehaviour로 남아있고,
// 인스펙터 설정값(아래 SerializeField들)도 그대로 여기 있다 - 프리팹 값을 그대로 쓰기 위해서다.
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(KTH_InitCardData))]
public class KTH_HandCard : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
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

    private Vector3 originalLocalPos;
    private Vector3 originalLocalRot;

    private KTH_CardSorting cardSorting;
    private Collider cardCollider;
    private KTH_InitCardData initCardData;

    private KTH_HandCardHoverController hoverController;
    private KTH_HandCardSelectionController selectionController;
    private KTH_HandCardDoubleClickController doubleClickController;
    private KTH_HandCardMotionAnimator motionAnimator;

    public LSO_CardSO CardData => cardData;
    public bool IsSelected => selectionController.IsSelected;
    public bool IsConfirmed => selectionController.IsConfirmed;
    public bool IsPlacementMode => selectionController.IsPlacementMode;
    public bool IsMovedDown => doubleClickController.IsMovedDown;
    public float SelectScale => selectScale;

    // 컨트롤러들이 원래 자리(손패에서의 정위치)를 읽을 때 쓴다.
    internal Vector3 OriginalLocalPosition => originalLocalPos;
    internal Vector3 OriginalLocalRotation => originalLocalRot;
    internal LSO_WillPanel WillPanel => willPanel;

    public static bool HasConfirmedSelection => KTH_HandCardSelectionController.HasConfirmedSelection;

    public event Action<KTH_HandCard> OnCardClicked;

    /// <summary>
    /// 카드가 더블클릭됐을 때 발생하는 static 이벤트.
    /// 파라미터로 더블클릭된 카드(KTH_HandCard)가 전달됨.
    /// 외부 오브젝트는 OnEnable/OnDisable에서 구독/해제하면 됨.
    /// </summary>
    public static event Action<KTH_HandCard> OnCardDoubleClicked
    {
        add => KTH_HandCardDoubleClickController.OnCardDoubleClicked += value;
        remove => KTH_HandCardDoubleClickController.OnCardDoubleClicked -= value;
    }

    /// <summary>
    /// 더블클릭으로 활성화됐던 상태가 취소됐을 때 발생하는 static 이벤트.
    /// (같은 카드를 다시 더블클릭했거나, 다른 카드로 넘어간 경우 둘 다.)
    /// 파라미터로 "방금까지" 활성화돼 있던 카드가 전달됨.
    /// OnCardDoubleClicked를 구독해 뭔가를 켰다면 이 이벤트에서 반드시 다시 꺼야 한다.
    /// </summary>
    public static event Action<KTH_HandCard> OnCardDoubleClickCancelled
    {
        add => KTH_HandCardDoubleClickController.OnCardDoubleClickCancelled += value;
        remove => KTH_HandCardDoubleClickController.OnCardDoubleClickCancelled -= value;
    }

    /// <summary>
    /// 지금 더블클릭으로 활성화된 카드가 있으면 취소한다. 없으면 아무 일도 하지 않는다.
    /// 실제로 뭔가를 취소했으면 true를 반환한다.
    /// </summary>
    public static bool CancelDoubleClick()
    {
        return KTH_HandCardDoubleClickController.CancelActive();
    }

    private void Awake()
    {
        cardSorting = GetComponent<KTH_CardSorting>();
        cardCollider = GetComponent<Collider>();
        initCardData = GetComponent<KTH_InitCardData>();

        hoverController = new KTH_HandCardHoverController(
            this, hoverEnterDelay, hoverExitDelay, infoPanelHoverDelay);

        selectionController = new KTH_HandCardSelectionController(
            this, selectMoveAxis, selectMoveAmount, selectDuration);

        doubleClickController = new KTH_HandCardDoubleClickController(
            this, moveDownAxis, moveDownAmount, moveDownDuration, moveDownEase);

        motionAnimator = new KTH_HandCardMotionAnimator(
            this, drawStartScale, drawDipDistance, drawHookDistance);
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
        initCardData.SettingUi(cardData);
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
    // Pointer Events (실제 처리는 컨트롤러들에 위임)
    // ============================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverController.HandlePointerEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoverController.HandlePointerExit();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (willPanel != null && willPanel.IsSelecting)
        {
            return;
        }

        // 더블클릭도 한 번 클릭했을 때와 완전히 같은 확정 처리를 그대로 받는다.
        // (이미 확정된 카드를 다시 클릭하면 HandleConfirmClick 안에서 조용히 무시된다.)
        // 더블클릭이면 그 위에 더블클릭 전용 처리(다른 카드 내리기 + 이벤트 발행)만 추가로 더한다.
        hoverController.KillAll();

        selectionController.HandleConfirmClick();

        if (eventData.clickCount >= 2)
        {
            doubleClickController.HandleDoubleClick();
        }
    }

    // ============================================================
    // Double Click (컨트롤러로 위임 - 다른 카드들이 static 레지스트리를 통해 호출)
    // ============================================================

    public static void RestoreAllCards()
    {
        KTH_HandCardDoubleClickController.RestoreAllCards();
    }

    public void PlayMoveDownAnimation()
    {
        doubleClickController.PlayMoveDownAnimation();
    }

    public void PlayMoveUpAnimation()
    {
        doubleClickController.PlayMoveUpAnimation();
    }

    /// <summary>
    /// 이미 더블클릭으로 내려가 있는 상태에서, "원래 자리"가 바뀌었을 때
    /// (부채꼴 재배치 등) 그 새 자리를 기준으로 내려간 오프셋을 다시 적용한다.
    /// 내려가 있지 않으면 아무 일도 하지 않는다.
    /// </summary>
    public void RefreshMoveDownOffset()
    {
        doubleClickController.RefreshMoveDownOffset();
    }

    /// <summary>
    /// 이 카드를 "지금 손패에 있는 카드" 더블클릭 대상 목록에 (다시) 등록한다.
    /// KTH_HandCardLayout.AddCard가 카드를 손패에 넣을 때마다 부른다. 버려졌다가
    /// 풀에서 재사용되는 카드는 Awake가 다시 안 돌아서, 여기서 다시 등록해주지
    /// 않으면 더블클릭 대상에서 영영 빠진 채로 남는다.
    /// </summary>
    public void RegisterForDoubleClick()
    {
        doubleClickController.RegisterInHand();
    }

    /// <summary>
    /// 이 카드를 더블클릭 대상 목록에서 뺀다. 카드가 손패를 완전히 떠나는 시점
    /// (KTH_HandCardDiscardHandler.ConsumeAndRearrange)에서 부른다.
    ///
    /// ResetForPool에서도 빼주고 있지만, 버림 연출(KTH_DiscardAnimation)이 있는
    /// 경로는 카드를 Destroy도 Pool.Release도 하지 않고 버림 더미의 자식으로
    /// 부모만 바꿔서 그대로 눌러앉힌다 - 즉 ResetForPool이 아예 안 불린다.
    /// 그래서 "손패를 떠나는" 시점 자체에서 한 번 더 확실히 빼준다.
    /// </summary>
    public void UnregisterFromDoubleClick()
    {
        doubleClickController.UnregisterFromHand();
    }

    // ============================================================
    // Selection (컨트롤러로 위임)
    // ============================================================

    public void CancelSelectionState()
    {
        selectionController.CancelSelectionState();
    }

    public void SetSelected(bool value)
    {
        selectionController.SetSelected(value);
    }

    public static void DeselectCurrent()
    {
        KTH_HandCardSelectionController.DeselectCurrent();
    }

    // 선택 컨트롤러가 확정 클릭 시 이 카드를 대신해 이벤트를 쏘기 위해 부르는 내부 통로.
    internal void RaiseCardClicked()
    {
        OnCardClicked?.Invoke(this);
    }

    internal void KillHoverTweens()
    {
        hoverController.KillAll();
    }

    internal void SetOutlineVisible(bool visible)
    {
        initCardData.SetOutlineVisible(visible);
    }

    internal void RestoreSorting()
    {
        cardSorting?.RestoreSorting();
    }

    // ============================================================
    // Spawn / Draw / Rearrange (모션 애니메이터로 위임)
    // ============================================================

    public void SetSpawnPosition(Vector3 worldPos)
    {
        motionAnimator.SetSpawnPosition(worldPos);
    }

    public void MoveToHandPositionWithDelay(
        Vector3 targetPos,
        Vector3 targetRot,
        float duration,
        float delay,
        Ease ease)
    {
        motionAnimator.MoveToHandPositionWithDelay(targetPos, targetRot, duration, delay, ease);
    }

    public void PlayDrawAnimation(
        Vector3 targetLocalPos,
        Vector3 targetLocalRot,
        float duration = 0.4f)
    {
        motionAnimator.PlayDrawAnimation(targetLocalPos, targetLocalRot, duration);
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
    // Consume / Discard (핸들러로 위임)
    // ============================================================

    public void ConsumeAndRearrange(
        KTH_DiscardCardUI discardPile = null,
        Action onComplete = null)
    {
        KTH_HandCardDiscardHandler.ConsumeAndRearrange(this, discardPile, onComplete);
    }

    // ============================================================
    // Pool Reset / Destroy
    // ============================================================

    public void ResetForPool()
    {
        transform.DOKill(true);

        hoverController.KillAll();
        hoverController.ResetForPool();
        selectionController.ResetForPool();
        doubleClickController.ResetForPool();
        initCardData.ResetForPool();

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

        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;

        cardData = null;
        willPanel = null;
    }

    private void OnDestroy()
    {
        hoverController?.KillAll();

        transform.DOKill();

        selectionController?.OnOwnerDestroyed();
        doubleClickController?.OnOwnerDestroyed();
    }
}
