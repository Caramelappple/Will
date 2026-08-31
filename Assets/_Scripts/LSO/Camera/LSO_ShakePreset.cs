using System;
using Unity.Cinemachine;
using UnityEngine;

namespace _Scripts.LSO.Camera
{
    /// <summary>
    /// 흔들림 한 가지. 세기와 길이, 모양을 묶어 이름으로 부른다.
    ///
    /// 부르는 쪽이 숫자를 넘기지 않고 이름만 넘기게 하려는 자리다.
    /// 숫자를 넘기게 두면 "타격은 0.4, 보스 등장은 1.2" 같은 값이
    /// 코드 여기저기에 흩어지고, 세게/약하게를 조절할 때 전부 찾아다녀야 한다.
    /// </summary>
    [Serializable]
    public class LSO_ShakePreset
    {
        [Tooltip("코드에서 이 이름으로 부른다. 비워두면 쓸 수 없다.")]
        public string id;

        [Tooltip("얼마나 세게. 0.1은 툭, 1은 확실히 흔들린다.\n" +
                 "화면 밖으로 밀려나 보이면 줄일 것.")]
        [Min(0f)] public float force = 0.3f;

        [Tooltip("흔들리는 시간(초). 짧을수록 타격감, 길수록 지진에 가깝다.")]
        [Min(0f)] public float duration = 0.2f;

        [Tooltip("흔들림의 모양.\n" +
                 "\n" +
                 "Recoil     한 번 확 밀렸다가 돌아온다. 총·강타\n" +
                 "Bump       짧게 툭. 가벼운 타격\n" +
                 "Explosion  크게 터지고 잦아든다. 폭발·처치\n" +
                 "Rumble     길게 웅웅거린다. 보스 등장·지진\n" +
                 "Custom     Custom Shape를 직접 그린다")]
        public CinemachineImpulseDefinition.ImpulseShapes shape =
            CinemachineImpulseDefinition.ImpulseShapes.Bump;

        [Tooltip("Shape가 Custom일 때만 쓴다. 시간·값 모두 0~1 범위로 그릴 것.")]
        public AnimationCurve customShape;

        [Tooltip("어느 쪽으로 밀릴지. 카메라 기준이다.\n" +
                 "아래(0,-1,0)가 기본이다. 위에서 내려다보는 화면에서 가장 자연스럽다.\n" +
                 "좌우로만 흔들고 싶으면 (1,0,0).")]
        public Vector3 direction = Vector3.down;

        /// <summary>부를 때 쓰는 이름.</summary>
        public string Key => id;

        /// <summary>
        /// 시네머신이 아는 형태로 옮겨 적는다.
        ///
        /// 정의 객체를 새로 만들지 않고 있는 것을 고쳐 쓰는 이유는,
        /// CinemachineImpulseSource가 자기 것을 계속 들고 있기 때문이다.
        /// 새로 만들어 대입해도 되지만 매번 쓰레기가 생긴다.
        /// </summary>
        public void ApplyTo(CinemachineImpulseDefinition definition)
        {
            if (definition == null) return;

            // 공간을 타고 퍼지지 않는다. 화면을 흔드는 것이 목적이라
            // 거리에 따라 약해지면 같은 연출이 자리마다 다르게 보인다.
            definition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;

            definition.ImpulseShape = shape;

            // Custom이 아닐 때는 건드리지 않는다. 비워둔 커브를 넣어두면
            // 나중에 Custom으로 바꿨을 때 아무 신호도 안 나오는 채로 시작한다.
            if (shape == CinemachineImpulseDefinition.ImpulseShapes.Custom && customShape != null)
                definition.CustomImpulseShape = customShape;

            definition.ImpulseDuration = Mathf.Max(0.01f, duration);

            definition.ImpulseChannel = LSO_CameraShake.Channel;
        }

        /// <summary>
        /// 밀어낼 방향과 세기를 합친 값.
        ///
        /// 방향이 (0,0,0)이면 세기를 아무리 올려도 흔들리지 않는다.
        /// 인스펙터에서 비워두기 쉬운 값이라 아래로 대신 채운다.
        /// </summary>
        public Vector3 Velocity
        {
            get
            {
                Vector3 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.down;

                return dir * force;
            }
        }
    }
}
