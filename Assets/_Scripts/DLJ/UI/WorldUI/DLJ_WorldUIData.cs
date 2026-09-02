using System;
using UnityEngine;

namespace _Scripts.DLJ.UI.WorldUI
{
    /// <summary>기물 위 UI 슬롯이 한 번에 그릴 데이터.</summary>
    public readonly struct DLJ_WorldUIData : IEquatable<DLJ_WorldUIData>
    {
        public enum ContentType
        {
            Text,
            Progress,
            Stacks
        }

        public bool Visible { get; }
        public ContentType Content { get; }
        public string Text { get; }
        public Sprite Icon { get; }
        public Color Tint { get; }
        public float FillAmount { get; }
        public bool OverrideFillTint { get; }
        public Color FillTint { get; }
        public int StackCount { get; }
        public int StackCapacity { get; }
        public Color InactiveTint { get; }

        private DLJ_WorldUIData(
            bool visible,
            ContentType content,
            string text,
            Sprite icon,
            Color tint,
            float fillAmount,
            bool overrideFillTint,
            Color fillTint,
            int stackCount,
            int stackCapacity,
            Color inactiveTint)
        {
            Visible = visible;
            Content = content;
            Text = text;
            Icon = icon;
            Tint = tint;
            FillAmount = Mathf.Clamp01(fillAmount);
            OverrideFillTint = overrideFillTint;
            FillTint = fillTint;
            StackCount = Mathf.Max(0, stackCount);
            StackCapacity = Mathf.Max(StackCount, stackCapacity);
            InactiveTint = inactiveTint;
        }

        /// <summary>아이콘과 숫자/문자열. 수탈 자원 같은 단일 값을 표시할 때 쓴다.</summary>
        public static DLJ_WorldUIData TextValue(string text, Sprite icon = null, Color? tint = null)
        {
            return new DLJ_WorldUIData(
                !string.IsNullOrEmpty(text) || icon != null,
                ContentType.Text,
                text ?? string.Empty,
                icon,
                tint ?? Color.white,
                0f,
                false,
                Color.clear,
                0,
                0,
                Color.clear);
        }

        /// <summary>0~1 게이지와 문자열. 체력처럼 현재/최대 값이 있는 항목에 쓴다.</summary>
        public static DLJ_WorldUIData Progress(
            float fillAmount,
            string text = null,
            Sprite icon = null,
            Color? tint = null,
            Color? fillTint = null)
        {
            return new DLJ_WorldUIData(
                true,
                ContentType.Progress,
                text ?? string.Empty,
                icon,
                tint ?? Color.white,
                fillAmount,
                fillTint.HasValue,
                fillTint ?? Color.clear,
                0,
                0,
                Color.clear);
        }

        /// <summary>같은 아이콘을 여러 개 나열한다. 별, 중첩 상태 등에 쓴다.</summary>
        public static DLJ_WorldUIData Stacks(
            int count,
            Sprite icon,
            Color? tint = null,
            string text = null,
            int capacity = 0,
            Color? inactiveTint = null)
        {
            return new DLJ_WorldUIData(
                (count > 0 || capacity > 0) && icon != null,
                ContentType.Stacks,
                text ?? string.Empty,
                icon,
                tint ?? Color.white,
                0f,
                false,
                Color.clear,
                count,
                capacity,
                inactiveTint ?? new Color(0.2f, 0.2f, 0.2f, 1f));
        }

        public static DLJ_WorldUIData Hidden()
        {
            return new DLJ_WorldUIData(
                false,
                ContentType.Text,
                string.Empty,
                null,
                Color.white,
                0f,
                false,
                Color.clear,
                0,
                0,
                Color.clear);
        }

        public bool Equals(DLJ_WorldUIData other)
        {
            return Visible == other.Visible &&
                   Content == other.Content &&
                   Text == other.Text &&
                   Icon == other.Icon &&
                   Tint.Equals(other.Tint) &&
                   FillAmount.Equals(other.FillAmount) &&
                   OverrideFillTint == other.OverrideFillTint &&
                   FillTint.Equals(other.FillTint) &&
                   StackCount == other.StackCount &&
                   StackCapacity == other.StackCapacity &&
                   InactiveTint.Equals(other.InactiveTint);
        }

        public override bool Equals(object obj)
        {
            return obj is DLJ_WorldUIData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Visible ? 1 : 0;
                hash = (hash * 397) ^ (int)Content;
                hash = (hash * 397) ^ (Text != null ? Text.GetHashCode() : 0);
                hash = (hash * 397) ^ (Icon != null ? Icon.GetHashCode() : 0);
                hash = (hash * 397) ^ Tint.GetHashCode();
                hash = (hash * 397) ^ FillAmount.GetHashCode();
                hash = (hash * 397) ^ (OverrideFillTint ? 1 : 0);
                hash = (hash * 397) ^ FillTint.GetHashCode();
                hash = (hash * 397) ^ StackCount;
                hash = (hash * 397) ^ StackCapacity;
                hash = (hash * 397) ^ InactiveTint.GetHashCode();
                return hash;
            }
        }
    }
}
