using System.Collections;
using _Scripts.LDY;
using _Scripts.LDY.Save;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Animal.Data;
using _Scripts.LSO.Deck.Data;
using UnityEngine;
using _Scripts.LSO.Interfaces;

public sealed class DLJ_Evolve : LSO_IAbility, IOnTurnStart, IStatModifier,
    LSO_IAbilityInitializable
{
    private const int TurnsToEvolve = 5;
    private const string EvolvedCardId = "DragonCard";

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

        // 아군과 적군 어느 쪽 턴이든 시작될 때마다 생존 턴을 센다.
        if (owner.health == null || owner.health.IsDestroyed)
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

        LSO_AnimalSO evolvedData = ResolveEvolvedData();
        if (evolvedData == null || evolvedData.unitPrefab == null)
        {
            Debug.LogError(
                $"{owner.name}: 카드 카탈로그에서 '{EvolvedCardId}'의 드래곤 데이터/프리팹을 찾지 못했습니다.",
                owner);
            return;
        }

        isEvolved = true;

        owner.data = evolvedData;
        owner.baseAtk = evolvedData.damage;
        owner.health.Init(Mathf.Max(1, evolvedData.maxHealth));
        owner.StartCoroutine(ReplaceVisualWithUnitPrefab(owner, evolvedData.unitPrefab));

        Debug.Log(
            $"<color=orange>{owner.name}: Evolve activated. " +
            $"ATK {owner.GetAtk()}, HP {owner.health.Value}/{owner.health.MaxValue}</color>",
            owner);
    }

    private static LSO_AnimalSO ResolveEvolvedData()
    {
        LDY_CardCatalogSO catalog = Resources.Load<LDY_CardCatalogSO>(LDY_CardCatalogSO.ResourcePath);
        LSO_CardSO card = catalog != null ? catalog.Find(EvolvedCardId) : null;
        return card != null && card.IsValid ? card.Animal : null;
    }

    private static IEnumerator ReplaceVisualWithUnitPrefab(LDY_Animal owner, GameObject unitPrefab)
    {
        Transform oldModel = owner.modelTransform;
        Renderer[] oldRenderers = owner.GetComponentsInChildren<Renderer>(true);

        // 완성 기물 프리팹의 루트에는 LDY_Animal/Health/팀 머티리얼 같은 기능 컴포넌트가 있다.
        // 그 루트를 통째로 복제한 뒤 컴포넌트를 제거하면 RequireComponent 의존성 때문에
        // Unity가 제거를 거부한다. 교체에는 시각 계층만 필요하므로 직계 자식만 복제한다.
        GameObject evolvedVisual = new GameObject();
        evolvedVisual.name = $"{unitPrefab.name}_EvolvedVisual";
        evolvedVisual.transform.SetParent(owner.transform, false);
        evolvedVisual.SetActive(false);

        // 유닛 프리팹 루트에는 프리팹 편집 당시의 월드 좌표가 남아 있을 수 있다.
        // 부모 지정 Instantiate는 그 값을 로컬 좌표로 보존하므로, 그대로 두면
        // 논리 기물은 타일에 남고 드래곤 모델만 옆으로 순간 이동한다.
        // 교체용 프리팹은 owner 아래의 시각 요소이므로 항상 기물 원점에 맞춘다.
        Transform evolvedTransform = evolvedVisual.transform;
        evolvedTransform.localPosition = Vector3.zero;
        evolvedTransform.localRotation = Quaternion.identity;
        evolvedTransform.localScale = Vector3.one;

        Transform prefabRoot = unitPrefab.transform;
        for (int i = 0; i < prefabRoot.childCount; i++)
            Object.Instantiate(prefabRoot.GetChild(i).gameObject, evolvedTransform, false);

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

        if (owner.TryGetComponent(out DLJ_PieceTeamMaterial teamMaterial))
            teamMaterial.RebindModel();
    }
}
