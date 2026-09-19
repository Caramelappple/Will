using UnityEngine;
using UnityEngine.UI;

/// <summary>RectTransform 크기에 맞춰 그리는 편집 가능한 UI 선. 점은 0~1 좌표다.</summary>
[AddComponentMenu("DLJ/UI/Sunfish Line Graphic")]
public sealed class DLJ_SunfishLineGraphic : MaskableGraphic
{
    [SerializeField, Min(0.1f)] private float thickness = 8f;
    [SerializeField] private Vector2[] points;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points == null || points.Length < 2) return;
        Rect rect = rectTransform.rect;
        for (int i = 1; i < points.Length; i++)
        {
            Vector2 from = rect.min + Vector2.Scale(points[i - 1], rect.size);
            Vector2 to = rect.min + Vector2.Scale(points[i], rect.size);
            Vector2 direction = (to - from).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);
            int start = vh.currentVertCount;
            vh.AddVert(from - normal, color, Vector2.zero);
            vh.AddVert(from + normal, color, Vector2.zero);
            vh.AddVert(to + normal, color, Vector2.zero);
            vh.AddVert(to - normal, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
