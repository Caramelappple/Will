using UnityEngine;
using UnityEngine.Rendering;

/// <summary>청록색 수면, 솟구치는 얇은 물막, 퍼지는 잔물결을 한 번 재생.</summary>
public sealed class DLJ_SharkWaterCrown : MonoBehaviour
{
    private const int Segments = 96;
    private const int Rows = 6;
    private Mesh crownMesh;
    private Mesh surfaceMesh;
    private MeshRenderer crownRenderer;
    private MeshRenderer poolRenderer;
    private MeshRenderer rippleRenderer;
    private MaterialPropertyBlock properties;
    private Vector3[] vertices;
    private float size;
    private float lifetime;
    private float height;
    private float poolRadius;
    private float elapsed;
    private Color tint;

    public void Initialize(Material material, float effectSize, float duration,
        float heightRatio, float radiusRatio, Color color)
    {
        // 네이티브 객체는 MonoBehaviour 필드 초기화 중 생성할 수 없음.
        // AddComponent 이후 메인 스레드에서 호출되는 재생 초기화 단계에 생성.
        properties = new MaterialPropertyBlock();
        size = effectSize;
        lifetime = Mathf.Max(0.4f, duration);
        height = Mathf.Clamp(heightRatio, 0.1f, 1.5f);
        poolRadius = Mathf.Clamp(radiusRatio, 0.4f, 1.5f);
        tint = color;
        crownMesh = BuildCrownMesh();
        surfaceMesh = BuildSurfaceMesh();
        poolRenderer = CreateRenderer("TurquoisePool", material, surfaceMesh);
        rippleRenderer = CreateRenderer("RecedingRipples", material, surfaceMesh);
        rippleRenderer.transform.localPosition = Vector3.up * 0.015f;
        crownRenderer = CreateRenderer("RisingWaterCrown", material, crownMesh);
        Sample(0f);
    }

    private void Update()
    {
        if (crownMesh == null) return;
        elapsed += Time.deltaTime;
        Sample(Mathf.Clamp01(elapsed / lifetime));
        if (elapsed >= lifetime) enabled = false;
    }

    private void Sample(float t)
    {
        float opening = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.15f), 3f);
        float crownRise = Mathf.Sin(Mathf.Clamp01((t - 0.025f) / 0.56f) * Mathf.PI);
        float crownFade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.37f, 0.64f, t));
        float poolFade = opening * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 0.96f, t)));
        float rippleFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.23f, 0.4f, t))
            * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, t)));

        poolRenderer.transform.localScale = Vector3.one * (size * poolRadius * Mathf.Lerp(0.65f, 1f, opening));
        rippleRenderer.transform.localScale = Vector3.one * (size * poolRadius * Mathf.Lerp(0.7f, 1.5f, t));
        Apply(poolRenderer, 1f, poolFade, t);
        Apply(rippleRenderer, 3f, rippleFade, t);

        crownRenderer.enabled = crownFade > 0.005f && crownRise > 0.005f;
        if (!crownRenderer.enabled) return;
        for (int s = 0; s <= Segments; s++)
        {
            float u = s / (float)Segments;
            float angle = u * Mathf.PI * 2f;
            // 불규칙한 아홉 개의 뾰족한 물막. 바닥은 이어지고 끝만 갈라짐.
            float peak = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(angle * 9f + 0.55f * Mathf.Sin(angle * 3f)), 3f);
            float tipHeight = 0.25f + peak * (0.65f + 0.15f * Mathf.Sin(angle * 4f));
            for (int row = 0; row < Rows; row++)
            {
                float v = row / (float)(Rows - 1);
                float radius = size * (0.33f + 0.15f * opening + 0.16f * t
                    + v * v * (0.11f + 0.07f * Mathf.Sin(angle * 5f)));
                float y = size * height * tipHeight * v * crownRise;
                float twist = angle + 0.1f * v * crownRise;
                vertices[s * Rows + row] = new Vector3(Mathf.Cos(twist) * radius, y, Mathf.Sin(twist) * radius);
            }
        }
        crownMesh.vertices = vertices;
        crownMesh.RecalculateBounds();
        Apply(crownRenderer, 2f, crownFade * opening, t);
    }

    private void Apply(Renderer renderer, float mode, float opacity, float age)
    {
        properties.Clear();
        properties.SetFloat("_Mode", mode);
        properties.SetFloat("_Opacity", opacity);
        properties.SetFloat("_Age", age);
        properties.SetColor("_Tint", tint);
        renderer.SetPropertyBlock(properties);
    }

    private MeshRenderer CreateRenderer(string label, Material material, Mesh mesh)
    {
        GameObject child = new(label);
        child.transform.SetParent(transform, false);
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        return renderer;
    }

    private Mesh BuildCrownMesh()
    {
        vertices = new Vector3[(Segments + 1) * Rows];
        Vector2[] uv = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[Segments * (Rows - 1) * 6];
        int index = 0;
        for (int s = 0; s <= Segments; s++)
        {
            for (int row = 0; row < Rows; row++)
            {
                int vertex = s * Rows + row;
                uv[vertex] = new Vector2(s / (float)Segments, row / (float)(Rows - 1));
                colors[vertex] = Color.white;
                if (s == Segments || row == Rows - 1) continue;
                triangles[index++] = vertex;
                triangles[index++] = vertex + 1;
                triangles[index++] = vertex + Rows;
                triangles[index++] = vertex + 1;
                triangles[index++] = vertex + Rows + 1;
                triangles[index++] = vertex + Rows;
            }
        }
        Mesh mesh = new() { name = "DLJ_WaterCrownRuntime" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.colors = colors;
        mesh.triangles = triangles;
        return mesh;
    }

    private static Mesh BuildSurfaceMesh()
    {
        Mesh mesh = new() { name = "DLJ_WaterSurfaceRuntime" };
        mesh.vertices = new[] { new Vector3(-1f, 0f, -1f), new Vector3(-1f, 0f, 1f),
            new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, -1f) };
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
        mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDestroy()
    {
        if (crownMesh != null) Destroy(crownMesh);
        if (surfaceMesh != null) Destroy(surfaceMesh);
    }
}
