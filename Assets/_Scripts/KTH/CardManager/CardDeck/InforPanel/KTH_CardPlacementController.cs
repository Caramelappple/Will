using System;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

// =========================================================
// SRP: LDY_CardPlacer / KTH_HandCardLayout 와의 소환 시작·취소 연동만 담당한다.
// KTH_InfoPanel은 "무엇을 할지"만 콜백으로 넘기고,
// "어떻게 소환기와 통신할지"는 이 클래스가 책임진다.
//
// cardPlacer.BeginPlacement의 onPlaced 콜백 파라미터 타입을 몰라도(추론에 맡김)
// 되도록, 바깥으로 노출하는 인터페이스는 성공/실패/취소를 Action 3개로만 표현한다.
//
// LDY_CardPlacer.cs는 건드리지 않는다. 대신 그쪽에 이미 공개돼 있는
// SetBoardActive(bool)를 그대로 가져다 쓴다 (보드 레이캐스트 마스크를
// 잠깐 비워서 클릭이 안 먹게 하는 이미 있는 기능).
// =========================================================
public interface ICardPlacementController
{
    bool IsPlacing { get; }
    bool TryBeginPlacement(
        LSO_CardSO cardToPlace,
        LDY_Team team,
        Action onPlacedSuccessfully,
        Action onPlacementFailed,
        Action onCancelled);
    void CancelPlacement();

    /// <summary>
    /// 인포 카드가 확대되어 화면을 덮고 있는 동안, 그 뒤 보드 칸이 같이
    /// 클릭되어 배치가 일어나는 걸 막는다(blocked = true). 원래대로 되돌리려면
    /// blocked = false로 다시 호출한다.
    /// </summary>
    void SetBoardBlocked(bool blocked);
}

public sealed class KTH_CardPlacementController : ICardPlacementController
{
    private readonly LDY_CardPlacer cardPlacer;

    public KTH_CardPlacementController(LDY_CardPlacer cardPlacer)
    {
        this.cardPlacer = cardPlacer;
    }

    public bool IsPlacing => cardPlacer != null && cardPlacer.IsPlacing;

    public bool TryBeginPlacement(
        LSO_CardSO cardToPlace,
        LDY_Team team,
        Action onPlacedSuccessfully,
        Action onPlacementFailed,
        Action onCancelled)
    {
        if (cardPlacer == null)
        {
            Debug.LogWarning(
                "[KTH_CardPlacementController] cardPlacer가 연결되어 있지 않습니다."
            );
            return false;
        }
        if (cardPlacer.IsPlacing)
        {
            return false;
        }
        bool started = cardPlacer.BeginPlacement(
            cardToPlace,
            team,
            onPlaced: animal =>
            {
                KTH_HandCardLayout.Instance?.MoveUpFromPlacement();
                if (animal == null)
                {
                    onPlacementFailed?.Invoke();
                    return;
                }
                onPlacedSuccessfully?.Invoke();
            },
            onCancelled: () =>
            {
                KTH_HandCardLayout.Instance?.MoveUpFromPlacement();
                onCancelled?.Invoke();
            }
        );
        if (started)
        {
            KTH_HandCardLayout.Instance?.MoveDownForPlacement();
        }
        return started;
    }

    public void CancelPlacement()
    {
        if (cardPlacer != null && cardPlacer.IsPlacing)
        {
            cardPlacer.CancelPlacement();
        }
    }

    public void SetBoardBlocked(bool blocked)
    {
        // SetBoardActive(active): active = true면 평소대로 클릭 가능,
        // false면 보드 레이캐스트 마스크를 비워 클릭이 안 먹는다.
        cardPlacer?.SetBoardActive(!blocked);
    }
}
