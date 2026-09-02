using DG.Tweening;

/// <summary>
/// KTH_HandCard의 호버(마우스 오버) 상태만 담당한다.
/// 진입/이탈 딜레이, 정보 패널 호버 딜레이, 그리고 그동안 카드를 잠깐 선택 상태로
/// 켜는 것까지가 이 클래스의 책임이다. 확정 클릭 이후의 선택/배치 상태는
/// KTH_HandCardSelectionController가 담당한다(이 클래스는 그쪽 상태를 읽기만 한다).
/// </summary>
public class KTH_HandCardHoverController
{
    private readonly KTH_HandCard owner;
    private readonly float hoverEnterDelay;
    private readonly float hoverExitDelay;
    private readonly float infoPanelHoverDelay;

    private bool isPointerOver;

    private Tween hoverEnterTween;
    private Tween hoverExitTween;
    private Tween infoPanelTween;

    public KTH_HandCardHoverController(
        KTH_HandCard owner,
        float hoverEnterDelay,
        float hoverExitDelay,
        float infoPanelHoverDelay)
    {
        this.owner = owner;
        this.hoverEnterDelay = hoverEnterDelay;
        this.hoverExitDelay = hoverExitDelay;
        this.infoPanelHoverDelay = infoPanelHoverDelay;
    }

    public void HandlePointerEnter()
    {
        isPointerOver = true;

        KillExitTween();

        if (!CanStartHover())
        {
            return;
        }

        KillEnterTween();

        hoverEnterTween = DOVirtual.DelayedCall(hoverEnterDelay, HandleHoverEnter);
    }

    private void HandleHoverEnter()
    {
        hoverEnterTween = null;

        if (!isPointerOver)
        {
            return;
        }

        if (!CanStartHover())
        {
            return;
        }

        owner.SetSelected(true);

        StartInfoPanelDelay();
    }

    // 원래 로직(willPanel 선택 중 / 다른 카드가 확정된 채로 막고 있음 / 카드 데이터 없음 / 이미 선택됨)
    // 을 그대로 옮긴 진입 가능 여부 판정.
    private bool CanStartHover()
    {
        if (owner.WillPanel != null && owner.WillPanel.IsSelecting)
        {
            return false;
        }

        if (KTH_HandCardSelectionController.IsBlockedByOtherConfirmedCard(owner))
        {
            return false;
        }

        if (owner.CardData == null || owner.IsSelected)
        {
            return false;
        }

        return true;
    }

    private void StartInfoPanelDelay()
    {
        KillInfoPanelTween();

        if (owner.IsConfirmed)
        {
            return;
        }

        infoPanelTween = DOVirtual.DelayedCall(infoPanelHoverDelay, ShowHoverInfo);
    }

    private void ShowHoverInfo()
    {
        infoPanelTween = null;

        if (!isPointerOver ||
            !owner.IsSelected ||
            owner.IsConfirmed ||
            owner.CardData == null)
        {
            return;
        }

        KTH_InfoPanel.Instance?.StartHoverInfo(owner.CardData, owner);
    }

    public void HandlePointerExit()
    {
        isPointerOver = false;

        KillEnterTween();
        KillInfoPanelTween();

        if (owner.IsConfirmed)
        {
            return;
        }

        KillExitTween();

        hoverExitTween = DOVirtual.DelayedCall(hoverExitDelay, HandleHoverExit);
    }

    private void HandleHoverExit()
    {
        hoverExitTween = null;

        if (isPointerOver || owner.IsConfirmed)
        {
            return;
        }

        KTH_InfoPanel.Instance?.CancelHoverSelection(owner);

        if (owner.IsSelected)
        {
            owner.CancelSelectionState();
        }
    }

    /// <summary>호버 관련 딜레이 콜백을 전부 끊는다. 클릭·풀 반납·파괴 시 호출.</summary>
    public void KillAll()
    {
        KillEnterTween();
        KillExitTween();
        KillInfoPanelTween();
    }

    /// <summary>풀에서 재사용하기 전 상태 초기화.</summary>
    public void ResetForPool()
    {
        isPointerOver = false;
    }

    private void KillEnterTween()
    {
        if (hoverEnterTween == null) return;
        hoverEnterTween.Kill();
        hoverEnterTween = null;
    }

    private void KillExitTween()
    {
        if (hoverExitTween == null) return;
        hoverExitTween.Kill();
        hoverExitTween = null;
    }

    private void KillInfoPanelTween()
    {
        if (infoPanelTween == null) return;
        infoPanelTween.Kill();
        infoPanelTween = null;
    }
}
