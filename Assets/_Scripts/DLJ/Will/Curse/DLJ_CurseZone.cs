using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

public class DLJ_CurseZone : MonoBehaviour
{
    private int damage;
    private int range;
    private LDY_TurnManager turnManager;
    private LDY_BoardManager board;
    private LDY_AttackSystem attackSystem;
    private LDY_Team sourceTeam;
    private Vector3Int center;
    private GameObject effectInstance;
    private float effectFadeOutTime;
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

        turnManager.OnTurnChanged += HandleTurnChanged;
        DamageAnimalsInArea();
        RecordCurrentOccupants();
    }

    private void Update()
    {
        // Initialize를 못 받은 저주 지역은 board가 null이라 그대로 두면 매 프레임 터진다.
        // 위쪽 early return이 Destroy를 부르지만 실제 파괴는 프레임 끝으로 미뤄지므로,
        // 그 사이에 Update가 최소 한 번 돈다. 컴포넌트가 코드 밖에서 붙는 경우도 여기서 막힌다.
        if (board == null) return;

        DamageNewEntrants();
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
                    DamageAnimal(target);
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

    private void Expire()
    {
        Unsubscribe();
        FadeOutEffect();
        Destroy(gameObject);
    }

    private void FadeOutEffect()
    {
        if (effectInstance == null)
            return;

        GameObject fadingEffect = effectInstance;
        effectInstance = null;

        if (effectFadeOutTime <= 0f)
        {
            Destroy(fadingEffect);
            return;
        }

        ParticleSystem[] particleSystems =
            fadingEffect.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = false;

            particleSystem.Stop(
                false,
                ParticleSystemStopBehavior.StopEmitting);

            int particleCount = particleSystem.particleCount;
            if (particleCount == 0)
                continue;

            ParticleSystem.Particle[] particles =
                new ParticleSystem.Particle[particleCount];
            int aliveCount = particleSystem.GetParticles(particles);

            for (int i = 0; i < aliveCount; i++)
            {
                float elapsedLifetime =
                    particles[i].startLifetime - particles[i].remainingLifetime;
                float remainingLifetime = Mathf.Min(
                    particles[i].remainingLifetime,
                    effectFadeOutTime);

                particles[i].startLifetime = elapsedLifetime + remainingLifetime;
                particles[i].remainingLifetime = remainingLifetime;
            }

            particleSystem.SetParticles(particles, aliveCount);
        }

        if (particleSystems.Length == 0)
        {
            Destroy(fadingEffect);
            return;
        }

        Destroy(fadingEffect, effectFadeOutTime + 0.1f);
    }

    private void Unsubscribe()
    {
        if (turnManager != null)
            turnManager.OnTurnChanged -= HandleTurnChanged;

        turnManager = null;
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (effectInstance != null)
            Destroy(effectInstance);
    }
}
