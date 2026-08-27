using System.Collections;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.HealthSystem;
using UnityEngine;
using _Scripts.LSO.Interfaces;

public sealed class DLJ_Evolve : LSO_IAbility, IOnTurnStart, IStatModifier,
    LSO_IAbilityInitializable
{
    private const int TurnsToEvolve = 6;

    private LSO_AbilityContext context;
    private int elapsedTurns;
    private bool isEggInitialized;
    private bool isEvolved;

    public void Initialize(LSO_AbilityContext abilityContext)
    {
        context = abilityContext;
        elapsedTurns = 0;
        isEggInitialized = false;
        isEvolved = false;

        LDY_Animal owner = context?.Owner;
        if (owner != null)
            owner.StartCoroutine(InitializeEggAfterAnimal());
    }

    public void OnTurnStart(LDY_Team team)
    {
        LDY_Animal owner = context?.Owner;
        if (!isEggInitialized || isEvolved || owner == null)
            return;

        if (owner.team != team || owner.health == null || owner.health.IsDestroyed)
            return;

        elapsedTurns++;
        if (elapsedTurns >= TurnsToEvolve)
            Evolve(owner);
    }

    public int ModifyAttack(LDY_Animal self, int atk)
    {
        return isEvolved ? atk : 0;
    }

    private IEnumerator InitializeEggAfterAnimal()
    {
        yield return null;

        LDY_Animal owner = context?.Owner;
        if (owner == null || owner.health == null || owner.health.IsDestroyed)
            yield break;

        owner.health.Init(1);
        isEggInitialized = true;
    }

    private void Evolve(LDY_Animal owner)
    {
        if (owner.data == null || owner.health == null)
            return;

        isEvolved = true;

        owner.health.Init(Mathf.Max(1, owner.data.maxHealth));
        owner.StartCoroutine(ReplaceVisualWithUnitPrefab(owner));

        Debug.Log(
            $"<color=orange>{owner.name}: Evolve activated. " +
            $"ATK {owner.GetAtk()}, HP {owner.health.Value}/{owner.health.MaxValue}</color>",
            owner);
    }

    private static IEnumerator ReplaceVisualWithUnitPrefab(LDY_Animal owner)
    {
        GameObject unitPrefab = owner.data != null ? owner.data.unitPrefab : null;
        if (unitPrefab == null)
        {
            Debug.LogWarning($"{owner.name}: AnimalSO unitPrefab is missing.", owner);
            yield break;
        }

        Transform oldModel = owner.modelTransform;
        Renderer[] oldRenderers = owner.GetComponentsInChildren<Renderer>(true);
        
        GameObject evolvedVisual = Object.Instantiate(unitPrefab, owner.transform, false);
        evolvedVisual.name = $"{unitPrefab.name}_EvolvedVisual";
        evolvedVisual.SetActive(false);

        foreach (LDY_Animal clonedAnimal in evolvedVisual.GetComponentsInChildren<LDY_Animal>(true))
            Object.Destroy(clonedAnimal);

        foreach (Health clonedHealth in evolvedVisual.GetComponentsInChildren<Health>(true))
            Object.Destroy(clonedHealth);

        foreach (Collider clonedCollider in evolvedVisual.GetComponentsInChildren<Collider>(true))
            clonedCollider.enabled = false;
        
        yield return null;

        if (owner == null)
        {
            Object.Destroy(evolvedVisual);
            yield break;
        }

        if (oldModel != null && oldModel != owner.transform)
        {
            Object.Destroy(oldModel.gameObject);
        }
        else
        {
            foreach (Renderer oldRenderer in oldRenderers)
            {
                if (oldRenderer != null)
                    oldRenderer.enabled = false;
            }
        }

        evolvedVisual.SetActive(true);
        owner.modelTransform = evolvedVisual.transform;
    }
}
