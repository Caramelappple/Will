using _Scripts.LDY;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DLJ_SharkKing))]
public sealed class DLJ_SharkKingEditor : Editor
{
    private LDY_BoardManager previewBoard;
    private Vector2Int previewOrigin = new(3, 3);
    private int previewAreaSize = 2;
    private LDY_Animal bitePreviewTarget;

    public override void OnInspectorGUI()
    {
        var shark = (DLJ_SharkKing)target;
        serializedObject.Update();
        EditorGUILayout.LabelField("일반 공격 · 이빨 미리보기", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("biteColor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("biteSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("biteAppearDuration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("biteCloseDuration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("biteFadeDuration"));
        serializedObject.ApplyModifiedProperties();
        bitePreviewTarget = (LDY_Animal)EditorGUILayout.ObjectField(
            "물기 대상", bitePreviewTarget, typeof(LDY_Animal), true);
        using (new EditorGUI.DisabledScope(!Application.isPlaying || !shark.isActiveAndEnabled ||
            bitePreviewTarget == null || bitePreviewTarget.health == null || bitePreviewTarget.health.IsDestroyed))
        {
            if (GUILayout.Button("반투명 이빨 재생 (피해 없음)"))
                shark.StartCoroutine(shark.PlayBiteAttack(bitePreviewTarget, null));
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("물보라 모양 · 양 조절", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackWaterCrownHeight"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackWaterPoolRadius"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackWaterFallScale"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackWaterParticleCount"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackWaterJetCount"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackWaterExtraBurstRatio"));
        EditorGUILayout.HelpBox(
            "수면 확장 → 왕관 모양 물막 → 잔물결 순서로 재생해. 개수를 0으로 설정하면 보조 입자만 꺼져. " +
            "값을 바꾼 뒤 아래 재생 버튼으로 확인해. Play 모드 변경값은 종료하면 되돌아가니, 저장할 값은 편집 모드에서 설정해.",
            MessageType.Info);
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("상어왕 이펙트 미리보기", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Play 모드에서 아래 버튼을 누르면 지정한 보드 칸에 즉시 재생해. 다시 누르면 처음부터 재생해. " +
            "실제 피해·턴·AP에는 영향이 없어. 머리부터 수직으로 솟은 뒤 꼬리부터 잠기는 모션이고, 카메라 흔들림은 없어.", MessageType.Info);

        if (previewBoard == null && Application.isPlaying)
            previewBoard = FindFirstObjectByType<LDY_BoardManager>();
        previewBoard = (LDY_BoardManager)EditorGUILayout.ObjectField(
            "미리보기 보드", previewBoard, typeof(LDY_BoardManager), true);
        previewAreaSize = EditorGUILayout.IntSlider("공격 영역 크기", previewAreaSize, 1, LDY_BoardManager.Size);
        previewOrigin = EditorGUILayout.Vector2IntField("시작 칸 (X, Z)", previewOrigin);
        previewOrigin.x = Mathf.Clamp(previewOrigin.x, 0, LDY_BoardManager.Size - previewAreaSize);
        previewOrigin.y = Mathf.Clamp(previewOrigin.y, 0, LDY_BoardManager.Size - previewAreaSize);

        if (Application.isPlaying && (!shark.isActiveAndEnabled || previewBoard == null))
            EditorGUILayout.HelpBox("상어왕 오브젝트와 컴포넌트를 활성화하고 보드를 지정해.", MessageType.Warning);

        using (new EditorGUI.DisabledScope(!Application.isPlaying || !shark.isActiveAndEnabled || previewBoard == null))
        {
            if (GUILayout.Button("상어 + 물보라 바로 재생 / 다시 재생", GUILayout.Height(32f)))
                shark.PreviewAttack(previewBoard, previewOrigin, previewAreaSize, false);
            if (GUILayout.Button("물보라만 바로 재생"))
                shark.PreviewAttack(previewBoard, previewOrigin, previewAreaSize, true);
        }
        using (new EditorGUI.DisabledScope(!Application.isPlaying || !shark.IsPreviewingAttack))
        {
            if (GUILayout.Button("미리보기 중지")) shark.StopAttackPreview();
        }

        EditorGUILayout.Space(12f);
        DrawPropertiesExcluding(serializedObject, "m_Script",
            "biteColor", "biteSize", "biteAppearDuration", "biteCloseDuration", "biteFadeDuration",
            "attackWaterParticleCount", "attackWaterJetCount", "attackWaterExtraBurstRatio",
            "attackWaterCrownHeight", "attackWaterPoolRadius", "attackWaterFallScale");
        serializedObject.ApplyModifiedProperties();
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;
}
