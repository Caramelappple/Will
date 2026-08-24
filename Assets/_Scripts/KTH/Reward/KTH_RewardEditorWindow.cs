#if UNITY_EDITOR
using _Scripts.LSO;
using _Scripts.LSO.Deck.Data; // LSO_CardSO 네임스페이스 추가
using _Scripts.LSO.Will;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class KTH_RewardEditorWindow : EditorWindow
{
    private KTH_RewardTableSO target;
    private Vector2 scroll;
    private GUIStyle headerStyle;
    private GUIStyle stageHeaderStyle;

    [MenuItem("KTH/리워드 테이블 편집기")]
    public static void Open()
    {
        var window = GetWindow<KTH_RewardEditorWindow>("리워드 테이블");
        window.minSize = new Vector2(640, 400);
    }

    private void OnEnable()
    {
        if (target == null)
            target = FindTableAsset();
    }

    private KTH_RewardTableSO FindTableAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:KTH_RewardTableSO");
        if (guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<KTH_RewardTableSO>(path);
    }

    private void OnGUI()
    {
        InitStyles();

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            target = (KTH_RewardTableSO)EditorGUILayout.ObjectField(
                "대상 (Reward Table)", target, typeof(KTH_RewardTableSO), false);

            if (GUILayout.Button("에셋 찾기", GUILayout.Width(90)))
                target = FindTableAsset();
        }

        if (target == null)
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
            GUILayout.Label($"스테이지 총 {target.Stages.Count}개", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 스테이지 추가", GUILayout.Width(120)))
            {
                target.Stages.Add(new KTH_StageRewardData
                {
                    chapter = 1,
                    stage = 1,
                    rewardChoiceCount = 3
                });
            }
        }

        EditorGUILayout.Space(4);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        KTH_StageRewardData toDelete = null;

        foreach (var stage in target.Stages)
            DrawStageRow(stage, ref toDelete);

        EditorGUILayout.EndScrollView();

        if (toDelete != null)
            target.Stages.Remove(toDelete);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }
    }

    private void InitStyles()
    {
        if (headerStyle == null)
            headerStyle = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold };

        if (stageHeaderStyle == null)
            stageHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
    }

    private void DrawStageRow(
    KTH_StageRewardData stage,
    ref KTH_StageRewardData toDelete)
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

    private void DrawPool(string title, List<KTH_RewardPoolEntry> pool)
    {
        float colWidth = (position.width - 60) / 2f;

        using (new EditorGUILayout.VerticalScope(GUILayout.Width(colWidth)))
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

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
                pool.Add(new KTH_RewardPoolEntry());
            }
        }
    }

    private void DrawWillPool(string title, List<KTH_WillRewardPoolEntry> pool)
    {
        float colWidth = (position.width - 60) / 2f;

        using (new EditorGUILayout.VerticalScope(GUILayout.Width(colWidth)))
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

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
                pool.Add(new KTH_WillRewardPoolEntry());
            }
        }
    }
}
#endif