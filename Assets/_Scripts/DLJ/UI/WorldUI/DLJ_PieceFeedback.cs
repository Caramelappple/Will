using System;
using System.Collections.Generic;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.DLJ.UI.WorldUI
{
    /// <summary>기물에 하나 붙이는 공통 순간 알림. 고정 상태 슬롯과 별도로 여러 효과를 표시한다.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1100)]
    public sealed class DLJ_PieceFeedback : MonoBehaviour
    {
        [Serializable]
        public sealed class EffectStyle
        {
            public string id;
            public Sprite icon;
            [Tooltip("아이콘이 없을 때 숫자 앞에 붙여 효과를 구분한다.")]
            public string fallbackLabel;
            public Color color = Color.white;
            [Min(0.1f)] public float duration = 1.1f;
        }

        [Header("종류별 표시 설정 — 항목을 추가해 확장")]
        [SerializeField] private List<EffectStyle> effects = new()
        {
            new EffectStyle { id = "heal", fallbackLabel = "HP", color = new Color(0.4f, 1f, 0.5f) },
            new EffectStyle { id = "attack", fallbackLabel = "ATK", color = new Color(1f, 0.8f, 0.3f) },
            new EffectStyle { id = "defense", fallbackLabel = "DEF", color = new Color(0.5f, 0.8f, 1f) },
            new EffectStyle { id = "resource", fallbackLabel = "RES", color = Color.white }
        };

        [Header("자동 연결")]
        [SerializeField] private bool showRecovery = true;
        [SerializeField] private Health health;

        [Header("위치와 크기 — 카메라 기준 월드 단위")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector3 cameraOffset = new(0.65f, 0.65f, -0.1f);
        [SerializeField, Min(0.01f)] private float worldScale = 1f;
        [SerializeField, Min(0.05f)] private float iconSize = 0.24f;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField, Min(1f)] private float fontSize = 24f;
        [SerializeField] private int sortingOrder = 120;

        [Header("동시 표시와 재사용")]
        [SerializeField, Range(1, 8)] private int maxVisible = 3;
        [SerializeField, Min(1)] private int maxQueued = 32;
        [SerializeField, Min(0.1f)] private float lineSpacing = 0.42f;
        [SerializeField, Min(0f)] private float riseDistance = 0.12f;
        [SerializeField] private bool useUnscaledTime = true;

        private readonly Queue<Notification> _pending = new();
        private readonly HashSet<string> _reportedUnknownIds = new();
        private DLJ_WorldFeedbackView[] _views;
        private GameObject _visualRoot;
        private Camera _camera;
        private Health _subscribedHealth;
        private int _nextCameraSearchFrame;
        private bool _reportedCapacity;
        private bool _reportedMissingCamera;
        private bool _reportedMissingFont;

        public int PendingCount => _pending.Count;

        private readonly struct Notification
        {
            public readonly Sprite Icon;
            public readonly string Text;
            public readonly Color Color;
            public readonly float Duration;

            public Notification(Sprite icon, string text, Color color, float duration)
            {
                Icon = icon;
                Text = text;
                Color = color;
                Duration = duration;
            }
        }

        private void OnEnable()
        {
            if (health == null) health = GetComponent<Health>();
            if (showRecovery && health != null)
            {
                _subscribedHealth = health;
                _subscribedHealth.OnRecover += HandleRecovery;
            }
        }

        private void OnDisable()
        {
            if (_subscribedHealth != null) _subscribedHealth.OnRecover -= HandleRecovery;
            _subscribedHealth = null;
            Clear();
        }

        private void OnDestroy()
        {
            if (_visualRoot != null) Destroy(_visualRoot);
        }

        /// <summary>예: ShowValue("attack", 1). 등록한 아이콘과 부호 있는 숫자를 표시한다.</summary>
        public bool ShowValue(string effectId, int amount)
        {
            if (amount == 0 || !isActiveAndEnabled || !Application.isPlaying) return false;
            if (effects != null)
            {
                foreach (EffectStyle effect in effects)
                {
                    if (effect == null || !string.Equals(effect.id, effectId, StringComparison.Ordinal))
                        continue;
                    string value = amount > 0 ? $"+{amount}" : amount.ToString();
                    string text = effect.icon == null && !string.IsNullOrEmpty(effect.fallbackLabel)
                        ? $"{effect.fallbackLabel} {value}" : value;
                    return ShowText(effect.icon, text, effect.color, effect.duration);
                }
            }

            if (_reportedUnknownIds.Add(effectId ?? string.Empty))
                Debug.LogWarning($"{name}: Feedback 종류 '{effectId}'을 Effects 목록에 등록해야 합니다.", this);
            return false;
        }

        /// <summary>슬롯이나 종류 등록 없이 임의의 아이콘과 문구를 표시한다.</summary>
        public bool ShowText(Sprite icon, string text, Color color, float duration = 1.1f)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || string.IsNullOrEmpty(text)) return false;
            if (_pending.Count >= Mathf.Max(1, maxQueued))
            {
                if (!_reportedCapacity)
                    Debug.LogWarning($"{name}: Feedback 대기열이 가득 차 새 알림을 거절했습니다.", this);
                _reportedCapacity = true;
                return false;
            }

            _pending.Enqueue(new Notification(icon, text, color,
                float.IsNaN(duration) || float.IsInfinity(duration) ? 1.1f : Mathf.Max(0.1f, duration)));
            return true;
        }

        public void Clear()
        {
            _pending.Clear();
            _reportedCapacity = false;
            if (_views != null)
                foreach (DLJ_WorldFeedbackView view in _views) view.Hide();
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        private void HandleRecovery(RecoverResultData result)
        {
            // 최대 체력 초과 요청량이 아닌 실제 회복량만 표시한다.
            ShowValue("heal", result.recoverValue);
        }

        private void LateUpdate()
        {
            if (_views == null && _pending.Count == 0) return;
            if (!ResolveCamera())
            {
                if (_visualRoot != null) _visualRoot.SetActive(false);
                return;
            }
            if (!EnsureViews()) return;

            Transform cameraTransform = _camera.transform;
            _visualRoot.transform.SetPositionAndRotation(
                transform.position + cameraTransform.rotation * cameraOffset, cameraTransform.rotation);
            _visualRoot.transform.localScale = Vector3.one * Mathf.Max(0.01f, worldScale);
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            bool anyActive = false;
            foreach (DLJ_WorldFeedbackView view in _views)
            {
                // 이동량을 행 간격보다 작게 제한해 동시 팝업의 겹침을 방지한다.
                view.Tick(deltaTime, Mathf.Min(riseDistance, lineSpacing * 0.35f));
                if (!view.IsActive && _pending.Count > 0)
                {
                    Notification item = _pending.Dequeue();
                    view.Show(item.Icon, item.Text, item.Color, item.Duration, iconSize);
                    _reportedCapacity = false;
                }
                anyActive |= view.IsActive;
            }
            _visualRoot.SetActive(anyActive);
        }

        private bool ResolveCamera()
        {
            if (targetCamera != null) _camera = targetCamera;
            else if ((_camera == null || !_camera.isActiveAndEnabled) && Time.frameCount >= _nextCameraSearchFrame)
            {
                _camera = Camera.main;
                _nextCameraSearchFrame = Time.frameCount + 30;
            }
            if (_camera != null && _camera.isActiveAndEnabled)
            {
                _reportedMissingCamera = false;
                return true;
            }
            if (!_reportedMissingCamera)
            {
                Debug.LogWarning($"{name}: Feedback에 사용할 Camera 또는 MainCamera가 필요합니다.", this);
                _reportedMissingCamera = true;
            }
            return false;
        }

        private bool EnsureViews()
        {
            if (_visualRoot != null) return true;
            // 프로젝트의 장식용 기본 글꼴에 공백/부호가 없을 수 있어 숫자용 글꼴을 우선한다.
            TMP_FontAsset actualFont = font != null ? font :
                Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (actualFont == null) actualFont = TMP_Settings.defaultFontAsset;
            if (actualFont == null)
            {
                if (!_reportedMissingFont)
                    Debug.LogWarning($"{name}: Feedback에 사용할 TMP Font를 연결해야 합니다.", this);
                _reportedMissingFont = true;
                return false;
            }

            _visualRoot = new GameObject($"{name}_Feedback");
            // 기물의 비균일 스케일을 상속하지 않고 같은 씬에서 함께 정리한다.
            SceneManager.MoveGameObjectToScene(_visualRoot, gameObject.scene);
            _visualRoot.SetActive(false);
            _views = new DLJ_WorldFeedbackView[Mathf.Clamp(maxVisible, 1, 8)];
            for (int i = 0; i < _views.Length; i++)
                _views[i] = new DLJ_WorldFeedbackView(_visualRoot.transform, i, lineSpacing,
                    actualFont, fontSize, sortingOrder);
            return true;
        }

        [ContextMenu("Preview/Heal +1 and Attack +1 (Play Mode)")]
        private void Preview()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Feedback 미리보기는 Play Mode에서 실행해야 합니다.", this);
                return;
            }
            ShowValue("heal", 1);
            ShowValue("attack", 1);
        }
    }
}
