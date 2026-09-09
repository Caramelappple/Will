using _Scripts.DLJ.SceneFlow;
using _Scripts.LDY.Stage;
using _Scripts.LSO.Stage;
using UnityEditor;
using UnityEngine;

namespace _Scripts.DLJ.Editor
{
    [CustomEditor(typeof(DLJ_StagePresentation))]
    public sealed class DLJ_StagePresentationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("플레이 중 테스트: 이동 버튼은 보상을 생략하고 해당 스테이지를 배치합니다. 클리어 버튼은 실제 보상 흐름을 실행합니다.", MessageType.Info);
            var intro = Object.FindAnyObjectByType<LSO_StageIntroDirector>();
            var flip = Object.FindAnyObjectByType<_Scripts.LDY.Effect.LDY_BoardFlipDirector>();
            bool busy = ((DLJ_StagePresentation)target).IsPlaying ||
                (intro != null && intro.IsPlaying) || (flip != null && (flip.IsPlaying || flip.IsFlipped)) ||
                (LSO_StageProgression.HasInstance && LSO_StageProgression.Instance.IsRunFinished);
            using (new EditorGUI.DisabledScope(!Application.isPlaying || busy))
            {
                if (GUILayout.Button("일반 스테이지 (1-2)")) Jump(0, 1);
                if (GUILayout.Button("보스 스테이지 (1-3)")) Jump(0, 2);
                if (GUILayout.Button("챕터 변경 (2-1)")) Jump(1, 0);
                if (GUILayout.Button("현재 스테이지 클리어 → 보상")) LSO_StageFlow.Instance.ClearStage();
            }
        }

        private static void Jump(int chapter, int stage)
        {
            var intro = Object.FindAnyObjectByType<LSO_StageIntroDirector>();
            var flip = Object.FindAnyObjectByType<_Scripts.LDY.Effect.LDY_BoardFlipDirector>();
            if ((intro != null && intro.IsPlaying) || (flip != null && (flip.IsPlaying || flip.IsFlipped)))
            {
                Debug.LogWarning("[DLJ] 보상과 보드 회전이 끝난 뒤 테스트해 주세요.");
                return;
            }
            LSO_StageProgression.Instance.SetPosition(chapter, stage);
            Object.FindAnyObjectByType<LDY_StageDirector>().LoadStage(LSO_StageProgression.Instance.Current);
        }
    }
}
