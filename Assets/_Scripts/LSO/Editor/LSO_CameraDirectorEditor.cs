using _Scripts.LSO.Camera;
using UnityEditor;
using UnityEngine;

namespace _Scripts.LSO.Editor
{
    /// <summary>
    /// LSO_CameraDirector 인스펙터에 샷을 눌러볼 버튼을 붙인다.
    ///
    /// 카메라 연출은 실제로 그 상황을 만들어야 보인다. 보스 등장 컷을 확인하려고
    /// 매번 보스 노드까지 진행할 수는 없다.
    ///
    /// 연출을 흉내 내지 않고 진짜 Play/Raise/Lower를 부른다.
    /// 여기서 보이는 것이 게임에서 보이는 것과 같아야 하기 때문이다.
    /// </summary>
    [CustomEditor(typeof(LSO_CameraDirector))]
    public class LSO_CameraDirectorEditor : UnityEditor.Editor
    {
        private const string ShotsPath = "shots";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("시험용", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "플레이 중에만 쓸 수 있습니다. 실제로 카메라를 전환해서 연출을 확인합니다.",
                    MessageType.Info);
                return;
            }

            LSO_CameraDirector director = (LSO_CameraDirector)target;

            DrawStatus(director);
            EditorGUILayout.Space(4f);
            DrawShotButtons(director);
            EditorGUILayout.Space(4f);
            DrawBackButton(director);
        }

        private void DrawStatus(LSO_CameraDirector director)
        {
            string current = string.IsNullOrEmpty(director.CurrentId) ? "(없음)" : director.CurrentId;
            string state = director.IsBlending ? "전환 중" : "정지";

            EditorGUILayout.LabelField($"현재: {current}    ({state})");
        }

        /// <summary>
        /// 샷마다 버튼 하나씩. 누르면 그 샷으로 넘어간다.
        /// </summary>
        private void DrawShotButtons(LSO_CameraDirector director)
        {
            SerializedProperty shots = serializedObject.FindProperty(ShotsPath);

            if (shots == null || shots.arraySize == 0)
            {
                EditorGUILayout.HelpBox("등록된 샷이 없습니다.", MessageType.Warning);
                return;
            }

            for (int i = 0; i < shots.arraySize; i++)
            {
                SerializedProperty element = shots.GetArrayElementAtIndex(i);

                string key = ResolveKey(element);
                if (string.IsNullOrEmpty(key)) continue;

                bool isCurrent = key == director.CurrentId;

                // 지금 보고 있는 것은 눌러도 아무 일이 없다. 미리 죽여서 구분되게 한다.
                using (new EditorGUI.DisabledScope(isCurrent))
                {
                    if (GUILayout.Button(isCurrent ? $"▶ {key}" : key))
                        director.Play(key);
                }
            }
        }

        private void DrawBackButton(LSO_CameraDirector director)
        {
            if (GUILayout.Button("직전 샷으로"))
                director.Back();
        }

        /// <summary>
        /// 목록 요소에서 부를 이름을 꺼낸다.
        ///
        /// LSO_CameraShot.Key와 같은 규칙이다 — id가 비어 있으면 카메라 오브젝트 이름을 쓴다.
        /// 직렬화 프로퍼티로 읽는 이유는, 인스펙터에서 방금 고친 값이
        /// 아직 대상 객체에 반영되기 전일 수 있기 때문이다.
        /// </summary>
        private static string ResolveKey(SerializedProperty element)
        {
            string id = element.FindPropertyRelative("id").stringValue;

            if (!string.IsNullOrEmpty(id)) return id;

            Object cam = element.FindPropertyRelative("camera").objectReferenceValue;

            return cam != null ? cam.name : string.Empty;
        }

        /// <summary>
        /// 플레이 중에는 매 프레임 다시 그린다.
        ///
        /// 없으면 카메라가 바뀌어도 위의 "현재"가 그대로 있다가
        /// 마우스를 올려야 갱신된다. 어느 버튼이 죽어 있는지도 어긋난다.
        /// </summary>
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
    }
}
