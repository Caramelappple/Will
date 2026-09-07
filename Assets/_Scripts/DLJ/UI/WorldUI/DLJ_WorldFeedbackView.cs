using TMPro;
using UnityEngine;

namespace _Scripts.DLJ.UI.WorldUI
{
    /// <summary>재사용하는 아이콘 + 숫자 한 줄. 기물 상태와 이벤트는 알지 못한다.</summary>
    internal sealed class DLJ_WorldFeedbackView
    {
        private readonly GameObject _root;
        private readonly SpriteRenderer _icon;
        private readonly TextMeshPro _label;
        private readonly Vector3 _origin;
        private Color _color;
        private float _elapsed;
        private float _duration;

        public bool IsActive => _root != null && _root.activeSelf;

        public DLJ_WorldFeedbackView(Transform parent, int lane, float spacing,
            TMP_FontAsset font, float fontSize, int sortingOrder)
        {
            _root = new GameObject($"Feedback_{lane + 1}");
            _root.transform.SetParent(parent, false);
            _origin = Vector3.up * (lane * spacing);

            var iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(_root.transform, false);
            _icon = iconObject.AddComponent<SpriteRenderer>();
            _icon.sortingOrder = sortingOrder;

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(_root.transform, false);
            _label = textObject.AddComponent<TextMeshPro>();
            _label.font = font;
            _label.fontSize = fontSize;
            _label.alignment = TextAlignmentOptions.MidlineLeft;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.rectTransform.pivot = new Vector2(0f, 0.5f);
            _label.rectTransform.sizeDelta = new Vector2(12f, 3f);
            _label.transform.localScale = Vector3.one * 0.1f;
            _label.renderer.sortingOrder = sortingOrder + 1;
            Hide();
        }

        public void Show(Sprite icon, string text, Color color, float duration, float iconSize)
        {
            _elapsed = 0f;
            _duration = duration;
            _color = color;
            _root.transform.localPosition = _origin;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _icon.color = Color.white;
            if (icon != null)
            {
                Vector3 size = icon.bounds.size;
                float longest = Mathf.Max(size.x, size.y);
                _icon.transform.localScale = Vector3.one *
                    (longest > 0f ? iconSize / longest : 1f);
            }

            _label.transform.localPosition = new Vector3(
                icon != null ? iconSize * 0.5f + 0.06f : 0f, 0f, -0.001f);
            _label.text = text;
            _label.color = color;
            _root.SetActive(true);
        }

        public void Tick(float deltaTime, float riseDistance)
        {
            if (!IsActive) return;
            _elapsed += deltaTime;
            if (_elapsed >= _duration)
            {
                Hide();
                return;
            }

            float progress = Mathf.Clamp01(_elapsed / _duration);
            float alpha = 1f - Mathf.InverseLerp(0.65f, 1f, progress);
            _root.transform.localPosition = _origin + Vector3.up * (progress * riseDistance);
            _icon.color = new Color(1f, 1f, 1f, alpha);
            _label.color = new Color(_color.r, _color.g, _color.b, _color.a * alpha);
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }
    }
}
