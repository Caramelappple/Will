using UnityEngine;

/// <summary>체력 양초에 마우스를 올리면 총 체력을 표시한다.</summary>
[DisallowMultipleComponent]
public sealed class DLJ_CandleHealthTooltip : DLJ_WorldValueTooltip
{
    private DLJ_HealthCandle[] candles;
    private Vector3 fixedAnchorLocalPosition;

    protected override void Awake()
    {
        base.Awake();
        candles = GetComponentsInChildren<DLJ_HealthCandle>(true);
    }

    private void Start()
    {
        // 저장된 체력이 낮아도 원래 높이를 기준으로 잡고, 이후 초가 줄어들어도 유지한다.
        bool hasAnchor = false;
        Bounds anchors = default;
        foreach (DLJ_HealthCandle candle in candles)
        {
            if (candle == null || !candle.isActiveAndEnabled) continue;
            Vector3 point = candle.FullHeightTooltipAnchor;
            if (!hasAnchor)
            {
                anchors = new Bounds(point, Vector3.zero);
                hasAnchor = true;
            }
            else anchors.Encapsulate(point);
        }
        Vector3 fixedPoint = hasAnchor
            ? new Vector3(anchors.center.x, anchors.max.y, anchors.center.z)
            : transform.position;
        fixedAnchorLocalPosition = transform.InverseTransformPoint(fixedPoint);
    }

    protected override Vector3 GetDefaultAnchor() => transform.TransformPoint(fixedAnchorLocalPosition);

    protected override bool TryGetValue(out int value, out int maximum)
    {
        value = 0;
        maximum = 0;
        DLJ_PlayerHealth health = DLJ_PlayerHealth.Instance;
        if (health == null) return false;

        value = health.TotalHealth;
        for (int i = 0; i < DLJ_PlayerHealth.CandleCount; i++)
            if (health.GetCandleHealth(i) > 0)
                maximum += DLJ_PlayerHealth.MaxHealthPerCandle;
        return true;
    }

    protected override Transform FindHoveredTarget(Ray ray, Camera camera, out float distance)
    {
        distance = camera.farClipPlane;
        if (DLJ_PlayerHealth.Instance == null) return null;

        Transform result = null;
        foreach (DLJ_HealthCandle candle in candles)
        {
            if (candle == null || (camera.cullingMask & (1 << candle.gameObject.layer)) == 0 ||
                !candle.TryGetTooltipBounds(out Bounds bounds)) continue;
            if (!bounds.IntersectRay(ray, out float hitDistance) || hitDistance >= distance) continue;
            distance = hitDistance;
            result = candle.transform;
        }
        return result;
    }
}
