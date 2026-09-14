using System;
using DG.Tweening;
using UnityEngine;
using _Scripts.LSO.UI.Effect;

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
    // OutBounce였을 때는 착지가 오버슈트로 여러 번 튀어 보였고, 그 직후 호버 연출(LSO_HoverMoveEffect)이
    // 재개되며 다시 한번 떠오르는 트윈이 걸려 "내려갔다가 다시 뜬다"는 이중 동작으로 두드러졌다.
    // 오버슈트 없는 이즈로 바꿔 착지 자체를 한 번의 매끄러운 동작으로 만든다.
    [SerializeField] private Ease settleEase = Ease.OutQuad;
    [Tooltip("착지 후 호버 연출을 재개하기까지 기다리는 시간. 배치 직후엔 커서가 이미 기물 위에 있어서\n" +
             "재개와 동시에 또 떠오르는 게 착지와 겹쳐 보인다 — 잠깐 눌러뒀다가 재개해 분리시킨다.\n" +
             "평소 호버 반응 속도(LSO_HoverMoveEffect.enterDuration)에는 영향 없다.")]
    [SerializeField] private float hoverResumeDelay = 1.5f;

    /// <summary>
    /// target을 생성되는 즉시 finalWorldPos(제 자리)에 놓는다 — 밑에서 솟아나지 않는다.
    /// 그 자리에서 riseHeight만큼 솟았다가 다시 finalWorldPos로 내려앉는다.
    /// 논리적 위치(격자 등록)는 호출하는 쪽이 이미 끝냈다고 보고, 여기서는 화면에 보이는 위치만 다룬다.
    /// </summary>
    public Sequence Play(Transform target, Vector3 finalWorldPos, GameObject linkTarget)
    {
        // 배치 연출과 호버 연출(LSO_HoverMoveEffect)이 같은 모델 트랜스폼을 함께 움직인다.
        // 배치는 대개 그 칸을 방금 클릭한 직후라 커서가 이미 기물 위에 있어서, 두 트윈이
        // 같은 프레임에 서로 다른 좌표로 이 트랜스폼을 끌어당겨 떨리는 원인이 된다.
        // LDY_MoveSystem/LDY_AttackSystem이 이동/공격 연출 중에 쓰는 것과 같은 방식으로,
        // 이 연출이 도는 동안만 호버를 잠깐 쉬게 하고 끝나면 되돌린다.
        LSO_HoverMoveEffect[] hoverEffects =
            linkTarget != null
                ? linkTarget.GetComponentsInChildren<LSO_HoverMoveEffect>(true)
                : Array.Empty<LSO_HoverMoveEffect>();

        foreach (LSO_HoverMoveEffect effect in hoverEffects)
        {
            if (effect != null)
            {
                effect.SetSuspended(true, restore: false);
            }
        }

        target.position = finalWorldPos;
        Vector3 risenPos = finalWorldPos + new Vector3(0f, riseHeight, 0f);

        Sequence sequence = DOTween.Sequence()
            .Append(target.DOMove(risenPos, duration).SetEase(riseEase))
            .Append(target.DOMove(finalWorldPos, duration).SetEase(settleEase))
            .AppendInterval(hoverResumeDelay)
            .SetLink(linkTarget);

        // OnKill은 정상 완료(자동 킬)와 도중에 끊기는 경우(오브젝트 파괴로 인한 SetLink 킬 등)
        // 모두에서 불려서, 어떻게 끝나든 호버 연출을 반드시 재개시킨다.
        sequence.OnKill(() =>
        {
            foreach (LSO_HoverMoveEffect effect in hoverEffects)
            {
                if (effect != null)
                {
                    effect.SetSuspended(false);
                }
            }
        });

        return sequence;
    }
}
