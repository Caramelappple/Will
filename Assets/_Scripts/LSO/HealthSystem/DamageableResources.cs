using System;
using _Scripts.LSO.HealthSystem;
using UnityEngine;

public class DamageableResources : MonoBehaviour
{
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
    public void Init(int maxValue, int minValue = 0, int startValue = -1)
    {
        MaxValue = maxValue;
        MinValue = minValue;
        _value   = startValue < 0 ? maxValue : startValue;
    }

    public int  GetValue() => _value;
    public void SetDamageable(bool value) => IsDamageable = value;

    public virtual void GetDamage(DamageData data)
    {
        OnHit?.Invoke(data);

        if (IsDestroyed || !IsDamageable) return;

        int before = Value;
        Value -= data.damage;

        if (before > Value)
        {
            var resultData = DamageResultData.Create(data.giver, data.damage, Value);
            OnDamage?.Invoke(resultData);
        }
    }
}