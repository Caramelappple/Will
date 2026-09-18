#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using _Scripts.LDY.Effect;
using _Scripts.LSO.Reward;
using _Scripts.LSO.Stage;
using _Scripts.LSO.UI.Feedback;
using _Scripts.LSO.Will.Candle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Scripts.LSO.Editor
{
    /// <summary>
    /// 열린 씬에 무엇이 걸려 있고 무엇이 빠졌는지 한 번에 본다.
    ///
    /// ── 왜 필요한가 ───────────────────────────────────────────
    /// 스테이지 한 바퀴는 컴포넌트들이 서로를 찾아 구독하는 구조라
    /// 인스펙터로 이을 것이 거의 없다. 대신 <b>하나가 씬에 아예 없으면
    /// 그 단계만 조용히 건너뛴다.</b> 클리어했는데 아무 일도 안 일어나거나,
    /// 회전을 건너뛰고 보상으로 가거나 하는 식이다.
    ///
    /// 그 증상만 보고 어느 컴포넌트가 빠졌는지 되짚는 것이 매번 오래 걸려서
    /// 목록을 한자리에 적어둔다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 씬 파일을 건드리는 것은 '빠진 것 걸기' 쪽뿐이고, 그것도 유니티 API로만 붙인다.
    /// Undo 로 되돌릴 수 있다.
    /// </summary>
    public static class LSO_WiringCheck
    {
        /// <summary>
        /// 씬에 있어야 하는 것. 개수는 "몇 개가 정상인가"다.
        ///
        /// 0 이면 없어도 되는 것, 1 이면 정확히 하나여야 하는 것이다.
        /// </summary>
        private readonly struct Entry
        {
            public readonly System.Type Type;
            public readonly int Expected;
            public readonly string Symptom;

            public Entry(System.Type type, int expected, string symptom)
            {
                Type = type;
                Expected = expected;
                Symptom = symptom;
            }
        }

        private static readonly Entry[] Required =
        {
            new Entry(typeof(LSO_StageProgression), 1, "\"시작할 스테이지가 없습니다\""),
            new Entry(typeof(LSO_StageFlow), 1, "클리어해도 아무 일 없음"),
            new Entry(typeof(LDY_BoardFlipDirector), 1, "회전을 건너뛰고 보상으로"),
            new Entry(typeof(LSO_RewardBox), 1, "보상을 건너뛰고 다음 스테이지로"),
            new Entry(typeof(LSO_StageIntroDirector), 1, "보상 뒤 판이 안 돌아옴"),
            new Entry(typeof(LSO_WillCandle), 1, "숫자키로 유언을 못 고름"),
            new Entry(typeof(EventSystem), 1, "클릭이 아예 안 옴"),
            new Entry(typeof(PhysicsRaycaster), 1, "3D 오브젝트 클릭이 안 옴"),
        };

        [MenuItem("LSO/배선 점검")]
        private static void Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[LSO 배선 점검]");

            int missing = 0;

            foreach (Entry entry in Required)
            {
                int count = CountInScene(entry.Type);

                if (count == entry.Expected)
                {
                    sb.AppendLine($"  OK    {entry.Type.Name} ({count})");
                    continue;
                }

                missing++;

                sb.AppendLine(count == 0
                    ? $"  없음  {entry.Type.Name} — {entry.Symptom}"
                    : $"  {count}개  {entry.Type.Name} — {entry.Expected}개여야 한다");
            }

            // 거부 신호는 없어도 게임이 돌아가므로 위 목록과 나눠서 적는다.
            int loggers = CountInScene(typeof(LSO_RejectLogger));
            sb.AppendLine(loggers == 0
                ? "  선택  LSO_RejectLogger — 없음. 거부 이유가 콘솔에 안 찍힌다"
                : $"  선택  LSO_RejectLogger ({loggers})");

            // 양초를 클릭하려면 Painter 와 Collider 가 같은 오브젝트에 있어야 한다.
            AppendPainterCheck(sb);

            sb.AppendLine();
            sb.AppendLine(missing == 0
                ? "필수 항목은 다 걸려 있다."
                : $"필수 {missing}개가 어긋나 있다. LSO/빠진 배선 걸기 로 일부는 자동으로 붙는다.");

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Painter 는 클릭을 받는 쪽이라 Collider 가 없으면 눌러도 아무 일이 없다.
        /// 컴포넌트는 붙어 있는데 반응이 없는 형태라 원인이 잘 안 보인다.
        /// </summary>
        private static void AppendPainterCheck(StringBuilder sb)
        {
            LSO_WillPainter[] painters =
                Object.FindObjectsByType<LSO_WillPainter>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (painters.Length == 0)
            {
                sb.AppendLine("  없음  LSO_WillPainter — 양초를 눌러도 유언이 안 붙는다");
                return;
            }

            foreach (LSO_WillPainter painter in painters)
            {
                bool hasCollider = painter.GetComponent<Collider>() != null;

                sb.AppendLine(hasCollider
                    ? $"  OK    LSO_WillPainter on '{painter.name}'"
                    : $"  주의  LSO_WillPainter on '{painter.name}' — Collider 가 없어 클릭이 안 온다");
            }
        }

        [MenuItem("LSO/빠진 배선 걸기")]
        private static void Apply()
        {
            var added = new List<string>();

            // 스테이지 진행. 씬 아무 곳에나 하나 있으면 되고, 스스로 DontDestroyOnLoad 한다.
            if (CountInScene(typeof(LSO_StageProgression)) == 0)
            {
                var go = new GameObject(nameof(LSO_StageProgression));
                Undo.RegisterCreatedObjectUndo(go, "LSO 배선 걸기");

                LSO_StageProgression progression =
                    Undo.AddComponent<LSO_StageProgression>(go);

                added.Add(AssignChapters(progression));
            }

            // 거부 신호 구독자. 스테이지 진행과 같은 오브젝트에 두지 않는다 —
            // 그쪽은 씬을 넘어가도 살아남아서, 다음 씬의 로거와 둘이 되어 로그가 두 줄로 찍힌다.
            if (CountInScene(typeof(LSO_RejectLogger)) == 0)
            {
                var go = new GameObject(nameof(LSO_RejectLogger));
                Undo.RegisterCreatedObjectUndo(go, "LSO 배선 걸기");
                Undo.AddComponent<LSO_RejectLogger>(go);

                added.Add($"{nameof(LSO_RejectLogger)} 를 새 오브젝트에 붙였다");
            }

            if (added.Count == 0)
            {
                Debug.Log("[LSO 배선] 자동으로 붙일 것이 없다. 나머지는 손으로 걸어야 한다.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene());

            Debug.Log("[LSO 배선] 붙였다 (Ctrl+Z 로 되돌릴 수 있다)\n  " +
                      string.Join("\n  ", added) +
                      "\n씬을 저장해야 남는다.");
        }

        /// <summary>
        /// 챕터 에셋을 찾아 목록에 넣는다.
        ///
        /// 챕터가 비면 Awake 에서 "챕터가 하나도 없어 진행할 수 없습니다"가 뜬다.
        /// 붙이자마자 그 경고를 보게 하지 않으려고 여기서 채워둔다.
        /// 여러 개면 어느 것이 1챕터인지 정할 방법이 없으므로 손에 맡긴다.
        /// </summary>
        private static string AssignChapters(LSO_StageProgression progression)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(LSO_ChapterSO)}");

            if (guids.Length != 1)
            {
                return $"{nameof(LSO_StageProgression)} 를 붙였다 — " +
                       (guids.Length == 0
                           ? "챕터 에셋을 못 찾아 Chapters 는 비어 있다. 손으로 넣을 것"
                           : $"챕터 에셋이 {guids.Length}개라 순서를 정할 수 없다. 손으로 넣을 것");
            }

            var chapter = AssetDatabase.LoadAssetAtPath<LSO_ChapterSO>(
                AssetDatabase.GUIDToAssetPath(guids[0]));

            var so = new SerializedObject(progression);
            SerializedProperty list = so.FindProperty("chapters");

            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = chapter;

            so.ApplyModifiedPropertiesWithoutUndo();

            return $"{nameof(LSO_StageProgression)} 를 붙이고 Chapters 에 '{chapter.name}' 를 넣었다";
        }

        private static int CountInScene(System.Type type)
        {
            return Object.FindObjectsByType(
                type, FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        }
    }
}
#endif
