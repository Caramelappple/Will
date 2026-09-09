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
    /// ── 그림은 스프라이트로 갈아끼운다 ─────────────────────────
    /// 머티리얼의 텍스처를 바꾸지 않는다. DLJ_WillDataSO.icon 은 Sprite 이고,
    /// 아틀라스에 묶이면 sprite.texture 는 아틀라스 전체를 가리킨다.
    /// 그걸 그대로 넣으면 엉뚱한 그림이 나온다.
    ///
    /// SpriteRenderer.sprite 를 바꾸면 아틀라스든 아니든 그 스프라이트만 그려진다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// ── 번지는 것은 셰이더가 한다 ──────────────────────────────
    /// LDY_Dissolve 의 _DissolveAmount 를 1 → 0 으로 돌리면 사라지는 대신 드러난다.
    /// _EdgeColor 기본값이 HDR (3.0, 1.0, 0.2) 라 가장자리가 이미 불씨 색이다.
    ///
    /// 그 속성이 없는 머티리얼이면 **투명도로 대신 페이드한다.**
    /// 셰이더를 아직 안 붙였어도 아이콘이 나오긴 해야 배선을 확인할 수 있다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 빈 초(유언 없음)를 대면 아이콘 대신 그을음이 번진다.
    ///
    /// 씬 배선: 카드의 유언 아이콘 자리(SpriteRenderer)에 붙일 것.
    /// 번지는 연출까지 쓰려면 그 머티리얼을 LDY/Dissolve 셰이더로 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_WillRevealEffect : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("아이콘을 그릴 곳. 비워두면 이 오브젝트에서 찾는다.")]
        [SerializeField] private SpriteRenderer target;

        [Header("그을음")]
        [Tooltip("빈 초(유언 없음)를 댔을 때 번질 자국.\n" +
                 "비워두면 아무것도 안 드러나서 '안 고른 것'과 구분되지 않는다.")]
        [SerializeField] private Sprite sootSprite;

        [Header("연출")]
        [Tooltip("드러나는 데 걸리는 시간.")]
        [SerializeField, Min(0.01f)] private float duration = 0.5f;

        [Tooltip("번지는 곡선. 처음에 빠르고 끝에서 천천히 스며드는 편이 종이처럼 보인다.")]
        [SerializeField] private Ease ease = Ease.OutCubic;

        [Tooltip("종이 결의 거칠기. 클수록 잘게 얼룩진다. 디졸브 셰이더일 때만 쓰인다.")]
        [SerializeField, Min(0f)] private float noiseScale = 8f;

        [Tooltip("번지는 가장자리의 두께. 디졸브 셰이더일 때만 쓰인다.")]
        [SerializeField, Range(0.001f, 0.3f)] private float edgeWidth = 0.06f;

        [Tooltip("불을 댄 쪽으로 치우치는 세기. 0이면 사방에서 고르게 번진다.")]
        [SerializeField, Min(0f)] private float directionStrength = 1f;

        // LDY_Dissolve 의 속성 이름. 셰이더가 바뀌면 여기도 같이 고칠 것.
        private static readonly int DissolveId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
        private static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
        private static readonly int DirBiasId = Shader.PropertyToID("_DirBias");

        private MaterialPropertyBlock _block;
        private Tween _tween;
        private bool _canDissolve;

        /// <summary>지금 드러나 있는 유언. 아무것도 안 드러났으면 None.</summary>
        public LSO_WillType Current { get; private set; } = LSO_WillType.None;

        /// <summary>드러나는 중인지.</summary>
        public bool IsPlaying => _tween != null && _tween.IsActive() && _tween.IsPlaying();

        private void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();

            if (target == null)
            {
                Debug.LogError($"{name}: SpriteRenderer가 없어 아이콘을 그릴 수 없습니다.", this);
                return;
            }

            _block = new MaterialPropertyBlock();

            // sharedMaterial 을 보는 이유는 material 을 읽는 순간 인스턴스가 하나 생기기 때문이다.
            // 값은 MaterialPropertyBlock 으로 넣으므로 인스턴스가 필요 없다.
            _canDissolve =
                target.sharedMaterial != null && target.sharedMaterial.HasProperty(DissolveId);

            if (!_canDissolve)
            {
                Debug.LogWarning(
                    $"{name}: 머티리얼에 _DissolveAmount 가 없어 번지는 연출 대신 투명도로 드러냅니다. " +
                    "종이에 배어나오는 모양을 쓰려면 LDY/Dissolve 셰이더 머티리얼로 바꿔 주세요.", this);
            }

            Clear();
        }

        private void OnDisable()
        {
            // 도중에 꺼지면 반쯤 드러난 채로 굳는다. 끝 상태로 못 박는다.
            _tween?.Kill();
            _tween = null;

            SetReveal(Current == LSO_WillType.None && sootSprite == null ? 0f : 1f);
        }

        /// <summary>
        /// 유언을 드러낸다. 불을 댄 방향을 주면 그쪽에서부터 번진다.
        /// </summary>
        /// <param name="type">붙일 유언. None이면 그을음이 번진다.</param>
        /// <param name="from">불이 있던 월드 좌표. 없으면 사방에서 고르게 번진다.</param>
        /// <param name="onComplete">다 드러난 뒤에 부를 것.</param>
        public void Play(LSO_WillType type, Vector3? from = null, Action onComplete = null)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                return;
            }

            _tween?.Kill();

            Current = type;

            ApplySprite(type);
            ApplyShaderSettings(from);

            // 0 = 아무것도 안 보임, 1 = 다 드러남.
            SetReveal(0f);

            if (!isActiveAndEnabled)
            {
                // 꺼져 있으면 트윈이 돌지 않는다. 결과만 못 박는다.
                SetReveal(1f);
                onComplete?.Invoke();
                return;
            }

            _tween = DOVirtual
                .Float(0f, 1f, duration, SetReveal)
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

            SetReveal(0f);
        }

        /// <summary>
        /// 얼마나 드러났는지. 0이 안 보임, 1이 다 보임.
        ///
        /// 디졸브는 반대 방향이라(1이 사라짐) 뒤집어 넣는다.
        /// 셰이더가 없으면 투명도로 대신한다.
        /// </summary>
        private void SetReveal(float shown)
        {
            if (target == null) return;

            if (_canDissolve)
            {
                target.GetPropertyBlock(_block);
                _block.SetFloat(DissolveId, 1f - shown);
                target.SetPropertyBlock(_block);
                return;
            }

            Color color = target.color;
            color.a = shown;
            target.color = color;
        }

        /// <summary>
        /// 드러날 그림을 넣는다. 유언이면 그 아이콘, 없음이면 그을음.
        ///
        /// 아이콘은 사전(DLJ_WillDataSO)에서 가져온다. 여기서 유언마다
        /// 그림을 또 들고 있으면 사전과 어긋날 자리가 하나 더 생긴다.
        /// </summary>
        private void ApplySprite(LSO_WillType type)
        {
            if (type == LSO_WillType.None)
            {
                target.sprite = sootSprite;

                if (sootSprite == null)
                {
                    Debug.LogWarning(
                        $"{name}: 그을음 스프라이트가 없어 '유언 없음'이 화면에 드러나지 않습니다. " +
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

            target.sprite = icon;
        }

        /// <summary>
        /// 번지는 모양과 방향을 넣는다. 디졸브 셰이더가 아니면 할 일이 없다.
        ///
        /// _DirBias 는 XYZ가 방향, W가 세기다. 이 오브젝트의 로컬 기준으로 넣어야
        /// 카드가 어느 쪽을 보고 있든 불 쪽에서 번진다.
        /// </summary>
        private void ApplyShaderSettings(Vector3? from)
        {
            if (!_canDissolve) return;

            target.GetPropertyBlock(_block);

            _block.SetFloat(NoiseScaleId, noiseScale);
            _block.SetFloat(EdgeWidthId, edgeWidth);
            _block.SetVector(DirBiasId, ResolveDirection(from));

            target.SetPropertyBlock(_block);
        }

        private Vector4 ResolveDirection(Vector3? from)
        {
            if (from == null || directionStrength <= 0f) return Vector4.zero;

            Vector3 local = transform.InverseTransformPoint(from.Value);

            if (local.sqrMagnitude <= Mathf.Epsilon) return Vector4.zero;

            local.Normalize();

            return new Vector4(local.x, local.y, local.z, directionStrength);
        }
    }
}
