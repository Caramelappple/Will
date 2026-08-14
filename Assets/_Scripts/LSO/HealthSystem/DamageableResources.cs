using System;
using System.Collections.Generic;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.LSO.HealthSystem
{
    public class DamageableResources : MonoBehaviour
    {
        // Priority 오름차순으로 유지되는 데미지 수정자 목록.
        private readonly List<LSO_IDamageModifier> _damageModifiers = new();

        private readonly List<LSO_IDamageModifier> _modifierBuffer = new();

        [field: SerializeField] public int MaxValue  { get; private set; }
        [field: SerializeField] public int MinValue  { get; private set; }

        public int Value
        {
            get => _value;
            set => _value = Mathf.Clamp(value, MinValue, MaxValue);
        }

        public bool IsDestroyed => _value <= MinValue;
        public bool IsDamageable { get; private set; } = true;

        public event Action<DamageResultData> OnDamage;
        public event Action<DamageData> OnHit;

        [SerializeField] private int _value;

        [Header("디버그")]
        [Tooltip("피해를 받을 때마다 콘솔에 찍는다. 콘솔이 시끄러우면 끌 것.")]
        [SerializeField] private bool logDamage = true;

        protected virtual void Awake()
        {
            // 인스펙터에서 값을 세팅한 경우, _value가 0이면 MaxValue로 초기화
            if (_value <= 0)
                _value = MaxValue;
        }

        /// <summary>
        /// 외부에서 코드로 초기화할 때 사용.
        /// SO, 다른 컴포넌트, 팩토리 등 어디서든 호출 가능.
        /// </summary>
        public void Init(int maxValue,bool heal = true)
        {
            MaxValue = maxValue;
            if (heal)
                Value = MaxValue;
        }

        public int  GetValue() => _value;
        public void SetDamageable(bool value) => IsDamageable = value;
    
        public void AddDamageModifier(LSO_IDamageModifier modifier)
        {
            if (modifier == null || _damageModifiers.Contains(modifier)) return;

            int index = _damageModifiers.FindIndex(m => m.Priority > modifier.Priority);
            if (index < 0)
                _damageModifiers.Add(modifier);
            else
                _damageModifiers.Insert(index, modifier);
        }

        public void RemoveDamageModifier(LSO_IDamageModifier modifier)
        {
            if (modifier == null) return;

            _damageModifiers.Remove(modifier);
        }

        public virtual void GetDamage(DamageData data)
        {
            // 가드가 OnHit보다 앞이어야 한다.
            // 뒤에 두면 이미 죽었거나 무적인 기물에도 피격 신호가 나가서,
            // 가시처럼 "맞으면 반격"인 특성이 맞지도 않은 공격에 반응한다.
            if (IsDestroyed || !IsDamageable) return;

            OnHit?.Invoke(data);

            //데미지 주기전에 변환
            int finalDamage = ApplyDamageModifiers(data);
            if (finalDamage <= 0) return;

            int before = Value;
            Value -= finalDamage;

            if (before > Value)
            {
                var resultData = DamageResultData.Create(data.giver, before - Value, Value);
                OnDamage?.Invoke(resultData);

                // 구독이 아니라 여기서 찍는다. Awake에서 람다로 붙이면 인스펙터에서 꺼도 반영되지 않는다.
                if (logDamage)
                    Debug.Log(
                        $"<color=red>{name}가 {data.giver}로부터 {before - Value}만큼 대미지를 받았습니다!</color>", this);
            }
        }
    
        // 순회 도중 특성이 목록을 건드려도 안전하도록 복사본을 돈다.
        // 매 피격마다 배열을 새로 만들지 않으려고 버퍼를 재사용한다.
        //
        // 재사용이 가능한 이유는 같은 Health가 자기 피해 계산 도중에 다시 GetDamage로
        // 들어오는 경로가 없기 때문이다. 그런 특성을 만들 거라면 이 버퍼부터 걷어낼 것.
        private int ApplyDamageModifiers(DamageData data)
        {
            _modifierBuffer.Clear();
            _modifierBuffer.AddRange(_damageModifiers);

            int damage = data.damage;

            for (int i = 0; i < _modifierBuffer.Count; i++)
                damage = _modifierBuffer[i].ModifyIncomingDamage(this, data, damage);

            return Mathf.Max(0, damage);
        }
    }
}