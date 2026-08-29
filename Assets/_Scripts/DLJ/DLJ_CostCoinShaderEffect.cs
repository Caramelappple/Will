using System;
using UnityEngine;

/// <summary>
/// 코스트 코인 사용 셰이더 연출을 연결할 자리.
/// 현재 구현은 비어 있으며, Play와 StopAndReset의 TODO 부분만 채우면 DLJ_CostCase가 자동으로 호출한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class DLJ_CostCoinShaderEffect : MonoBehaviour, IDLJ_CostCoinSpendEffect
{
    /// <summary>
    /// 연출을 시작했으면 true를 반환하고 완료 시 onComplete를 호출해야 한다.
    /// 구현 전에는 false를 반환해 코인이 즉시 사라지게 한다.
    /// </summary>
    public bool Play(Transform coin, Action onComplete = null)
    {
        // TODO: 셰이더 머티리얼 준비, 진행 값 애니메이션, 완료 콜백 호출을 구현한다.
        return false;
    }

    /// <summary>진행 중인 연출을 중지하고 코인의 머티리얼 상태를 원래대로 복구한다.</summary>
    public void StopAndReset(Transform coin)
    {
        // TODO: 실행 중인 셰이더 연출을 중지하고 변경한 머티리얼 값을 복구한다.
    }
}
