using _Scripts.LDY.Boss.BullKing;
using UnityEditor;
using UnityEngine;

namespace _Scripts.LDY.Editor
{
    /// <summary>
    /// 재생 중에 지금 어떤 수치로 돌진하고 있는지 인스펙터에서 바로 보여준다.
    /// 페이즈 전환이 제대로 먹었는지는 로그를 뒤지는 것보다 이걸 보는 편이 빠르다.
    /// </summary>
    [CustomEditor(typeof(LDY_BullKingBoss))]
    [CanEditMultipleObjects]
    public sealed class LDY_BullKingBossEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            var boss = (LDY_BullKingBoss)target;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromMonoBehaviour(boss),
                    typeof(MonoScript),
                    false);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("현재 상태 (읽기 전용)", EditorStyles.boldLabel);
                EditorGUILayout.IntField("페이즈", boss.Phase);

                LDY_BullChargeRule rule = boss.Rule;
                if (rule != null)
                {
                    EditorGUILayout.IntField("적용 중인 돌진 거리", rule.chargeRange);
                    EditorGUILayout.IntField("적용 중인 충돌 피해", rule.collisionDamage);
                    EditorGUILayout.IntField("적용 중인 벽 충돌 피해", rule.wallDamage);
                    EditorGUILayout.IntField("적용 중인 연쇄 한도", rule.maxChainPush);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("마지막 돌진", EditorStyles.boldLabel);
                EditorGUILayout.IntField("밀어낸 기물", boss.LastPushedCount);
                EditorGUILayout.IntField("죽인 기물", boss.LastKilledCount);
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
    }
}
