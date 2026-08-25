using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.LDY.Editor
{
    /// <summary>
    /// 사용법: 상단 메뉴 "LDY > ESC 옛 핸들러 제거" 한 번 클릭.
    ///
    /// 창을 한 단계씩 닫던 LDY_EscapeKeyHandler를 프로젝트에서 걷어낸다.
    /// 씬에서 컴포넌트를 떼고, 스크립트 파일까지 지운다.
    /// ESC는 LDY_GameplayEscapeHandler 하나만 보게 된다(짧게=정지, 길게=나가기).
    ///
    /// ── 타입이 아니라 이름으로 찾는 이유 ────────────────────────
    /// 이 도구는 자기가 지울 클래스를 컴파일 시점에 참조하면 안 된다.
    /// 참조해두면 스크립트를 지운 순간 이 도구부터 컴파일이 깨져서,
    /// 되돌리려 해도 메뉴가 사라진 상태가 된다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// ── 순서가 중요하다 ────────────────────────────────────────
    /// 씬에서 먼저 떼고 저장한 뒤에 스크립트를 지운다. 반대로 하면
    /// 씬에 "Missing (Mono Script)"만 남고, 그때는 어느 컴포넌트였는지
    /// 알 수 없어 안전하게 지울 방법이 없다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    public static class LDY_EscapeKeyHandlerRemoval
    {
        private const string TargetTypeName = "LDY_EscapeKeyHandler";
        private const string SceneFolder = "Assets/_Scenes";

        private static readonly string[] ScriptsToDelete =
        {
            "Assets/_Scripts/LDY/LDY_EscapeKeyHandler.cs",
            "Assets/_Scripts/LDY/Editor/LDY_EscapeKeyHandlerSetup.cs",
        };

        [MenuItem("LDY/ESC 옛 핸들러 제거")]
        public static void Remove()
        {
            // 씬을 갈아끼우며 도는 도구라 열려 있던 작업물이 날아갈 수 있다.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string scenePathBefore = SceneManager.GetActiveScene().path;
            var report = new StringBuilder();
            int cleanedScenes = 0;
            int removedComponents = 0;

            foreach (string scenePath in FindScenePaths())
            {
                int removed = CleanScene(scenePath, report);
                if (removed <= 0) continue;

                cleanedScenes++;
                removedComponents += removed;
            }

            int deletedScripts = DeleteScripts(report);

            RestoreScene(scenePathBefore);

            AssetDatabase.Refresh();

            if (cleanedScenes == 0 && deletedScripts == 0)
            {
                Debug.Log($"[ESC 정리] 지울 게 없습니다. 이미 정리된 상태입니다.\n{report}");
                return;
            }

            Debug.Log(
                $"[ESC 정리] 씬 {cleanedScenes}개에서 컴포넌트 {removedComponents}개, " +
                $"스크립트 {deletedScripts}개를 지웠습니다.\n{report}");
        }

        /// <summary>
        /// 씬 하나를 열어 대상 컴포넌트를 떼고 저장한다. 뗀 개수를 돌려준다.
        /// 뗄 게 없으면 저장하지 않는다 — 안 건드린 씬까지 수정된 것으로 만들지 않기 위해서다.
        /// </summary>
        private static int CleanScene(string scenePath, StringBuilder report)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            List<Component> found = FindTargets(scene);
            if (found.Count == 0) return 0;

            var names = new List<string>();

            // 돌면서 지우면 순회 중인 목록이 흔들린다. 다 모은 뒤에 지운다.
            foreach (Component component in found)
            {
                names.Add(component.gameObject.name);
                Object.DestroyImmediate(component);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            report.AppendLine($"● {scene.name} — {string.Join(", ", names)}에서 뗌");

            return found.Count;
        }

        /// <summary>
        /// 이름이 같은 컴포넌트를 모은다.
        /// 스크립트가 없어진 자리(Missing)는 null로 들어오므로 반드시 걸러야 한다.
        /// </summary>
        private static List<Component> FindTargets(Scene scene)
        {
            var found = new List<Component>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) continue;
                    if (component.GetType().Name != TargetTypeName) continue;

                    found.Add(component);
                }
            }

            return found;
        }

        private static int DeleteScripts(StringBuilder report)
        {
            int deleted = 0;

            foreach (string path in ScriptsToDelete)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) == null) continue;

                if (!AssetDatabase.DeleteAsset(path))
                {
                    report.AppendLine($"⚠ {path} 를 지우지 못했습니다. 손으로 지우세요.");
                    continue;
                }

                report.AppendLine($"● 스크립트 삭제 — {path}");
                deleted++;
            }

            return deleted;
        }

        /// <summary>
        /// 도구를 부르기 전에 열려 있던 씬으로 돌려놓는다.
        /// 마지막으로 훑은 씬을 열어둔 채로 끝나면, 이어서 하던 작업이 엉뚱한 씬에서 계속된다.
        /// </summary>
        private static void RestoreScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath)) return;
            if (SceneManager.GetActiveScene().path == scenePath) return;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        /// <summary>
        /// 작업 중인 씬만 본다. Assets/_Recovery 밑의 복구 덤프는 건드리지 않는다.
        /// 열 일이 없는 파일이라 손대봐야 변경 이력만 늘어난다.
        /// </summary>
        private static IEnumerable<string> FindScenePaths()
        {
            var paths = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { SceneFolder }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));

            paths.Sort();

            return paths;
        }
    }
}
