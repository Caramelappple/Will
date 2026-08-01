using System;
using System.Collections.Generic;
using _Scripts.LSO.HealthSystem;
using UnityEngine;

public class DamageableResources : MonoBehaviour
{
    // Priority 오름차순으로 유지되는 데미지 수정자 목록.
    private readonly List<LSO_IDamageModifier> _damageModifiers = new();

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

    protected virtual void Awake()
    {
        // 인스펙터에서 값을 세팅한 경우, _value가 0이면 MaxValue로 초기화
        if (_value <= 0)
            _value = MaxValue;

        OnDamage += data =>
            Debug.Log($"<color=red>{gameObject}가 {data.giver}로부터 {data.damage}만큼 대미지를 받았습니다!</color>");
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
        OnHit?.Invoke(data);

        if (IsDestroyed || !IsDamageable) return;
        
        //데미지 주기전에 변환
        int finalDamage = ApplyDamageModifiers(data);
        if (finalDamage <= 0) return;

        int before = Value;
        Value -= finalDamage;

        if (before > Value)
        {
            var resultData = DamageResultData.Create(data.giver, before - Value, Value);
            OnDamage?.Invoke(resultData);
        }
    }
    
    private int ApplyDamageModifiers(DamageData data)
    {
        int damage = data.damage;
        
        LSO_IDamageModifier[] modifiers = _damageModifiers.ToArray();
        foreach (LSO_IDamageModifier modifier in modifiers)
            damage = modifier.ModifyIncomingDamage(this, data, damage);

        return Mathf.Max(0, damage);
    }
}