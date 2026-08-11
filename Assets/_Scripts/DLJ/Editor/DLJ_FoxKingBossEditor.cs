using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DLJ_FoxKingBoss))]
[CanEditMultipleObjects]
public sealed class DLJ_FoxKingBossEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DLJ_FoxKingBoss boss = (DLJ_FoxKingBoss)target;

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Script",
                MonoScript.FromMonoBehaviour(boss),
                typeof(MonoScript),
                false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Boss Resources (Read Only)", EditorStyles.boldLabel);
            EditorGUILayout.IntField("Stolen Resources", boss.StolenResources);
            EditorGUILayout.IntField("Greed", boss.Greed);
        }
    }

    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying;
    }
}
