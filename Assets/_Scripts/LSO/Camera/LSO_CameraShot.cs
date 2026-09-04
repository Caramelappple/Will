using System;
using Unity.Cinemachine;
using UnityEngine;

namespace _Scripts.LSO.Camera
{
    /// <summary>
    /// 카메라 하나와 그 카메라로 갈 때의 규칙.
    ///
    /// 카메라마다 "어떻게 넘어가고 얼마나 머무는가"를 따로 갖는다.
    /// 보스 클로즈업은 느리게 들어가 오래 머물고, 되돌아오는 것은 빠르게 —
    /// 같은 것을 코드가 아니라 인스펙터에서 정하기 위한 자리다.
    /// </summary>
    [Serializable]
    public sealed class LSO_CameraShot
    {
        [Tooltip("코드에서 이 이름으로 부른다. 비워두면 카메라 오브젝트 이름을 쓴다.")]
        public string id;

        [Tooltip("이 샷에서 쓸 시네머신 카메라.")]
        public CinemachineCamera camera;

        [Tooltip("이 카메라가 켜졌을 때 가질 우선순위. 샷마다 서로 다른 값을 줄 것.\n" +
                 "여러 카메라가 동시에 켜지면 이 숫자가 큰 쪽이 이긴다.\n" +
                 "값이 겹치면 어느 쪽이 잡힐지 보장되지 않는다.")]
        public int priority = 10;

        [Header("전환")]
        [Tooltip("이 카메라로 넘어갈 때의 이징.\n" +
                 "\n" +
                 "Cut          즉시 전환. Blend Time을 무시한다\n" +
                 "Ease In Out  S자. 양끝이 부드럽다\n" +
                 "Ease In      빠르게 떠나 살며시 도착\n" +
                 "Ease Out     살며시 떠나 빠르게 도착\n" +
                 "Hard In      아주 느리게 떠나 확 꽂힌다\n" +
                 "Hard Out     확 튀어나가 아주 느리게 안착\n" +
                 "Linear       등속. 기계적이지만 아주 느릴 땐 오히려 낫다\n" +
                 "Custom       Custom Curve를 직접 그린다\n" +
                 "\n" +
                 "주의: DOTween과 In/Out 의미가 반대다.\n" +
                 "시네머신은 '들어오는 샷' 기준이라\n" +
                 "Ease In = DOTween의 OutQuad, Ease Out = InQuad 에 가깝다.")]
        public CinemachineBlendDefinition.Styles style = CinemachineBlendDefinition.Styles.EaseInOut;

        [Tooltip("넘어가는 데 걸리는 시간(초).")]
        [Min(0f)] public float blendTime = 1f;

        [Tooltip("Style이 Custom일 때만 쓴다. 시간·값 모두 0~1 범위로 정규화할 것.")]
        public AnimationCurve customCurve;

        [Header("머무는 시간")]
        [Tooltip("이 샷에 머물 시간(초). 0이면 다음 지시가 올 때까지 계속 머문다.")]
        [Min(0f)] public float holdTime;

        [Tooltip("머무는 시간이 끝나면 돌아갈 곳. 비워두면 직전 샷으로 돌아간다.")]
        public string nextId;

        [Header("돌아가기")]
        [Tooltip("클릭 같은 조작으로 이 샷에서 빠져나올 수 있는지.\n" +
                 "\n" +
                 "끄면 Hold Time이 끝나거나 다른 곳에서 Play를 부를 때만 벗어난다.\n" +
                 "보스 등장 컷신처럼 끝까지 보여줘야 하는 샷에 끈다.\n" +
                 "\n" +
                 "이걸 끄고 Hold Time도 0으로 두면 어떤 조작으로도 못 빠져나온다.\n" +
                 "그때는 Next Id를 채우거나 코드에서 Play를 불러줄 것.")]
        public bool canReturn = true;

        /// <summary>부를 때 쓰는 이름. id를 비워두면 카메라 오브젝트 이름이 된다.</summary>
        public string Key =>
            !string.IsNullOrEmpty(id) ? id
            : camera != null ? camera.name
            : string.Empty;

        /// <summary>이 샷의 전환 규칙을 시네머신이 아는 형태로 만든다.</summary>
        public CinemachineBlendDefinition ToBlend()
        {
            return new CinemachineBlendDefinition(style, blendTime)
            {
                CustomCurve = customCurve
            };
        }
    }
}
