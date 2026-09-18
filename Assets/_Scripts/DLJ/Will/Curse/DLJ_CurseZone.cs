using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LDY.Stage;
using _Scripts.LSO.HealthSystem.Data;
using _Scripts.LSO.Stage;
using UnityEngine;

public class DLJ_CurseZone : MonoBehaviour
{
    private int damage;
    private int range;
    private LDY_TurnManager turnManager;
    private LDY_StageDirector stageDirector;
    private LSO_StageFlow stageFlow;
    private LDY_BoardManager board;
    private LDY_AttackSystem attackSystem;
    private LDY_Team sourceTeam;
    private Vector3Int center;
    private GameObject effectInstance;
    private float effectFadeOutTime;
    private bool expired;
    private readonly HashSet<LDY_Animal> animalsInside = new();
    private readonly HashSet<LDY_Animal> currentAnimalsInside = new();

    public int RemainingTurn { get; private set; }

    public void Initialize(
        DLJ_CurseActivationData data,
        GameObject visualInstance = null)
    {
        if (data == null ||
            data.turnManager == null ||
            data.board == null ||
            data.attackSystem == null)
        {
            Debug.LogError($"{name}: Curse zone data is missing.", this);
            Destroy(gameObject);
            return;
        }

        RemainingTurn = data.duration;
        damage = data.damage;
        range = data.range;
        turnManager = data.turnManager;
        board = data.board;
        attackSystem = data.attackSystem;
        sourceTeam = data.sourceTeam;
        center = data.center;
        effectInstance = visualInstance;
        effectFadeOutTime = Mathf.Max(0f, data.effectFadeOutTime);

        stageDirector = FindAnyObjectByType<LDY_StageDirector>();
        if (stageDirector != null)
            stageDirector.OnStageLoaded += HandleStageLoaded;

        if (LSO_StageFlow.HasInstance)
        {
            stageFlow = LSO_StageFlow.Instance;
            stageFlow.StageEnded += HandleStageEnded;
        }

        turnManager.OnTurnChanged += HandleTurnChanged;
        DamageAnimalsInArea();
        if (!expired) RecordCurrentOccupants();
    }

    private void Update()
    {
        // Initialize를 못 받은 저주 지역은 board가 null이라 그대로 두면 매 프레임 터진다.
        // 위쪽 early return이 Destroy를 부르지만 실제 파괴는 프레임 끝으로 미뤄지므로,
        // 그 사이에 Update가 최소 한 번 돈다. 컴포넌트가 코드 밖에서 붙는 경우도 여기서 막힌다.
        if (board == null || expired) return;

        DamageNewEntrants();
    }

    private void HandleTurnChanged(LDY_Team team)
    {
        if (expired) return;
        DamageAnimalsInArea();
        if (expired) return;
        RemainingTurn--;

        if (RemainingTurn <= 0)
            Expire();
    }

    private void HandleStageEnded() => Expire(false);

    private void HandleStageLoaded(LDY_StageSO stage) => Expire(false);

    private void DamageAnimalsInArea()
    {
        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector3Int tile = center + new Vector3Int(x, 0, z);

                if (!board.IsInside(tile))
                    continue;

                LDY_Animal target = board.Get(tile);

                if (target == null ||
                    target.health == null ||
                    target.health.IsDestroyed ||
                    target.team == sourceTeam)
                    continue;

                DamageAnimal(target);
                if (expired) return;
            }
        }
    }

    private void DamageNewEntrants()
    {
        currentAnimalsInside.Clear();

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector3Int tile = center + new Vector3Int(x, 0, z);
                if (!board.IsInside(tile))
                    continue;

                LDY_Animal target = board.Get(tile);
                if (!IsValidTarget(target))
                    continue;

                currentAnimalsInside.Add(target);

                if (!animalsInside.Contains(target))
                {
                    DamageAnimal(target);
                    if (expired) return;
                }
            }
        }

        animalsInside.RemoveWhere(animal =>
            animal == null || !currentAnimalsInside.Contains(animal));

        foreach (LDY_Animal animal in currentAnimalsInside)
        {
            if (animal != null && animal.health != null && !animal.health.IsDestroyed)
                animalsInside.Add(animal);
        }
    }

    private void RecordCurrentOccupants()
    {
        animalsInside.Clear();

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector3Int tile = center + new Vector3Int(x, 0, z);
                if (!board.IsInside(tile))
                    continue;

                LDY_Animal target = board.Get(tile);
                if (IsValidTarget(target))
                    animalsInside.Add(target);
            }
        }
    }

    private bool IsValidTarget(LDY_Animal target)
    {
        return target != null &&
               target.health != null &&
               !target.health.IsDestroyed &&
               target.team != sourceTeam;
    }

    private void DamageAnimal(LDY_Animal target)
    {
        DamageData damageData = DamageData.Create(
            null,
            damage,
            LSO_DamageSource.Curse);
        target.health.GetDamage(damageData);

        if (target.health.IsDestroyed)
            attackSystem.HandleDeath(target);
    }

    private void Expire(bool fadeEffect = true)
    {
        if (expired) return;
        expired = true;
        enabled = false;
        Unsubscribe();
        if (fadeEffect)
            FadeOutEffect();
        else if (effectInstance != null)
        {
            effectInstance.SetActive(false);
            Destroy(effectInstance);
            effectInstance = null;
        }
        Destroy(gameObject);
    }

    private void FadeOutEffect()
    {
        if (effectInstance == null)
            return;

        GameObject fadingEffect = effectInstance;
        effectInstance = null;

        DLJ_CurseFade fade = fadingEffect.GetComponent<DLJ_CurseFade>();
        if (fade == null) fade = fadingEffect.AddComponent<DLJ_CurseFade>();
        fade.FadeOut(effectFadeOutTime);
    }

    private void Unsubscribe()
    {
        if (turnManager != null)
            turnManager.OnTurnChanged -= HandleTurnChanged;

        if (stageDirector != null)
            stageDirector.OnStageLoaded -= HandleStageLoaded;

        if (stageFlow != null)
            stageFlow.StageEnded -= HandleStageEnded;

        turnManager = null;
        stageDirector = null;
        stageFlow = null;
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (effectInstance != null)
            Destroy(effectInstance);
    }
}
