using System;
using _Scripts.LSO.UI.Text;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.Will.Candle
{
    /// <summary>
    /// 카드에 촛불을 대면 유언 아이콘이 열에 배어나오듯 드러난다.
    /// 숨은 글씨가 불에 드러나는 모양이다.
    ///
    /// ── 새 셰이더를 안 만든 이유 ──────────────────────────────
    /// LDY_Dissolve 셰이더가 이미 필요한 것을 다 갖고 있다.
    /// 디졸브는 사라지는 효과지만 _DissolveAmount 를 1 → 0 으로 돌리면
    /// 반대로 드러난다. _EdgeColor 기본값이 HDR (3.0, 1.0, 0.2) 라
    /// 번지는 가장자리가 이미 불씨 색이다.
    ///
    /// _DirBias 에 촛불 방향을 넣으면 불을 댄 쪽에서부터 번진다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 빈 초(유언 없음)를 대면 아이콘 대신 그을음이 번진다.
    /// 불이 없어서 아무것도 배어나오지 않았다는 뜻이고,
    /// "아직 안 골랐다"와 "없음을 골랐다"가 눈으로 구분된다.
    ///
    /// 씬 배선: 카드의 유언 아이콘 자리(Quad 등)에 붙이고,
    /// 그 Renderer 에 LDY_Dissolve 를 쓰는 머티리얼을 넣을 것.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_WillRevealEffect : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("아이콘이 그려질 곳. 비워두면 이 오브젝트에서 찾는다.\n" +
                 "머티리얼은 LDY_Dissolve 셰이더를 써야 한다.")]
        [SerializeField] private Renderer target;

        [Header("그을음")]
        [Tooltip("빈 초(유언 없음)를 댔을 때 번질 자국. 비워두면 아무것도 안 드러난다.")]
        [SerializeField] private Texture2D sootTexture;

        [Header("연출")]
        [Tooltip("드러나는 데 걸리는 시간.")]
        [SerializeField, Min(0.01f)] private float duration = 0.5f;

        [Tooltip("번지는 곡선. 처음에 빠르고 끝에서 천천히 스며드는 편이 종이처럼 보인다.")]
        [SerializeField] private Ease ease = Ease.OutCubic;

        [Tooltip("종이 결의 거칠기. 클수록 잘게 얼룩진다.")]
        [SerializeField, Min(0f)] private float noiseScale = 8f;

        [Tooltip("번지는 가장자리의 두께.")]
        [SerializeField, Range(0.001f, 0.3f)] private float edgeWidth = 0.06f;

        [Tooltip("불을 댄 쪽으로 치우치는 세기. 0이면 사방에서 고르게 번진다.")]
        [SerializeField, Min(0f)] private float directionStrength = 1f;

        // LDY_Dissolve 의 속성 이름. 셰이더가 바뀌면 여기도 같이 고칠 것.
        private static readonly int DissolveId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
        private static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
        private static readonly int DirBiasId = Shader.PropertyToID("_DirBias");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        private Material _material;
        private Tween _tween;

        /// <summary>지금 드러나 있는 유언. 아무것도 안 드러났으면 None.</summary>
        public LSO_WillType Current { get; private set; } = LSO_WillType.None;

        /// <summary>드러나는 중인지.</summary>
        public bool IsPlaying => _tween != null && _tween.IsActive() && _tween.IsPlaying();

        private void Awake()
        {
            if (target == null) target = GetComponent<Renderer>();

            if (target == null)
            {
                Debug.LogError($"{name}: Renderer가 없어 아이콘을 그릴 수 없습니다.", this);
                return;
            }

            // 머티리얼 인스턴스를 하나만 만들어 들고 간다.
            // target.material 을 매번 읽으면 호출할 때마다 새 인스턴스가 생겨 씬에 쌓인다.
            _material = target.material;

            Clear();
        }

        private void OnDestroy()
        {
            _tween?.Kill();

            // Awake 에서 만든 인스턴스는 우리가 치운다.
            if (_material != null) Destroy(_material);
        }

        private void OnDisable()
        {
            // 도중에 꺼지면 반쯤 드러난 채로 굳는다. 끝 상태로 못 박는다.
            _tween?.Kill();
            _tween = null;

            if (_material != null)
                _material.SetFloat(DissolveId, Current == LSO_WillType.None ? 1f : 0f);
        }

        /// <summary>
        /// 유언을 드러낸다. 불을 댄 방향을 주면 그쪽에서부터 번진다.
        /// </summary>
        /// <param name="type">붙일 유언. None이면 그을음이 번진다.</param>
        /// <param name="from">불이 있던 월드 좌표. 없으면 사방에서 고르게 번진다.</param>
        /// <param name="onComplete">다 드러난 뒤에 부를 것.</param>
        public void Play(LSO_WillType type, Vector3? from = null, Action onComplete = null)
        {
            if (_material == null)
            {
                onComplete?.Invoke();
                return;
            }

            _tween?.Kill();

            Current = type;

            ApplyTexture(type);
            ApplyDirection(from);

            _material.SetFloat(NoiseScaleId, noiseScale);
            _material.SetFloat(EdgeWidthId, edgeWidth);

            // 1 = 아무것도 안 보임, 0 = 다 드러남. 디졸브를 거꾸로 쓰는 것이 핵심이다.
            _material.SetFloat(DissolveId, 1f);

            if (!isActiveAndEnabled)
            {
                // 꺼져 있으면 트윈이 돌지 않는다. 결과만 못 박는다.
                _material.SetFloat(DissolveId, 0f);
                onComplete?.Invoke();
                return;
            }

            // Material.DOFloat 대신 DOVirtual 을 쓴다.
            // 머티리얼 확장은 DOTween 설치 구성에 따라 없을 수 있는데,
            // DOVirtual 은 어디서나 있고 이 프로젝트가 이미 쓰고 있다.
            _tween = DOVirtual
                .Float(1f, 0f, duration, value =>
                {
                    if (_material != null) _material.SetFloat(DissolveId, value);
                })
                .SetEase(ease)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _tween = null;
                    onComplete?.Invoke();
                });
        }

        /// <summary>아무것도 안 드러난 상태로 되돌린다. 카드를 다시 쓸 때 부른다.</summary>
        public void Clear()
        {
            _tween?.Kill();
            _tween = null;

            Current = LSO_WillType.None;

            if (_material != null)
                _material.SetFloat(DissolveId, 1f);
        }

        /// <summary>
        /// 드러날 그림을 넣는다. 유언이면 그 아이콘, 없음이면 그을음.
        ///
        /// 아이콘은 사전(DLJ_WillDataSO)에서 가져온다. 여기서 유언마다
        /// 텍스처를 또 들고 있으면 사전과 어긋날 자리가 하나 더 생긴다.
        /// </summary>
        private void ApplyTexture(LSO_WillType type)
        {
            if (type == LSO_WillType.None)
            {
                _material.SetTexture(BaseMapId, sootTexture);

                if (sootTexture == null)
                {
                    Debug.LogWarning(
                        $"{name}: 그을음 텍스처가 없어 '유언 없음'이 화면에 드러나지 않습니다. " +
                        "안 고른 것과 구분되지 않습니다.", this);
                }

                return;
            }

            Sprite icon = LSO_WillText.IconOf(type);

            if (icon == null)
            {
                Debug.LogWarning(
                    $"{name}: {LSO_WillText.NameOf(type)} 유언의 아이콘이 없습니다. " +
                    "DLJ_WillDataSO 에 아이콘을 넣어 주세요.", this);
            }

            _material.SetTexture(BaseMapId, icon != null ? icon.texture : null);
        }

        /// <summary>
        /// 불이 있던 쪽에서부터 번지게 방향을 넣는다.
        ///
        /// _DirBias 는 XYZ가 방향, W가 세기다. 이 오브젝트의 로컬 기준으로 넣어야
        /// 카드가 어느 쪽을 보고 있든 불 쪽에서 번진다.
        /// </summary>
        private void ApplyDirection(Vector3? from)
        {
            if (from == null || directionStrength <= 0f)
            {
                _material.SetVector(DirBiasId, Vector4.zero);
                return;
            }

            Vector3 local = transform.InverseTransformPoint(from.Value);

            if (local.sqrMagnitude <= Mathf.Epsilon)
            {
                _material.SetVector(DirBiasId, Vector4.zero);
                return;
            }

            local.Normalize();

            _material.SetVector(
                DirBiasId,
                new Vector4(local.x, local.y, local.z, directionStrength));
        }
    }
}
