// Assets/Editor/KTH_RewardEditorWindow.cs
#if UNITY_EDITOR
using _Scripts.LSO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class KTH_RewardEditorWindow : EditorWindow
{
    // 테이블이 씬이 아니라 에셋으로 옮겨졌다. 씬을 열지 않고도 밸런싱을 고칠 수 있다.
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

    /// <summary>프로젝트에서 첫 번째 보상 테이블 에셋을 찾는다.</summary>
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
                    stage = 1
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

    private void DrawStageRow(KTH_StageRewardData stage, ref KTH_StageRewardData toDelete)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("챕터", GUILayout.Width(40));
            stage.chapter = EditorGUILayout.IntField(stage.chapter, GUILayout.Width(50));

            GUILayout.Space(10);

            EditorGUILayout.LabelField("스테이지", GUILayout.Width(55));
            stage.stage = EditorGUILayout.IntField(stage.stage, GUILayout.Width(50));

            GUILayout.Space(10);

            EditorGUILayout.LabelField("기물", GUILayout.Width(30));
            stage.pieceCount = Mathf.Max(0, EditorGUILayout.IntField(stage.pieceCount, GUILayout.Width(35)));

            EditorGUILayout.LabelField("유언", GUILayout.Width(30));
            stage.willCount = Mathf.Max(0, EditorGUILayout.IntField(stage.willCount, GUILayout.Width(35)));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("스테이지 삭제", GUILayout.Width(90)))
                toDelete = stage;
        }

        EditorGUILayout.Space(2);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawPool("기물 풀", stage.possiblePieces);
            GUILayout.Space(8);
            DrawWillPool("유언 풀", stage.possibleWills);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
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
                    pool[i].willType =
                        (LSO_WillType)EditorGUILayout.EnumPopup(pool[i].willType);

                    pool[i].weight =
                        EditorGUILayout.FloatField(pool[i].weight, GUILayout.Width(60));

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
                    pool[i].animalName = EditorGUILayout.TextField(pool[i].animalName);
                    pool[i].weight = EditorGUILayout.FloatField(pool[i].weight, GUILayout.Width(60));

                    if (GUILayout.Button("-", GUILayout.Width(24)))
                    {
                        pool.RemoveAt(i);
                        break;
                    }
                }
            }

            if (GUILayout.Button("+ 기물 추가"))
            {
                pool.Add(new KTH_RewardPoolEntry());
            }
        }
    }
}
#endif