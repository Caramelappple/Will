using System;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.DLJ.UI.WorldUI
{
    /// <summary>Health의 변화를 공통 기물 UI의 게이지 슬롯으로 전달한다.</summary>
    [DisallowMultipleComponent]
    public sealed class DLJ_WorldHealthUIBinder : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private DLJ_WorldUIController worldUI;
        [SerializeField] private DLJ_WorldUISlotId slotId = DLJ_WorldUISlotId.Health;
        [SerializeField] private Sprite icon;
        [SerializeField] private Color iconTint = Color.white;
        [SerializeField] private Color fillTint = new(0.85f, 0.18f, 0.18f, 1f);
        [SerializeField] private string labelFormat = "{0}/{1}";
        [SerializeField] private bool hideWhenFull;

        private int _lastValue = int.MinValue;
        private int _lastMinValue = int.MinValue;
        private int _lastMaxValue = int.MinValue;
        private bool _reportedInvalidFormat;
        private bool _reportedInvalidRange;

        private void Awake()
        {
            if (health == null)
                health = GetComponentInParent<Health>();
            if (health == null)
                health = GetComponentInChildren<Health>(true);

            if (worldUI == null)
                worldUI = GetComponentInChildren<DLJ_WorldUIController>(true);

            if (health == null)
                Debug.LogWarning($"{name}: World Health UI에 연결할 Health가 없습니다.", this);
            if (worldUI == null)
                Debug.LogWarning($"{name}: World UI Controller가 없습니다.", this);
        }

        private void OnEnable()
        {
            if (health == null) return;

            health.OnDamage += HandleDamage;
            health.OnRecover += HandleRecover;
            Refresh();
        }

        private void Start()
        {
            // 기물 Setup에서 최대 체력이 뒤늦게 정해질 수 있다.
            Refresh();
        }

        private void LateUpdate()
        {
            if (health == null) return;

            // Health.Init과 직접 Value 대입은 이벤트를 내보내지 않으므로 값 변경을 가볍게 보완 감시한다.
            if (_lastValue != health.Value ||
                _lastMinValue != health.MinValue ||
                _lastMaxValue != health.MaxValue)
            {
                Refresh();
            }
        }

        private void OnDisable()
        {
            if (health == null) return;

            health.OnDamage -= HandleDamage;
            health.OnRecover -= HandleRecover;
        }

        public void Refresh()
        {
            if (health == null || worldUI == null) return;

            int span = health.MaxValue - health.MinValue;
            float ratio;
            if (span > 0)
            {
                ratio = Mathf.Clamp01((float)(health.Value - health.MinValue) / span);
            }
            else
            {
                ratio = health.Value >= health.MaxValue ? 1f : 0f;
                if (!_reportedInvalidRange)
                {
                    _reportedInvalidRange = true;
                    Debug.LogWarning($"{name}: Health의 최대값이 최소값보다 크지 않습니다.", health);
                }
            }

            _lastValue = health.Value;
            _lastMinValue = health.MinValue;
            _lastMaxValue = health.MaxValue;

            if (hideWhenFull && ratio >= 1f)
            {
                worldUI.Hide(slotId);
                return;
            }

            string text = FormatLabel();
            worldUI.Set(
                slotId,
                DLJ_WorldUIData.Progress(ratio, text, icon, iconTint, fillTint));
        }

        private string FormatLabel()
        {
            try
            {
                return string.Format(labelFormat ?? "{0}/{1}", health.Value, health.MaxValue);
            }
            catch (FormatException)
            {
                if (!_reportedInvalidFormat)
                {
                    _reportedInvalidFormat = true;
                    Debug.LogWarning($"{name}: 체력 Label Format이 잘못되어 기본 형식을 사용합니다.", this);
                }

                return $"{health.Value}/{health.MaxValue}";
            }
        }

        private void HandleDamage(DamageResultData data)
        {
            Refresh();
        }

        private void HandleRecover(RecoverResultData data)
        {
            Refresh();
        }
    }
}
