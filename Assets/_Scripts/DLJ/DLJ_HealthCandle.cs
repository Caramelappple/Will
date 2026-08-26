using System.Collections;
using UnityEngine;

public class DLJ_HealthCandle : MonoBehaviour
{
    [Header("References")]
    [Tooltip("이름이 Candle, Candle (1), Candle (2)면 자동으로 0, 1, 2를 사용한다.")]
    [SerializeField, Range(0, DLJ_PlayerHealth.CandleCount - 1)] private int candleIndex;
    [Tooltip("체력에 따라 세로 길이가 변할 초 몸통")]
    [SerializeField] private Transform candleBody;
    [Tooltip("줄어든 초의 윗면을 따라 내려갈 불꽃")]
    [SerializeField] private Transform flame;
    [SerializeField] private ParticleSystemRenderer flameRenderer;

    [Header("Animation")]
    [Tooltip("체력이 변한 뒤 목표 길이에 도달할 때까지 걸리는 시간")]
    [SerializeField, Min(0.01f)] private float resizeDuration = 2f;

    private Vector3 originalBodyPosition;
    private Vector3 originalBodyScale;
    private Vector3 originalFlamePosition;
    private Vector3 bodyBottom;
    private Vector3 bodyTop;
    private Bounds bodyBounds;
    private DLJ_PlayerHealth playerHealth;
    private int resolvedCandleIndex;
    private float displayedHealthRatio = 1f;
    private Coroutine resizeCoroutine;

    private void Awake()
    {
        resolvedCandleIndex = ResolveCandleIndex();

        if (flame == null && flameRenderer != null)
            flame = flameRenderer.transform;

        if (candleBody == null && flame != null && flame.parent != null)
        {
            MeshRenderer bodyRenderer = flame.parent.GetComponentInChildren<MeshRenderer>(true);
            if (bodyRenderer != null)
                candleBody = bodyRenderer.transform;
        }

        if (candleBody == null)
        {
            Debug.LogError($"{name}: 길이를 조절할 Candle Body가 지정되지 않았어.", this);
            enabled = false;
            return;
        }

        originalBodyPosition = candleBody.localPosition;
        originalBodyScale = candleBody.localScale;

        if (flame != null)
            originalFlamePosition = flame.localPosition;

        bodyBounds = CalculateBodyBounds();
        CacheBodyEndpoints();
    }

    private void OnEnable()
    {
        TryBindPlayerHealth();
    }

    private void Start()
    {
        TryBindPlayerHealth();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnCandleHealthChanged -= HandleHealthChanged;

        playerHealth = null;
        resizeCoroutine = null;
    }

    private void TryBindPlayerHealth()
    {
        if (playerHealth != null || DLJ_PlayerHealth.Instance == null)
            return;

        playerHealth = DLJ_PlayerHealth.Instance;
        playerHealth.OnCandleHealthChanged += HandleHealthChanged;
        ApplyHealthImmediately(playerHealth.GetCandleHealth(resolvedCandleIndex));
    }

    private void HandleHealthChanged(int changedIndex, int health)
    {
        if (changedIndex == resolvedCandleIndex)
            AnimateToHealth(health);
    }

    private int ResolveCandleIndex()
    {
        if (gameObject.name == "Candle")
            return 0;

        const string prefix = "Candle (";
        string objectName = gameObject.name;

        if (objectName.StartsWith(prefix) && objectName.EndsWith(")"))
        {
            string numberText = objectName.Substring(
                prefix.Length,
                objectName.Length - prefix.Length - 1);

            if (int.TryParse(numberText, out int nameIndex) &&
                nameIndex >= 0 && nameIndex < DLJ_PlayerHealth.CandleCount)
                return nameIndex;
        }

        return Mathf.Clamp(candleIndex, 0, DLJ_PlayerHealth.CandleCount - 1);
    }

    private void ApplyHealthImmediately(int health)
    {
        displayedHealthRatio = Mathf.Clamp01(
            health / (float)DLJ_PlayerHealth.MaxHealthPerCandle);
        ApplyHealthRatio(displayedHealthRatio);
    }

    private void AnimateToHealth(int health)
    {
        float targetRatio = Mathf.Clamp01(
            health / (float)DLJ_PlayerHealth.MaxHealthPerCandle);

        if (resizeCoroutine != null)
            StopCoroutine(resizeCoroutine);

        resizeCoroutine = StartCoroutine(ResizeOverTime(targetRatio));
    }

    private IEnumerator ResizeOverTime(float targetRatio)
    {
        float startRatio = displayedHealthRatio;
        float elapsed = 0f;
        float duration = resizeDuration > 0f ? resizeDuration : 2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            displayedHealthRatio = Mathf.Lerp(
                startRatio,
                targetRatio,
                Mathf.Clamp01(elapsed / duration));
            ApplyHealthRatio(displayedHealthRatio);
            yield return null;
        }

        displayedHealthRatio = targetRatio;
        ApplyHealthRatio(displayedHealthRatio);
        resizeCoroutine = null;
    }

    private void ApplyHealthRatio(float healthRatio)
    {

        Vector3 scale = originalBodyScale;
        scale.y = originalBodyScale.y * healthRatio;
        candleBody.localScale = scale;

        // 피벗 위치와 무관하게 원래 바닥을 고정한다. 그래서 초는 위에서부터 줄어든다.
        Vector3 scaledBottomOffset = candleBody.localRotation * Vector3.Scale(
            GetLocalBottom(), scale);
        candleBody.localPosition = bodyBottom - scaledBottomOffset;

        if (flame == null)
            return;

        Vector3 newTop = Vector3.Lerp(bodyBottom, bodyTop, healthRatio);
        Vector3 topDeltaInBodyParent = newTop - bodyTop;
        Vector3 topDeltaWorld = candleBody.parent != null
            ? candleBody.parent.TransformVector(topDeltaInBodyParent)
            : topDeltaInBodyParent;
        Vector3 topDeltaInFlameParent = flame.parent != null
            ? flame.parent.InverseTransformVector(topDeltaWorld)
            : topDeltaWorld;

        flame.localPosition = originalFlamePosition + topDeltaInFlameParent;
        flame.gameObject.SetActive(healthRatio > 0f);
    }

    private void CacheBodyEndpoints()
    {
        Vector3 localBottom = GetLocalBottom();
        Vector3 localTop = GetLocalTop();

        bodyBottom = originalBodyPosition
            + candleBody.localRotation * Vector3.Scale(localBottom, originalBodyScale);
        bodyTop = originalBodyPosition
            + candleBody.localRotation * Vector3.Scale(localTop, originalBodyScale);
    }

    private Vector3 GetLocalBottom()
    {
        return new Vector3(bodyBounds.center.x, bodyBounds.min.y, bodyBounds.center.z);
    }

    private Vector3 GetLocalTop()
    {
        return new Vector3(bodyBounds.center.x, bodyBounds.max.y, bodyBounds.center.z);
    }

    private Bounds CalculateBodyBounds()
    {
        Renderer[] renderers = candleBody.GetComponentsInChildren<Renderer>(true);
        Bounds combinedBounds = default;
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer is ParticleSystemRenderer)
                continue;

            Bounds rendererBounds;
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;

            if (meshFilter != null && meshFilter.sharedMesh != null)
                rendererBounds = meshFilter.sharedMesh.bounds;
            else if (skinnedRenderer != null)
                rendererBounds = skinnedRenderer.localBounds;
            else
                continue;

            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;

            for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            for (int z = 0; z <= 1; z++)
            {
                Vector3 corner = new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z);
                Vector3 worldCorner = renderer.transform.TransformPoint(corner);
                Vector3 bodyLocalCorner = candleBody.InverseTransformPoint(worldCorner);

                if (!hasBounds)
                {
                    combinedBounds = new Bounds(bodyLocalCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(bodyLocalCorner);
                }
            }
        }

        if (hasBounds)
            return combinedBounds;

        Debug.LogWarning($"{name}: Candle Body에서 메시를 찾지 못해 기본 높이를 사용해.", this);
        return new Bounds(Vector3.zero, Vector3.one);
    }
}
