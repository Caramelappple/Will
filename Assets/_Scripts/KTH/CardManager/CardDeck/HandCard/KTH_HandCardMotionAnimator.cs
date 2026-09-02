using DG.Tweening;
using UnityEngine;

/// <summary>
/// 카드가 손패 안팎에서 다른 위치로 옮겨가는 애니메이션(스폰 위치 지정, 드로우 연출,
/// 재정렬 이동)을 담당한다. "지금 선택됐는지" 정도만 확인하고, 그 외 판단은 호출한
/// 쪽(KTH_HandCardLayout 등)이 이미 끝내고 좌표만 넘겨준다고 가정한다.
/// </summary>
public class KTH_HandCardMotionAnimator
{
    private readonly KTH_HandCard owner;
    private readonly float drawStartScale;
    private readonly float drawDipDistance;
    private readonly float drawHookDistance;

    public KTH_HandCardMotionAnimator(
        KTH_HandCard owner,
        float drawStartScale,
        float drawDipDistance,
        float drawHookDistance)
    {
        this.owner = owner;
        this.drawStartScale = drawStartScale;
        this.drawDipDistance = drawDipDistance;
        this.drawHookDistance = drawHookDistance;
    }

    public void SetSpawnPosition(Vector3 worldPos)
    {
        owner.transform.position = worldPos;
        owner.transform.localRotation = Quaternion.identity;
        owner.transform.localScale = Vector3.one;
    }

    public void MoveToHandPositionWithDelay(
        Vector3 targetPos,
        Vector3 targetRot,
        float duration,
        float delay,
        Ease ease)
    {
        if (owner.IsSelected)
        {
            return;
        }

        owner.transform.DOKill();

        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(owner.transform);

        sequence.Join(owner.transform.DOLocalMove(targetPos, duration).SetDelay(delay).SetEase(ease));
        sequence.Join(owner.transform.DOLocalRotate(targetRot, duration).SetDelay(delay).SetEase(ease));
        sequence.Join(owner.transform.DOScale(Vector3.one, duration).SetDelay(delay).SetEase(ease));
    }

    public void PlayDrawAnimation(Vector3 targetLocalPos, Vector3 targetLocalRot, float duration)
    {
        owner.UpdateOriginalTransform(targetLocalPos, targetLocalRot);

        Transform t = owner.transform;

        t.DOKill();

        t.localScale = Vector3.one * drawStartScale;

        Vector3 startPos = t.localPosition;

        Vector3 midPos = Vector3.Lerp(startPos, targetLocalPos, 0.5f);
        midPos.y -= drawDipDistance;

        Vector3 preTargetPos = targetLocalPos;
        preTargetPos.y -= drawHookDistance;
        preTargetPos.x -= (targetLocalPos.x - startPos.x) * 0.08f;

        Vector3[] path = { startPos, midPos, preTargetPos, targetLocalPos };

        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(t);

        sequence.Join(t.DOLocalPath(path, duration, PathType.CatmullRom).SetEase(Ease.InOutSine));
        sequence.Join(t.DOLocalRotate(targetLocalRot, duration).SetEase(Ease.OutCubic));
        sequence.Join(t.DOScale(Vector3.one, duration).SetEase(Ease.OutBack));
    }
}
