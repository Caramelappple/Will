using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>체력 양초에 마우스를 올리면 총 체력을 장부의 잉크 번짐으로 표시한다.</summary>
[DisallowMultipleComponent]
public sealed class DLJ_CandleHealthTooltip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("고정 표시 기준점. 비우면 처음 배치된 양초 묶음의 위쪽 중앙을 사용한다.")]
    [SerializeField] private Transform tooltipAnchor;
    [SerializeField] private TMP_FontAsset font;
    [Tooltip("여우왕 장부의 DLJ_LedgerInk 머티리얼. 전용 복사본에 폰트 아틀라스를 연결한다.")]
    [SerializeField] private Material inkMaterial;

    [Header("Appearance")]
    [SerializeField, Min(.1f)] private float fontSize = 5f;
    [SerializeField] private Color textColor = new Color(1f, .92f, .75f, 1f);
    [Tooltip("고정 기준점에서 월드 위쪽으로 띄우는 거리")]
    [SerializeField, Min(0f)] private float heightOffset = .65f;
    [SerializeField, Min(.05f)] private float spreadDuration = .65f;
    [SerializeField, Min(.05f)] private float fadeOutDuration = .2f;

    private static readonly int ProgressId = Shader.PropertyToID("_InkProgress");
    private DLJ_HealthCandle[] candles;
    private DLJ_HealthCandle hoveredCandle;
    private Vector3 fixedAnchorLocalPosition;
    private TextMeshPro label;
    private Material labelMaterial;
    private float inkProgress;
    private float opacity;
    private int displayedHealth = -1;
    private int displayedMaxHealth = -1;

    private void Awake()
    {
        candles = GetComponentsInChildren<DLJ_HealthCandle>(true);
        if (font == null || inkMaterial == null)
        {
            Debug.LogError("[DLJ_CandleHealthTooltip] Font와 Ink Material 연결이 필요해.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        // 저장된 체력이 낮아도 원래 높이를 기준으로 잡고, 이후 초가 줄어들어도 유지한다.
        bool hasAnchor = false;
        Bounds anchors = default;
        foreach (var candle in candles)
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

    private void LateUpdate()
    {
        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            targetCamera = Camera.main;

        var health = DLJ_PlayerHealth.Instance;
        DLJ_HealthCandle next = FindHoveredCandle(health);
        bool entering = next != null && hoveredCandle == null;
        hoveredCandle = next;
        if (next != null)
        {
            if (label == null) CreateLabel();
            int total = health.TotalHealth;
            int maximum = 0;
            for (int i = 0; i < DLJ_PlayerHealth.CandleCount; i++)
                if (health.GetCandleHealth(i) > 0)
                    maximum += DLJ_PlayerHealth.MaxHealthPerCandle;
            if (total != displayedHealth || maximum != displayedMaxHealth)
            {
                displayedHealth = total;
                displayedMaxHealth = maximum;
                label.SetText("{0}/{1}", total, maximum);
                inkProgress = 0f;
            }
            // 빠르게 재진입하면 진행 중인 연출을 이어가서 깜빡임을 피한다.
            if (entering && opacity <= 0f) inkProgress = 0f;
            label.gameObject.SetActive(true);
            inkProgress = Mathf.MoveTowards(inkProgress, 1f,
                Time.unscaledDeltaTime / Mathf.Max(.05f, spreadDuration));
            opacity = Mathf.MoveTowards(opacity, 1f, Time.unscaledDeltaTime / .1f);
        }
        else
        {
            opacity = Mathf.MoveTowards(opacity, 0f,
                Time.unscaledDeltaTime / Mathf.Max(.05f, fadeOutDuration));
        }

        if (label == null) return;
        if (targetCamera == null)
            opacity = 0f;
        label.gameObject.SetActive(opacity > 0f);
        if (opacity <= 0f) return;

        label.transform.SetPositionAndRotation(
            (tooltipAnchor != null ? tooltipAnchor.position : transform.TransformPoint(fixedAnchorLocalPosition))
                + Vector3.up * heightOffset,
            targetCamera.transform.rotation);
        label.fontSize = fontSize;
        label.color = new Color(textColor.r, textColor.g, textColor.b, textColor.a * opacity);
        labelMaterial.SetFloat(ProgressId, inkProgress);
    }

    private DLJ_HealthCandle FindHoveredCandle(DLJ_PlayerHealth health)
    {
        if (health == null || targetCamera == null || Mouse.current == null || !Application.isFocused)
            return null;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return null;

        Vector2 pointer = Mouse.current.position.ReadValue();
        if (!targetCamera.pixelRect.Contains(pointer)) return null;
        Ray ray = targetCamera.ScreenPointToRay(pointer);
        float nearest = targetCamera.farClipPlane;
        DLJ_HealthCandle result = null;
        foreach (var candle in candles)
        {
            if (candle == null || (targetCamera.cullingMask & (1 << candle.gameObject.layer)) == 0 ||
                !candle.TryGetTooltipBounds(out Bounds bounds)) continue;
            if (!bounds.IntersectRay(ray, out float distance) || distance >= nearest) continue;
            nearest = distance;
            result = candle;
        }
        if (result != null && Physics.Raycast(ray, out RaycastHit hit, nearest,
                targetCamera.cullingMask, QueryTriggerInteraction.Ignore) &&
            !hit.transform.IsChildOf(result.transform))
            return null;
        return result;
    }

    private void CreateLabel()
    {
        // 초의 비균일 스케일을 상속하지 않도록 월드 루트에 두고 수명은 직접 관리한다.
        var labelObject = new GameObject("DLJ_CandleHealthTooltip_Text");
        labelObject.SetActive(false);
        labelObject.layer = gameObject.layer;
        SceneManager.MoveGameObjectToScene(labelObject, gameObject.scene);
        label = labelObject.AddComponent<TextMeshPro>();
        label.font = font;
        labelMaterial = new Material(inkMaterial) { name = "DLJ_CandleHealthTooltip_Ink" };
        labelMaterial.mainTexture = font.atlasTexture;
        labelMaterial.SetFloat("_GradientScale", font.material.GetFloat("_GradientScale"));
        labelMaterial.SetFloat("_TextureWidth", font.atlasWidth);
        labelMaterial.SetFloat("_TextureHeight", font.atlasHeight);
        labelMaterial.SetFloat(ProgressId, 0f);
        label.fontSharedMaterial = labelMaterial;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        label.rectTransform.sizeDelta = new Vector2(5f, 1f);
        label.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        label.renderer.receiveShadows = false;
    }

    private void OnDisable()
    {
        hoveredCandle = null;
        inkProgress = 0f;
        opacity = 0f;
        if (label != null) label.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (label != null) Destroy(label.gameObject);
        if (labelMaterial != null) Destroy(labelMaterial);
    }
}
