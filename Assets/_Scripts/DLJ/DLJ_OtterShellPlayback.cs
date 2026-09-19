using System.Collections.Generic;
using UnityEngine;

/// <summary>타격 위치에서 상승 → 공중 폭발 → 보드 전체 폭발. 원본 에셋은 수정하지 않는다.</summary>
public sealed class DLJ_OtterShellPlayback : MonoBehaviour
{
    private Transform shell;
    private Vector3 origin, spin, boardCenter;
    private Quaternion shellRotation;
    private GameObject burstPrefab, sacrificePrefab;
    private float riseDuration, riseHeight, burstScale, boardScale, boardDelay, emissionDuration;
    private float elapsed;
    private float minimumEffectLifetime;
    private float boardParticleSize;
    private int boardParticleCount;
    private float boardParticleBrightness;
    private const float FollowupBurstDelay = 0.18f;
    private bool initialized, burstPlayed, boardPlayed;
    private readonly List<ParticleBatch> batches = new();

    private sealed class ParticleBatch
    {
        public GameObject Root;
        public ParticleSystem[] Systems;
        public float StopAt;
        public float ClearAt;
        public float KeepUntil;
        public bool Stopped;
    }

    public void Initialize(GameObject shellPrefab, Vector3 shellScale, Vector3 rotation,
        Vector3 spinSpeed, float duration, float height, GameObject airBurst, float airScale,
        GameObject boardBurst, Vector3 center, float scale, float delay, float emitDuration,
        float minimumLifetime = 1.5f, float maxBoardParticleSize = 0.4875f,
        int particleCount = 96, float particleBrightness = 1.15f)
    {
        origin = transform.position;
        spin = spinSpeed;
        shellRotation = Quaternion.Euler(rotation);
        riseDuration = Mathf.Max(0.01f, duration);
        riseHeight = Mathf.Max(0f, height);
        burstPrefab = airBurst;
        burstScale = Mathf.Max(0.01f, airScale);
        sacrificePrefab = boardBurst;
        boardCenter = center;
        boardScale = Mathf.Max(0.01f, scale);
        boardDelay = Mathf.Max(0f, delay);
        emissionDuration = Mathf.Max(0.01f, emitDuration);
        minimumEffectLifetime = Mathf.Max(0f, minimumLifetime);
        boardParticleSize = Mathf.Max(0.01f, maxBoardParticleSize);
        boardParticleCount = Mathf.Clamp(particleCount, 16, 192);
        boardParticleBrightness = Mathf.Clamp(particleBrightness, 1f, 2f);
        if (shellPrefab != null)
        {
            shell = Instantiate(shellPrefab, transform).transform;
            shell.position = origin;
            shell.rotation = shellRotation;
            shell.localScale = Vector3.Scale(shell.localScale, shellScale);
            // 조개는 연출 소품이므로 보드의 클릭/물리 판정에 참여하지 않는다.
            foreach (Collider collider in shell.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (Rigidbody body in shell.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }
            shell.gameObject.SetActive(true);
        }
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / riseDuration);
        Vector3 top = origin + Vector3.up * riseHeight;
        if (shell != null)
        {
            // 정점에 가까워질수록 감속하는 포물선의 상승 구간.
            shell.position = Vector3.Lerp(origin, top, 1f - (1f - t) * (1f - t));
            shell.rotation = shellRotation * Quaternion.Euler(spin * Mathf.Min(elapsed, riseDuration));
        }
        if (!burstPlayed && elapsed >= riseDuration)
        {
            burstPlayed = true;
            if (shell != null)
            {
                shell.gameObject.SetActive(false);
                Destroy(shell.gameObject);
            }
            SpawnParticles(burstPrefab, top, burstScale);
        }
        if (!boardPlayed && elapsed >= riseDuration + boardDelay)
        {
            boardPlayed = true;
            SpawnParticles(sacrificePrefab, boardCenter, boardScale, true);
        }

        for (int i = batches.Count - 1; i >= 0; i--)
        {
            ParticleBatch batch = batches[i];
            if (!batch.Stopped && elapsed >= batch.StopAt)
            {
                batch.Stopped = true;
                foreach (ParticleSystem system in batch.Systems)
                    if (system != null) system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
            bool alive = false;
            foreach (ParticleSystem system in batch.Systems)
                if (system != null && system.IsAlive(false)) { alive = true; break; }
            if (elapsed < batch.KeepUntil ||
                ((!batch.Stopped || alive) && elapsed < batch.ClearAt)) continue;
            if (batch.Root != null) Destroy(batch.Root);
            batches.RemoveAt(i);
        }
        if (boardPlayed && batches.Count == 0) Destroy(gameObject);
    }

    private void SpawnParticles(GameObject prefab, Vector3 position, float scale, bool boardEffect = false)
    {
        if (prefab == null) return;
        GameObject instance = Instantiate(prefab, position, prefab.transform.rotation, transform);
        instance.transform.localScale = prefab.transform.localScale * scale;
        instance.SetActive(true);
        // Heal 원본의 MeshCollider가 보드 클릭을 가로채지 않게 재생 사본만 끈다.
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        float drainDuration = 0f;
        float lastDelay = 0f;
        foreach (ParticleSystem system in systems)
        {
            system.gameObject.SetActive(true);
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.useUnscaledTime = true;
            if (boardEffect)
            {
                // 보드 범위만 넓힌다. Hierarchy는 입자 크기와 상승 거리까지 확대해 화면을 가린다.
                main.scalingMode = ParticleSystemScalingMode.Shape;
                main.startSize = new ParticleSystem.MinMaxCurve(boardParticleSize * 0.65f, boardParticleSize);
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.6f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.15f);
                main.maxParticles = Mathf.Max(main.maxParticles, boardParticleCount);
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(boardParticleBrightness, boardParticleBrightness, boardParticleBrightness, 1f));

                // 한 번에 큰 덩어리로 덮지 않고 작은 빛을 두 번에 나누어 채운다.
                ParticleSystem.EmissionModule emission = system.emission;
                emission.enabled = true;
                emission.rateOverTime = 0f;
                emission.rateOverDistance = 0f;
                int firstCount = boardParticleCount * 2 / 3;
                emission.SetBursts(new[]
                {
                    new ParticleSystem.Burst(0f, (short)firstCount),
                    new ParticleSystem.Burst(FollowupBurstDelay, (short)(boardParticleCount - firstCount))
                });

                // 원본은 생성 직후부터 투명해진다. 잠깐 빛을 유지한 뒤 부드럽게 사라지게 한다.
                ParticleSystem.ColorOverLifetimeModule colors = system.colorOverLifetime;
                colors.enabled = true;
                Gradient fade = new Gradient();
                fade.SetKeys(new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.85f, 1f, 0.9f), 1f)
                }, new[]
                {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(1f, 0.12f),
                    new GradientAlphaKey(0.9f, 0.4f),
                    new GradientAlphaKey(0f, 1f)
                });
                colors.color = new ParticleSystem.MinMaxGradient(fade);
                // Heal은 XY 평면의 Quad를 바닥으로 회전한 구조. 원형 방출이 보드 밖으로
                // 튀어나오지 않게 사본의 방출 영역을 Quad와 같은 사각형으로 맞춘다.
                ParticleSystem.ShapeModule shape = system.shape;
                if (shape.enabled)
                {
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(1f, 1f, 0.01f);
                }
            }
            else
            {
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            lastDelay = Mathf.Max(lastDelay, main.startDelay.constantMax +
                (boardEffect ? FollowupBurstDelay : 0f));
            drainDuration = Mathf.Max(drainDuration,
                main.startLifetime.constantMax / Mathf.Max(0.01f, main.simulationSpeed));
            system.Play(false);
        }
        float stopAt = elapsed + lastDelay + emissionDuration;
        batches.Add(new ParticleBatch
        {
            Root = instance, Systems = systems, StopAt = stopAt,
            KeepUntil = elapsed + minimumEffectLifetime,
            ClearAt = Mathf.Max(elapsed + minimumEffectLifetime,
                stopAt + Mathf.Max(0.1f, drainDuration) + 0.1f)
        });
    }
}
