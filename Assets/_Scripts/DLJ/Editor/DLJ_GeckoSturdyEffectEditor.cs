using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DLJ_GeckoSturdyEffect))]
public sealed class DLJ_GeckoSturdyEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox(
            "Rise Curve를 클릭해서 상승 이징을 편집해. X는 시간(0~1), Y는 높이 비율이야. " +
            "Rise Duration은 시간(초), Rise Height는 월드 상승 거리야. " +
            "Fade Start 이후에는 꼬리가 서서히 투명해져.", MessageType.Info);
        EditorGUILayout.HelpBox("미리보기는 Play 모드에서 사용해. HP와 실제 특성 소모 상태는 바뀌지 않아.", MessageType.Info);
        var effect = (DLJ_GeckoSturdyEffect)target;
        using (new EditorGUI.DisabledScope(!Application.isPlaying || !effect.isActiveAndEnabled))
        {
            if (GUILayout.Button("꼬리 상승 미리보기 / 다시 재생")) effect.Preview();
            if (GUILayout.Button("꼬리 원상 복구 (연출만)")) effect.RestoreTail();
        }
    }
}
