using _Scripts.DLJ.UI.WorldUI;
using _Scripts.LSO.HealthSystem;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>선택한 기물에 SpriteRenderer 기반 월드 UI를 생성한다.</summary>
public static class DLJ_WorldUIBuilder
{
    private const string MenuPath = "GameObject/DLJ/Add World UI to Piece";
    private const int BackgroundOrder = 100;
    private const int FillOrder = 101;
    private const int IconOrder = 102;
    private const int TextOrder = 103;

    private static Sprite _defaultSprite;

    [MenuItem(MenuPath, false, 20)]
    private static void AddToSelectedPieces()
    {
        foreach (Transform selected in Selection.transforms)
        {
            if (selected == null || EditorUtility.IsPersistent(selected.gameObject))
                continue;

            if (selected.GetComponent<Health>() == null)
            {
                Debug.LogWarning(
                    $"{selected.name}: 루트에 Health가 없어 기물로 판단할 수 없습니다.",
                    selected);
                continue;
            }

            if (HasDirectWorldUI(selected))
            {
                Debug.LogWarning($"{selected.name}: 이미 DLJ World UI가 있어 생성을 건너뜁니다.", selected);
                continue;
            }

            GameObject uiRoot = CreateWorldUI(selected);
            AddBinders(selected.gameObject);
            uiRoot.GetComponent<DLJ_WorldUIController>().RefreshSlotCache();

            EditorUtility.SetDirty(selected.gameObject);
            Selection.activeGameObject = uiRoot;
        }
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateAddToSelectedPieces()
    {
        return Selection.transforms != null && Selection.transforms.Length > 0;
    }

    private static GameObject CreateWorldUI(Transform piece)
    {
        GameObject root = CreateWorldObject("DLJ_WorldUI", piece);
        DLJ_WorldUIController controller = Undo.AddComponent<DLJ_WorldUIController>(root);
        DLJ_WorldUIAnchor anchor = Undo.AddComponent<DLJ_WorldUIAnchor>(root);
        Undo.AddComponent<DLJ_WorldUIBillboard>(root);

        GameObject visualRoot = CreateWorldObject("VisualRoot", root.transform);
        anchor.Configure(piece, visualRoot, new Vector3(0f, 1.5f, 0f), 1f);

        CreateHealthSlot(visualRoot.transform, 0.45f);
        CreateStackSlot(visualRoot.transform, "StatusSlot", DLJ_WorldUISlotId.Status, 0.15f);
        CreateStackSlot(visualRoot.transform, "GreedSlot", DLJ_WorldUISlotId.Greed, -0.15f);
        CreateResourceSlot(visualRoot.transform, -0.45f);

        controller.RefreshSlotCache();
        return root;
    }

    private static void CreateHealthSlot(Transform parent, float y)
    {
        SlotObjects slot = CreateSlotBase(parent, "HealthSlot", y);

        SpriteRenderer icon = CreateSpriteRenderer(
            "Icon", slot.Content.transform, new Vector3(-0.65f, 0f, 0f), Vector2.one,
            Color.white, IconOrder, null);

        SpriteRenderer background = CreateSpriteRenderer(
            "FillBackground", slot.Content.transform, new Vector3(-0.08f, 0f, 0f),
            new Vector2(0.82f, 0.12f), new Color(0.1f, 0.1f, 0.1f, 0.92f),
            BackgroundOrder, GetDefaultSprite());

        SpriteRenderer fill = CreateSpriteRenderer(
            "Fill", slot.Content.transform, new Vector3(-0.08f, 0f, -0.001f),
            new Vector2(0.78f, 0.08f), new Color(0.85f, 0.18f, 0.18f, 1f),
            FillOrder, GetDefaultSprite());

        TMP_Text label = CreateLabel(
            "Label", slot.Content.transform, new Vector3(0.58f, 0f, -0.002f), "0/0", 0.42f);

        slot.Slot.Configure(
            DLJ_WorldUISlotId.Health,
            slot.Content,
            label,
            icon,
            background,
            fill);

        slot.Content.SetActive(false);
    }

    private static void CreateStackSlot(
        Transform parent,
        string slotName,
        DLJ_WorldUISlotId slotId,
        float y)
    {
        SlotObjects slot = CreateSlotBase(parent, slotName, y);
        GameObject stackRoot = CreateWorldObject("StackContainer", slot.Content.transform);
        stackRoot.transform.localPosition = new Vector3(-0.12f, 0f, 0f);

        SpriteRenderer template = CreateSpriteRenderer(
            "StackTemplate", stackRoot.transform, Vector3.zero, Vector2.one,
            new Color(1f, 0.85f, 0.25f, 1f), IconOrder, null);
        template.gameObject.SetActive(false);

        TMP_Text label = CreateLabel(
            "Label", slot.Content.transform, new Vector3(0.78f, 0f, -0.002f),
            string.Empty, 0.48f);

        slot.Slot.Configure(
            slotId,
            slot.Content,
            label,
            stacksRoot: stackRoot.transform,
            stacksTemplate: template);

        slot.Content.SetActive(false);
    }

    private static void CreateResourceSlot(Transform parent, float y)
    {
        SlotObjects slot = CreateSlotBase(parent, "ResourceSlot", y);
        SpriteRenderer icon = CreateSpriteRenderer(
            "Icon", slot.Content.transform, new Vector3(-0.18f, 0f, 0f), Vector2.one,
            Color.white, IconOrder, null);
        TMP_Text label = CreateLabel(
            "Label", slot.Content.transform, new Vector3(0.2f, 0f, -0.002f), "0", 0.5f);

        slot.Slot.Configure(DLJ_WorldUISlotId.Resource, slot.Content, label, icon);
        slot.Content.SetActive(false);
    }

    private static SlotObjects CreateSlotBase(Transform parent, string name, float y)
    {
        GameObject slotObject = CreateWorldObject(name, parent);
        slotObject.transform.localPosition = new Vector3(0f, y, 0f);
        DLJ_WorldUISlot slot = Undo.AddComponent<DLJ_WorldUISlot>(slotObject);

        GameObject content = CreateWorldObject("Content", slotObject.transform);
        return new SlotObjects(slot, content);
    }

    private static SpriteRenderer CreateSpriteRenderer(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector2 localSize,
        Color color,
        int sortingOrder,
        Sprite sprite)
    {
        GameObject spriteObject = CreateWorldObject(name, parent);
        spriteObject.transform.localPosition = localPosition;
        spriteObject.transform.localScale = new Vector3(localSize.x, localSize.y, 1f);

        SpriteRenderer renderer = Undo.AddComponent<SpriteRenderer>(spriteObject);
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static TMP_Text CreateLabel(
        string name,
        Transform parent,
        Vector3 localPosition,
        string text,
        float worldWidth)
    {
        GameObject labelObject = new(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(labelObject, "Add DLJ World UI");
        Undo.SetTransformParent(labelObject.transform, parent, "Parent DLJ World UI");

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.localPosition = localPosition;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * 0.01f;
        rect.sizeDelta = new Vector2(worldWidth * 100f, 24f);

        TextMeshPro label = Undo.AddComponent<TextMeshPro>(labelObject);
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.text = text;
        label.renderer.sortingOrder = TextOrder;
        return label;
    }

    private static GameObject CreateWorldObject(string name, Transform parent)
    {
        GameObject gameObject = new(name);
        Undo.RegisterCreatedObjectUndo(gameObject, "Add DLJ World UI");
        Undo.SetTransformParent(gameObject.transform, parent, "Parent DLJ World UI");
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.identity;
        gameObject.transform.localScale = Vector3.one;
        return gameObject;
    }

    private static Sprite GetDefaultSprite()
    {
        if (_defaultSprite == null)
            _defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        return _defaultSprite;
    }

    private static void AddBinders(GameObject piece)
    {
        if (piece.GetComponent<Health>() != null &&
            piece.GetComponent<DLJ_WorldHealthUIBinder>() == null)
        {
            Undo.AddComponent<DLJ_WorldHealthUIBinder>(piece);
        }

        if (piece.GetComponent<global::DLJ_FoxKingBoss>() != null &&
            piece.GetComponent<DLJ_FoxKingWorldUIBinder>() == null)
        {
            Undo.AddComponent<DLJ_FoxKingWorldUIBinder>(piece);
        }
    }

    private static bool HasDirectWorldUI(Transform piece)
    {
        for (int i = 0; i < piece.childCount; i++)
        {
            if (piece.GetChild(i).GetComponent<DLJ_WorldUIController>() != null)
                return true;
        }

        return false;
    }

    private readonly struct SlotObjects
    {
        public readonly DLJ_WorldUISlot Slot;
        public readonly GameObject Content;

        public SlotObjects(DLJ_WorldUISlot slot, GameObject content)
        {
            Slot = slot;
            Content = content;
        }
    }
}
