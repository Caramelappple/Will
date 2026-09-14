using TMPro;
using UnityEngine;

/// <summary>달성한 보상 문구 위에 종이 곡면을 따라 한 획씩 취소선을 긋기.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class DLJ_LedgerStrike : MonoBehaviour
{
    [SerializeField] private TextMeshPro row;
    [SerializeField] private Transform pageSpace;
    [SerializeField, Min(.05f)] private float drawDuration = .22f;
    [SerializeField, Min(.001f)] private float strokeWidth = .018f;
    private const int Segments = 40;
    private Mesh mesh;
    private MeshRenderer strokeRenderer;
    private readonly Vector3[] vertices = new Vector3[(Segments + 1) * 2];
    private float startsAt;
    private float left, right, centerZ;
    public bool IsAchieved { get; private set; }
    public float DrawDuration => drawDuration;
    public float Progress { get; private set; }

    public void SetAchieved(bool achieved, float delay = 0)
    {
        if (!achieved)
        {
            IsAchieved = false;
            Progress = 0;
            if (strokeRenderer == null) strokeRenderer = GetComponent<MeshRenderer>();
            strokeRenderer.enabled = false;
            return;
        }
        if (IsAchieved || row == null || pageSpace == null) return;
        row.ForceMeshUpdate(true);
        left = float.PositiveInfinity;
        right = float.NegativeInfinity;
        float bottom = float.PositiveInfinity, top = float.NegativeInfinity;
        Matrix4x4 toPage = pageSpace.worldToLocalMatrix * row.transform.localToWorldMatrix;
        var info = row.textInfo;
        for (int c = 0; c < info.characterCount; c++)
        {
            var character = info.characterInfo[c];
            if (!character.isVisible) continue;
            var positions = info.meshInfo[character.materialReferenceIndex].vertices;
            for (int v = character.vertexIndex; v < character.vertexIndex + 4; v++)
            {
                Vector3 p = toPage.MultiplyPoint3x4(positions[v]);
                left = Mathf.Min(left, p.x); right = Mathf.Max(right, p.x);
                bottom = Mathf.Min(bottom, p.z); top = Mathf.Max(top, p.z);
            }
        }
        if (float.IsInfinity(left)) return;
        left = Mathf.Max(.12f, left - .025f);
        right = Mathf.Min(1.94f, right + .025f);
        centerZ = (bottom + top) * .5f;
        EnsureMesh();
        IsAchieved = true;
        startsAt = Time.unscaledTime + Mathf.Max(0, delay);
        Progress = 0;
        strokeRenderer.enabled = false;
    }

    private void EnsureMesh()
    {
        if (mesh != null) return;
        mesh = new Mesh { name = "DLJ_LedgerStrike_Runtime" };
        mesh.MarkDynamic();
        int[] triangles = new int[Segments * 6];
        for (int i = 0; i < Segments; i++)
        {
            int v = i * 2, t = i * 6;
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
            triangles[t + 3] = v + 2; triangles[t + 4] = v + 1; triangles[t + 5] = v + 3;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        GetComponent<MeshFilter>().sharedMesh = mesh;
        strokeRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        if (!IsAchieved || Progress >= 1 || Time.unscaledTime < startsAt) return;
        Progress = Mathf.Clamp01((Time.unscaledTime - startsAt) / Mathf.Max(.05f, drawDuration));
        // A quick pen stroke: accelerate briefly, then settle at the end.
        float reveal = 1 - Mathf.Pow(1 - Progress, 2);
        Matrix4x4 toLocal = transform.worldToLocalMatrix * pageSpace.localToWorldMatrix;
        for (int i = 0; i <= Segments; i++)
        {
            float u = i / (float)Segments * reveal;
            float x = Mathf.Lerp(left, right, u);
            float z = centerZ + .014f * (u - .5f) + Mathf.Sin(u * Mathf.PI * 3) * .004f;
            float width = strokeWidth * (.65f + .35f * Mathf.Sin(u * Mathf.PI));
            for (int side = 0; side < 2; side++)
            {
                float edgeZ = z + (side == 0 ? -width : width) * .5f;
                float y = .153f + .095f * Mathf.Sin(Mathf.Abs(x) * .5f * Mathf.PI)
                    + .008f * Mathf.Sin((edgeZ / 2.53f + .5f) * Mathf.PI) + .014f;
                vertices[i * 2 + side] = toLocal.MultiplyPoint3x4(new Vector3(x, y, edgeZ));
            }
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        strokeRenderer.enabled = Progress > 0;
    }

    private void OnDisable() => SetAchieved(false);
    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }
}
