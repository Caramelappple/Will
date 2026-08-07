// Assets/Editor/KTH_RewardEditorWindow.cs
#if UNITY_EDITOR
using _Scripts.LSO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class KTH_RewardEditorWindow : EditorWindow
{
    private KTH_Reward target;
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
            target = FindTargetInScene();
    }

    private KTH_Reward FindTargetInScene()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<KTH_Reward>();
#else
        return FindObjectOfType<KTH_Reward>();
#endif
    }

    private void OnGUI()
    {
        InitStyles();

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            target = (KTH_Reward)EditorGUILayout.ObjectField("대상 (KTH_Reward)", target, typeof(KTH_Reward), true);
            if (GUILayout.Button("씬에서 찾기", GUILayout.Width(90)))
                target = FindTargetInScene();
        }

        if (target == null)
        {
            EditorGUILayout.HelpBox("씬에서 KTH_Reward 컴포넌트를 찾을 수 없습니다. 위 필드에 직접 드래그해주세요.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(6);

        EditorGUI.BeginChangeCheck();

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label($"스테이지 총 {target.stageRewardTable.Count}개", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 스테이지 추가", GUILayout.Width(120)))
            {
                target.stageRewardTable.Add(new KTH_StageRewardData
                {
                    chapter = 1,
                    stage = 1
                });
            }
        }

        EditorGUILayout.Space(4);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        KTH_StageRewardData toDelete = null;

        foreach (var stage in target.stageRewardTable)
            DrawStageRow(stage, ref toDelete);

        EditorGUILayout.EndScrollView();

        if (toDelete != null)
            target.stageRewardTable.Remove(toDelete);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target);
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