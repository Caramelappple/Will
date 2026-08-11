using System;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using UnityEngine;

namespace _Scripts.LSO.Boss
{
    [RequireComponent(typeof(LDY_Animal))]
    public class LSO_BossPhase : MonoBehaviour
    {
        private const int MaxPhase = 2;

        [Header("페이즈의 전환 기준점이 되는 체력 경계")]
        [SerializeField, Min(1)] private int healthThreshold = 15;
        
        public int CurrentPhase { get; private set; } = 1;

        public event Action<int> OnPhaseChange;

        private LDY_Animal _animal;

        private void Awake()
        {
            _animal = GetComponent<LDY_Animal>();

            if (_animal.health == null)
                Debug.LogError($"{name}: Health가 없어 페이즈 전환이 동작하지 않습니다.", this);
        }

        private void OnEnable()
        {
            if (_animal.health == null) return;

            _animal.health.OnDamage += CheckPhase;
        }

        private void OnDisable()
        {
            if (_animal == null || _animal.health == null) return;

            _animal.health.OnDamage -= CheckPhase;
        }

        private void CheckPhase(DamageResultData data)
        {
            if (CurrentPhase >= MaxPhase) return;

            if (data.currentHealth > healthThreshold) return;
            
            CurrentPhase = MaxPhase;
            
            OnPhaseChange?.Invoke(CurrentPhase);

            LSO_AbilityNotify.Notify<LSO_IPhaseAware>(
                _animal.Abilities, a => a.OnPhaseChanged(_animal, CurrentPhase));
        }
    }
}
