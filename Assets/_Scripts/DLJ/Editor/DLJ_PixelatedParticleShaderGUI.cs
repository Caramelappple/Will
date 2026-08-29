using UnityEditor;
using UnityEngine;

public sealed class DLJ_PixelatedParticleShaderGUI : ShaderGUI
{
    private const int MinColorCount = 2;
    private const int MaxColorCount = 8;

    private static bool showPalette = true;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        MaterialProperty colorCount = FindProperty("_DitherColorCount", properties, false);
        MaterialProperty[] palette = FindPaletteProperties(properties);

        if (colorCount == null || palette == null)
        {
            base.OnGUI(materialEditor, properties);
            return;
        }

        foreach (MaterialProperty property in properties)
        {
            if ((property.propertyFlags & UnityEngine.Rendering.ShaderPropertyFlags.HideInInspector) != 0)
                continue;

            materialEditor.ShaderProperty(property, property.displayName);

            if (property.name == "_DitherScale")
                DrawPalette(materialEditor, colorCount, palette);
        }

        EditorGUILayout.Space();
        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
        materialEditor.DoubleSidedGIField();
    }

    private static MaterialProperty[] FindPaletteProperties(MaterialProperty[] properties)
    {
        MaterialProperty[] palette = new MaterialProperty[MaxColorCount];

        for (int index = 0; index < MaxColorCount; index++)
        {
            palette[index] = FindProperty($"_DitherColor{index}", properties, false);
            if (palette[index] == null)
                return null;
        }

        return palette;
    }

    private static void DrawPalette(
        MaterialEditor materialEditor,
        MaterialProperty colorCountProperty,
        MaterialProperty[] palette)
    {
        int colorCount = Mathf.Clamp(
            Mathf.RoundToInt(colorCountProperty.floatValue),
            MinColorCount,
            MaxColorCount);

        EditorGUILayout.Space(3);
        showPalette = EditorGUILayout.Foldout(
            showPalette,
            $"Dither Color Layers ({colorCount})",
            true);

        if (!showPalette)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.HelpBox(
            "Order colors from darkest to brightest. Bayer dithering interleaves each adjacent pair.",
            MessageType.None);

        for (int index = 0; index < colorCount; index++)
        {
            string suffix = index switch
            {
                0 => " (Darkest)",
                _ when index == colorCount - 1 => " (Brightest)",
                _ => string.Empty
            };

            materialEditor.ColorProperty(palette[index], $"Layer {index + 1}{suffix}");
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(colorCount <= MinColorCount);
        if (GUILayout.Button("Remove Layer", GUILayout.Width(100)))
        {
            materialEditor.RegisterPropertyChangeUndo("Remove dither color layer");
            colorCountProperty.floatValue = colorCount - 1;
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(colorCount >= MaxColorCount);
        if (GUILayout.Button("Add Layer", GUILayout.Width(100)))
        {
            materialEditor.RegisterPropertyChangeUndo("Add dither color layer");

            Color previousColor = palette[colorCount - 1].colorValue;
            palette[colorCount].colorValue = Color.Lerp(previousColor, Color.white, 0.35f);
            colorCountProperty.floatValue = colorCount + 1;
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
        EditorGUI.indentLevel--;
    }
}
