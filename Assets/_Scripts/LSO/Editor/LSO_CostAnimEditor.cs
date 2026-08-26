using _Scripts.LDY;
using _Scripts.LSO.Cost;
using UnityEditor;
using UnityEngine;

namespace _Scripts.LSO.Editor
{
    /// <summary>
    /// LSO_CostAnim 인스펙터에 시험용 버튼을 붙인다.
    ///
    /// 코스트 연출은 실제로 코스트를 쓰거나 받아야 보이는데, 그러려면 전투를 굴려
    /// 기물을 소환하거나 유언이 터지기를 기다려야 한다. 값 하나를 확인하려고
    /// 그 과정을 매번 반복하면 연출을 다듬을 수가 없다.
    ///
    /// 그래서 여기서는 LDY_ActionPointManager를 직접 흔든다.
    /// 연출을 흉내 내지 않고 진짜 값을 바꾸므로, 여기서 보이는 것이 실제 동작이다.
    /// </summary>
    [CustomEditor(typeof(LSO_CostAnim))]
    public class LSO_CostAnimEditor : UnityEditor.Editor
    {
        private static LDY_ActionPointManager Points => LDY_ActionPointManager.instance;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("시험용", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "플레이 중에만 쓸 수 있습니다. 실제 행동력 값을 바꿔서 연출을 확인합니다.",
                    MessageType.Info);
                return;
            }

            if (Points == null)
            {
                EditorGUILayout.HelpBox(
                    "씬에 LDY_ActionPointManager가 없습니다.",
                    MessageType.Warning);
                return;
            }

            DrawStatus();
            EditorGUILayout.Space(4f);
            DrawPointButtons();
            EditorGUILayout.Space(4f);
            DrawViewButtons();
        }

        private void DrawStatus()
        {
            int max = Points.Max;
            int addMax = Points.AddMax;
            int caseCount = max > 0 ? Mathf.CeilToInt((float)addMax / max) : 0;

            EditorGUILayout.LabelField(
                $"{Points.Current} / {max}    (상한 {addMax} · 케이스 {caseCount}개 · 여유 {Points.Headroom})");
        }

        private void DrawPointButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // 남은 것이 없으면 눌러도 아무 일이 없다. 연출이 안 나오는 것인지
                // 조건에 걸린 것인지 구분되도록 버튼을 미리 죽인다.
                using (new EditorGUI.DisabledScope(!Points.CanAfford()))
                {
                    if (GUILayout.Button("－1 쓰기"))
                        Points.TryConsume();
                }

                using (new EditorGUI.DisabledScope(!Points.CanAdd()))
                {
                    if (GUILayout.Button("＋1 받기"))
                        Points.AddActionPoints();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("전부 쓰기"))
                    Points.TryConsume(Points.Current);

                if (GUILayout.Button("상한까지 채우기"))
                    Points.AddActionPoints(Points.Headroom);

                if (GUILayout.Button("턴 리셋"))
                    Points.ResetPoints();
            }
        }

        private void DrawViewButtons()
        {
            LSO_CostAnim anim = (LSO_CostAnim)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("케이스 다시 만들기"))
                    anim.Rebuild();

                if (GUILayout.Button("등장 연출 다시"))
                    anim.Replay();
            }
        }

        /// <summary>
        /// 플레이 중에는 인스펙터를 매 프레임 다시 그린다.
        ///
        /// 이것이 없으면 코스트가 바뀌어도 위의 숫자가 그대로 있다가
        /// 마우스를 올려야 갱신된다. 버튼이 죽어 있는 이유를 오해하게 된다.
        /// </summary>
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
    }
}
