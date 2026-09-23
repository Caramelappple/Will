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

    /// <summary>
    /// 손을 올린 채로 유언을 바꿔도 잉크를 처음부터 다시 번지게 하지 않는다.
    ///
    /// 유언은 숫자키·휠로 연달아 바뀐다. 바뀔 때마다 0.65초짜리 번짐을 새로 시작하면
    /// 끝까지 번지기 전에 다음 것이 들어와서, 글씨가 늘 번지다 만 상태로 보인다.
    /// 이미 떠 있는 동안에는 글씨만 갈아 끼우는 편이 읽기 좋다.
    ///
    /// 처음 뜰 때의 번짐은 RevealInkOnShow 가 맡으므로 그대로 나온다.
    /// </summary>
    protected override bool ReplayInkOnValueChange => false;
    protected override float TextWrapWidth => 6f;

    protected override bool TryGetText(out string text)
    {
        text = null;
        if (candle == null) return false;
        LSO_WillType will = candle.Current;
        text = will == LSO_WillType.None ? "유언 없음" :
            $"{LSO_WillText.NameOf(will)}\n<size=65%>{LSO_WillText.DescriptionOf(will)}</size>";
        return true;
    }

    protected override Transform FindHoveredTarget(Ray ray, Camera camera, out float distance)
    {
        distance = camera.farClipPlane;
        if (candle == null || hoverCollider == null ||
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
