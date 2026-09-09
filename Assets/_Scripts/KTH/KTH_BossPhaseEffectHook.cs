using System;
using System.Collections.Generic;
using _Scripts.LSO.Boss;
using UnityEngine;

namespace _Scripts.KTH
{
    /// <summary>
    /// LSO_BossPhase의 페이즈 전환에 파티클 이펙트를 인스펙터에서 바로 연결할 수 있게 한다.
    ///
    /// UnityEvent로 씬 오브젝트를 직접 연결하는 방식은 대상이 보스 프리팹 밖(다른 프리팹 내부 등)을
    /// 가리키면 조용히 아무 일도 안 일어나는 문제가 있었다. 그래서 오브젝트 참조 대신
    /// "재생할 파티클 프리팹"만 받고, 페이즈가 바뀌는 순간 직접 Instantiate한다.
    /// 프리팹을 필드에 끌어넣기만 하면 되고, 어디 있는 프리팹이든 상관없다.
    ///
    /// ── 만든 이펙트는 여기서 치운다 ────────────────────────────
    /// 예전에는 프리팹의 Stop Action 설정에만 기댔다. 한 프리팹만 그 설정을 빠뜨려도
    /// 페이즈가 바뀔 때마다 씬에 쌓이는데, 쌓이는 것을 알아챌 방법이 없었다.
    /// 지금은 재생 길이를 재서 코드에서 한 번 더 지운다(ScheduleCleanup).
    ///
    /// 부모도 잡는다. 예전에는 월드에 그냥 띄워서, 보스가 움직이면 이펙트만 제자리에
    /// 남았다. 자리에 고정돼야 하는 이펙트는 Follow Spawn Point 를 끄면 된다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    [RequireComponent(typeof(LSO_BossPhase))]
    public class KTH_BossPhaseEffectHook : MonoBehaviour
    {
        [Serializable]
        private struct PhaseEffectEntry
        {
            [Tooltip("이 값과 같은 페이즈로 전환되는 순간 아래 프리팹이 생성된다.")]
            public int phase;

            [Tooltip("재생할 파티클 프리팹을 여기에 끌어넣기만 하면 된다. " +
                      "다 재생된 뒤 자동으로 사라지게 하려면 프리팹의 ParticleSystem > Stop Action을 Destroy로 설정해둔다.")]
            public ParticleSystem effectPrefab;
        }

        [Header("페이즈별로 재생할 파티클 프리팹을 여기에 끌어넣는다")]
        [SerializeField] private List<PhaseEffectEntry> phaseEffects = new();

        [Header("생성 위치 (비워두면 보스 위치에서 생성)")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("켜면 생성 위치의 자식으로 붙어 보스를 따라다닌다.\n" +
                 "\n" +
                 "끄면 그 자리에 남는다. 바닥에서 솟는 것처럼 자리에 고정돼야 하는 이펙트에 쓴다.\n" +
                 "다만 보스가 죽거나 씬이 바뀌어도 이펙트만 남을 수 있으니 수명을 짧게 둘 것.")]
        [SerializeField] private bool followSpawnPoint = true;

        [Header("정리")]
        [Tooltip("끝이 없는(Looping) 이펙트를 몇 초 뒤에 지울지.\n" +
                 "\n" +
                 "반복 재생은 언제 끝나는지 계산할 수 없어서 이 값으로 끊는다.\n" +
                 "반복이 아닌 이펙트는 재생 길이를 스스로 재므로 이 값을 쓰지 않는다.")]
        [SerializeField, Min(0.1f)] private float loopingLifetime = 5f;

        private LSO_BossPhase _bossPhase;

        private void Awake()
        {
            _bossPhase = GetComponent<LSO_BossPhase>();

            if(spawnPoint==null)
                spawnPoint = transform;
        }

        private void OnEnable()
        {
            _bossPhase.OnPhaseChange += HandlePhaseChange;
        }

        private void OnDisable()
        {
            if (_bossPhase != null)
                _bossPhase.OnPhaseChange -= HandlePhaseChange;
        }

        private void HandlePhaseChange(int phase)
        {
            bool matched = false;

            foreach (PhaseEffectEntry entry in phaseEffects)
            {
                if (entry.phase != phase) continue;

                matched = true;
                SpawnEffect(entry.effectPrefab);
            }

            if (!matched)
                Debug.LogWarning($"{name}: Phase Effects에 phase={phase}에 대응하는 항목이 없습니다.", this);
        }

        private void SpawnEffect(ParticleSystem effectPrefab)
        {
            if (effectPrefab == null)
            {
                Debug.LogWarning($"{name}: Phase Effects에 프리팹이 비어 있습니다.", this);
                return;
            }

            Transform origin = spawnPoint != null ? spawnPoint : transform;

            // 부모를 잡아야 보스를 따라간다. 예전에는 월드에 그냥 띄워서,
            // 보스가 움직이면 이펙트만 제자리에 남았다.
            Transform parent = followSpawnPoint ? origin : null;

            ParticleSystem instance =
                Instantiate(effectPrefab, origin.position, origin.rotation, parent);

            ScheduleCleanup(instance);
        }

        /// <summary>
        /// 다 재생된 이펙트를 치운다.
        ///
        /// 예전에는 프리팹의 Stop Action 설정에만 기댔다. 한 프리팹만 그 설정을
        /// 빠뜨려도 페이즈가 바뀔 때마다 씬에 쌓이는데, 쌓이는 것을 알아챌 방법이 없었다.
        /// 그래서 코드에서 한 번 더 보장한다.
        ///
        /// Stop Action이 이미 Destroy면 맡기고 넘어간다 — 두 곳이 같은 일을 하면
        /// 나중에 수명을 바꿀 때 어느 쪽이 이기는지 따져야 한다.
        /// </summary>
        private void ScheduleCleanup(ParticleSystem instance)
        {
            if (instance == null) return;

            ParticleSystem.MainModule main = instance.main;

            // 프리팹이 스스로 사라지게 돼 있으면 그쪽에 맡긴다.
            if (main.stopAction == ParticleSystemStopAction.Destroy) return;

            if (main.loop)
            {
                Debug.LogWarning(
                    $"{name}: '{instance.name}'이 Looping이라 언제 끝나는지 알 수 없습니다. " +
                    $"{loopingLifetime:0.#}초 뒤에 지웁니다. " +
                    "Looping을 끄거나 Stop Action을 Destroy로 두는 편이 낫습니다.", instance);

                Destroy(instance.gameObject, loopingLifetime);
                return;
            }

            Destroy(instance.gameObject, LifetimeOf(instance));
        }

        /// <summary>
        /// 다 재생되는 데 걸리는 시간. 자식 파티클까지 보고 가장 늦게 끝나는 것에 맞춘다.
        ///
        /// 재생 길이(duration)가 끝나도 마지막에 태어난 입자는 자기 수명(startLifetime)만큼
        /// 더 살아 있다. 그래서 둘을 더한다.
        ///
        /// startLifetime이 곡선이면 constantMax가 정확한 값은 아니지만 늘 실제보다
        /// 크거나 같다. 일찍 지워서 이펙트가 잘리는 것보다 조금 늦게 지우는 편이 낫다.
        /// </summary>
        private static float LifetimeOf(ParticleSystem root)
        {
            float longest = 0f;

            foreach (ParticleSystem system in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;

                float life = main.duration + main.startLifetime.constantMax;

                if (life > longest) longest = life;
            }

            return longest;
        }
    }
}
