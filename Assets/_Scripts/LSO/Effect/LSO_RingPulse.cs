using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.Effect
{
    /// <summary>
    /// LSO/Expanding Ring 쉐이더의 진행도를 한 번 밀어준다.
    ///
    /// ── 여기가 연출 길이를 정하는 유일한 곳이다 ────────────────
    /// 쉐이더는 "지금 얼마나 퍼졌는가"만 안다. 얼마나 걸리는지는 모른다.
    /// 그래서 속도를 맞추려고 쉐이더와 오브젝트 수명을 따로 손볼 일이 없다 —
    /// 여기 적힌 duration 하나가 곧 연출의 길이다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 켜질 때마다 처음부터 돈다. 특성 이펙트처럼 띄웠다 치우는 쪽에서도,
    /// 풀에서 꺼내 다시 쓰는 쪽에서도 따로 불러줄 것이 없다.
    ///
    /// 머티리얼을 복제하지 않고 MaterialPropertyBlock 을 쓴다. 인스턴스마다
    /// Material 을 만들면 링을 여러 개 띄울 때 머티리얼이 그만큼 새로 생기고,
    /// 에디터에서 멈췄을 때 누수로 남는다.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    [DisallowMultipleComponent]
    public sealed class LSO_RingPulse : MonoBehaviour
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        [Tooltip("한 번 퍼지는 데 걸리는 시간(초).")]
        [SerializeField, Min(0.01f)] private float duration = 0.8f;

        [Tooltip("퍼지는 속도 곡선. 왼쪽이 시작, 오른쪽이 끝이다.\n" +
                 "\n" +
                 "처음에 빠르고 끝에서 느려지는 모양이 보통 낫다 —\n" +
                 "터져 나갔다가 힘이 풀리는 것처럼 보인다.")]
        [SerializeField] private AnimationCurve curve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("끝나면 이 오브젝트를 끌지.\n" +
                 "\n" +
                 "특성 이펙트처럼 띄운 쪽이 치워주는 경우에는 꺼둬도 된다.\n" +
                 "풀에서 돌려쓰는 경우에는 켜두는 편이 맞다.")]
        [SerializeField] private bool disableWhenDone;

        [Tooltip("시간 배율을 무시할지.\n" +
                 "\n" +
                 "유언·계승 연출이 timeScale 을 쥐는 구간이 있다.\n" +
                 "그때 링만 멈춰 서 있으면 화면이 어긋난다.")]
        [SerializeField] private bool useUnscaledTime = true;

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private Tween _tween;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();

            WarnIfWrongShader();
        }

        private void OnEnable()
        {
            Replay();
        }

        /// <summary>
        /// 꺼질 때 트윈을 끊는다.
        ///
        /// SetLink 는 오브젝트가 **파괴될** 때를 맡는다. 풀에 돌려놓느라 꺼두는
        /// 경우는 파괴가 아니라서, 안 끊으면 꺼진 동안에도 계속 돌며 죽은
        /// 렌더러에 값을 밀어넣는다.
        /// </summary>
        private void OnDisable()
        {
            KillTween();

#if UNITY_EDITOR
            StopEditorPreview();
#endif
        }

        // ============================================================
        // 인스펙터에서 눌러보기 (톱니바퀴 메뉴)
        // ============================================================

        /// <summary>
        /// 한 번 퍼지는 것을 지금 본다.
        ///
        /// 플레이 중이면 실제와 똑같이 돈다. 편집 중이면 에디터가 시간을 밀어준다 —
        /// DOTween 은 편집 모드에서 돌지 않으므로 그쪽 길을 따로 둔다.
        /// </summary>
        [ContextMenu("▶ 재생")]
        private void ContextPlay()
        {
            if (Application.isPlaying)
            {
                Replay();
                return;
            }

#if UNITY_EDITOR
            StartEditorPreview();
#else
            Debug.LogWarning($"{name}: 플레이 중에만 재생할 수 있습니다.", this);
#endif
        }

        /// <summary>퍼지기 전 모습. 링도 파티클도 아무것도 안 보이는 것이 맞다.</summary>
        [ContextMenu("■ 처음으로")]
        private void ContextRewind()
        {
            SetProgress(0f);

            // 미리보기가 남겨둔 입자까지 치운다. 안 치우면 링만 사라지고
            // 파티클은 공중에 그대로 떠 있다.
            StopParticles();
        }

        /// <summary>다 퍼진 순간. 링이 어디까지 가는지 볼 때 쓴다.</summary>
        [ContextMenu("끝 모습 보기")]
        private void ContextEnd()
        {
            SetProgress(1f);
        }

        /// <summary>처음부터 다시 퍼지게 한다. 자식 파티클도 같이 처음으로 돌린다.</summary>
        public void Replay()
        {
            KillTween();

            // 첫 프레임부터 0 으로 그린다. 안 그러면 지난번에 다 퍼진 모습이
            // 한 프레임 비친다 — 풀에서 꺼낼 때 눈에 띈다.
            Apply(0f);

            PlayParticles();

            _tween = DOVirtual
                .Float(0f, 1f, duration, Apply)
                .SetEase(curve)
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject)
                .OnComplete(HandleComplete);
        }

        /// <summary>
        /// 진행도를 직접 넣는다. 밖에서 타이밍을 쥐고 싶을 때 쓴다.
        ///
        /// 부르는 순간 스스로 도는 것은 멈춘다. 두 곳이 같은 값을 밀면
        /// 어느 쪽이 맞는지 정할 방법이 없다.
        /// </summary>
        public void SetProgress(float value)
        {
            KillTween();

#if UNITY_EDITOR
            StopEditorPreview();
#endif

            Apply(Mathf.Clamp01(value));
        }

        private void HandleComplete()
        {
            _tween = null;

            if (disableWhenDone) gameObject.SetActive(false);
        }

        private void KillTween()
        {
            if (_tween == null) return;

            _tween.Kill();
            _tween = null;
        }

        // ============================================================
        // 자식 파티클
        // ============================================================

        /// <summary>
        /// 시간을 밀어줄 파티클을 모은다.
        ///
        /// **맨 위의 것만 모은다.** Play 도 Simulate 도 자식까지 같이 돌리므로,
        /// 자식을 따로 넣으면 같은 파티클이 두 번 돌아 이상하게 빨라진다.
        ///
        /// 쓸 때마다 다시 모은다. 들고 있으면 자식이 바뀌었을 때 옛 목록이 남는데,
        /// 자주 부르는 자리가 아니라 다시 세는 값이 더 싸다.
        /// </summary>
        private ParticleSystem[] CollectParticleRoots()
        {
            ParticleSystem[] all = GetComponentsInChildren<ParticleSystem>(true);

            var roots = new System.Collections.Generic.List<ParticleSystem>(all.Length);

            foreach (ParticleSystem ps in all)
            {
                if (ps == null) continue;

                // 위에 또 파티클이 있으면 그쪽이 같이 돌려준다.
                if (ps.transform.parent != null &&
                    ps.transform.parent.GetComponentInParent<ParticleSystem>() != null)
                    continue;

                roots.Add(ps);
            }

            return roots.ToArray();
        }

        /// <summary>파티클을 처음부터 다시 튼다. 플레이 중에만 뜻이 있다.</summary>
        private void PlayParticles()
        {
            foreach (ParticleSystem ps in CollectParticleRoots())
            {
                if (ps == null) continue;

                // 지난번 입자가 남아 있으면 처음부터 다시 도는 것으로 안 보인다.
                ps.Clear(true);
                ps.Play(true);
            }
        }

        /// <summary>파티클을 멈추고 지운다.</summary>
        private void StopParticles()
        {
            foreach (ParticleSystem ps in CollectParticleRoots())
            {
                if (ps == null) continue;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void Apply(float progress)
        {
            // 편집 중에는 Awake 가 돌지 않았을 수 있다. 인스펙터에서 눌러 부르는
            // 길이 생겼으므로 여기서 한 번 챙긴다.
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            _block ??= new MaterialPropertyBlock();

            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(ProgressId, progress);
            _renderer.SetPropertyBlock(_block);
        }

#if UNITY_EDITOR
        // ============================================================
        // 편집 모드 미리보기
        //
        // DOTween 은 편집 모드에서 돌지 않는다. 플레이를 켜지 않고 값을 맞추려면
        // 에디터가 시간을 밀어주는 수밖에 없다.
        //
        // 특성 이펙트 미리보기(LSO_AbilityCatalogEditor)와 같은 방식이다.
        // ============================================================

        private double _previewStartedAt;
        private double _previewLastTick;
        private bool _previewing;
        private ParticleSystem[] _previewParticles;

        private void StartEditorPreview()
        {
            StopEditorPreview();

            _previewStartedAt = UnityEditor.EditorApplication.timeSinceStartup;
            _previewLastTick = _previewStartedAt;
            _previewing = true;

            // 미리보는 동안만 들고 있는다. 매 틱마다 다시 모으면 그만큼 느려진다.
            _previewParticles = CollectParticleRoots();

            foreach (ParticleSystem ps in _previewParticles)
            {
                if (ps == null) continue;

                ps.Clear(true);
                ps.Simulate(0f, true, true, false);
            }

            UnityEditor.EditorApplication.update += TickEditorPreview;

            Apply(0f);
        }

        private void StopEditorPreview()
        {
            if (!_previewing) return;

            _previewing = false;
            _previewParticles = null;

            UnityEditor.EditorApplication.update -= TickEditorPreview;
        }

        private void TickEditorPreview()
        {
            // 미리보는 사이에 지워졌을 수 있다. 안 끊으면 에디터가 죽은 것을 계속 부른다.
            if (this == null)
            {
                UnityEditor.EditorApplication.update -= TickEditorPreview;
                return;
            }

            double now = UnityEditor.EditorApplication.timeSinceStartup;
            float delta = (float)(now - _previewLastTick);
            _previewLastTick = now;

            float t = (float)((now - _previewStartedAt) / Mathf.Max(0.01f, duration));

            t = Mathf.Clamp01(t);

            Apply(curve.Evaluate(t));

            // 지난 프레임에서 흐른 만큼만 더 민다(restart: false).
            //
            // 매번 0부터 다시 돌리면(restart: true) 자동 시드가 켜진 파티클은
            // 그때마다 다른 난수가 나와 깜박인다. 이어서 밀면 시드를 건드릴
            // 필요가 없다 — 남의 프리팹 값을 말없이 바꾸지 않아도 된다.
            foreach (ParticleSystem ps in _previewParticles)
            {
                if (ps == null) continue;

                ps.Simulate(delta, true, false, false);
            }

            UnityEditor.SceneView.RepaintAll();

            if (t >= 1f) StopEditorPreview();
        }
#endif

        /// <summary>
        /// 진행도를 받을 줄 모르는 머티리얼이 꽂혀 있으면 알린다.
        ///
        /// 조용히 두면 "링이 안 움직인다"만 남는다. 값은 정상적으로 들어가는데
        /// 받는 쪽에 그 칸이 없는 것이라, 쉐이더를 의심하기까지 한참 걸린다.
        /// </summary>
        private void WarnIfWrongShader()
        {
            Material material = _renderer != null ? _renderer.sharedMaterial : null;

            if (material == null)
            {
                Debug.LogWarning($"{name}: 머티리얼이 없어 링이 그려지지 않습니다.", this);
                return;
            }

            if (material.HasProperty(ProgressId)) return;

            Debug.LogWarning(
                $"{name}: 머티리얼 '{material.name}' 에 _Progress 가 없습니다. " +
                "쉐이더를 LSO/Expanding Ring 으로 바꿔주세요. " +
                "지금은 진행도를 밀어도 화면이 바뀌지 않습니다.",
                this);
        }
    }
}
