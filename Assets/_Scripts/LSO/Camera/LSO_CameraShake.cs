using System.Collections.Generic;
using _Scripts.LSO.CoreLib;
using Unity.Cinemachine;
using UnityEngine;

namespace _Scripts.LSO.Camera
{
    /// <summary>
    /// 카메라를 흔든다. 흔들림을 만드는 곳은 여기 하나뿐이다.
    ///
    /// 카메라 트랜스폼을 직접 만지지 않는다. 자리를 정하는 주체는 시네머신이고,
    /// 밖에서 같은 값을 덮어쓰면 블렌드 중이나 샷을 바꾸는 순간 서로 밀어낸다.
    /// 대신 시네머신이 열어둔 임펄스로 보낸다 — 카메라가 바뀌어도, 전환 중이어도
    /// 지금 화면을 잡고 있는 카메라가 알아서 받는다.
    ///
    /// 그래서 LSO_CameraDirector와 겹치지 않는다.
    /// 디렉터는 "어느 카메라를 볼지", 이쪽은 "그 화면을 얼마나 흔들지"를 정한다.
    ///
    /// 씬 배선: 씬 아무 곳에나 하나 두면 된다.
    /// 카메라 쪽 수신기(CinemachineImpulseListener)는 이 스크립트가 붙여준다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LSO_CameraShake : MonoSingleton<LSO_CameraShake>
    {
        /// <summary>
        /// 보내는 쪽과 받는 쪽이 같은 번호를 봐야 신호가 닿는다.
        /// 양쪽을 여기 한 곳에서 정해두면 어긋날 일이 없다.
        /// </summary>
        public const int Channel = 1;

        [Header("세기 목록")]
        [Tooltip("여기 등록한 이름으로 코드나 UnityEvent에서 부른다.")]
        [SerializeField] private List<LSO_ShakePreset> presets = new List<LSO_ShakePreset>();

        [Tooltip("이름 없이 Shake()만 불렀을 때 쓸 것. 비워두면 목록의 첫 번째.")]
        [SerializeField] private string defaultId;

        [Header("전체 세기")]
        [Tooltip("모든 흔들림에 곱한다. 0이면 흔들리지 않는다.\n" +
                 "화면 흔들림에 멀미를 느끼는 사람이 있어, 옵션에서 낮출 수 있게 열어둔다.")]
        [SerializeField, Range(0f, 2f)] private float globalScale = 1f;

        [Header("수신기")]
        [Tooltip("켜면 씬의 시네머신 카메라마다 수신기를 붙인다.\n" +
                 "\n" +
                 "끄면 카메라마다 CinemachineImpulseListener를 손으로 붙여야 하고,\n" +
                 "Channel Mask와 Gain을 직접 맞춰야 한다. 하나만 빠뜨려도\n" +
                 "그 카메라를 볼 때만 안 흔들려서 원인을 찾기 어렵다.")]
        [SerializeField] private bool attachListeners = true;

        [Tooltip("수신기에 줄 배율. 카메라 쪽에서 한 번 더 곱해진다. 보통 1.")]
        [SerializeField, Min(0f)] private float listenerGain = 1f;

        private CinemachineImpulseSource _source;

        /// <summary>목록에 아무것도 없을 때 쓸 한 벌. 조용히 아무 일도 안 하는 것을 막는다.</summary>
        private readonly LSO_ShakePreset _fallback = new LSO_ShakePreset { id = "기본" };

        /// <summary>지금 전체 세기. 옵션 화면에서 읽고 쓴다.</summary>
        public float GlobalScale
        {
            get => globalScale;
            set => globalScale = Mathf.Clamp(value, 0f, 2f);
        }

        protected override void Awake()
        {
            base.Awake();

            // base.Awake는 이미 있는 것이 있으면 자신을 지운다. 그 뒤로는 아무것도 하지 않는다.
            if (Instance != this) return;

            _source = GetComponent<CinemachineImpulseSource>();

            if (_source == null)
                _source = gameObject.AddComponent<CinemachineImpulseSource>();

            if (attachListeners) AttachListeners();
        }

        /// <summary>이름으로 흔든다. 버튼이나 UnityEvent에서 부를 수 있다.</summary>
        public void Shake(string id)
        {
            LSO_ShakePreset preset = Find(id);

            if (preset == null)
            {
                Debug.LogWarning($"{name}: '{id}' 흔들림을 찾지 못했습니다.", this);
                return;
            }

            Play(preset);
        }

        /// <summary>기본 세기로 흔든다.</summary>
        public void Shake()
        {
            Play(Default);
        }

        /// <summary>
        /// 목록에 없는 세기로 한 번만 흔든다.
        ///
        /// 값을 코드에 적게 되므로 되도록 쓰지 않는다.
        /// 데미지에 비례해 흔드는 것처럼 그때그때 계산해야 할 때만 쓸 것.
        /// </summary>
        public void Shake(LSO_ShakePreset preset)
        {
            Play(preset);
        }

        private void Play(LSO_ShakePreset preset)
        {
            if (preset == null) return;

            if (globalScale <= 0f) return;

            if (_source == null)
            {
                Debug.LogWarning($"{name}: 임펄스 소스가 없어 흔들지 못했습니다.", this);
                return;
            }

            preset.ApplyTo(_source.ImpulseDefinition);

            _source.GenerateImpulseWithVelocity(preset.Velocity * globalScale);
        }

        private LSO_ShakePreset Default
        {
            get
            {
                LSO_ShakePreset byId = Find(defaultId);

                if (byId != null) return byId;

                return presets.Count > 0 && presets[0] != null ? presets[0] : _fallback;
            }
        }

        private LSO_ShakePreset Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (LSO_ShakePreset preset in presets)
            {
                if (preset != null && preset.Key == id) return preset;
            }

            return null;
        }

        /// <summary>
        /// 씬의 시네머신 카메라마다 수신기를 붙인다.
        ///
        /// AddComponent는 인스펙터로 붙일 때와 달리 Reset을 부르지 않는다.
        /// 그래서 Channel Mask와 Gain이 0인 채로 붙고, 신호를 받아도 아무 일이 없다.
        /// 아무 소리도 안 나는 실패라 값을 직접 채워준다.
        /// </summary>
        private void AttachListeners()
        {
            CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (cameras.Length == 0)
            {
                Debug.LogWarning($"{name}: 씬에 시네머신 카메라가 없어 흔들림이 보이지 않습니다.", this);
                return;
            }

            foreach (CinemachineCamera camera in cameras)
            {
                if (camera.GetComponent<CinemachineImpulseListener>() != null) continue;

                CinemachineImpulseListener listener =
                    camera.gameObject.AddComponent<CinemachineImpulseListener>();

                listener.ApplyAfter = CinemachineCore.Stage.Noise;
                listener.ChannelMask = Channel;
                listener.Gain = listenerGain;
                listener.Use2DDistance = false;
                listener.UseCameraSpace = true;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<string>();

            for (int i = 0; i < presets.Count; i++)
            {
                LSO_ShakePreset preset = presets[i];

                if (preset == null) continue;

                // 이름이 없으면 부를 방법이 없다. 목록에만 남아 아무도 못 쓴다.
                if (string.IsNullOrEmpty(preset.Key))
                {
                    Debug.LogWarning($"{name}: Presets {i}번에 이름이 없습니다.", this);
                    continue;
                }

                // 같은 이름이 둘이면 앞의 것만 잡히고 뒤의 것은 영영 안 쓰인다.
                if (!seen.Add(preset.Key))
                    Debug.LogWarning($"{name}: '{preset.Key}' 이름이 두 번 쓰였습니다.", this);
            }
        }

        [ContextMenu("테스트: 기본 세기로 흔들기")]
        private void TestShake()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 중에만 흔들 수 있습니다.", this);
                return;
            }

            Shake();
        }
#endif
    }
}
