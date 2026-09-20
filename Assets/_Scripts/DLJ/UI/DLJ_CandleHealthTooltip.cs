using UnityEngine;

/// <summary>체력 양초에 마우스를 올리면 총 체력을 표시한다.</summary>
[DisallowMultipleComponent]
public sealed class DLJ_CandleHealthTooltip : DLJ_WorldValueTooltip
{
    [Header("Damage Display")]
    [Tooltip("양초가 녹는 속도와 체력 숫자가 내려가는 속도를 함께 조절한다. 1이 기본 속도")]
    [SerializeField, Range(.25f, 4f)] private float animationSpeed = 1f;
    [Tooltip("피해 숫자가 다 내려간 뒤 툴팁을 유지하는 시간")]
    [SerializeField, Min(0f)] private float damageTooltipHoldDuration = 2f;

    private DLJ_HealthCandle[] candles;
    private Vector3 fixedAnchorLocalPosition;
    private DLJ_PlayerHealth boundHealth;
    private int displayedHealth = -1;
    private int targetHealth = -1;
    private float holdRemaining;
    private bool showingDamage;

    protected override bool ShouldShowWithoutHover() => showingDamage;
    protected override bool ReplayInkOnValueChange => false;
    protected override bool RevealInkOnShow => false;

    protected override void Awake()
    {
        base.Awake();
        candles = GetComponentsInChildren<DLJ_HealthCandle>(true);
        ApplyAnimationSpeed();
    }

    private void OnEnable()
    {
        BindPlayerHealth();
    }

    private void Start()
    {
        BindPlayerHealth();
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

    protected override void LateUpdate()
    {
        BindPlayerHealth();
        ApplyAnimationSpeed();
        if (boundHealth != null && boundHealth.TotalHealth != targetHealth)
        {
            // 저장 복원과 회복은 피해 카운트다운을 거치지 않는다.
            displayedHealth = targetHealth = boundHealth.TotalHealth;
            showingDamage = false;
        }
        if (displayedHealth > targetHealth)
        {
            // 각 초의 실제 표시 높이를 따라간다. 앞 초가 녹는 동안 뒤 초가
            // 기다리면, 숫자도 그 경계에서 함께 기다린다.
            int visualHealth = GetVisualHealth();
            if (visualHealth < displayedHealth)
                displayedHealth = Mathf.Max(visualHealth, displayedHealth - 1);
        }
        else if (showingDamage)
        {
            holdRemaining -= Time.unscaledDeltaTime;
            if (holdRemaining <= 0f) showingDamage = false;
        }

        base.LateUpdate();
    }

    protected override void OnDisable()
    {
        if (boundHealth != null)
            boundHealth.OnHealthDamaged -= HandleDamage;
        boundHealth = null;
        displayedHealth = -1;
        targetHealth = -1;
        showingDamage = false;
        base.OnDisable();
    }

    private void BindPlayerHealth()
    {
        DLJ_PlayerHealth current = DLJ_PlayerHealth.Instance;
        if (boundHealth == current) return;
        if (boundHealth != null)
            boundHealth.OnHealthDamaged -= HandleDamage;

        boundHealth = current;
        displayedHealth = current != null ? current.TotalHealth : -1;
        targetHealth = displayedHealth;
        showingDamage = false;
        if (boundHealth != null)
            boundHealth.OnHealthDamaged += HandleDamage;
    }

    private void HandleDamage(int previousHealth, int nextHealth)
    {
        if (nextHealth >= previousHealth) return;
        if (displayedHealth < 0) displayedHealth = previousHealth;
        targetHealth = nextHealth;
        holdRemaining = damageTooltipHoldDuration;
        showingDamage = true;
    }

    private void ApplyAnimationSpeed()
    {
        if (candles == null) return;
        foreach (DLJ_HealthCandle candle in candles)
            if (candle != null)
                candle.PlaybackSpeed = animationSpeed;
    }

    private int GetVisualHealth()
    {
        int total = 0;
        for (int i = 0; i < DLJ_PlayerHealth.CandleCount; i++)
        {
            DLJ_HealthCandle visual = null;
            foreach (DLJ_HealthCandle candle in candles)
                if (candle != null && candle.isActiveAndEnabled && candle.CandleIndex == i)
                {
                    visual = candle;
                    break;
                }

            total += visual != null ? visual.DisplayedHealth : boundHealth.GetCandleHealth(i);
        }
        return total;
    }

    protected override Vector3 GetDefaultAnchor() => transform.TransformPoint(fixedAnchorLocalPosition);

    protected override bool TryGetValue(out int value, out int maximum)
    {
        value = 0;
        maximum = 0;
        DLJ_PlayerHealth health = boundHealth;
        if (health == null) return false;

        value = displayedHealth;
        for (int i = 0; i < DLJ_PlayerHealth.CandleCount; i++)
            if (health.GetCandleHealth(i) > 0)
                maximum += DLJ_PlayerHealth.MaxHealthPerCandle;
        // 초가 막 꺼졌어도 숫자가 그 경계를 지날 때까지 분모가 분자보다 작아지지 않게 한다.
        maximum = Mathf.Max(maximum, Mathf.CeilToInt(value / (float)DLJ_PlayerHealth.MaxHealthPerCandle)
            * DLJ_PlayerHealth.MaxHealthPerCandle);
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
