using System.Collections.Generic;
using UnityEngine;

/// <summary>투자 종류에 따라 여우왕 본체에 짧은 발광 펄스를 재생한다.</summary>
[DisallowMultipleComponent]
public sealed class DLJ_FoxKingInvestmentGlow : MonoBehaviour
{
    private const float PulseDuration = 0.55f;
    private const float GlowIntensity = 0.8f;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private readonly Queue<Color> pendingColors = new();
    private readonly List<MaterialSlot> materialSlots = new();
    private DLJ_FoxKingBoss foxKing;
    private Color activeColor;
    private float elapsed = PulseDuration;

    private void Awake()
    {
        CacheMaterials();
    }

    private void OnEnable()
    {
        foxKing = GetComponent<DLJ_FoxKingBoss>();
        if (foxKing != null)
            foxKing.OnInvestmentMade += HandleInvestment;
    }

    private void OnDisable()
    {
        if (foxKing != null)
            foxKing.OnInvestmentMade -= HandleInvestment;

        pendingColors.Clear();
        elapsed = PulseDuration;
        ApplyGlow(0f);
    }

    private void HandleInvestment(DLJ_FoxKingInvestmentEntry entry)
    {
        Color color = entry.Effect == DLJ_InvestmentEffectType.Heal
            ? new Color(0.08f, 1f, 0.2f)
            : new Color(1f, 0.08f, 0.04f);

        if (elapsed >= PulseDuration)
        {
            activeColor = color;
            elapsed = 0f;
            return;
        }

        pendingColors.Enqueue(color);
    }

    private void Update()
    {
        if (elapsed >= PulseDuration)
        {
            if (pendingColors.Count == 0)
                return;

            activeColor = pendingColors.Dequeue();
            elapsed = 0f;
        }

        elapsed = Mathf.Min(PulseDuration, elapsed + Time.deltaTime);
        float progress = elapsed / PulseDuration;
        ApplyGlow(Mathf.Sin(progress * Mathf.PI));

        if (elapsed >= PulseDuration)
            ApplyGlow(0f);
    }

    private void CacheMaterials()
    {
        foreach (Renderer bodyRenderer in GetComponentsInChildren<Renderer>(true))
        {
            if (bodyRenderer is not MeshRenderer && bodyRenderer is not SkinnedMeshRenderer)
                continue;

            // renderer.materials로 여우왕 인스턴스만의 머티리얼을 만든다.
            // 공유 머티리얼의 emission 키워드를 켜면 다른 기물까지 같이 빛난다.
            Material[] materials = bodyRenderer.materials;
            for (int index = 0; index < materials.Length; index++)
            {
                Material material = materials[index];
                if (material == null || !material.HasProperty(EmissionColorId))
                    continue;

                material.EnableKeyword("_EMISSION");
                materialSlots.Add(new MaterialSlot(material, material.GetColor(EmissionColorId)));
            }
        }
    }

    private void ApplyGlow(float strength)
    {
        foreach (MaterialSlot slot in materialSlots)
            if (slot.Material != null)
                slot.Material.SetColor(
                    EmissionColorId,
                    slot.BaseEmission + activeColor * (GlowIntensity * strength));
    }

    private readonly struct MaterialSlot
    {
        public Material Material { get; }
        public Color BaseEmission { get; }

        public MaterialSlot(Material material, Color baseEmission)
        {
            Material = material;
            BaseEmission = baseEmission;
        }
    }
}
