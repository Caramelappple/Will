using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LDY_MapManager))]
public class LDY_MapManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("별자리 맵 에디터 열기", GUILayout.Height(28)))
        {
            LDY_ConstellationMapEditorWindow.Open((LDY_MapManager)target);
        }
    }
}
