using DG.Tweening;
using UnityEngine;

/// <summary>
/// 카드 한 장의 "선택 / 확정(배치 모드)" 상태 기계와 그 시각 연출(선택·해제 애니메이션)을
/// 담당한다. "한 번에 하나의 카드만 확정될 수 있다"는 규칙(원래 currentSelectedCard static)도
/// 여기서 지킨다.
/// </summary>
public class KTH_HandCardSelectionController
{
    // 지금 "확정(배치 모드)"된 카드의 컨트롤러. 손패 전체에 하나만 존재해야 하므로 static이다.
    private static KTH_HandCardSelectionController currentConfirmed;

    private readonly KTH_HandCard owner;
    private readonly KTH_Axis3D selectMoveAxis;
    private readonly float selectMoveAmount;
    private readonly float selectDuration;

    private bool isSelected;
    private bool isConfirmed;
    private bool isPlacementMode;

    public bool IsSelected => isSelected;
    public bool IsConfirmed => isConfirmed;
    public bool IsPlacementMode => isPlacementMode;

    public static bool HasConfirmedSelection =>
        currentConfirmed != null && currentConfirmed.isConfirmed;

    public KTH_HandCardSelectionController(
        KTH_HandCard owner,
        KTH_Axis3D selectMoveAxis,
        float selectMoveAmount,
        float selectDuration)
    {
        this.owner = owner;
        this.selectMoveAxis = selectMoveAxis;
        this.selectMoveAmount = selectMoveAmount;
        this.selectDuration = selectDuration;
    }

    /// <summary>다른 카드가 지금 확정된 채로 호버를 막고 있는지. card 자신은 막지 않는다.</summary>
    public static bool IsBlockedByOtherConfirmedCard(KTH_HandCard card)
    {
        return currentConfirmed != null &&
               currentConfirmed.owner != card &&
               currentConfirmed.isConfirmed;
    }

    public static void DeselectCurrent()
    {
        if (currentConfirmed == null)
        {
            return;
        }

        KTH_HandCard card = currentConfirmed.owner;

        card.CancelSelectionState();

        KTH_InfoPanel.Instance?.CancleInfoPanl();
    }

    /// <summary>
    /// 단일 확정 클릭 처리. KTH_HandCard.OnPointerClick의 단일클릭 분기가 그대로 위임한다.
    ///
    /// 이미 확정된 카드를 다시 눌러도 취소하지 않는다(취소는 보드 우클릭 등 다른
    /// 경로로만 이뤄진다). 그래서 이미 확정된 상태면 아무 것도 하지 않고 끝낸다.
    /// </summary>
    public void HandleConfirmClick()
    {
        if (isConfirmed)
        {
            return;
        }

        if (currentConfirmed != null && currentConfirmed != this)
        {
            currentConfirmed.CancelSelectionState();
        }

        isConfirmed = true;
        isPlacementMode = true;

        currentConfirmed = this;

        owner.BringToFront();

        SetSelected(true);

        KTH_HandCardLayout.Instance?.EnterPlacementMode(owner);

        if (KTH_InfoPanel.Instance != null)
        {
            if (KTH_InfoPanel.Instance.CurrentCard != owner)
            {
                KTH_InfoPanel.Instance.StartInfoPanl(owner.CardData, owner);
            }

            KTH_InfoPanel.Instance.SelectInfoPanl();
        }

        owner.RaiseCardClicked();
    }

    public void CancelSelectionState()
    {
        bool wasPlacementMode = isPlacementMode;

        isConfirmed = false;
        isPlacementMode = false;

        if (currentConfirmed == this)
        {
            currentConfirmed = null;
        }

        owner.KillHoverTweens();

        SetSelected(false);

        if (wasPlacementMode)
        {
            KTH_HandCardLayout.Instance?.ExitPlacementMode();
        }
    }

    public void SetSelected(bool value)
    {
        if (isSelected == value)
        {
            return;
        }

        isSelected = value;

        owner.transform.DOKill();

        owner.SetOutlineVisible(isSelected);

        if (isSelected)
        {
            owner.BringToFront();
            PlaySelectAnimation();
        }
        else
        {
            PlayDeselectAnimation();
            owner.RestoreSorting();
        }

        KTH_HandCardLayout.Instance?.OnCardSelectionChanged(owner, isSelected);
    }

    private void PlaySelectAnimation()
    {
        if (isPlacementMode)
        {
            return;
        }

        Vector3 targetPos = owner.OriginalLocalPosition;

        switch (selectMoveAxis)
        {
            case KTH_Axis3D.X: targetPos.x += selectMoveAmount; break;
            case KTH_Axis3D.Y: targetPos.y += selectMoveAmount; break;
            case KTH_Axis3D.Z: targetPos.z += selectMoveAmount; break;
        }

        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(owner.transform);

        sequence.Join(
            owner.transform.DOLocalMove(targetPos, selectDuration).SetEase(Ease.OutBack, 0.7f));

        sequence.Join(
            owner.transform.DOScale(Vector3.one * owner.SelectScale, selectDuration).SetEase(Ease.OutBack, 0.7f));
    }

    private void PlayDeselectAnimation()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(owner.transform);

        sequence.Join(
            owner.transform.DOLocalMove(owner.OriginalLocalPosition, selectDuration).SetEase(Ease.OutCubic));

        sequence.Join(
            owner.transform.DOLocalRotate(owner.OriginalLocalRotation, selectDuration).SetEase(Ease.OutCubic));

        sequence.Join(
            owner.transform.DOScale(Vector3.one, selectDuration).SetEase(Ease.OutCubic));
    }

    /// <summary>풀에서 재사용하기 전 상태 초기화.</summary>
    public void ResetForPool()
    {
        isSelected = false;
        isConfirmed = false;
        isPlacementMode = false;

        if (currentConfirmed == this)
        {
            currentConfirmed = null;
        }
    }

    public void OnOwnerDestroyed()
    {
        if (currentConfirmed == this)
        {
            currentConfirmed = null;
        }
    }
}
