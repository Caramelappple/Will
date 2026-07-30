/*
using _Scripts.LDY;
using DG.Tweening;
using UnityEngine;

public class DLJ_RageSystem : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private int damage = 1;
    [SerializeField] private int range = 1;

    private GameObject effectPrefab;
    private float expandTime;
    private float holdTime;
    private float effectHeight;
    private LDY_BoardManager activationBoard;
    private LDY_AttackSystem activationAttackSystem;

    private LDY_BoardManager effectBoard;
    private Vector3Int effectCenter;
    private LDY_AttackSystem effectAttackSystem;

    public void Configure(
        GameObject prefab,
        float sourceExpandTime,
        float sourceHoldTime,
        float sourceEffectHeight,
        LDY_BoardManager boardManager,
        LDY_AttackSystem attackSystem)
    {
        effectPrefab = prefab;
        expandTime = sourceExpandTime;
        holdTime = sourceHoldTime;
        effectHeight = sourceEffectHeight;
        activationBoard = boardManager;
        activationAttackSystem = attackSystem;
    }

    public bool Activate()
    {
        if (!TryGetEffectData(out Vector3Int center, out Vector3 centerWorld,
                out Vector3 targetScale))
            return false;

        GameObject instance = Instantiate(effectPrefab, centerWorld, Quaternion.identity);
        instance.transform.position =
            centerWorld + Vector3.up * (effectHeight * 0.5f);
        instance.transform.localScale = Vector3.zero;
        instance.SetActive(true);

        DLJ_RageSystem effectSystem = instance.GetComponent<DLJ_RageSystem>();

        if (effectSystem == null)
        {
            Debug.LogError($"{instance.name}: RageSystem is missing.", instance);
            Destroy(instance);
            return false;
        }

        effectSystem.InitializeEffect(
            activationBoard,
            center,
            activationAttackSystem);

        DOTween.Sequence()
            .Append(instance.transform
                .DOScale(targetScale, expandTime)
                .SetEase(Ease.Linear))
            .AppendInterval(holdTime)
            .Append(instance.transform
                .DOScale(Vector3.zero, expandTime)
                .SetEase(Ease.Linear))
            .OnComplete(() => Destroy(instance));

        Debug.Log("Rage Activated");
        return true;
    }

    private void InitializeEffect(
        LDY_BoardManager boardManager,
        Vector3Int center,
        LDY_AttackSystem attackSystem)
    {
        effectBoard = boardManager;
        effectCenter = center;
        effectAttackSystem = attackSystem;
        DamageAnimalsInArea();
    }

    private bool TryGetEffectData(
        out Vector3Int center,
        out Vector3 centerWorld,
        out Vector3 targetScale)
    {
        center = default;
        centerWorld = default;
        targetScale = default;

        if (activationBoard == null || activationAttackSystem == null)
        {
            return false;
        }

        if (effectPrefab == null)
        {
            Debug.LogError($"{name}: Rage effect prefab is missing.", this);
            return false;
        }

        center = activationBoard.WorldToGrid(transform.position);

        if (!activationBoard.IsInside(center))
        {
            Debug.LogError($"{name}: Animal is outside the board.", this);
            return false;
        }

        centerWorld = activationBoard.GridToWorld(center);
        Vector3 verticalWorld =
            activationBoard.GridToWorld(center + new Vector3Int(0, 0, 1));
        Vector3 horizontalWorld =
            activationBoard.GridToWorld(center + new Vector3Int(1, 0, 0));

        float cellWidth = Vector3.Distance(centerWorld, verticalWorld);
        float cellDepth = Vector3.Distance(centerWorld, horizontalWorld);
        targetScale = new Vector3(cellWidth * 3f, effectHeight, cellDepth * 3f);
        return true;
    }

    private void DamageAnimalsInArea()
    {
        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector3Int tile = effectCenter + new Vector3Int(x, 0, z);

                if (!effectBoard.IsInside(tile))
                    continue;

                LDY_Animal target = effectBoard.Get(tile);

                if (target == null || target.IsDead)
                    continue;

                target.hp -= damage;

                if (target.hp <= 0)
                    effectAttackSystem.HandleDeath(target);
            }
        }
    }
}
*/
