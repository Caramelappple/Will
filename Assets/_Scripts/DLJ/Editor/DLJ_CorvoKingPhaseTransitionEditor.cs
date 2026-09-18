using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DLJ_CorvoKingPhaseTransition))]
public sealed class DLJ_CorvoKingPhaseTransitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var transition = (DLJ_CorvoKingPhaseTransition)target;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Wing Surrounding Feathers는 날개 주변에서 흩날리는 월드 깃털이야. Scene/Game 뷰 모두 표시돼. 화면을 덮는 기존 깃털은 Game 뷰에만 표시되며, Screen Feathers Enabled를 끄면 날개 펼침과 주변 깃털만 바로 재생해.", MessageType.Info);
        EditorGUILayout.HelpBox("Wing Unfold Curve는 펼치는 각도, Wing Rise Curve는 상승 움직임이야. 그래프를 클릭해서 편집해. X는 시간 비율(0~1), Y는 진행 정도(0=시작, 1=완료). 시작 (0,0), 끝 (1,1)을 유지하면 자연스럽게 연결돼. 전체 시간은 Wing Unfold Duration으로 조절해.", MessageType.Info);
        EditorGUILayout.HelpBox("커스텀 이미지는 Texture Type을 Sprite (2D and UI)로 설정하고 Apply한 뒤 Feather Sprite에 넣어. 비워두면 기존 깃털 모양을 사용해. 이미지 변경은 다음 재생부터 적용돼.", MessageType.Info);

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Unity 상단 Play를 켜면 아래 미리보기 버튼을 사용할 수 있어.", MessageType.Info);
        else if (!transition.isActiveAndEnabled)
            EditorGUILayout.HelpBox("CorvoKing 오브젝트와 이 컴포넌트를 활성화해야 재생할 수 있어.", MessageType.Warning);

        using (new EditorGUI.DisabledScope(!Application.isPlaying || !transition.isActiveAndEnabled))
        {
            if (GUILayout.Button("2페이즈 깃털 연출 미리보기"))
                transition.PlayTransition();
        }
    }
}
