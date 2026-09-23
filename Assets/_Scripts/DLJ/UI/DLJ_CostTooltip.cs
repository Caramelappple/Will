using System.Collections.Generic;
using UnityEngine;

/// <summary>코스트 케이스에 마우스를 올리면 현재/기본 코스트를 표시한다.</summary>
[DisallowMultipleComponent]
public sealed class DLJ_CostTooltip : DLJ_WorldValueTooltip
{
    [SerializeField] private DLJ_CostSystem costSystem;

    private readonly List<DLJ_CostCase> cases = new List<DLJ_CostCase>();

    protected override void Awake()
    {
        base.Awake();
        if (costSystem == null) costSystem = GetComponent<DLJ_CostSystem>();
    }

    protected override bool TryGetValue(out int value, out int maximum)
    {
        value = 0;
        maximum = 0;
        if (costSystem == null || !costSystem.HasActionPoints) return false;
        value = costSystem.VisibleCost;
        maximum = costSystem.MaxCost;
        return true;
    }

    protected override bool ShouldShowWithoutHover() => costSystem != null && costSystem.IsReplayingSpend;

    protected override bool TryGetText(out string text)
    {
        text = null;
        if (costSystem == null || !costSystem.IsReplayingSpend) return false;
        CollectCases();
        text = $"{costSystem.LastSpendFrom} → {costSystem.LastSpendTo}  (-{costSystem.LastSpendFrom - costSystem.LastSpendTo})";
        return true;
    }

    protected override Transform FindHoveredTarget(Ray ray, Camera camera, out float distance)
    {
        distance = camera.farClipPlane;
        if (costSystem == null || !costSystem.HasActionPoints) return null;

        CollectCases();
        Transform result = null;
        foreach (DLJ_CostCase costCase in cases)
        {
            if (costCase == null || (camera.cullingMask & (1 << costCase.gameObject.layer)) == 0 ||
                !costCase.TryGetTooltipBounds(out Bounds bounds)) continue;
            if (!bounds.IntersectRay(ray, out float hitDistance) || hitDistance >= distance) continue;
            distance = hitDistance;
            result = costCase.transform;
        }
        return result;
    }

    protected override Vector3 GetDefaultAnchor()
    {
        bool found = false;
        Bounds combined = default;
        foreach (DLJ_CostCase costCase in cases)
        {
            if (costCase == null || !costCase.TryGetTooltipBounds(out Bounds bounds)) continue;
            if (!found)
            {
                combined = bounds;
                found = true;
            }
            else combined.Encapsulate(bounds);
        }
        return found
            ? new Vector3(combined.center.x, combined.max.y, combined.center.z)
            : transform.position;
    }

    private void CollectCases()
    {
        cases.Clear();
        GetComponentsInChildren(true, cases);
    }
}
