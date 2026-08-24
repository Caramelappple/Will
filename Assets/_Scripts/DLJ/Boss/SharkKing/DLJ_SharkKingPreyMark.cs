using System.Collections.Generic;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

/// <summary>어느 상어왕의 영역 공격으로 포식 상태가 됐는지 기록한다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class DLJ_SharkKingPreyMark : MonoBehaviour, LSO_IDamageModifier
{
    private const int BonusDamage = 2;

    private readonly HashSet<Health> _sharks = new();
    private Health _health;
    private bool _registered;

    // 회피·면역보다 먼저 기본 피해에 +2를 더하고, 이후 기존 방어 능력이 정상 적용되게 한다.
    public int Priority => -2000;

    public void MarkBy(Health shark)
    {
        if (shark == null) return;

        _sharks.Add(shark);
        RegisterModifier();
    }

    public bool IsMarkedBy(Health shark)
    {
        return shark != null && _sharks.Contains(shark);
    }

    public int ModifyIncomingDamage(
        DamageableResources target,
        DamageData data,
        int damage)
    {
        if (data.source != LSO_DamageSource.Melee || !_sharks.Contains(data.giver))
            return damage;

        return damage + BonusDamage;
    }

    private void RegisterModifier()
    {
        if (_registered) return;

        if (_health == null)
            _health = GetComponent<Health>();
        if (_health == null) return;

        _health.AddDamageModifier(this);
        _registered = true;
    }

    private void OnDestroy()
    {
        if (_registered && _health != null)
            _health.RemoveDamageModifier(this);
    }
}
