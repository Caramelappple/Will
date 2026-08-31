using System.Collections.Generic;

/// <summary>코스트 코인의 등장 표현에 필요한 최소 계약.</summary>
public interface IDLJ_CostCoinEntranceEffect
{
    bool PlayOnStart { get; }
    void Bind(IReadOnlyList<DLJ_CostCoinSlot> slots);
    void PrepareInitialCoins();
    void PrepareSlot(DLJ_CostCoinSlot slot);
    void PlayInitial(int filledCount);
    void PlayRange(int startIndex, int endIndex, float initialDelay = 0f);
    bool IsAnimating(DLJ_CostCoinSlot slot);
    void Stop(DLJ_CostCoinSlot slot);
}
