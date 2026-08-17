using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LDY_MapManager))]
public class LDY_MapManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        LDY_MapManager mapManager = (LDY_MapManager)target;

        if (GUILayout.Button("별자리 맵 에디터 열기", GUILayout.Height(28)))
        {
            LDY_ConstellationMapEditorWindow.Open(mapManager);
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("진행도 1-1로 리셋", GUILayout.Height(28)))
        {
            bool confirm = EditorUtility.DisplayDialog(
                "진행도 초기화",
                "현재 플레이 진행도를 전부 1-1로 초기화하시겠습니까?\n\n" +
                "별자리 맵 데이터는 삭제되지 않습니다.",
                "초기화",
                "취소"
            );

            if (confirm)
            {
                mapManager.ResetProgress();

                EditorUtility.SetDirty(mapManager);
                AssetDatabase.SaveAssets();
            }
        }
    }
}