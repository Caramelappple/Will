using System;
using System.Collections.Generic;
using _Scripts.LSO.Attributes;
using UnityEditor;
using UnityEngine;

namespace _Scripts.LSO.Editor
{
    /// <summary>
    /// [SerializeReference] 필드에 구현 타입을 고르는 드롭다운을 그린다.
    ///
    /// 유니티는 managed reference를 직렬화만 해주고 타입을 고를 UI는 주지 않는다.
    /// 그래서 LDY_ScorerRegistry의 scorers 목록이 요소를 추가해도 계속 null로 남아 있었다.
    ///
    /// 값이 바뀔 때 Activator로 새 인스턴스를 만든다.
    /// 인자 없는 생성자가 필요한 이유가 이것이다.
    /// </summary>
    [CustomPropertyDrawer(typeof(LSO_SubclassPickerAttribute))]
    public class LSO_SubclassPickerDrawer : PropertyDrawer
    {
        private const string NullLabel = "선택 안 됨";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // managed reference가 아니면 이 드로어가 할 일이 없다.
            // [SerializeReference]를 빠뜨렸을 때 조용히 사라지지 않도록 기본 그리기로 넘긴다.
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            // 먼저 원래대로 그리고, 그 위에 드롭다운을 얹는다.
            // 나중에 그린 쪽이 클릭을 먼저 가져가므로 폴드아웃과 겹치지 않는다.
            EditorGUI.PropertyField(position, property, label, true);

            Rect dropdown = new Rect(
                position.x + EditorGUIUtility.labelWidth + 2f,
                position.y,
                Mathf.Max(60f, position.width - EditorGUIUtility.labelWidth - 2f),
                EditorGUIUtility.singleLineHeight);

            if (EditorGUI.DropdownButton(dropdown, new GUIContent(CurrentTypeName(property)), FocusType.Keyboard))
                ShowMenu(property);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private static string CurrentTypeName(SerializedProperty property)
        {
            return ShortName(property.managedReferenceFullTypename) ?? NullLabel;
        }

        private void ShowMenu(SerializedProperty property)
        {
            Type baseType = FieldType(property);
            if (baseType == null)
            {
                Debug.LogWarning($"{fieldInfo?.Name}: 필드 타입을 알아내지 못해 후보를 찾을 수 없습니다.");
                return;
            }

            // 메뉴 콜백은 OnGUI가 끝난 뒤에 불린다.
            // SerializedProperty는 그때까지 못 버티므로 경로만 들고 있다가 다시 찾는다.
            SerializedObject owner = property.serializedObject;
            string path = property.propertyPath;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(NullLabel), property.managedReferenceValue == null,
                () => Assign(owner, path, null));
            menu.AddSeparator(string.Empty);

            List<Type> candidates = Candidates(baseType);
            if (candidates.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent($"{baseType.Name} 구현이 없습니다"));
            }
            else
            {
                string current = property.managedReferenceFullTypename;

                foreach (Type type in candidates)
                {
                    // 네임스페이스를 하위 메뉴로 묶어서 LDY / LSO 것을 구분해 본다.
                    string entry = string.IsNullOrEmpty(type.Namespace)
                        ? type.Name
                        : $"{type.Namespace.Replace('.', '/')}/{type.Name}";

                    bool on = ShortName(current) == type.Name;
                    Type captured = type;

                    menu.AddItem(new GUIContent(entry), on, () => Assign(owner, path, captured));
                }
            }

            menu.ShowAsContext();
        }

        private static void Assign(SerializedObject owner, string path, Type type)
        {
            if (owner == null || owner.targetObject == null) return;

            owner.Update();

            SerializedProperty property = owner.FindProperty(path);
            if (property == null) return;

            property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);

            // Undo 기록과 에셋 더티 표시가 여기서 함께 일어난다.
            owner.ApplyModifiedProperties();
        }

        /// <summary>
        /// 이 필드가 담을 수 있는 기준 타입.
        /// managedReferenceFieldTypename은 "어셈블리이름 네임스페이스.타입" 형태로 온다.
        /// </summary>
        private static Type FieldType(SerializedProperty property)
        {
            string full = property.managedReferenceFieldTypename;
            if (string.IsNullOrEmpty(full)) return null;

            string[] parts = full.Split(' ');
            if (parts.Length != 2) return null;

            return Type.GetType($"{parts[1]}, {parts[0]}");
        }

        /// <summary>드롭다운에 올릴 수 있는 구현만 고른다.</summary>
        private static List<Type> Candidates(Type baseType)
        {
            var result = new List<Type>();

            foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType))
            {
                if (type.IsAbstract || type.IsInterface) continue;
                if (type.IsGenericTypeDefinition) continue;

                // [Serializable]이 없으면 유니티가 저장하지 못한다.
                if (!type.IsSerializable) continue;

                // ScriptableObject·MonoBehaviour는 managed reference로 담을 수 없다.
                if (typeof(UnityEngine.Object).IsAssignableFrom(type)) continue;

                // Activator가 만들 수 있어야 한다.
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                result.Add(type);
            }

            result.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
            return result;
        }

        /// <summary>"어셈블리 네임스페이스.타입"에서 타입 이름만 뽑는다. 비어 있으면 null.</summary>
        private static string ShortName(string managedTypename)
        {
            if (string.IsNullOrEmpty(managedTypename)) return null;

            int space = managedTypename.IndexOf(' ');
            string qualified = space >= 0 ? managedTypename.Substring(space + 1) : managedTypename;

            int dot = qualified.LastIndexOf('.');
            return dot >= 0 ? qualified.Substring(dot + 1) : qualified;
        }
    }
}
