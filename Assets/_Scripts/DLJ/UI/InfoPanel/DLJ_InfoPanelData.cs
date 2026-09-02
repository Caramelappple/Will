using System.Collections.Generic;
using System.Text;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Animal.Data;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.UI.Text;
using _Scripts.LSO.Will;
using UnityEngine;

/// <summary>
/// 기물과 무관하게 인포창이 공통으로 사용하는 스탯 아이콘 묶음.
/// </summary>
public readonly struct DLJ_InfoPanelPortraits
{
    public readonly Sprite Attack;
    public readonly Sprite Health;
    public readonly Sprite AttackRange;
    public readonly Sprite MoveRange;

    public DLJ_InfoPanelPortraits(
        Sprite attack,
        Sprite health,
        Sprite attackRange,
        Sprite moveRange)
    {
        Attack = attack;
        Health = health;
        AttackRange = attackRange;
        MoveRange = moveRange;
    }
}

/// <summary>
/// 카드와 보드 기물의 원본을 인포창이 표시할 하나의 형태로 변환한다.
/// </summary>
public readonly struct DLJ_InfoPanelData
{
    public readonly Sprite portrait;
    public readonly Sprite attackPortrait;
    public readonly Sprite healthPortrait;
    public readonly Sprite aRPortrait;
    public readonly Sprite mRPortrait;
    public readonly Sprite willPortrait;
    public readonly string Name;
    public readonly string Attack;
    public readonly string Health;
    public readonly string TraitName;
    public readonly string TraitDescription;
    public readonly string WillName;
    public readonly string WillDescription;
    public readonly string AttackRange;
    public readonly string MoveRange;
    public readonly string Cost;
    public readonly string PlayerHealthPoints;

    private DLJ_InfoPanelData(
        LSO_AnimalSO animal,
        Sprite portrait,
        DLJ_InfoPanelPortraits portraits,
        int currentAttack,
        int currentHealth,
        LSO_WillType willType,
        DLJ_WillDataSO willData)
    {
        this.portrait = portrait;
        attackPortrait = portraits.Attack;
        healthPortrait = portraits.Health;
        aRPortrait = portraits.AttackRange;
        mRPortrait = portraits.MoveRange;
        willPortrait = willData != null ? willData.icon : null;
        Name = animal.animalName;
        Attack = FormatChangedStat(animal.damage, currentAttack);
        Health = FormatChangedStat(animal.maxHealth, currentHealth);
        TraitName = DescribeTraits(animal.AbilityTypes);
        TraitDescription = animal.description ?? string.Empty;
        WillName = LSO_DisplayNames.Of(willType);
        WillDescription = willData != null ? willData.description ?? string.Empty : string.Empty;
        AttackRange = FormatAttackRange(animal.range);
        MoveRange = animal.MoveRange.ToString();
        Cost = animal.cost.ToString();
        PlayerHealthPoints = animal.playerHealthPoints.ToString();
    }

    public static bool TryFromCard(
        LSO_CardSO card,
        DLJ_InfoPanelPortraits portraits,
        DLJ_WillDatabaseSO willDatabase,
        out DLJ_InfoPanelData result)
    {
        if (card == null || !card.IsValid)
        {
            result = default;
            return false;
        }

        LSO_AnimalSO animal = card.Animal;
        LSO_WillType willType = card.DefaultWill;

        result = new DLJ_InfoPanelData(
            animal,
            card.Image,
            portraits,
            animal.damage,
            animal.maxHealth,
            willType,
            ResolveWill(willType, willDatabase));
        return true;
    }

    public static bool TryFromAnimal(
        LSO_AnimalSO animal,
        DLJ_InfoPanelCatalogSO catalog,
        DLJ_InfoPanelPortraits portraits,
        DLJ_WillDatabaseSO willDatabase,
        out DLJ_InfoPanelData result)
    {
        if (animal == null)
        {
            result = default;
            return false;
        }

        Sprite portrait = ResolvePortrait(animal, catalog);
        LSO_WillType willType = animal.defaultWill;

        result = new DLJ_InfoPanelData(
            animal,
            portrait,
            portraits,
            animal.damage,
            animal.maxHealth,
            willType,
            ResolveWill(willType, willDatabase));
        return true;
    }

    public static bool TryFromUnit(
        LDY_Animal unit,
        DLJ_InfoPanelCatalogSO catalog,
        DLJ_InfoPanelPortraits portraits,
        DLJ_WillDatabaseSO willDatabase,
        out DLJ_InfoPanelData result)
    {
        if (unit == null || unit.data == null)
        {
            result = default;
            return false;
        }

        Sprite portrait = ResolvePortrait(unit.data, catalog);

        if (portrait == null)
        {
            SpriteRenderer renderer = unit.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null) portrait = renderer.sprite;
        }

        int currentHealth = unit.health != null
            ? unit.health.Value
            : unit.data.maxHealth;

        result = new DLJ_InfoPanelData(
            unit.data,
            portrait,
            portraits,
            unit.GetAtk(),
            currentHealth,
            unit.WillType,
            ResolveWill(unit.WillType, willDatabase));
        return true;
    }

    /// <summary>기본값과 현재값이 다르면 "3 + 1" 또는 "3 - 1"로 표시한다.</summary>
    public static string FormatChangedStat(int baseValue, int currentValue)
    {
        int delta = currentValue - baseValue;
        if (delta == 0) return baseValue.ToString();

        string operation = delta > 0 ? "+" : "-";
        return $"{baseValue} {operation} {Mathf.Abs(delta)}";
    }

    /// <summary>실제 공격 판정에서 사용하는 최대 사거리를 숫자로 표시한다.</summary>
    private static string FormatAttackRange(LDY_RangeType rangeType)
    {
        switch (rangeType)
        {
            case LDY_RangeType.Melee:
            case LDY_RangeType.MeleeOrthogonal:
                return "1";
            case LDY_RangeType.Ranged:
                return "2";
            case LDY_RangeType.Jump:
                return "3";
            case LDY_RangeType.None:
            default:
                return "0";
        }
    }

    private static Sprite ResolvePortrait(
        LSO_AnimalSO animal,
        DLJ_InfoPanelCatalogSO catalog)
    {
        if (catalog != null &&
            catalog.TryGetCard(animal, out LSO_CardSO card))
            return card.Image;

        return null;
    }

    private static string DescribeTraits(IReadOnlyList<LSO_AbilityType> traits)
    {
        if (traits == null || traits.Count == 0) return "없음";

        var builder = new StringBuilder();
        for (int i = 0; i < traits.Count; i++)
        {
            LSO_AbilityType trait = traits[i];
            if (trait == LSO_AbilityType.None) continue;

            if (builder.Length > 0) builder.Append(", ");
            builder.Append(LSO_DisplayNames.Of(trait));
        }

        return builder.Length > 0 ? builder.ToString() : "없음";
    }

    private static DLJ_WillDataSO ResolveWill(
        LSO_WillType willType,
        DLJ_WillDatabaseSO willDatabase)
    {
        if (willType == LSO_WillType.None || willDatabase == null)
            return null;

        return willDatabase.Get(willType);
    }
}
