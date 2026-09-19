using System.Collections.Generic;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Ability.Catalog;
using UnityEditor;
using UnityEngine;

namespace _Scripts.LSO.Editor
{
    /// <summary>
    /// 특성 사전 인스펙터에 **이펙트를 씬에 띄워보는 자리**를 붙인다.
    ///
    /// ── 왜 필요했나 ───────────────────────────────────────────
    /// 위치·회전·크기를 맞추려면 플레이를 켜고, 늑대를 뽑고, 옆에 놓아서
    /// 이펙트가 터지는 순간을 봐야 했다. 한 번 보고 값을 고치면 다시 그 과정을
    /// 처음부터 반복해야 한다.
    ///
    /// 여기서는 플레이 없이 씬에 그대로 띄운다. 값을 고치면 곧바로 다시 놓이므로
    /// 화면을 보면서 숫자를 맞출 수 있다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 자리 계산은 LSO_AbilityEffectPlacement 를 쓴다. 실제 재생과 같은 코드라
    /// **여기서 맞춘 값이 게임에서도 그대로 나온다.**
    /// </summary>
    [CustomEditor(typeof(LSO_AbilityCatalogSO))]
    public class LSO_AbilityCatalogEditor : UnityEditor.Editor
    {
        private const string PreviewName = "~AbilityEffectPreview";

        private LSO_AbilityType _picked = LSO_AbilityType.None;
        private Transform _at;
        private GameObject _preview;
        private GameObject _previewPrefab;
        private bool _follow = true;

        // 편집 모드에서는 파티클이 저절로 돌지 않는다. 시간을 손으로 밀어줘야 한다.
        private ParticleSystem[] _roots;
        private double _lastTick;
        private float _time;
        private float _length = 1f;
        private bool _playing = true;

        private void OnEnable()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;

            Clear();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("이펙트 미리보기", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "플레이 중에는 실제 재생을 쓰세요. 미리보기는 편집 중에만 씁니다.",
                    MessageType.Info);

                return;
            }

            DrawPicker();
            DrawButtons();
            DrawPlayback();
            DrawStatus();

            if (_follow && _preview != null)
            {
                if (TryGetInfo(out LSO_AbilityInfo info) && info.effectPrefab != _previewPrefab)
                    Spawn();
                else
                    Place();
            }
        }

        /// <summary>
        /// 파티클 시간을 밀어준다.
        ///
        /// 편집 모드에는 게임 루프가 없어서 ParticleSystem 이 스스로 돌지 않는다.
        /// 유니티가 파티클 프리팹을 열었을 때 보여주는 미리보기도 이렇게 만든다.
        ///
        /// restart:true 로 매번 0부터 _time 까지 다시 돌린다. 절대 시간으로 계산되므로
        /// 에디터 프레임이 끊겨도 화면이 튀지 않는다.
        /// </summary>
        private void Tick()
        {
            if (_preview == null || _roots == null) return;

            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - _lastTick);
            _lastTick = now;

            if (_playing)
            {
                _time += delta;

                // 끝까지 가면 처음으로 돌아간다. 한 번 보고 마는 것보다 반복해서 보는 편이 맞추기 쉽다.
                if (_time > _length) _time = 0f;
            }

            foreach (ParticleSystem ps in _roots)
            {
                if (ps == null) continue;

                // fixedTimeStep 을 끈다. 켜두면 정해진 간격으로만 계산해서
                // 슬라이더를 천천히 끌 때 값이 계단처럼 튄다.
                ps.Simulate(_time, true, true, false);
            }

            SceneView.RepaintAll();
            Repaint();
        }

        private void DrawPicker()
        {
            var catalog = (LSO_AbilityCatalogSO)target;

            List<LSO_AbilityType> withEffect = new List<LSO_AbilityType>();

            foreach (LSO_AbilityInfo info in catalog.All)
            {
                if (info.effectPrefab != null) withEffect.Add(info.type);
            }

            if (withEffect.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Effect Prefab 이 꽂힌 특성이 없습니다. 먼저 위 목록에 프리팹을 넣으세요.",
                    MessageType.Warning);

                return;
            }

            string[] labels = new string[withEffect.Count];

            for (int i = 0; i < withEffect.Count; i++)
                labels[i] = LSO_AbilityText.NameOf(withEffect[i]) + $"  ({withEffect[i]})";

            int current = Mathf.Max(0, withEffect.IndexOf(_picked));
            int next = EditorGUILayout.Popup("특성", current, labels);

            bool changed = _picked != withEffect[next];
            _picked = withEffect[next];

            if (changed && _preview != null) Spawn();

            _at = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("놓을 자리", "비워두면 원점(0,0,0)에 놓는다. 기물이나 타일을 꽂으면 그 자리에 놓인다."),
                _at, typeof(Transform), true);

            _follow = EditorGUILayout.Toggle(
                new GUIContent("값 바꾸면 바로 반영", "끄면 '다시 놓기'를 눌러야 반영된다."),
                _follow);
        }

        private void DrawButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(_preview == null ? "띄우기" : "다시 놓기"))
                    Spawn();

                using (new EditorGUI.DisabledScope(_preview == null))
                {
                    if (GUILayout.Button("치우기")) Clear();
                }
            }
        }

        private void DrawPlayback()
        {
            if (_preview == null || _roots == null || _roots.Length == 0) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                _playing = GUILayout.Toggle(
                    _playing, _playing ? "■ 멈춤" : "▶ 재생", EditorStyles.miniButton, GUILayout.Width(70f));

                _time = EditorGUILayout.Slider(_time, 0f, _length);
            }
        }

        private void DrawStatus()
        {
            if (_preview == null) return;

            Transform t = _preview.transform;

            EditorGUILayout.HelpBox(
                $"자리 {t.position}\n회전 {t.eulerAngles}\n크기 {t.localScale}",
                MessageType.None);

            if (_roots == null || _roots.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "이 프리팹에 ParticleSystem 이 없습니다. 모델이나 스프라이트만 있다면 " +
                    "그대로 보이는 것이 맞고, 파티클을 넣었는데 안 보이면 프리팹 쪽을 확인하세요.",
                    MessageType.Warning);
            }

            EditorGUILayout.HelpBox(
                "씬에 잠깐 띄운 것이라 저장되지 않습니다. 인스펙터를 닫으면 사라집니다.\n" +
                "씬 뷰에서만 보입니다 — 게임 뷰에는 안 나옵니다.",
                MessageType.None);
        }

        private void Spawn()
        {
            Clear();

            if (!TryGetInfo(out LSO_AbilityInfo info)) return;

            _preview = Instantiate(info.effectPrefab);
            _previewPrefab = info.effectPrefab;
            _preview.name = PreviewName;

            // 씬에 저장되지 않고 하이어라키에도 안 보인다. 지우는 것을 잊어도 남지 않는다.
            _preview.hideFlags = HideFlags.HideAndDontSave;

            CollectParticleRoots();

            // 첫 화면부터 입자가 보이게 하고 실제 크기로 프레이밍한다.
            _time = 0.1f;
            _playing = true;
            _lastTick = EditorApplication.timeSinceStartup;

            Place();

            foreach (ParticleSystem ps in _roots)
                ps.Simulate(_time, true, true, false);

            Bounds bounds = new Bounds(_preview.transform.position, Vector3.one);
            foreach (ParticleSystem ps in _preview.GetComponentsInChildren<ParticleSystem>())
            {
                if (ps.particleCount > 0 && ps.TryGetComponent(out ParticleSystemRenderer renderer))
                    bounds.Encapsulate(renderer.bounds);
            }
            bounds.Expand(0.5f);

            // 씬 뷰가 이 자리를 비추게 한다. 안 그러면 화면 밖에 놓여 안 보인다.
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.Frame(bounds, false);
        }

        /// <summary>
        /// 시간을 밀어줄 파티클을 모으고, 한 바퀴가 몇 초인지 잰다.
        ///
        /// **맨 위의 것만 모은다.** Simulate 는 자식까지 같이 돌리므로,
        /// 자식을 따로 넣으면 같은 파티클이 두 번 돌아 이상하게 빨라진다.
        /// </summary>
        private void CollectParticleRoots()
        {
            var roots = new List<ParticleSystem>();
            float longest = 0f;

            foreach (ParticleSystem ps in _preview.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = ps.main;

                // 시드를 고정한다.
                //
                // 매 프레임 0부터 다시 돌리는데(restart:true) 자동 시드가 켜져 있으면
                // 그때마다 다른 난수가 나와 모양이 달라진다. 그게 깜박임으로 보인다.
                // 시드를 박아두면 같은 시각은 언제나 같은 모양이라 이어져 보인다.
                //
                // 미리보기 사본에만 손대는 것이라 프리팹 원본은 그대로다.

                ps.useAutoRandomSeed = false;
                ps.randomSeed = 1234;

                longest = Mathf.Max(
                    longest,
                    main.duration + main.startDelay.constantMax + main.startLifetime.constantMax);

                // 위에 또 파티클이 있으면 그쪽이 같이 돌려준다.
                if (ps.transform.parent != null &&
                    ps.transform.parent.GetComponentInParent<ParticleSystem>() != null)
                    continue;

                roots.Add(ps);
            }

            _roots = roots.ToArray();
            _length = Mathf.Max(0.1f, longest);
        }

        private void Place()
        {
            if (_preview == null) return;
            if (!TryGetInfo(out LSO_AbilityInfo info)) return;

            Vector3 at = _at != null ? _at.position : Vector3.zero;

            LSO_AbilityEffectPlacement.Apply(_preview.transform, info, at);
        }

        private bool TryGetInfo(out LSO_AbilityInfo info)
        {
            var catalog = (LSO_AbilityCatalogSO)target;

            if (catalog.TryGet(_picked, out info) && info.effectPrefab != null) return true;

            info = null;
            return false;
        }

        private void Clear()
        {
            if (_preview == null) return;

            DestroyImmediate(_preview);
            _preview = null;
            _previewPrefab = null;
            _roots = null;
        }
    }
}
