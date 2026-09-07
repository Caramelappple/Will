using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.Boss
{
    /// <summary>
    /// LSO_BossPhase의 페이즈 전환에 파티클 이펙트를 인스펙터에서 바로 연결할 수 있게 한다.
    ///
    /// UnityEvent로 씬 오브젝트를 직접 연결하는 방식은 대상이 보스 프리팹 밖(다른 프리팹 내부 등)을
    /// 가리키면 조용히 아무 일도 안 일어나는 문제가 있었다. 그래서 오브젝트 참조 대신
    /// "재생할 파티클 프리팹"만 받고, 페이즈가 바뀌는 순간 직접 Instantiate한다.
    /// 프리팹을 필드에 끌어넣기만 하면 되고, 어디 있는 프리팹이든 상관없다.
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
            Instantiate(effectPrefab, origin.position, origin.rotation);
        }
    }
}
