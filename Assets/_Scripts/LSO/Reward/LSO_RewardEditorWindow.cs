#if UNITY_EDITOR
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Will;
using UnityEditor;
using UnityEngine;
// LSO_CardSO 네임스페이스 추가

namespace _Scripts.LSO.Reward
{
    public class LSO_RewardEditorWindow : EditorWindow
    {
        private LSO_RewardTableSO _target;
        private Vector2 _scroll;
        private GUIStyle _headerStyle;
        private GUIStyle _stageHeaderStyle;

        [MenuItem("LSO/리워드 테이블 편집기")]
        public static void Open()
        {
            var window = GetWindow<LSO_RewardEditorWindow>("리워드 테이블");
            window.minSize = new Vector2(640, 400);
        }

        private void OnEnable()
        {
            if (_target == null)
                _target = FindTableAsset();
        }

        private LSO_RewardTableSO FindTableAsset()
        {
            string[] guids = AssetDatabase.FindAssets("t:LSO_RewardTableSO");
            if (guids.Length == 0) return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<LSO_RewardTableSO>(path);
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                _target = (LSO_RewardTableSO)EditorGUILayout.ObjectField(
                    "대상 (Reward Table)", _target, typeof(LSO_RewardTableSO), false);

                if (GUILayout.Button("에셋 찾기", GUILayout.Width(90)))
                    _target = FindTableAsset();
            }

            if (_target == null)
            {
                EditorGUILayout.HelpBox(
                    "보상 테이블 에셋이 없습니다.\nProject 창에서 Create > KTH > Reward Table 로 만든 뒤 여기에 드래그하세요.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(6);

            EditorGUI.BeginChangeCheck();

            // 카드 장수는 테이블 전체가 하나를 쓴다. 스테이지 줄이 아니라 맨 위에 둔다.
            DrawTableSettings();

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"스테이지 총 {_target.Stages.Count}개", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+ 스테이지 추가", GUILayout.Width(120)))
                {
                    _target.Stages.Add(new LSO_StageRewardData
                    {
                        chapter = 1,
                        stage = 1
                    });
                }
            }

            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            LSO_StageRewardData toDelete = null;

            foreach (var stage in _target.Stages)
                DrawStageRow(stage, ref toDelete);

            EditorGUILayout.EndScrollView();

            if (toDelete != null)
                _target.Stages.Remove(toDelete);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_target);
                AssetDatabase.SaveAssetIfDirty(_target);
            }
        }

        /// <summary>
        /// 테이블 전체에 걸리는 값.
        ///
        /// private [SerializeField]라 SerializedObject로 만진다.
        /// 필드 이름이 바뀌면 조용히 아무 일도 안 하는 대신 이름을 짚어준다.
        /// </summary>
        private void DrawTableSettings()
        {
            var so = new SerializedObject(_target);
            SerializedProperty count = so.FindProperty("cardCount");

            if (count == null)
            {
                EditorGUILayout.HelpBox(
                    "LSO_RewardTableSO에 'cardCount' 필드가 없습니다 — 이 창을 고쳐야 합니다.",
                    MessageType.Error);
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("상자에서 나올 카드 장수", GUILayout.Width(150));

                count.intValue = Mathf.Max(1, EditorGUILayout.IntField(count.intValue, GUILayout.Width(40)));

                EditorGUILayout.LabelField(
                    "모든 스테이지가 같은 값을 쓴다. 그중 하나를 고른다.",
                    EditorStyles.miniLabel);
            }

            so.ApplyModifiedProperties();
        }

        private void InitStyles()
        {
            _headerStyle ??= new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold };

            _stageHeaderStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        }

        private void DrawStageRow(
            LSO_StageRewardData stage,
            ref LSO_StageRewardData toDelete)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    "챕터",
                    GUILayout.Width(40)
                );

                stage.chapter = EditorGUILayout.IntField(
                    stage.chapter,
                    GUILayout.Width(50)
                );

                GUILayout.Space(10);

                EditorGUILayout.LabelField(
                    "스테이지",
                    GUILayout.Width(55)
                );

                stage.stage = EditorGUILayout.IntField(
                    stage.stage,
                    GUILayout.Width(50)
                );

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(
                        "스테이지 삭제",
                        GUILayout.Width(90)))
                {
                    toDelete = stage;
                }
            }

            EditorGUILayout.Space(2);

            // 스테이지가 주는 유언은 하나뿐이라 풀 위에 한 줄로 둔다.
            // 카드 풀 옆에 두면 "카드마다 하나씩"으로 읽힌다.
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    "이 스테이지가 주는 유언", EditorStyles.boldLabel, GUILayout.Width(150));

                stage.stageWill = (DLJ_WillDataSO)EditorGUILayout.ObjectField(
                    stage.stageWill, typeof(DLJ_WillDataSO), false, GUILayout.Width(220));

                EditorGUILayout.LabelField(
                    stage.stageWill != null
                        ? "어느 카드를 골라도 이것 하나가 풀린다"
                        : "비어 있음 — 이 스테이지는 유언을 주지 않는다",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(2);

            DrawPool("카드 풀 (CardSO)", stage.possiblePieces);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private void DrawPool(string poolTitle, List<LSO_RewardPoolEntry> pool)
        {
            // 유언 풀이 없어져 옆에 나란히 놓을 것이 사라졌다. 창 폭을 그대로 쓴다.
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(poolTitle, EditorStyles.boldLabel);

                for (int i = 0; i < pool.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // LSO_CardSO 타입으로 변경
                        pool[i].pieceSO = (LSO_CardSO)EditorGUILayout.ObjectField(
                            pool[i].pieceSO, typeof(LSO_CardSO), false);

                        pool[i].weight = EditorGUILayout.FloatField(pool[i].weight, GUILayout.Width(60));

                        if (GUILayout.Button("-", GUILayout.Width(24)))
                        {
                            pool.RemoveAt(i);
                            break;
                        }
                    }
                }

                if (GUILayout.Button("+ 카드 추가"))
                {
                    pool.Add(new LSO_RewardPoolEntry());
                }
            }
        }

    }
}
#endif