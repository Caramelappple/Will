using UnityEngine;

/// <summary>장부 가운데 구분선의 색상과 왼쪽부터 그리는 등장 연출.</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class DLJ_LedgerRule : MonoBehaviour
{
    [SerializeField] private Color lineColor = new Color(.105f, .055f, .025f, 1);
    [SerializeField, Min(.05f)] private float drawDuration = .55f;
    [SerializeField, Min(0)] private float startDelay = .1f;
    private static readonly int ColorId = Shader.PropertyToID("_LineColor");
    private static readonly int ProgressId = Shader.PropertyToID("_DrawProgress");
    private static readonly int MinXId = Shader.PropertyToID("_MinX");
    private static readonly int MaxXId = Shader.PropertyToID("_MaxX");
    private MeshRenderer lineRenderer;
    private MeshFilter meshFilter;
    private MaterialPropertyBlock properties;
    private float startedAt;
    private bool dirty;
    public float Progress { get; private set; } = 1;
    public Color LineColor
    {
        get => lineColor;
        set { lineColor = value; dirty = true; }
    }

    private void OnEnable()
    {
        lineRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        properties ??= new MaterialPropertyBlock();
        Replay();
    }

    [ContextMenu("Replay Line")]
    public void Replay()
    {
        startedAt = Time.unscaledTime;
        Progress = Application.IsPlaying(gameObject) ? 0 : 1;
        Apply();
    }

    private void OnValidate() => dirty = true;

    private void Update()
    {
        if (Application.IsPlaying(gameObject) && Progress < 1)
        {
            float elapsed = Time.unscaledTime - startedAt;
            float t = Mathf.Clamp01((elapsed - startDelay) / Mathf.Max(.05f, drawDuration));
            Progress = t * t * (3 - 2 * t);
            dirty = true;
        }
        if (dirty) Apply();
    }

    private void OnDisable()
    {
        // Disabling this effect leaves the ordinary complete divider visible.
        Progress = 1;
        Apply();
    }

    private void Apply()
    {
        if (lineRenderer == null || meshFilter == null || meshFilter.sharedMesh == null) return;
        properties ??= new MaterialPropertyBlock();
        Bounds bounds = meshFilter.sharedMesh.bounds;
        lineRenderer.GetPropertyBlock(properties);
        properties.SetColor(ColorId, lineColor);
        properties.SetFloat(ProgressId, Progress);
        properties.SetFloat(MinXId, bounds.min.x);
        properties.SetFloat(MaxXId, bounds.max.x);
        lineRenderer.SetPropertyBlock(properties);
        dirty = false;
    }
}
