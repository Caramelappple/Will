using System;
using DG.Tweening;
using UnityEngine;

// 기물을 보드에 배치할 때 제 위치에서 살짝 솟았다가 내려앉는 연출.
// 모노비헤이버가 아니다 — 실행 주체(씬 배선)는 이 클래스를 들고 쓰는 쪽(LDY_BoardManager)이다.
[Serializable]
public class KTH_PlacementAnimation
{
    [Tooltip("제 자리에서 얼마나 솟아올랐다가 내려앉을지.")]
    [SerializeField] private float riseHeight = 0.3f;
    [Tooltip("솟아오르는 데, 그리고 다시 내려앉는 데 각각 걸리는 시간.")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private Ease riseEase = Ease.OutQuad;
    [SerializeField] private Ease settleEase = Ease.OutBounce;

    /// <summary>
    /// target을 생성되는 즉시 finalWorldPos(제 자리)에 놓는다 — 밑에서 솟아나지 않는다.
    /// 그 자리에서 riseHeight만큼 솟았다가 다시 finalWorldPos로 내려앉는다.
    /// 논리적 위치(격자 등록)는 호출하는 쪽이 이미 끝냈다고 보고, 여기서는 화면에 보이는 위치만 다룬다.
    /// </summary>
    public Sequence Play(Transform target, Vector3 finalWorldPos, GameObject linkTarget)
    {
        target.position = finalWorldPos;
        Vector3 risenPos = finalWorldPos + new Vector3(0f, riseHeight, 0f);

        return DOTween.Sequence()
            .Append(target.DOMove(risenPos, duration).SetEase(riseEase))
            .Append(target.DOMove(finalWorldPos, duration).SetEase(settleEase))
            .SetLink(linkTarget);
    }
}
