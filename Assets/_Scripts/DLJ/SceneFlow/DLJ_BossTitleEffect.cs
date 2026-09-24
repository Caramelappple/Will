using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.DLJ.SceneFlow
{
    /// <summary>보스 이름 전체의 등장과 연소만 담당한다. 원본 폰트 재질은 변경하지 않는다.</summary>
    public sealed class DLJ_BossTitleEffect : IDisposable
    {
        private static readonly int BurnProgress = Shader.PropertyToID("_BurnProgress");
        private static readonly Color HeatColor = new Color(1f, 0.16f, 0.055f);
        private readonly RectTransform root;
        private readonly CanvasGroup group;
        private readonly TMP_Text[] labels;
        private readonly Color[] colors;
        private readonly List<Material> materials = new List<Material>();
        private readonly Material[][] originalMaterials;
        private readonly Image[] embers = new Image[24];
        private readonly Vector2[] emberOrigins = new Vector2[24];
        private bool hasMaterials;
        private bool warned;

        public DLJ_BossTitleEffect(RectTransform root, CanvasGroup group, params TMP_Text[] labels)
        {
            this.root = root;
            this.group = group;
            this.labels = labels;
            colors = new Color[labels.Length];
            originalMaterials = new Material[labels.Length][];
            for (int i = 0; i < labels.Length; i++) colors[i] = labels[i].color;
            for (int i = 0; i < embers.Length; i++)
            {
                var go = new GameObject("BossEmber_" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                Image ember = go.GetComponent<Image>();
                ember.raycastTarget = false;
                ember.color = Color.clear;
                ember.rectTransform.sizeDelta = new Vector2(1.2f + i % 3 * 0.4f, 2.5f + i % 4);
                ember.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -25f + i % 6 * 10f);
                embers[i] = ember;
            }
        }

        public IEnumerator Reveal(float duration)
        {
            group.alpha = 0f;
            for (int i = 0; i < labels.Length; i++)
            {
                colors[i] = labels[i].color;
                labels[i].ForceMeshUpdate();
            }
            PrepareMaterials();
            duration = Mathf.Max(0.1f, duration);
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                group.alpha = Mathf.SmoothStep(0f, 1f, t);
                root.anchoredPosition = new Vector2(Mathf.Lerp(-24f, 0f, ease), 0f);
                for (int i = 0; i < labels.Length; i++)
                    labels[i].color = Color.Lerp(HeatColor, colors[i], ease);
                yield return null;
            }
            root.anchoredPosition = Vector2.zero;
            group.alpha = 1f;
            for (int i = 0; i < labels.Length; i++) labels[i].color = colors[i];
        }

        public IEnumerator Burn(float duration)
        {
            PrepareEmbers();
            duration = Mathf.Max(0.1f, duration);
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                foreach (Material material in materials) material.SetFloat(BurnProgress, t);
                if (!hasMaterials) group.alpha = 1f - t;
                for (int i = 0; i < embers.Length; i++)
                {
                    // 일정한 시드로 흩어지게 해 게임플레이의 Random 상태를 건드리지 않는다.
                    float start = 0.12f + Mathf.Repeat(i * 0.173f, 0.48f);
                    float age = (t - start) / 0.36f;
                    float alpha = age > 0f && age < 1f ? Mathf.Sin(age * Mathf.PI) : 0f;
                    Vector2 drift = new Vector2(Mathf.Sin(i * 2.4f) * 15f, 18f + i % 5 * 4f);
                    embers[i].rectTransform.anchoredPosition = emberOrigins[i] + drift * Mathf.Clamp01(age);
                    embers[i].color = new Color(1f, Mathf.Lerp(0.65f, 0.12f, Mathf.Clamp01(age)), 0.04f, alpha);
                }
                yield return null;
            }
            group.alpha = 0f;
            Reset();
        }

        private void PrepareMaterials()
        {
            RestoreMaterials();
            Shader shader = Resources.Load<Shader>("DLJ/DLJ_BossTitleBurn");
            if (shader == null || !shader.isSupported)
            {
                if (!warned)
                    Debug.LogWarning("[보스 이름] 연소 셰이더를 사용할 수 없어 페이드로 표시합니다.", root);
                warned = true;
                return;
            }
            for (int i = 0; i < labels.Length; i++)
            {
                // 한글 동적 폰트의 추가 atlas / fallback 재질도 같은 효과로 처리한다.
                Material[] sources = labels[i].fontSharedMaterials;
                // TMP는 반환 배열을 다시 사용하므로 원본 참조 목록을 별도로 보관한다.
                originalMaterials[i] = (Material[])sources.Clone();
                var copies = new Material[sources.Length];
                for (int j = 0; j < sources.Length; j++)
                {
                    copies[j] = new Material(sources[j]) { shader = shader, name = "Boss Title Burn (Runtime)" };
                    copies[j].SetFloat(BurnProgress, 0f);
                    materials.Add(copies[j]);
                }
                labels[i].fontSharedMaterials = copies;
            }
            hasMaterials = materials.Count > 0;
        }

        private void PrepareEmbers()
        {
            var points = new List<Vector2>();
            foreach (TMP_Text label in labels)
            {
                label.ForceMeshUpdate();
                for (int i = 0; i < label.textInfo.characterCount; i++)
                {
                    TMP_CharacterInfo character = label.textInfo.characterInfo[i];
                    if (!character.isVisible) continue;
                    Vector3 center = (character.bottomLeft + character.topRight) * 0.5f;
                    points.Add(root.InverseTransformPoint(label.transform.TransformPoint(center)));
                }
            }
            for (int i = 0; i < emberOrigins.Length; i++)
                emberOrigins[i] = points.Count == 0 ? Vector2.zero :
                    points[i % points.Count] + new Vector2(Mathf.Sin(i * 4.1f) * 6f, Mathf.Cos(i * 2.7f) * 5f);
        }

        public void Reset()
        {
            RestoreMaterials();
            if (root != null) root.anchoredPosition = Vector2.zero;
            for (int i = 0; i < labels.Length; i++)
                if (labels[i] != null) labels[i].color = colors[i];
            foreach (Image ember in embers)
                if (ember != null) ember.color = Color.clear;
        }

        private void RestoreMaterials()
        {
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null && originalMaterials[i] != null)
                    labels[i].fontSharedMaterials = originalMaterials[i];
                originalMaterials[i] = null;
            }
            foreach (Material material in materials) UnityEngine.Object.Destroy(material);
            materials.Clear();
            hasMaterials = false;
        }

        public void Dispose()
        {
            Reset();
            foreach (Image ember in embers)
                if (ember != null) UnityEngine.Object.Destroy(ember.gameObject);
        }
    }
}
