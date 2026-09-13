using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DLJ_CorvoKingEffects))]
public sealed class DLJ_CorvoKingEffectsEditor : Editor
{
    private double lastTime;

    private void OnEnable()
    {
        lastTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += PreviewUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.update -= PreviewUpdate;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        if (!Application.isPlaying && target != null) ((DLJ_CorvoKingEffects)target).ReleaseVisuals();
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode && target != null)
            ((DLJ_CorvoKingEffects)target).ReleaseVisuals();
    }

    private void PreviewUpdate()
    {
        double now = EditorApplication.timeSinceStartup;
        float dt = (float)(now - lastTime);
        lastTime = now;
        if (Application.isPlaying || target == null) return;
        var effect = (DLJ_CorvoKingEffects)target;
        if (!effect.IsPlaying) return;
        effect.Tick(dt);
        SceneView.RepaintAll();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var effect = (DLJ_CorvoKingEffects)target;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Auto Play Skills를 켜면 실제 스킬과 자동 연동. 플레이 중 수동 미리보기는 이 옵션을 끄고 사용. 편집 모드에서도 아래 버튼으로 미리보기 가능", MessageType.Info);
        if (!effect.isActiveAndEnabled)
            EditorGUILayout.HelpBox("CorvoKing과 컴포넌트를 활성화하면 이펙트가 보임", MessageType.Warning);
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Binding", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Auto Play", effect.AutoPlaySkills);
                EditorGUILayout.Toggle("Predation Bound", effect.IsPredationBound);
                EditorGUILayout.Toggle("Memory Frenzy Bound", effect.IsFrenzyBound);
                EditorGUILayout.Toggle("Turn Events Bound", effect.AreTurnEventsBound);
                EditorGUILayout.ObjectField("Current Prey", effect.CurrentPrey, typeof(_Scripts.LDY.LDY_Animal), true);
                EditorGUILayout.IntField("Inherited ATK", effect.InheritedAttack);
                EditorGUILayout.IntField("Devour Events Received", effect.DevourEventCount);
                EditorGUILayout.IntField("Kill Attempts", effect.KillAttempts);
                EditorGUILayout.Toggle("Frenzy Visual Active", effect.IsFrenzyActive);
                EditorGUILayout.Toggle("Mesh / Material Ready", effect.HasVisualSource);
            }
            EditorGUILayout.HelpBox(effect.LastDevourResult, MessageType.Info);
            if (effect.AutoPlaySkills && effect.isActiveAndEnabled && !effect.AreTurnEventsBound)
                EditorGUILayout.HelpBox("턴 이벤트 등록이 빠져 있어 사냥감 지정이 진행되지 않을 수 있어", MessageType.Warning);
        }
        using (new EditorGUI.DisabledScope(!effect.isActiveAndEnabled))
        {
            if (GUILayout.Button("포식 · 붉은 섬광")) effect.PlayPredation();
            if (GUILayout.Button("되먹임 · 강화 섬광")) effect.PlayFeedback();
            if (GUILayout.Button("기억 폭주 · 붉은 눈 켜기")) effect.PlayMemoryFrenzy();
            if (GUILayout.Button("기억 폭주 · 붉은 눈 끄기")) effect.ClearMemoryFrenzy();
            if (GUILayout.Button("전체 이펙트 초기화")) effect.StopAndReset();
        }
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;
}
