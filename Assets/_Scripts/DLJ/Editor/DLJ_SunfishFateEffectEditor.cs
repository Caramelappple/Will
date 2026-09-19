using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DLJ_SunfishFateEffect))]
public sealed class DLJ_SunfishFateEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var effect = (DLJ_SunfishFateEffect)target;
        EditorGUILayout.HelpBox(
            "내부 배치는 Layout Prefab을 열어서 편집해. Canvas 아래 Background, Top Wave, " +
            "Bottom Wave, Left Arrow, Right Arrow, Text Group의 RectTransform을 움직이면 돼. " +
            "Text Group을 움직이면 글자와 그림자가 함께 이동해. 색상은 각 UI의 Color에서 변경해.",
            MessageType.Info);
        using (new EditorGUI.DisabledScope(effect.LayoutPrefab == null || Application.isPlaying))
        {
            if (GUILayout.Button("내부 배치 프리팹 열기"))
                AssetDatabase.OpenAsset(effect.LayoutPrefab.gameObject);
            if (GUILayout.Button("저장한 배치로 미리보기 새로고침"))
            {
                effect.RefreshLayoutPreview();
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
        }
    }
}

// 레이아웃 저장 후 씬에 떠 있는 임시 미리보기만 교체한다. 씬/에셋 설정은 수정하지 않는다.
internal sealed class DLJ_SunfishLayoutRefresh : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
        string[] moved, string[] movedFrom)
    {
        if (Application.isPlaying) return;
        foreach (string path in imported)
        {
            if (!path.EndsWith(".prefab")) continue;
            string savedPath = path;
            EditorApplication.delayCall += () => Refresh(savedPath);
        }
    }

    private static void Refresh(string path)
    {
        if (Application.isPlaying) return;
        foreach (var effect in Resources.FindObjectsOfTypeAll<DLJ_SunfishFateEffect>())
        {
            if (!effect.isActiveAndEnabled || EditorUtility.IsPersistent(effect) ||
                effect.LayoutPrefab == null || !effect.gameObject.scene.IsValid()) continue;
            if (AssetDatabase.GetAssetPath(effect.LayoutPrefab) == path)
                effect.RefreshLayoutPreview();
        }
        EditorApplication.QueuePlayerLoopUpdate();
    }
}
