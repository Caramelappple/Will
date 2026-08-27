using UnityEditor;
using UnityEngine;
using _Scripts.LSO.Reward;

[CustomEditor(typeof(DLJ_FoxKingBoss))]
[CanEditMultipleObjects]
public sealed class DLJ_FoxKingBossEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        serializedObject.ApplyModifiedProperties();

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
            EditorGUILayout.IntField("Phase", boss.Phase);
            EditorGUILayout.IntField("Stolen Resources", boss.StolenResources);
            EditorGUILayout.IntField("Greed", boss.Greed);
            EditorGUILayout.IntField("Pending Attack Bonus", boss.PendingAttackBonus);
        }
    }

    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying;
    }
}
