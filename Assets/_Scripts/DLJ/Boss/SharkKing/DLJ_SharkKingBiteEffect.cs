using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.LDY;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>일반 공격 전용. 대상 앞에 유령 턱을 펼친 뒤 닫고 사라진다.</summary>
public sealed class DLJ_SharkKingBiteEffect : MonoBehaviour
{
    private DLJ_SharkKing owner;
    private Transform upper;
    private Transform lower;
    private Mesh jawMesh;
    private Material material;
    private float expiresAt;
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");

    public static IEnumerator Play(DLJ_SharkKing shark, LDY_Animal target, Color color,
        float size, float appearDuration, float closeDuration, float fadeDuration, Action onImpact)
    {
        if (shark == null || target == null) yield break;
        float appear = Mathf.Max(0.05f, appearDuration);
        float close = Mathf.Max(0.05f, closeDuration);
        float fade = Mathf.Max(0.05f, fadeDuration);
        var root = new GameObject("DLJ_SharkKingBite");
        var effect = root.AddComponent<DLJ_SharkKingBiteEffect>();
        try
        {
            effect.Initialize(shark, target, color, Mathf.Max(0.5f, size), appear + close + fade);
            float elapsed = 0f;
            while (elapsed < appear + close)
            {
                if (shark == null || !shark.isActiveAndEnabled || target == null ||
                    target.health == null || target.health.IsDestroyed || effect == null) yield break;
                float closing = Mathf.Clamp01((elapsed - appear) / close);
                // 처음에는 열린 채 드러나고, 마지막에 빠르게 맞물린다.
                effect.Sample(closing * closing * closing, Mathf.Clamp01(elapsed / appear));
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (effect == null || shark == null || !shark.isActiveAndEnabled || target == null ||
                target.health == null || target.health.IsDestroyed) yield break;
            effect.Sample(1f, 1f);
            onImpact?.Invoke();

            elapsed = 0f;
            while (elapsed < fade && effect != null)
            {
                float progress = elapsed / fade;
                effect.Sample(1f - 0.12f * progress, 1f - Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
                elapsed += Time.deltaTime;
            }
        }
        finally
        {
            if (root != null) Destroy(root);
        }
    }

    private void Initialize(DLJ_SharkKing shark, LDY_Animal target, Color color, float size, float duration)
    {
        owner = shark;
        expiresAt = Time.time + duration + 0.5f;
        Camera camera = Camera.main;
        Quaternion rotation = camera != null ? camera.transform.rotation : Quaternion.Euler(35f, 0f, 0f);
        Transform model = target.modelTransform != null ? target.modelTransform : target.transform;
        Bounds bounds = new Bounds(model.position + Vector3.up * 0.4f, Vector3.one * 0.8f);
        bool found = false;
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            // 체력 UI, 파티클, 바닥 표식은 크기 계산에서 제외.
            if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) || !renderer.enabled) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }

        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;
        Vector3 forward = rotation * Vector3.forward;
        Vector3 extents = bounds.extents;
        float width = Mathf.Max(0.65f, ProjectExtent(extents, right) * 2f) * size;
        float height = Mathf.Max(0.8f, ProjectExtent(extents, up) * 2f) * size;
        // 카메라 쪽 표면 앞으로 빼서 반투명 이빨 뒤로 피격 기물이 보이게 한다.
        transform.SetPositionAndRotation(bounds.center - forward * (ProjectExtent(extents, forward) + 0.04f), rotation);
        transform.localScale = new Vector3(width, height, 1f);

        Shader shader = Resources.Load<Shader>("DLJ/DLJ_SharkBite");
        if (shader == null) throw new InvalidOperationException("DLJ_SharkBite shader is missing.");
        material = new Material(shader) { name = "DLJ_SharkBite_Runtime" };
        material.SetColor("_Tint", color);
        jawMesh = BuildJaw();
        upper = CreateJaw("UpperTeeth", Quaternion.identity);
        lower = CreateJaw("LowerTeeth", Quaternion.Euler(0f, 0f, 180f));
        Sample(0f, 0f);
    }

    private static float ProjectExtent(Vector3 extent, Vector3 axis)
        => Mathf.Abs(axis.x) * extent.x + Mathf.Abs(axis.y) * extent.y + Mathf.Abs(axis.z) * extent.z;

    private Transform CreateJaw(string label, Quaternion rotation)
    {
        var jaw = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
        jaw.transform.SetParent(transform, false);
        jaw.transform.localRotation = rotation;
        jaw.GetComponent<MeshFilter>().sharedMesh = jawMesh;
        var renderer = jaw.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return jaw.transform;
    }

    private void Sample(float closing, float opacity)
    {
        float gap = Mathf.Lerp(0.46f, 0.025f, closing);
        upper.localPosition = Vector3.up * gap;
        lower.localPosition = Vector3.down * gap;
        material.SetFloat(OpacityId, opacity);
    }

    private static Mesh BuildJaw()
    {
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        Color rim = new Color(0.45f, 0.75f, 0.86f, 0.68f);
        Color enamel = new Color(0.9f, 0.98f, 1f, 1f);
        // 둥근 위턱 안쪽으로 길고 뾰족한 이빨을 배치한다.
        const int segments = 32;
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.PI * i / segments;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            vertices.Add(new Vector3(x * 0.53f, y * 0.30f, 0f));
            vertices.Add(new Vector3(x * 0.47f, y * 0.21f, 0f));
            colors.Add(rim);
            colors.Add(enamel);
            if (i == segments) continue;
            int index = i * 2;
            triangles.AddRange(new[] { index, index + 2, index + 1, index + 1, index + 2, index + 3 });
        }
        const int teeth = 9;
        for (int i = 0; i < teeth; i++)
        {
            float angle = Mathf.Lerp(0.10f, Mathf.PI - 0.10f, (i + 0.5f) / teeth);
            float half = 0.14f;
            Vector3 left = new Vector3(Mathf.Cos(angle - half) * 0.475f, Mathf.Sin(angle - half) * 0.215f, 0f);
            Vector3 right = new Vector3(Mathf.Cos(angle + half) * 0.475f, Mathf.Sin(angle + half) * 0.215f, 0f);
            float length = 0.17f + 0.08f * Mathf.Sin(angle);
            Vector3 tip = new Vector3(Mathf.Cos(angle) * 0.43f, Mathf.Sin(angle) * 0.21f - length, 0f);
            int index = vertices.Count;
            vertices.Add(left); vertices.Add(right); vertices.Add(tip);
            colors.Add(enamel); colors.Add(enamel); colors.Add(Color.white);
            triangles.AddRange(new[] { index, index + 1, index + 2 });
        }
        var mesh = new Mesh { name = "DLJ_SharkBite_Jaw" };
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private void Update()
    {
        // 공격 코루틴이 중단돼도 분리 생성한 이펙트가 씬에 남지 않게 한다.
        if (owner == null || !owner.isActiveAndEnabled || Time.time >= expiresAt) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (jawMesh != null) Destroy(jawMesh);
        if (material != null) Destroy(material);
    }
}
