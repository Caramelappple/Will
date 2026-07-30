using _Scripts.LDY;
using DG.Tweening;
using UnityEngine;

public class DLJ_CurseSystem : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private int remainingTurn = 2;
    [SerializeField] private int damage = 1;
    [SerializeField] private int range = 1;

    private GameObject effectPrefab;
    private float expandTime;
    private float effectHeight;
    private LDY_TurnManager activationTurnManager;
    private LDY_BoardManager activationBoard;
    private LDY_AttackSystem activationAttackSystem;
    private LDY_Team activationSourceTeam;

    private LDY_TurnManager effectTurnManager;
    private LDY_BoardManager effectBoard;
    private Vector3Int effectCenter;
    private LDY_AttackSystem effectAttackSystem;
    private LDY_Team effectSourceTeam;

    public int RemainingTurn { get; private set; }

    public void Configure(
        GameObject prefab,
        float sourceExpandTime,
        float sourceEffectHeight,
        LDY_TurnManager turnManager,
        LDY_BoardManager boardManager,
        LDY_AttackSystem attackSystem,
        LDY_Team sourceTeam)
    {
        effectPrefab = prefab;
        expandTime = sourceExpandTime;
        effectHeight = sourceEffectHeight;
        activationTurnManager = turnManager;
        activationBoard = boardManager;
        activationAttackSystem = attackSystem;
        activationSourceTeam = sourceTeam;
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

        DLJ_CurseSystem effectSystem = instance.GetComponent<DLJ_CurseSystem>();

        if (effectSystem == null)
        {
            Debug.LogError($"{instance.name}: CurseSystem is missing.", instance);
            Destroy(instance);
            return false;
        }

        effectSystem.InitializeEffect(
            activationTurnManager,
            activationBoard,
            center,
            activationAttackSystem,
            activationSourceTeam);

        instance.transform
            .DOScale(targetScale, expandTime)
            .SetEase(Ease.Linear);

        Debug.Log("Curse Activated");
        return true;
    }

    private void InitializeEffect(
        LDY_TurnManager turnManager,
        LDY_BoardManager boardManager,
        Vector3Int center,
        LDY_AttackSystem attackSystem,
        LDY_Team sourceTeam)
    {
        effectTurnManager = turnManager;
        effectBoard = boardManager;
        effectCenter = center;
        effectAttackSystem = attackSystem;
        effectSourceTeam = sourceTeam;
        RemainingTurn = remainingTurn;

        effectTurnManager.OnTurnChanged += HandleTurnChanged;
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

        if (activationTurnManager == null ||
            activationBoard == null ||
            activationAttackSystem == null)
        {
            return false;
        }

        if (effectPrefab == null)
        {
            Debug.LogError($"{name}: Curse effect prefab is missing.", this);
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

    private void HandleTurnChanged(LDY_Team team)
    {
        DamageAnimalsInArea();
        RemainingTurn--;

        if (RemainingTurn <= 0)
            Expire();
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

                if (effectSourceTeam == target.team)
                    continue;

                target.hp -= damage;

                if (target.hp <= 0)
                    effectAttackSystem.HandleDeath(target);
            }
        }
    }

    private void Expire()
    {
        Unsubscribe();
        Destroy(gameObject);
    }

    private void Unsubscribe()
    {
        if (effectTurnManager != null)
            effectTurnManager.OnTurnChanged -= HandleTurnChanged;

        effectTurnManager = null;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}
