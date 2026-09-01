using System;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.UI.Effect
{
    /// <summary>
    /// LSO_HoverMoveEffect의 연출 값 묶음.
    ///
    /// 컴포넌트에서 따로 떼어둔 이유는, 프리팹마다 인스펙터에 적는 것 말고
    /// 코드가 값 한 벌을 통째로 건네줄 수 있어야 해서다.
    /// 기물처럼 런타임에 붙이는 경우가 그렇다.
    ///
    /// 값이 무엇을 뜻하는지는 LSO_HoverMoveEffect의 툴팁과 같다.
    /// </summary>
    [Serializable]
    public struct LSO_HoverMoveTuning
    {
        [Tooltip("원래 자리에서 얼마나 옮길지. 대상의 로컬 기준이다.")]
        public Vector3 offset;

        [Tooltip("커서가 올라갈 때 걸리는 시간")]
        [Min(0f)] public float enterDuration;

        [Tooltip("커서가 벗어날 때 걸리는 시간")]
        [Min(0f)] public float exitDuration;

        public Ease easeEnter;
        public Ease easeExit;

        [Tooltip("timescale 영향 여부")]
        public bool ignoreTimeScale;

        /// <summary>
        /// 기본값. struct는 필드가 전부 0으로 시작하므로,
        /// 새로 만든 것을 그냥 쓰면 옮기지도 않고 시간도 0이 된다.
        /// 값을 안 채운 채로 쓰는 일이 없게 여기서 한 벌 준다.
        /// </summary>
        public static LSO_HoverMoveTuning Default => new LSO_HoverMoveTuning
        {
            offset = new Vector3(0f, 0.2f, 0f),
            enterDuration = 0.15f,
            exitDuration = 0.2f,
            easeEnter = Ease.OutQuad,
            easeExit = Ease.OutQuad,
            ignoreTimeScale = true
        };
    }
}
