using System;
using System.Collections.Generic;
using _Scripts.LDY;
using UnityEngine;

/// <summary>턴 매니저별 계약 환급 대기열. 컴포넌트를 생성하지 않는다.</summary>
public sealed class DLJ_ContractRefund
{
    private static readonly Dictionary<LDY_TurnManager, DLJ_ContractRefund>
        Instances = new();

    private LDY_ActionPointManager actionPoints;
    private readonly LDY_TurnManager turnManager;
    private int pendingRefund;
    private readonly List<RefundRequest> requests = new();

    private readonly struct RefundRequest
    {
        public readonly int Amount;
        public readonly Action<int, int> OnPaid;

        public RefundRequest(int amount, Action<int, int> onPaid)
        {
            Amount = amount;
            OnPaid = onPaid;
        }
    }

    private DLJ_ContractRefund(
        LDY_ActionPointManager sourceActionPoints,
        LDY_TurnManager sourceTurnManager)
    {
        actionPoints = sourceActionPoints;
        turnManager = sourceTurnManager;
        turnManager.OnTurnChanged += HandleTurnChanged;
    }

    public static DLJ_ContractRefund GetOrCreate(
        LDY_ActionPointManager sourceActionPoints,
        LDY_TurnManager sourceTurnManager)
    {
        if (sourceActionPoints == null || sourceTurnManager == null)
            return null;

        if (Instances.TryGetValue(sourceTurnManager, out DLJ_ContractRefund service))
        {
            service.actionPoints = sourceActionPoints;
            return service;
        }

        service = new DLJ_ContractRefund(
            sourceActionPoints,
            sourceTurnManager);
        Instances.Add(sourceTurnManager, service);
        return service;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        foreach (DLJ_ContractRefund service in Instances.Values)
        {
            if (service.turnManager != null)
                service.turnManager.OnTurnChanged -= service.HandleTurnChanged;
        }

        Instances.Clear();
    }

    public void QueueRefund(int amount, Action<int, int> onPaid = null)
    {
        amount = Mathf.Max(0, amount);
        if (amount == 0)
            return;

        pendingRefund += amount;
        requests.Add(new RefundRequest(amount, onPaid));
        Debug.Log(
            $"Queued {amount} action points " +
            $"(pending: {pendingRefund}).");
    }

    private void HandleTurnChanged(LDY_Team team)
    {
        if (team != LDY_Team.Player || pendingRefund <= 0)
            return;

        int amount = pendingRefund;
        pendingRefund = 0;
        RefundRequest[] paying = requests.ToArray();
        requests.Clear();

        if (actionPoints == null)
        {
            Debug.LogError("Failed to refund action points.");
            foreach (RefundRequest request in paying)
                request.OnPaid?.Invoke(0, 0);
            return;
        }

        int firstSlot = actionPoints.Current;
        int remaining = actionPoints.AddActionPoints(amount);
        foreach (RefundRequest request in paying)
        {
            int gained = Mathf.Min(request.Amount, remaining);
            request.OnPaid?.Invoke(firstSlot, gained);
            firstSlot += gained;
            remaining -= gained;
        }

        Debug.Log(
            $"Refunded {amount} action points " +
            $"(current: {actionPoints.Current}).");
    }
}
