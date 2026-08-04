using _Scripts.LDY;
using UnityEngine;

public sealed class DLJ_ContractRefund : MonoBehaviour
{
    private LDY_ActionPointManager actionPoints;
    private LDY_TurnManager turnManager;
    private int pendingRefund;

    public static DLJ_ContractRefund GetOrCreate(
        LDY_ActionPointManager sourceActionPoints,
        LDY_TurnManager sourceTurnManager)
    {
        if (sourceActionPoints == null)
            return null;

        DLJ_ContractRefund service =
            sourceActionPoints.GetComponent<DLJ_ContractRefund>();

        if (service == null)
            service = sourceActionPoints.gameObject.AddComponent<DLJ_ContractRefund>();

        service.Configure(sourceActionPoints, sourceTurnManager);
        return service;
    }

    public void QueueRefund(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount == 0)
            return;

        if (turnManager == null)
        {
            Debug.LogError(
                "Contract cannot queue an action point refund without a turn manager.",
                this);
            return;
        }

        // 사망한 턴과 관계없이 다음 플레이어 턴까지 보관한다.
        pendingRefund += amount;
        Debug.Log(
            $"Contract queued {amount} action points " +
            $"(pending: {pendingRefund}).",
            this);
    }

    private void Configure(
        LDY_ActionPointManager sourceActionPoints,
        LDY_TurnManager sourceTurnManager)
    {
        if (turnManager != null)
            turnManager.OnTurnChanged -= HandleTurnChanged;

        actionPoints = sourceActionPoints;
        turnManager = sourceTurnManager;

        if (turnManager != null)
            turnManager.OnTurnChanged += HandleTurnChanged;
    }

    private void HandleTurnChanged(LDY_Team team)
    {
        if (team != LDY_Team.Player || pendingRefund <= 0)
            return;

        int amount = pendingRefund;
        pendingRefund = 0;
        ApplyRefund(amount);
    }

    private void ApplyRefund(int amount)
    {
        if (actionPoints == null)
        {
            Debug.LogError("Contract action point receiver is missing.", this);
            return;
        }

        int previousPoints = actionPoints.Current;

        // 기존 공개 경로를 음수 소비로 사용하면 최대치를 넘겨 충전하면서 UI 이벤트도 발생한다.
        if (!actionPoints.TryConsume(-amount))
        {
            Debug.LogError("Contract failed to refund action points.", this);
            return;
        }

        int restored = actionPoints.Current - previousPoints;
        Debug.Log(
            $"Contract refunded {restored} action points " +
            $"(current: {actionPoints.Current}).",
            this);
    }

    private void OnDestroy()
    {
        if (turnManager != null)
            turnManager.OnTurnChanged -= HandleTurnChanged;
    }
}
