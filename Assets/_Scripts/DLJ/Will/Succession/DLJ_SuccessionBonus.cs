using _Scripts.LDY;
using UnityEngine;

/// <summary>Stores succession bonuses on one unit without modifying its AnimalSO.</summary>
[DisallowMultipleComponent]
public sealed class DLJ_SuccessionBonus : MonoBehaviour
{
    private LDY_Animal owner;
    private int healthBonus;
    private int attackBonus;

    public static void Apply(LDY_Animal target, int addedHealth, int addedAttack)
    {
        if (target == null || target.health == null || target.health.IsDestroyed)
            return;

        DLJ_SuccessionBonus bonus = target.GetComponent<DLJ_SuccessionBonus>();
        if (bonus == null)
            bonus = target.gameObject.AddComponent<DLJ_SuccessionBonus>();

        bonus.owner = target;
        bonus.Add(addedHealth, addedAttack);
    }

    public static void RemoveFrom(LDY_Animal target)
    {
        if (target == null)
            return;

        DLJ_SuccessionBonus bonus = target.GetComponent<DLJ_SuccessionBonus>();
        bonus?.Remove();
    }

    private void Add(int addedHealth, int addedAttack)
    {
        addedHealth = Mathf.Max(0, addedHealth);
        addedAttack = Mathf.Max(0, addedAttack);

        if (addedHealth > 0)
        {
            int increasedHealth = owner.health.Value + addedHealth;
            owner.health.Init(owner.health.MaxValue + addedHealth, false);
            owner.health.Value = increasedHealth;
            healthBonus += addedHealth;
        }

        if (addedAttack > 0)
        {
            owner.baseAtk += addedAttack;
            attackBonus += addedAttack;
        }
    }

    private void Remove()
    {
        if (owner != null)
        {
            owner.baseAtk = Mathf.Max(0, owner.baseAtk - attackBonus);

            if (owner.health != null && healthBonus > 0)
            {
                int originalMaxHealth = Mathf.Max(1, owner.health.MaxValue - healthBonus);
                int currentHealth = Mathf.Min(owner.health.Value, originalMaxHealth);
                owner.health.Init(originalMaxHealth, false);
                owner.health.Value = currentHealth;
            }
        }

        healthBonus = 0;
        attackBonus = 0;
        Destroy(this);
    }
}
