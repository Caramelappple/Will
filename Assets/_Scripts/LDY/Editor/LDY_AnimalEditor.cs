using System.Collections.Generic;
using _Scripts.LSO.Ability;
using UnityEditor;
using UnityEngine;

namespace _Scripts.LDY.Editor
{
    /// <summary>
    /// LDY_Animal 인스펙터에 연결된 LSO_AnimalSO의 값을 함께 보여준다.
    /// 값을 복사해두는 것이 아니라 SO를 그대로 읽어 표시하므로, 원본은 항상 AnimalSO 하나뿐이다.
    /// </summary>
    [CustomEditor(typeof(LDY_Animal))]
    public class LDY_AnimalEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LDY_Animal animal = target as LDY_Animal;
            if (animal == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AnimalSO에서 가져오는 값 (읽기 전용)", EditorStyles.boldLabel);

            if (animal.data == null)
            {
                EditorGUILayout.HelpBox(
                    "data(AnimalSO)가 비어 있습니다.\n" +
                    "특성·체력이 적용되지 않고, 사거리는 아래 Test Only 값이 대신 쓰입니다.",
                    MessageType.Warning);
                return;
            }

            DrawAnimalData(animal);
            DrawRuntimeAbilities(animal);
        }

        private static void DrawAnimalData(LDY_Animal animal)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("이름", animal.data.animalName);
                EditorGUILayout.TextField("특성", DescribeAbilities(animal));
                EditorGUILayout.EnumPopup("사거리", animal.RangeType);
                EditorGUILayout.IntField("최대 체력", animal.data.maxHealth);
                EditorGUILayout.IntField("기본 공격력", animal.data.damage);
                EditorGUILayout.IntField("코스트", animal.data.cost);
            }

            if (!string.IsNullOrEmpty(animal.data.description))
                EditorGUILayout.HelpBox(animal.data.description, MessageType.None);
        }

        // 특성은 여러 개일 수 있어 EnumPopup으로는 다 보여줄 수 없다.
        // 어차피 읽기 전용 표시라 한 줄로 이어 붙인다.
        private static string DescribeAbilities(LDY_Animal animal)
        {
            IReadOnlyList<LSO_AbilityType> types = animal.AbilityTypes;
            if (types == null || types.Count == 0) return "없음";

            return string.Join(", ", types);
        }

        // 팩토리가 만들어낸 실제 특성 인스턴스는 재생 중에만 존재한다.
        private static void DrawRuntimeAbilities(LDY_Animal animal)
        {
            if (!Application.isPlaying) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("실행 중인 특성 인스턴스", EditorStyles.boldLabel);

            if (animal.Abilities == null || animal.Abilities.Count == 0)
            {
                EditorGUILayout.LabelField("없음");
                return;
            }

            foreach (var ability in animal.Abilities)
                EditorGUILayout.LabelField("· " + ability.GetType().Name);
        }
    }
}
