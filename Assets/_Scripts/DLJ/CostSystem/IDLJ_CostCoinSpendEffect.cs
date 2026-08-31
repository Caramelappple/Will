using System;
using UnityEngine;

/// <summary>코인 사용 연출 구현이 따라야 하는 최소 계약.</summary>
public interface IDLJ_CostCoinSpendEffect
{
    bool Play(Transform coin, Action onComplete = null);
    void StopAndReset(Transform coin);
}
