using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Animal.Data;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;
using _Scripts.LSO.Interfaces;

public sealed class DLJ_LifeSteal : LSO_IAbility, IOnTurnStart, IOnAnimalAttack,
    LSO_IAbilityInitializable
{
    private const int TurnDamage = 1;
    private const int AttackRecovery = 1;

    private LSO_AbilityContext context;

    public void Initialize(LSO_AbilityContext abilityContext)
    {
        context = abilityContext;
    }

    public void OnTurnStart(LDY_Team team)
    {
        LDY_Animal owner = context?.Owner;
        if (owner == null || owner.team != team)
            return;

        if (owner.health == null || owner.health.IsDestroyed)
            return;

        owner.health.GetDamage(
            DamageData.Create(owner.health, TurnDamage, LSO_DamageSource.Ability));

        Debug.Log(
            $"<color=red>{owner.name}: Life Steal lost {TurnDamage} HP.</color>",
            owner);

        if (owner.health.IsDestroyed)
            KillOwner(owner);
    }

    public void OnAttack(LSO_AnimalSO animal)
    {
        LDY_Animal owner = context?.Owner;
        if (owner == null || owner.health == null || owner.health.IsDestroyed)
            return;

        owner.health.Recover(RecoverData.Create(owner.health, AttackRecovery));

        // 카탈로그 이펙트 플레이어는 특성 발동 신호를 받아야만 연출을 띄운다.
        // 광견은 공격 시 회복만 처리하고 신호를 보내지 않아, 프리팹이 연결돼 있어도
        // 이펙트가 재생되지 않았다.
        LSO_AbilitySignal.Raise(LSO_AbilityType.LifeSteal, owner);

        Debug.Log(
            $"<color=green>{owner.name}: Life Steal recovered {AttackRecovery} HP.</color>",
            owner);
    }

    private void KillOwner(LDY_Animal owner)
    {
        if (context?.Deaths != null)
        {
            context.Deaths.Kill(owner, null);
            return;
        }

        Debug.LogError(
            $"{owner.name}: No death service was provided through the ability context.",
            owner);
    }
}
