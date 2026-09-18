using System;
using DG.Tweening;
using UnityEngine;

// 기물이 이동할 때 미끄러지지 않고 떠오르고-이동하고-내려놓도록 하는 연출.
// 모노비헤이버가 아니다 — 실행 주체(코루틴/씬 배선)는 이 클래스를 들고 쓰는 쪽(LDY_MoveSystem)이다.
[Serializable]
public class KTH_MoveAnimation
{
    [Tooltip("바닥에서 얼마나 떠서 이동할지. Play에 riseHeightOverride를 넘기면 이 값 대신 그걸 쓴다\n" +
             "(예: 호버 때 뜨는 높이와 맞추고 싶을 때).")]
    [SerializeField] private float riseHeight = 0.3f;
    [Tooltip("떠오르는 데 걸리는 시간.")]
    [SerializeField] private float riseDuration = 0.15f;
    [Tooltip("내려놓는 데 걸리는 시간.")]
    [SerializeField] private float fallDuration = 0.15f;
    [SerializeField] private Ease riseEase = Ease.OutQuad;
    [SerializeField] private Ease fallEase = Ease.InQuad;

    /// <summary>
    /// target을 targetWorldPos(x, z, y)로 옮긴다. targetWorldPos.y가 곧 "다 내려놓았을 때의 바닥
    /// 높이"다 — 부르는 쪽(LDY_BoardManager/LDY_MoveSystem)이 기물별 restHeight까지 반영해 정해서
    /// 넘기므로, 여기서는 그 값을 그대로 착지 높이로 쓴다. 도중에 다시 계산하지 않는다.
    ///
    /// 뜨기 시작하는 높이(baseHeight)만 예외다 — 출발 시점엔 target이 이미 떠 있는 중(호버 등)일 수
    /// 있어서 지금 target.position.y를 그대로 바닥으로 못 쓴다. baseHeightOverride를 안 넘기면
    /// target.position.y를 쓰고, 정확한 바닥이 필요하면 호출하는 쪽이 구해서 넘겨야 한다.
    ///
    /// travelDuration/travelEasing은 호출하는 쪽(칸 수·특성에 따라 달라지는 이동 시간)이 정해서 넘긴다.
    /// travelEasing이 없으면 등속으로 가로지른다.
    /// </summary>
    public Sequence Play(
        Transform target, Vector3 targetWorldPos,
        float travelDuration, AnimationCurve travelEasing,
        GameObject linkTarget, float? riseHeightOverride = null, float? baseHeightOverride = null)
    {
        float baseHeight = baseHeightOverride ?? target.position.y;
        float rise = riseHeightOverride ?? riseHeight;

        Vector3 startPos = target.position;
        Vector3 raisedStart = new Vector3(startPos.x, baseHeight + rise, startPos.z);
        Vector3 raisedEnd = new Vector3(targetWorldPos.x, targetWorldPos.y + rise, targetWorldPos.z);
        Vector3 landPos = targetWorldPos;

        Tweener travelTween = target.DOMove(raisedEnd, Mathf.Max(0f, travelDuration));
        if (travelEasing != null) travelTween.SetEase(travelEasing);
        else travelTween.SetEase(Ease.Linear);

        return DOTween.Sequence()
            .Append(target.DOMove(raisedStart, riseDuration).SetEase(riseEase))
            .Append(travelTween)
            .Append(target.DOMove(landPos, fallDuration).SetEase(fallEase))
            .SetLink(linkTarget);
    }
}
