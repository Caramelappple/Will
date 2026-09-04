using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace _Scripts.DLJ.UI.WorldUI
{
    /// <summary>
    /// SpriteRenderer와 3D TextMeshPro로 기물 위 UI 한 칸을 그린다.
    /// 지속 데이터와 일회성 데이터를 분리해 임시 표시 뒤 최신 값으로 복구한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DLJ_WorldUISlot : MonoBehaviour
    {
        [Header("식별")]
        [SerializeField] private DLJ_WorldUISlotId id;

        [Header("공통")]
        [Tooltip("Slot 컴포넌트는 유지하고 실제 렌더러가 든 이 자식만 켜고 끈다.")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private TMP_Text label;
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private Vector2 iconWorldSize = new(0.22f, 0.22f);

        [Header("게이지")]
        [SerializeField] private SpriteRenderer fillBackground;
        [SerializeField] private SpriteRenderer fillRenderer;

        [Header("중첩 아이콘")]
        [SerializeField] private Transform stackContainer;
        [SerializeField] private SpriteRenderer stackTemplate;
        [SerializeField] private Vector2 stackIconWorldSize = new(0.16f, 0.16f);
        [SerializeField] private Vector3 stackSpacing = new(0.19f, 0f, 0f);
        [SerializeField, Min(1)] private int maxVisibleStacks = 10;

        [Header("전환")]
        [SerializeField, Min(0f)] private float transitionDuration = 0.12f;
        [SerializeField, Range(0.1f, 1f)] private float hiddenScale = 0.88f;
        [SerializeField] private bool useUnscaledTime = true;

        private readonly List<SpriteRenderer> _stackRenderers = new();
        private DLJ_WorldUIData _persistentData;
        private DLJ_WorldUIData _lastRenderedData;
        private Coroutine _temporaryRoutine;
        private Coroutine _transitionRoutine;
        private Vector3 _contentBaseScale = Vector3.one;
        private Vector3 _fillBaseScale = Vector3.one;
        private Vector3 _fillBasePosition;
        private bool _hasPersistentData;
        private bool _hasRenderedData;
        private bool _showingTemporary;
        private bool _isVisible;
        private bool _initialized;
        private bool _reportedMissingStackWiring;
        private bool _reportedMissingContentRoot;

        public DLJ_WorldUISlotId Id => id;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            if (!_showingTemporary)
                Render(_hasPersistentData ? _persistentData : DLJ_WorldUIData.Hidden());
        }

        private void OnDisable()
        {
            StopRoutine(ref _temporaryRoutine);
            StopRoutine(ref _transitionRoutine);
            _showingTemporary = false;
            _isVisible = false;
            _hasRenderedData = false;
            HideImmediately();
        }

        public void SetPersistent(DLJ_WorldUIData data)
        {
            EnsureInitialized();
            _persistentData = data;
            _hasPersistentData = true;

            if (!_showingTemporary)
                Render(data);
        }

        public void ShowTemporary(DLJ_WorldUIData data, float duration)
        {
            EnsureInitialized();
            StopRoutine(ref _temporaryRoutine);
            _showingTemporary = true;
            Render(data);

            if (duration <= 0f)
            {
                RestorePersistent();
                return;
            }

            _temporaryRoutine = StartCoroutine(RestoreAfter(duration));
        }

        public void Hide()
        {
            EnsureInitialized();
            StopRoutine(ref _temporaryRoutine);
            _showingTemporary = false;
            _persistentData = DLJ_WorldUIData.Hidden();
            _hasPersistentData = true;
            Render(_persistentData);
        }

        public void Configure(
            DLJ_WorldUISlotId slotId,
            GameObject root,
            TMP_Text textLabel = null,
            SpriteRenderer mainIcon = null,
            SpriteRenderer progressBackground = null,
            SpriteRenderer progressFill = null,
            Transform stacksRoot = null,
            SpriteRenderer stacksTemplate = null)
        {
            id = slotId;
            contentRoot = root;
            label = textLabel;
            icon = mainIcon;
            fillBackground = progressBackground;
            fillRenderer = progressFill;
            stackContainer = stacksRoot;
            stackTemplate = stacksTemplate;

            if (stackTemplate != null)
                stackTemplate.gameObject.SetActive(false);

            _initialized = false;
            _hasRenderedData = false;
        }

        [ContextMenu("Auto Wire Missing References")]
        public void AutoWireMissingReferences()
        {
            if (contentRoot == null)
            {
                Transform content = transform.Find("Content");
                if (content != null)
                    contentRoot = content.gameObject;
            }

            Transform searchRoot = contentRoot != null ? contentRoot.transform : transform;

            if (label == null)
                label = FindNamedComponent<TMP_Text>(searchRoot, "Label");
            if (icon == null)
                icon = FindNamedComponent<SpriteRenderer>(searchRoot, "Icon");
            if (fillBackground == null)
                fillBackground = FindNamedComponent<SpriteRenderer>(searchRoot, "FillBackground");
            if (fillRenderer == null)
                fillRenderer = FindNamedComponent<SpriteRenderer>(searchRoot, "Fill");
            if (stackContainer == null)
                stackContainer = FindNamedTransform(searchRoot, "StackContainer");
            if (stackTemplate == null)
                stackTemplate = FindNamedComponent<SpriteRenderer>(searchRoot, "StackTemplate");
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;

            _initialized = true;
            AutoWireMissingReferences();

            if (contentRoot != null)
                _contentBaseScale = contentRoot.transform.localScale;
            if (fillRenderer != null)
            {
                _fillBaseScale = fillRenderer.transform.localScale;
                _fillBasePosition = fillRenderer.transform.localPosition;
            }
            if (stackTemplate != null)
                stackTemplate.gameObject.SetActive(false);

            _isVisible = false;
            HideImmediately();
            ValidateWiring();
        }

        private void Render(DLJ_WorldUIData data)
        {
            if (_hasRenderedData && data.Equals(_lastRenderedData))
                return;

            _lastRenderedData = data;
            _hasRenderedData = true;

            if (!data.Visible)
            {
                SetVisible(false);
                return;
            }

            ActivateContent();
            ApplyText(data);
            ApplyIcon(data);
            ApplyFill(data);
            ApplyStacks(data);
            SetVisible(true);
        }

        private void ApplyText(DLJ_WorldUIData data)
        {
            if (label == null) return;

            string displayText = data.Text;
            if (data.Content == DLJ_WorldUIData.ContentType.Stacks &&
                string.IsNullOrEmpty(displayText) &&
                data.StackCapacity > maxVisibleStacks)
            {
                displayText = $"{data.StackCount}/{data.StackCapacity}";
            }

            label.gameObject.SetActive(!string.IsNullOrEmpty(displayText));
            label.text = displayText;
        }

        private void ApplyIcon(DLJ_WorldUIData data)
        {
            if (icon == null) return;

            bool showIcon = data.Content != DLJ_WorldUIData.ContentType.Stacks && data.Icon != null;
            icon.gameObject.SetActive(showIcon);
            if (!showIcon) return;

            icon.sprite = data.Icon;
            icon.color = data.Tint;
            FitSprite(icon, iconWorldSize);
        }

        private void ApplyFill(DLJ_WorldUIData data)
        {
            bool showFill = data.Content == DLJ_WorldUIData.ContentType.Progress &&
                            fillRenderer != null &&
                            fillRenderer.sprite != null;

            if (fillBackground != null)
                fillBackground.gameObject.SetActive(showFill);
            if (fillRenderer == null) return;

            fillRenderer.gameObject.SetActive(showFill);
            if (!showFill) return;

            float ratio = data.FillAmount;
            Vector3 scale = _fillBaseScale;
            scale.x *= ratio;
            fillRenderer.transform.localScale = scale;

            float halfWidth = fillRenderer.sprite.bounds.extents.x;
            Vector3 position = _fillBasePosition;
            position.x -= (_fillBaseScale.x - scale.x) * halfWidth;
            fillRenderer.transform.localPosition = position;

            if (data.OverrideFillTint)
                fillRenderer.color = data.FillTint;
        }

        private void ApplyStacks(DLJ_WorldUIData data)
        {
            bool showStacks = data.Content == DLJ_WorldUIData.ContentType.Stacks &&
                              data.StackCapacity > 0 &&
                              data.Icon != null;

            if (stackContainer != null)
                stackContainer.gameObject.SetActive(showStacks);

            int visibleCount = showStacks
                ? Mathf.Min(data.StackCapacity, maxVisibleStacks)
                : 0;
            EnsureStackRenderers(visibleCount);

            Vector3 start = -stackSpacing * ((visibleCount - 1) * 0.5f);
            for (int i = 0; i < _stackRenderers.Count; i++)
            {
                SpriteRenderer stackRenderer = _stackRenderers[i];
                bool visible = i < visibleCount;
                stackRenderer.gameObject.SetActive(visible);
                if (!visible) continue;

                stackRenderer.sprite = data.Icon;
                stackRenderer.color = i < data.StackCount ? data.Tint : data.InactiveTint;
                stackRenderer.transform.localPosition = start + stackSpacing * i;
                FitSprite(stackRenderer, stackIconWorldSize);
            }
        }

        private void EnsureStackRenderers(int count)
        {
            if (count <= _stackRenderers.Count) return;

            if (stackContainer == null || stackTemplate == null)
            {
                if (!_reportedMissingStackWiring)
                {
                    _reportedMissingStackWiring = true;
                    Debug.LogWarning($"{name}: StackContainer 또는 StackTemplate이 비어 있습니다.", this);
                }
                return;
            }

            while (_stackRenderers.Count < count)
            {
                SpriteRenderer instance = Instantiate(stackTemplate, stackContainer);
                instance.name = $"Stack_{_stackRenderers.Count + 1}";
                instance.gameObject.SetActive(false);
                _stackRenderers.Add(instance);
            }
        }

        private void SetVisible(bool visible)
        {
            if (visible)
            {
                bool wasVisible = _isVisible;
                _isVisible = true;
                ActivateContent();

                if (!wasVisible && transitionDuration > 0f && isActiveAndEnabled)
                {
                    contentRoot.transform.localScale = _contentBaseScale * hiddenScale;
                    StartTransition(_contentBaseScale, false);
                }
                else if (_transitionRoutine == null)
                {
                    contentRoot.transform.localScale = _contentBaseScale;
                }
                return;
            }

            if (!_isVisible)
            {
                HideImmediately();
                return;
            }

            _isVisible = false;
            if (transitionDuration > 0f && isActiveAndEnabled)
                StartTransition(_contentBaseScale * hiddenScale, true);
            else
                HideImmediately();
        }

        private void StartTransition(Vector3 targetScale, bool deactivateAfter)
        {
            StopRoutine(ref _transitionRoutine);
            _transitionRoutine = StartCoroutine(ScaleTo(targetScale, deactivateAfter));
        }

        private IEnumerator ScaleTo(Vector3 targetScale, bool deactivateAfter)
        {
            Vector3 startScale = contentRoot.transform.localScale;
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                contentRoot.transform.localScale = Vector3.Lerp(
                    startScale,
                    targetScale,
                    elapsed / transitionDuration);
                yield return null;
            }

            contentRoot.transform.localScale = targetScale;
            _transitionRoutine = null;

            if (deactivateAfter)
                HideImmediately();
        }

        private IEnumerator RestoreAfter(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            _temporaryRoutine = null;
            RestorePersistent();
        }

        private void RestorePersistent()
        {
            _showingTemporary = false;
            Render(_hasPersistentData ? _persistentData : DLJ_WorldUIData.Hidden());
        }

        private void ActivateContent()
        {
            if (contentRoot != null && !contentRoot.activeSelf)
                contentRoot.SetActive(true);
        }

        private void HideImmediately()
        {
            if (contentRoot == null)
            {
                if (!_reportedMissingContentRoot)
                {
                    _reportedMissingContentRoot = true;
                    Debug.LogWarning($"{name}: Content 자식이 없어 슬롯을 숨길 수 없습니다.", this);
                }
                return;
            }

            contentRoot.transform.localScale = _contentBaseScale;
            contentRoot.SetActive(false);
        }

        private void ValidateWiring()
        {
            if (id == DLJ_WorldUISlotId.None)
                Debug.LogWarning($"{name}: World UI 슬롯 ID가 지정되지 않았습니다.", this);
            if (contentRoot == null)
                Debug.LogWarning($"{name}: Content 자식을 연결해야 합니다.", this);
        }

        private void StopRoutine(ref Coroutine routine)
        {
            if (routine == null) return;
            StopCoroutine(routine);
            routine = null;
        }

        private static void FitSprite(SpriteRenderer renderer, Vector2 worldSize)
        {
            if (renderer == null || renderer.sprite == null) return;

            Vector3 spriteSize = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(
                spriteSize.x > 0f ? worldSize.x / spriteSize.x : 1f,
                spriteSize.y > 0f ? worldSize.y / spriteSize.y : 1f,
                1f);
        }

        private static T FindNamedComponent<T>(Transform root, string objectName) where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            foreach (T component in components)
            {
                if (component.name == objectName)
                    return component;
            }
            return null;
        }

        private static Transform FindNamedTransform(Transform root, string objectName)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in transforms)
            {
                if (child.name == objectName)
                    return child;
            }
            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxVisibleStacks = Mathf.Max(1, maxVisibleStacks);
            transitionDuration = Mathf.Max(0f, transitionDuration);
            AutoWireMissingReferences();
            ValidateWiring();
        }
#endif
    }
}
