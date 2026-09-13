using System.Collections.Generic;
using _Scripts.LSO.Ability.Catalog;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 특성이 발동하면 사전에 적힌 프리팹을 그 자리에 띄운다.
    ///
    /// 특성 코드는 이 클래스를 모른다. LSO_AbilitySignal 만 보고 있으므로
    /// 특성이 스물여섯 개든 쉰 개든 여기는 그대로다.
    ///
    /// ── 유언 쪽과 같은 방식이다 ───────────────────────────────
    /// DLJ_RageEffect 가 하는 일과 같다 — 프리팹을 놓고, 파티클을 돌리고,
    /// 제일 오래 사는 것에 맞춰 치운다. 다른 점은 유언은 종류마다 클래스가
    /// 하나씩 있고 특성은 이 하나가 전부를 맡는다는 것이다.
    ///
    /// 특성은 발동이 "띄우고 치우기"뿐이라 종류별로 나눌 이유가 없다.
    /// 특별한 연출이 필요한 특성이 생기면 그때 그 특성만 따로 빼면 된다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 씬 배선: 씬 아무 곳에나 하나. 연결할 것이 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_AbilityEffectPlayer : MonoBehaviour
    {
        [Header("설정")]
        [Tooltip("띄운 이펙트를 담아둘 곳. 비워두면 이 오브젝트 밑에 붙는다.\n" +
                 "\n" +
                 "기물의 자식으로 붙이지 않는 것이 중요하다. 특성은 기물이 죽는 순간에도\n" +
                 "발동하는데, 기물에 붙여두면 기물이 파괴될 때 연출이 같이 사라진다.")]
        [SerializeField] private Transform parent;

        [Tooltip("한 번에 떠 있을 수 있는 이펙트 수의 상한.\n" +
                 "\n" +
                 "가시처럼 연쇄로 터지는 특성이 있어 한 프레임에 여러 개가 날 수 있다.\n" +
                 "상한을 넘으면 가장 오래된 것을 먼저 치운다.")]
        [SerializeField, Min(1)] private int maxActive = 12;

        [Tooltip("프리팹에 파티클이 하나도 없을 때 얼마나 두고 치울지(초).")]
        [SerializeField, Min(0.1f)] private float fallbackLifetime = 2f;

        [Header("진단")]
        [Tooltip("켜면 어떤 특성이 발동했고 이펙트를 띄웠는지 찍는다.\n" +
                 "사전에 프리팹이 비어 있는 특성도 여기서 드러난다.")]
        [SerializeField] private bool logSteps;

        private LSO_AbilityCatalogSO _catalog;

        /// <summary>지금 떠 있는 것들. 오래된 것이 앞이다.</summary>
        private readonly List<Live> _live = new();

        private readonly struct Live
        {
            public Live(GameObject instance, float diesAt)
            {
                Instance = instance;
                DiesAt = diesAt;
            }

            public GameObject Instance { get; }
            public float DiesAt { get; }
        }

        private void Awake()
        {
            if (parent == null) parent = transform;

            _catalog = Resources.Load<LSO_AbilityCatalogSO>(LSO_AbilityCatalogSO.ResourcePath);

            if (_catalog == null)
            {
                Debug.LogWarning(
                    $"{name}: Resources/{LSO_AbilityCatalogSO.ResourcePath} 를 찾지 못해 " +
                    "특성 이펙트를 띄울 수 없습니다.", this);
            }
        }

        private void OnEnable()
        {
            LSO_AbilitySignal.Fired -= HandleFired;
            LSO_AbilitySignal.Fired += HandleFired;
        }

        private void OnDisable()
        {
            // 정적 이벤트라 반드시 끊는다. 안 끊으면 씬을 넘긴 뒤에도
            // 파괴된 이 컴포넌트가 불려 나온다.
            LSO_AbilitySignal.Fired -= HandleFired;

            ClearAll();
        }

        private void Update()
        {
            if (_live.Count == 0) return;

            float now = Time.unscaledTime;

            // 앞에서부터 본다. 수명 순이 아닐 수 있으므로 전부 훑되,
            // 지우면서 도는 것이라 뒤에서 앞으로 간다.
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Live live = _live[i];

                if (live.Instance != null && now < live.DiesAt) continue;

                if (live.Instance != null) Destroy(live.Instance);

                _live.RemoveAt(i);
            }
        }

        private void HandleFired(LSO_AbilityFired fired)
        {
            if (_catalog == null) return;
            if (!fired.HasPosition) return;

            if (!_catalog.TryGet(fired.Type, out LSO_AbilityInfo info))
            {
                Log($"{fired.Type} — 사전에 없습니다.");
                return;
            }

            if (info.effectPrefab == null)
            {
                Log($"{fired.Type} — 사전에 이펙트 프리팹이 없습니다.");
                return;
            }

            Spawn(info, fired.Position);
        }

        private void Spawn(LSO_AbilityInfo info, Vector3 at)
        {
            TrimToLimit();

            Vector3 where = at + Vector3.up * info.effectHeight;

            GameObject instance = Instantiate(info.effectPrefab, where, Quaternion.identity, parent);

            float lifetime = ResolveLifetime(instance);

            // 시간을 unscaled로 재는 이유는 유언·계승 연출이 timeScale을 쥐는 구간이 있어서다.
            // 그때 이펙트만 멈춰 서 있으면 화면이 어긋난다.
            _live.Add(new Live(instance, Time.unscaledTime + lifetime));

            Log($"{info.type} — {info.effectPrefab.name} ({lifetime:0.##}초)");
        }

        /// <summary>
        /// 얼마나 두고 치울지. 제일 오래 사는 파티클에 맞춘다.
        ///
        /// 프리팹에 파티클을 더 넣어도 여기를 고칠 필요가 없다.
        /// </summary>
        private float ResolveLifetime(GameObject instance)
        {
            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);

            if (systems.Length == 0) return fallbackLifetime;

            float longest = 0f;

            foreach (ParticleSystem system in systems)
            {
                ParticleSystem.MainModule main = system.main;

                longest = Mathf.Max(
                    longest,
                    main.duration + main.startDelay.constantMax + main.startLifetime.constantMax);
            }

            return Mathf.Max(0.1f, longest);
        }

        /// <summary>상한을 넘으면 오래된 것부터 치운다.</summary>
        private void TrimToLimit()
        {
            while (_live.Count >= maxActive)
            {
                Live oldest = _live[0];

                if (oldest.Instance != null) Destroy(oldest.Instance);

                _live.RemoveAt(0);
            }
        }

        /// <summary>
        /// 떠 있는 것을 전부 치운다.
        ///
        /// 꺼질 때 안 치우면 이펙트만 화면에 남는다. 이 컴포넌트가 없어지면
        /// 치워줄 사람도 없어진다.
        /// </summary>
        private void ClearAll()
        {
            foreach (Live live in _live)
            {
                if (live.Instance != null) Destroy(live.Instance);
            }

            _live.Clear();
        }

        private void Log(string message)
        {
            if (logSteps) Debug.Log($"[{name}] {message}", this);
        }
    }
}
