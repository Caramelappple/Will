using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using _Scripts.LSO.UI.Panel;

namespace _Scripts.LDY.Editor
{
    /// <summary>
    /// 사용법: 배선할 씬을 열고 상단 메뉴 "LDY > ESC 키 배선" 클릭.
    ///
    /// 열려 있는 모든 씬을 훑어 LDY_EscapeKeyHandler를 놓고 배선까지 채운다.
    /// 여러 번 눌러도 같은 결과가 나오므로 씬을 고친 뒤 다시 눌러도 된다.
    ///
    /// ── 손으로 하지 않고 도구로 하는 이유 ────────────────────────
    /// 창을 닫는 배선은 창마다 다르다. 뒤로가기 버튼이 있는 창도 있고
    /// (설정·오디오·해상도), 버튼 없이 스스로 닫는 창도 있다(크레딧).
    /// 인스펙터에서 손으로 맞추면 On Escape 칸에 함수를 안 고른 채로 두기 쉬운데,
    /// 그러면 에러 없이 ESC만 조용히 먹통이 된다.
    /// 어느 창이 어느 방식인지는 씬을 읽으면 알 수 있으므로 여기서 판별한다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    public static class LDY_EscapeKeyHandlerSetup
    {
        private const string HandlerObjectName = "LDY_EscapeKeyHandler";

        /// <summary>
        /// LSO_MenuActions가 들고 있는 창과, 그 창을 닫는 메서드 이름.
        /// 위에 있을수록 먼저 닫힌다 — 안쪽 창(오디오·해상도)이 바깥 창(설정)보다 앞이어야
        /// 오디오 창이 열린 상태에서 ESC를 눌렀을 때 설정 창이 먼저 닫히지 않는다.
        ///
        /// 필드 이름은 LSO_MenuActions의 [SerializeField] 이름 그대로다.
        /// 그쪽 이름이 바뀌면 여기도 같이 고칠 것(경고 로그로 알려준다).
        /// </summary>
        private static readonly (string field, string closeMethod)[] MenuPanels =
        {
            ("audioPanel", "CloseAudio"),
            ("videoPanel", "CloseVideo"),
            ("creditsPanel", "CloseCredits"),
            ("settingsPanel", "CloseSettings"),
        };

        [MenuItem("LDY/ESC 키 배선")]
        public static void Wire()
        {
            var report = new StringBuilder();
            int wiredScenes = 0;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                if (WireScene(scene, report))
                    wiredScenes++;
            }

            if (wiredScenes == 0)
            {
                Debug.LogWarning(
                    "[ESC 배선] 배선할 것을 찾지 못했습니다.\n" +
                    "  이 도구는 LSO_MenuActions(설정 창) 또는 LDY_CardPlacer(전투)가 있는 씬에서 동작합니다.\n" +
                    "  대상 씬을 열고 다시 실행하세요.");
                return;
            }

            Debug.Log($"[ESC 배선] 씬 {wiredScenes}개 완료.\n{report}");
        }

        /// <summary>씬 하나를 배선한다. 배선할 게 없으면 false.</summary>
        private static bool WireScene(Scene scene, StringBuilder report)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            LSO_MenuActions menuActions = FindInScene<LSO_MenuActions>(roots);
            LDY_CardPlacer cardPlacer = FindInScene<LDY_CardPlacer>(roots);
            LDY_MoveSystem moveSystem = FindInScene<LDY_MoveSystem>(roots);
            LDY_AttackSystem attackSystem = FindInScene<LDY_AttackSystem>(roots);

            // 둘 다 없으면 ESC가 할 일이 없는 씬이다. 빈 핸들러를 남기지 않는다.
            if (menuActions == null && cardPlacer == null) return false;

            var targets = new List<LDY_EscapeKeyHandler.EscapeTarget>();
            var lines = new List<string>();

            CollectOpenerPanels(roots, targets, lines);
            CollectMenuPanels(menuActions, targets, lines);

            LDY_EscapeKeyHandler handler = GetOrCreateHandler(scene, roots, menuActions, cardPlacer, lines);

            Undo.RecordObject(handler, "ESC 키 배선");
            handler.EditorApplyWiring(targets.ToArray(), menuActions, cardPlacer, moveSystem, attackSystem);
            EditorUtility.SetDirty(handler);
            EditorSceneManager.MarkSceneDirty(scene);

            report.AppendLine($"● {scene.name}");
            foreach (string line in lines)
                report.AppendLine($"    {line}");

            if (targets.Count == 0)
                report.AppendLine("    닫을 창 없음 — 카드 배치 취소만 담당합니다");

            report.AppendLine(
                $"    설정 열기: {(menuActions != null ? "가능" : "이 씬엔 설정 창이 없어 생략")}" +
                $" / 전투 가드: {(moveSystem != null || attackSystem != null ? "연결됨" : "없음")}");

            return true;
        }

        // ── 창 모으기 ────────────────────────────────────────

        /// <summary>
        /// 맵의 상점·이벤트 창. 이들은 SetActive로만 여닫히고 Close()를 스스로 갖고 있다.
        /// 메뉴 창보다 앞에 둔다 — 맵 위에 겹쳐 뜨는 창이라 더 안쪽이다.
        /// </summary>
        private static void CollectOpenerPanels(
            GameObject[] roots,
            List<LDY_EscapeKeyHandler.EscapeTarget> targets,
            List<string> lines)
        {
            foreach (LDY_ShopUIOpener opener in FindAllInScene<LDY_ShopUIOpener>(roots))
                AddOpener(opener, "shopUI", opener.Close, "상점", targets, lines);

            foreach (LDY_EventUIOpener opener in FindAllInScene<LDY_EventUIOpener>(roots))
                AddOpener(opener, "eventUI", opener.Close, "이벤트", targets, lines);
        }

        private static void AddOpener(
            MonoBehaviour opener,
            string fieldName,
            UnityAction close,
            string label,
            List<LDY_EscapeKeyHandler.EscapeTarget> targets,
            List<string> lines)
        {
            GameObject panel = ReadPanelField(opener, fieldName);
            if (panel == null)
            {
                lines.Add($"⚠ {label} 창: {opener.name}의 {fieldName}가 비어 있어 건너뜁니다");
                return;
            }

            var target = new LDY_EscapeKeyHandler.EscapeTarget { panel = panel, onEscape = new UnityEvent() };
            UnityEventTools.AddVoidPersistentListener(target.onEscape, close);
            targets.Add(target);

            lines.Add($"{label} 창 '{panel.name}' → {opener.GetType().Name}.Close()");
        }

        /// <summary>
        /// 설정 계열 창. 창 안의 뒤로가기 버튼을 찾으면 그 버튼을 쓰고,
        /// 없으면 LSO_MenuActions의 짝이 되는 Close 메서드를 직접 건다.
        /// </summary>
        private static void CollectMenuPanels(
            LSO_MenuActions menuActions,
            List<LDY_EscapeKeyHandler.EscapeTarget> targets,
            List<string> lines)
        {
            if (menuActions == null) return;

            var serialized = new SerializedObject(menuActions);

            foreach ((string field, string closeMethod) in MenuPanels)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null)
                {
                    lines.Add($"⚠ LSO_MenuActions에 '{field}' 필드가 없습니다 — 이 도구를 고쳐야 합니다");
                    continue;
                }

                var panel = property.objectReferenceValue as GameObject;
                if (panel == null) continue; // 그 씬에서 안 쓰는 창이면 조용히 넘어간다

                var target = new LDY_EscapeKeyHandler.EscapeTarget { panel = panel, onEscape = new UnityEvent() };

                Button returnButton = FindReturnButton(panel, menuActions, closeMethod);
                if (returnButton != null)
                {
                    // 버튼을 쓰면 배선이 한 곳(버튼의 OnClick)에만 남는다.
                    // 나중에 그 버튼의 동작이 바뀌어도 ESC가 저절로 따라간다.
                    target.returnButton = returnButton;
                    lines.Add($"'{panel.name}' → 버튼 '{returnButton.name}' 누르기");
                }
                else
                {
                    UnityAction close = GetCloseAction(menuActions, closeMethod);
                    if (close == null)
                    {
                        lines.Add($"⚠ '{panel.name}': {closeMethod}를 찾지 못해 그냥 닫기로 둡니다");
                    }
                    else
                    {
                        UnityEventTools.AddVoidPersistentListener(target.onEscape, close);
                        lines.Add($"'{panel.name}' → LSO_MenuActions.{closeMethod}() (뒤로가기 버튼 없음)");
                    }
                }

                targets.Add(target);
            }
        }

        /// <summary>
        /// 창 안에서 "이 창을 닫는" 버튼을 찾는다.
        /// 이름이 아니라 OnClick에 실제로 걸린 메서드로 판별한다.
        /// 이름으로 찾으면 ReturnBtn처럼 같은 이름을 쓰는 다른 창의 버튼을 집을 수 있다.
        /// </summary>
        private static Button FindReturnButton(GameObject panel, LSO_MenuActions menuActions, string closeMethod)
        {
            foreach (Button button in panel.GetComponentsInChildren<Button>(true))
            {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    if (!ReferenceEquals(button.onClick.GetPersistentTarget(i), menuActions)) continue;
                    if (button.onClick.GetPersistentMethodName(i) != closeMethod) continue;

                    return button;
                }
            }

            return null;
        }

        /// <summary>
        /// 이름을 델리게이트로 바꾼다. 문자열로 리플렉션하지 않고 여기서 직접 잇는 이유는,
        /// LSO_MenuActions 쪽 메서드 이름이 바뀌면 컴파일 에러로 잡히게 하기 위해서다.
        /// </summary>
        private static UnityAction GetCloseAction(LSO_MenuActions menuActions, string closeMethod)
        {
            switch (closeMethod)
            {
                case "CloseAudio": return menuActions.CloseAudio;
                case "CloseVideo": return menuActions.CloseVideo;
                case "CloseCredits": return menuActions.CloseCredits;
                case "CloseSettings": return menuActions.CloseSettings;
                default: return null;
            }
        }

        // ── 핸들러 놓기 ──────────────────────────────────────

        /// <summary>
        /// 이미 있으면 그걸 쓰고, 없으면 놓는다.
        /// 붙일 자리는 설정 창을 가진 오브젝트 → 카드 배치 오브젝트 → 새 오브젝트 순이다.
        /// 이미 있는 것을 지우고 새로 만들지 않는 이유는, 손으로 더한 배선이 있을 수 있어서다.
        /// </summary>
        private static LDY_EscapeKeyHandler GetOrCreateHandler(
            Scene scene,
            GameObject[] roots,
            LSO_MenuActions menuActions,
            LDY_CardPlacer cardPlacer,
            List<string> lines)
        {
            List<LDY_EscapeKeyHandler> existing = FindAllInScene<LDY_EscapeKeyHandler>(roots);

            if (existing.Count > 1)
            {
                lines.Add(
                    $"⚠ 핸들러가 {existing.Count}개 있습니다. '{existing[0].name}'만 배선했습니다 — " +
                    "나머지는 지우세요(둘 다 있으면 ESC가 두 번 처리됩니다)");
            }

            if (existing.Count > 0)
            {
                lines.Add($"핸들러: 기존 '{existing[0].name}' 사용");
                return existing[0];
            }

            GameObject host =
                menuActions != null ? menuActions.gameObject :
                cardPlacer != null ? cardPlacer.gameObject :
                null;

            if (host == null)
            {
                host = new GameObject(HandlerObjectName);
                SceneManager.MoveGameObjectToScene(host, scene);
                Undo.RegisterCreatedObjectUndo(host, "ESC 키 배선");
            }

            lines.Add($"핸들러: '{host.name}'에 새로 붙임");

            return Undo.AddComponent<LDY_EscapeKeyHandler>(host);
        }

        // ── 씬 훑기 ──────────────────────────────────────────

        /// <summary>
        /// private [SerializeField]로 들고 있는 창을 읽는다.
        /// 남의 컴포넌트를 읽기만 하는 것이라 SerializedObject로 접근한다.
        /// </summary>
        private static GameObject ReadPanelField(MonoBehaviour owner, string fieldName)
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(fieldName);

            return property?.objectReferenceValue as GameObject;
        }

        private static T FindInScene<T>(GameObject[] roots) where T : Component
        {
            foreach (GameObject root in roots)
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }

            return null;
        }

        private static List<T> FindAllInScene<T>(GameObject[] roots) where T : Component
        {
            var found = new List<T>();

            foreach (GameObject root in roots)
                found.AddRange(root.GetComponentsInChildren<T>(true));

            return found;
        }
    }
}
