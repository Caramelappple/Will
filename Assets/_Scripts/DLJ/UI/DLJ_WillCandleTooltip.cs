using _Scripts.LSO.UI.Text;
using _Scripts.LSO.Will;
using _Scripts.LSO.Will.Candle;
using UnityEngine;

/// <summary>유언 선택 초에 마우스를 올리면 현재 유언의 이름을 표시한다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LSO_WillCandle))]
public sealed class DLJ_WillCandleTooltip : DLJ_WorldValueTooltip
{
    [SerializeField] private LSO_WillCandle candle;
    [SerializeField] private Collider hoverCollider;

    protected override void Awake()
    {
        base.Awake();
        if (candle == null) candle = GetComponent<LSO_WillCandle>();
        if (hoverCollider == null) hoverCollider = GetComponent<Collider>();
        if (candle != null && hoverCollider != null) return;
        Debug.LogError($"[{nameof(DLJ_WillCandleTooltip)}] Will Candle과 Collider 연결이 필요해.", this);
        enabled = false;
    }

    protected override bool TryGetText(out string text)
    {
        text = null;
        if (candle == null || !candle.isActiveAndEnabled) return false;
        LSO_WillType will = candle.Current;
        text = will == LSO_WillType.None ? "유언 없음" : LSO_WillText.NameOf(will);
        return true;
    }

    protected override Transform FindHoveredTarget(Ray ray, Camera camera, out float distance)
    {
        distance = camera.farClipPlane;
        if (candle == null || !candle.isActiveAndEnabled || hoverCollider == null ||
            !hoverCollider.enabled || (camera.cullingMask & (1 << gameObject.layer)) == 0)
            return null;

        if (!hoverCollider.Raycast(ray, out RaycastHit hit, distance) || hit.distance >= distance)
            return null;

        distance = hit.distance;
        return transform;
    }

    protected override Vector3 GetDefaultAnchor()
    {
        if (hoverCollider == null) return transform.position;
        Bounds bounds = hoverCollider.bounds;
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }
}
