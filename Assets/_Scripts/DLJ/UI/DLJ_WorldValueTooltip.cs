using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>월드 오브젝트의 숫자나 문자열을 잉크 번짐 텍스트로 보여주는 공통 툴팁.</summary>
public abstract class DLJ_WorldValueTooltip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("고정 표시 기준점. 비우면 대상의 기본 위치를 사용한다.")]
    [SerializeField] private Transform tooltipAnchor;
    [SerializeField] private TMP_FontAsset font;
    [Tooltip("여우왕 장부의 DLJ_LedgerInk 머티리얼. 전용 복사본에 폰트 아틀라스를 연결한다.")]
    [SerializeField] private Material inkMaterial;

    [Header("Appearance")]
    [SerializeField, Min(.1f)] private float fontSize = 5f;
    [SerializeField] private Color textColor = new Color(1f, .92f, .75f, 1f);
    [Tooltip("기준점에서 월드 위쪽으로 띄우는 거리")]
    [SerializeField, Min(0f)] private float heightOffset = .65f;
    [SerializeField, Min(.05f)] private float spreadDuration = .65f;
    [SerializeField, Min(.05f)] private float fadeOutDuration = .2f;

    private static readonly int ProgressId = Shader.PropertyToID("_InkProgress");
    private Transform hoveredTarget;
    private TextMeshPro label;
    private Material labelMaterial;
    private float inkProgress;
    private float opacity;
    private int displayedValue = -1;
    private int displayedMaximum = -1;
    private string displayedText;
    private bool displayingText;
    private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();
    private EventSystem pointerEventSystem;
    private PointerEventData pointerEventData;

    protected virtual void Awake()
    {
        if (font != null && inkMaterial != null) return;
        Debug.LogError($"[{GetType().Name}] Font와 Ink Material 연결이 필요해.", this);
        enabled = false;
    }

    protected virtual void LateUpdate()
    {
        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            targetCamera = Camera.main;

        Transform next = FindHoveredTarget();
        int value = 0;
        int maximum = 0;
        string text = null;
        bool hasText = next != null && TryGetText(out text);
        if (next != null && !hasText && !TryGetValue(out value, out maximum)) next = null;

        bool entering = next != null && hoveredTarget == null;
        hoveredTarget = next;
        if (next != null)
        {
            if (label == null) CreateLabel();
            if (hasText)
            {
                text ??= string.Empty;
                if (!displayingText || displayedText != text)
                {
                    displayedText = text;
                    displayingText = true;
                    label.text = text;
                    inkProgress = 0f;
                }
            }
            else if (displayingText || value != displayedValue || maximum != displayedMaximum)
            {
                displayedValue = value;
                displayedMaximum = maximum;
                displayingText = false;
                label.SetText("{0}/{1}", value, maximum);
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
        if (targetCamera == null) opacity = 0f;
        label.gameObject.SetActive(opacity > 0f);
        if (opacity <= 0f) return;

        label.transform.SetPositionAndRotation(
            (tooltipAnchor != null ? tooltipAnchor.position : GetDefaultAnchor()) +
                Vector3.up * heightOffset,
            targetCamera.transform.rotation);
        label.fontSize = fontSize;
        label.color = new Color(textColor.r, textColor.g, textColor.b, textColor.a * opacity);
        labelMaterial.SetFloat(ProgressId, inkProgress);
    }

    private Transform FindHoveredTarget()
    {
        if (targetCamera == null || Mouse.current == null || !Application.isFocused)
            return null;

        Vector2 pointer = Mouse.current.position.ReadValue();
        if (!targetCamera.pixelRect.Contains(pointer)) return null;
        if (IsPointerOverUI(pointer)) return null;
        Ray ray = targetCamera.ScreenPointToRay(pointer);
        Transform result = FindHoveredTarget(ray, targetCamera, out float distance);
        if (result != null && Physics.Raycast(ray, out RaycastHit hit, distance,
                targetCamera.cullingMask, QueryTriggerInteraction.Ignore) &&
            !hit.transform.IsChildOf(result))
            return null;
        return result;
    }

    private bool IsPointerOverUI(Vector2 pointer)
    {
        EventSystem current = EventSystem.current;
        if (current == null) return false;

        if (pointerEventData == null || pointerEventSystem != current)
        {
            pointerEventSystem = current;
            pointerEventData = new PointerEventData(current);
        }
        pointerEventData.position = pointer;
        pointerHits.Clear();
        current.RaycastAll(pointerEventData, pointerHits);
        foreach (RaycastResult hit in pointerHits)
            if (hit.module is GraphicRaycaster)
                return true;
        return false;
    }

    protected abstract Transform FindHoveredTarget(Ray ray, Camera camera, out float distance);
    protected virtual bool TryGetText(out string text)
    {
        text = null;
        return false;
    }

    protected virtual bool TryGetValue(out int value, out int maximum)
    {
        value = 0;
        maximum = 0;
        return false;
    }

    protected abstract Vector3 GetDefaultAnchor();

    private void CreateLabel()
    {
        // 부모의 비균일 스케일을 상속하지 않도록 월드 루트에 두고 수명은 직접 관리한다.
        var labelObject = new GameObject($"{GetType().Name}_Text");
        labelObject.SetActive(false);
        labelObject.layer = gameObject.layer;
        SceneManager.MoveGameObjectToScene(labelObject, gameObject.scene);
        label = labelObject.AddComponent<TextMeshPro>();
        label.font = font;
        labelMaterial = new Material(inkMaterial) { name = $"{GetType().Name}_Ink" };
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

    protected virtual void OnDisable()
    {
        hoveredTarget = null;
        inkProgress = 0f;
        opacity = 0f;
        if (label != null) label.gameObject.SetActive(false);
    }

    protected virtual void OnDestroy()
    {
        if (label != null) Destroy(label.gameObject);
        if (labelMaterial != null) Destroy(labelMaterial);
    }
}
