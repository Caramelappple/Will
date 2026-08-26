using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 커서가 올라갔을 때 크기로 반응한다.
    /// 연출 자체는 LSO_ScalePunchEffectBase가 담당하고, 여기서는 호버 트리거만 연결한다.
    ///
    /// 두 방식이 있고 인스펙터에서 고른다.
    ///   WhileHovering  올려둔 동안 커진 채로 있는다. 지금 고른 것이 무엇인지 보여줄 때
    ///   OnEnter        올린 순간 한 번 눌렸다 펴진다. 반응만 주면 될 때
    /// </summary>
    public class LSO_HoverScaleEffect : LSO_ScalePunchEffectBase, LSO_IHoverEffect
    {
        [Header("호버 방식")]
        [SerializeField] private LSO_HoverScaleMode mode = LSO_HoverScaleMode.WhileHovering;

        [Header("올려둔 동안 (WhileHovering 전용)")]
        [Tooltip("커서가 올라갔을 때의 크기 배율. 1보다 크면 커지고 작으면 줄어든다.")]
        [SerializeField, Range(0.5f, 1.5f)] private float hoverRatio = 1.06f;

        [Tooltip("커질 때 걸리는 시간.")]
        [SerializeField, Min(0f)] private float enterDuration = 0.12f;

        [Tooltip("원래 크기로 돌아올 때 걸리는 시간.")]
        [SerializeField, Min(0f)] private float exitDuration = 0.12f;

        [SerializeField] private Ease enterEase = Ease.OutBack;

        [SerializeField] private Ease exitEase = Ease.OutQuad;

        public void OnHoverEnter()
        {
            if (mode == LSO_HoverScaleMode.OnEnter)
            {
                Play();
                return;
            }

            ScaleTo(hoverRatio, enterDuration, enterEase);
        }

        /// <summary>   
        /// OnEnter 방식은 축소 → 복귀가 한 번에 끝나므로 이탈 시 할 일이 없다.
        /// WhileHovering 방식만 원래 크기로 되돌린다.
        /// </summary>
        public void OnHoverExit()
        {
            if (mode == LSO_HoverScaleMode.OnEnter) return;

            ScaleTo(1f, exitDuration, exitEase);
        }
    }
}
