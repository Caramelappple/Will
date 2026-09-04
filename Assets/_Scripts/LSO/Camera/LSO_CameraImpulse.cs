using Unity.Cinemachine;
using UnityEngine;

namespace _Scripts.LSO.Camera
{
    /// <summary>
    /// 화면을 잠깐 흔든다. 시네머신 Impulse를 거친다.
    ///
    /// ── 왜 카메라를 직접 안 흔드나 ────────────────────────────
    /// 예전 LDY_CameraShake는 Camera.main의 localPosition을 직접 밀었다.
    /// 그런데 같은 카메라를 LSO_CameraDirector가 시네머신으로 잡고 있어서,
    /// 둘이 매 프레임 같은 트랜스폼을 밀어댔다. 흔들리는 동안 샷이 어긋났다.
    ///
    /// Impulse는 흔들림을 카메라가 아니라 **신호**로 보낸다.
    /// 시네머신이 자기 계산을 끝낸 뒤 그 위에 얹으므로 서로 밀어내지 않는다.
    /// 자리를 정하는 주체는 여전히 시네머신 하나다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 씬 배선: **흔들릴 카메라마다 CinemachineImpulseListener를 붙여야 한다.**
    /// 안 붙이면 신호는 나가는데 받는 쪽이 없어 아무 일도 일어나지 않는다.
    /// 그 경우를 알아채도록 처음 한 번 경고를 남긴다.
    ///
    /// 붙일 때 Channel Mask와 Gain을 확인할 것. 인스펙터로 추가하면 기본값이 들어가지만,
    /// 코드로 AddComponent 하면 Reset이 불리지 않아 둘 다 0이 되고 조용히 죽는다.
    /// </summary>
    public static class LSO_CameraImpulse
    {
        /// <summary>ImpulseDefinition.ImpulseChannel의 기본값과 같아야 한다. 리스너의 Channel Mask도 이 값.</summary>
        private const int Channel = 1;

        private static CinemachineImpulseSource _source;
        private static bool _warnedNoListener;

        /// <summary>
        /// 화면 전체를 흔든다. 거리와 무관하게 같은 세기로 느껴진다.
        /// </summary>
        /// <param name="duration">흔드는 시간(초).</param>
        /// <param name="strength">
        /// 세기. 예전 LDY_CameraShake의 흔들림 폭과 대략 같은 단위로 맞춰뒀지만,
        /// 계산 방식이 달라 눈으로 보면서 다시 맞추는 편이 낫다.
        /// </param>
        public static void Shake(float duration, float strength)
        {
            if (duration <= 0f || strength <= 0f) return;

            CinemachineImpulseSource source = Resolve();
            if (source == null) return;

            source.ImpulseDefinition.ImpulseDuration = duration;

            WarnIfNoListener(source);

            // 아래로 치는 신호를 보낸다. 아래로 꺼지는 편이 부딪힌 충격으로 읽힌다.
            source.GenerateImpulseWithVelocity(Vector3.down * strength);
        }

        /// <summary>
        /// 지정한 자리에서 흔든다. 가까울수록 세게 느껴진다.
        ///
        /// 화면에 여러 곳에서 충격이 생기는 연출에 쓴다.
        /// 지금은 부르는 곳이 없지만, 있으면 Shake 대신 이쪽을 쓰면 된다.
        /// </summary>
        public static void ShakeAt(Vector3 position, float duration, float strength, float radius = 8f)
        {
            if (duration <= 0f || strength <= 0f) return;

            CinemachineImpulseSource source = Resolve();
            if (source == null) return;

            source.ImpulseDefinition.ImpulseDuration = duration;
            source.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Dissipating;
            source.ImpulseDefinition.DissipationDistance = Mathf.Max(0.01f, radius);

            WarnIfNoListener(source);

            source.GenerateImpulseAtPositionWithVelocity(position, Vector3.down * strength);
        }

        /// <summary>
        /// 신호를 보낼 곳을 마련한다. 없으면 만든다.
        ///
        /// 씬마다 손으로 달아두게 하지 않는 이유는, 안 달린 씬에서 조용히 아무 일도
        /// 일어나지 않고 원인이 배선이라는 걸 알아채기까지 한참 걸리기 때문이다.
        /// (LDY_CameraShake가 같은 이유로 스스로 붙었다. 그 판단은 그대로 가져온다.)
        /// </summary>
        private static CinemachineImpulseSource Resolve()
        {
            if (_source != null) return _source;

            var go = new GameObject("[LSO_CameraImpulse]");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;

            _source = go.AddComponent<CinemachineImpulseSource>();

            // AddComponent는 Reset을 부르지 않는다. 기본값을 직접 넣는다.
            _source.ImpulseDefinition.ImpulseChannel = Channel;
            _source.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            _source.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            _source.ImpulseDefinition.ImpulseDuration = 0.2f;

            return _source;
        }

        /// <summary>
        /// 받는 쪽이 하나도 없으면 짚어준다. 처음 한 번만.
        ///
        /// 이게 없으면 "흔들림이 안 보인다"의 원인이 세기 값인지 배선인지 알 수 없다.
        /// 매번 찍으면 충돌마다 콘솔이 덮이므로 한 번만 남긴다.
        /// </summary>
        private static void WarnIfNoListener(CinemachineImpulseSource source)
        {
            if (_warnedNoListener) return;

            CinemachineImpulseListener[] listeners =
                Object.FindObjectsByType<CinemachineImpulseListener>(FindObjectsSortMode.None);

            foreach (CinemachineImpulseListener listener in listeners)
            {
                // 채널이 겹치고 이득이 0이 아니어야 실제로 흔들린다.
                bool channelMatches = (listener.ChannelMask & Channel) != 0;

                if (channelMatches && !Mathf.Approximately(listener.Gain, 0f)) return;
            }

            _warnedNoListener = true;

            Debug.LogWarning(
                "화면 흔들림 신호를 받을 카메라가 없습니다. 흔들림이 보이지 않습니다.\n" +
                "흔들릴 CinemachineCamera 에 Cinemachine Impulse Listener 를 추가하고,\n" +
                $"Channel Mask 에 채널 {Channel} 을 켜고 Gain 을 1 로 두세요.\n" +
                $"(지금 씬의 리스너 {listeners.Length}개 중 조건을 만족하는 것이 없습니다)",
                source);
        }

        /// <summary>
        /// 도메인 리로드를 끈 에디터에서는 static이 지난 플레이의 값을 그대로 들고 있다.
        /// 파괴된 오브젝트를 붙들지 않도록 플레이할 때마다 비운다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _source = null;
            _warnedNoListener = false;
        }
    }
}
