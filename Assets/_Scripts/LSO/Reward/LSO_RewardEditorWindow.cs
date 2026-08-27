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

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"스테이지 총 {_target.Stages.Count}개", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+ 스테이지 추가", GUILayout.Width(120)))
                {
                    _target.Stages.Add(new LSO_StageRewardData
                    {
                        chapter = 1,
                        stage = 1,
                        rewardChoiceCount = 3
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

                GUILayout.Space(10);

                // ⭐ 보상 후보 개수
                EditorGUILayout.LabelField(
                    "보상 선택 수",
                    GUILayout.Width(75)
                );

                stage.rewardChoiceCount = Mathf.Max(
                    1,
                    EditorGUILayout.IntField(
                        stage.rewardChoiceCount,
                        GUILayout.Width(40)
                    )
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

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawPool(
                    "카드 풀 (CardSO)",
                    stage.possiblePieces
                );

                GUILayout.Space(8);

                DrawWillPool(
                    "유언 풀 (WillDataSO)",
                    stage.possibleWills
                );
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private void DrawPool(string poolTitle, List<LSO_RewardPoolEntry> pool)
        {
            float colWidth = (position.width - 60) / 2f;

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(colWidth)))
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

        private void DrawWillPool(string poolTitle, List<LSO_WillRewardPoolEntry> pool)
        {
            float colWidth = (position.width - 60) / 2f;

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(colWidth)))
            {
                EditorGUILayout.LabelField(poolTitle, EditorStyles.boldLabel);

                for (int i = 0; i < pool.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // DLJ_WillDataSO 타입 필터링
                        pool[i].willSO = (DLJ_WillDataSO)EditorGUILayout.ObjectField(
                            pool[i].willSO, typeof(DLJ_WillDataSO), false);

                        pool[i].weight = EditorGUILayout.FloatField(pool[i].weight, GUILayout.Width(60));

                        if (GUILayout.Button("-", GUILayout.Width(24)))
                        {
                            pool.RemoveAt(i);
                            break;
                        }
                    }
                }

                if (GUILayout.Button("+ 유언 추가"))
                {
                    pool.Add(new LSO_WillRewardPoolEntry());
                }
            }
        }
    }
}
#endif