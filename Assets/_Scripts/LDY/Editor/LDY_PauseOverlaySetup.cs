using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using _Scripts.LSO.UI.Text;

namespace _Scripts.LDY.Editor
{
    /// <summary>
    /// 사용법: 전투·맵 씬을 열고 상단 메뉴 "LDY > ESC 안내 문구 만들기" 클릭.
    ///
    /// 열려 있는 모든 씬에 LDY_GameplayEscapeHandler와 안내 문구를 놓고 배선까지 채운다.
    ///
    /// ── 일시정지 오버레이는 없어졌다 ────────────────────────────
    /// 예전에는 화면을 덮는 반투명 정지 화면과 원형 게이지를 만들었다.
    /// 지금은 벅샷 룰렛처럼 글자 한 줄로만 알린다.
    ///
    /// 씬에 옛 LDY_PauseCanvas가 남아 있으면 지워야 한다. 이 도구가 찾아서 짚어준다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// ── 이미 있는 오브젝트는 건드리지 않는다 ─────────────────────
    /// 없을 때만 만든다. 이미 있으면 배선만 다시 채운다.
    /// 누를 때마다 색·문구·크기를 다시 써넣으면, 씬에서 고쳐둔 것이 통째로 되돌아간다.
    ///
    /// 기본 모양으로 되돌리고 싶으면 씬에서 캔버스를 지우고 다시 누를 것.
    /// ─────────────────────────────────────────────────────────
    ///
    /// ── 프리팹이 있으면 그것을 쓴다 ─────────────────────────────
    /// PrefabPath에 프리팹이 있으면 그것을 놓는다. 씬마다 모양이 갈라지지 않고,
    /// 프리팹 하나를 고치면 놓아둔 모든 씬이 함께 바뀐다.
    ///
    /// 만드는 법: 씬에서 만들어진 캔버스를 PrefabPath 위치로 끌어다 놓는다.
    /// 안쪽 이름(LDY_EscapePrompt)은 그대로 둘 것 — 배선할 때 이름으로 찾는다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    public static class LDY_PauseOverlaySetup
    {
        /// <summary>있으면 이것을 놓는다. 없으면 코드로 만든다.</summary>
        private const string PrefabPath = "Assets/_Prefabs/LDY/LDY_EscapeCanvas.prefab";

        private const string CanvasName = "LDY_EscapeCanvas";
        private const string PromptName = "LDY_EscapePrompt";
        private const string BaseLabelName = "Base";
        private const string FillLabelName = "Fill";
        private const string HandlerObjectName = "LDY_GameplayEscapeHandler";

        /// <summary>지워야 할 옛 오브젝트. 남아 있으면 화면을 덮는다.</summary>
        private const string LegacyCanvasName = "LDY_PauseCanvas";

        /// <summary>맵의 아이리스 연출 캔버스(1000)보다는 아래, 보통 UI보다는 위.</summary>
        private const int CanvasSortingOrder = 900;

        private const string DefaultMessage = "ESC를 누르고 있으면 나갑니다";

        [MenuItem("LDY/ESC 안내 문구 만들기")]
        public static void Build()
        {
            var report = new StringBuilder();
            int builtScenes = 0;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                BuildScene(scene, report);
                builtScenes++;
            }

            if (builtScenes == 0)
            {
                Debug.LogWarning("[ESC 안내] 열려 있는 씬이 없습니다.");
                return;
            }

            Debug.Log($"[ESC 안내] 씬 {builtScenes}개 완료.\n{report}");
        }

        private static void BuildScene(Scene scene, StringBuilder report)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            var lines = new List<string>();

            WarnAboutLegacy(roots, lines);

            LDY_CardPlacer cardPlacer = FindInScene<LDY_CardPlacer>(roots);
            LDY_MoveSystem moveSystem = FindInScene<LDY_MoveSystem>(roots);
            LDY_AttackSystem attackSystem = FindInScene<LDY_AttackSystem>(roots);

            Transform canvas = EnsureCanvas(scene, roots, lines);
            LSO_HoldTextPrompt prompt = EnsurePrompt(canvas, lines);

            LDY_GameplayEscapeHandler handler = GetOrCreateHandler(scene, roots, cardPlacer, lines);

            Undo.RecordObject(handler, "ESC 안내 문구 배선");
            handler.EditorApplyWiring(cardPlacer, moveSystem, attackSystem, prompt);
            EditorUtility.SetDirty(handler);
            EditorSceneManager.MarkSceneDirty(scene);

            lines.Add(
                $"전투 참조 — 배치: {(cardPlacer != null ? "연결됨" : "없음(맵 씬이면 정상)")}" +
                $" / 이동: {(moveSystem != null ? "연결됨" : "없음")}" +
                $" / 공격: {(attackSystem != null ? "연결됨" : "없음")}");

            report.AppendLine($"● {scene.name}");
            foreach (string line in lines)
                report.AppendLine($"    {line}");
        }

        /// <summary>
        /// 옛 정지 오버레이가 남아 있는지 본다.
        ///
        /// 자동으로 지우지 않는 이유: 그 아래에 다른 사람이 뭔가를 붙여뒀을 수 있다.
        /// 지우는 것은 사람이 보고 정할 일이다.
        /// </summary>
        private static void WarnAboutLegacy(GameObject[] roots, List<string> lines)
        {
            GameObject legacy = FindRootByName(roots, LegacyCanvasName);
            if (legacy == null) return;

            lines.Add($"⚠ 옛 '{LegacyCanvasName}' 가 남아 있습니다 — 지우세요(화면을 덮습니다)");

            Debug.LogWarning(
                $"[ESC 안내] '{LegacyCanvasName}' 는 이제 쓰지 않습니다. 씬에서 지우세요.",
                legacy);
        }

        // ── 만들기 ──────────────────────────────────────────

        /// <summary>
        /// 전용 캔버스를 따로 만든다. 기존 캔버스에 얹으면 그 캔버스의 정렬 순서에 끌려다녀
        /// 다른 UI 밑에 깔리는 일이 생긴다. 안내 문구는 언제나 맨 위여야 한다.
        /// </summary>
        private static Transform EnsureCanvas(Scene scene, GameObject[] roots, List<string> lines)
        {
            GameObject existing = FindRootByName(roots, CanvasName);
            if (existing != null)
            {
                lines.Add($"캔버스: 기존 '{CanvasName}' 사용 (모양은 건드리지 않음)");
                return existing.transform;
            }

            Transform fromPrefab = TryPlacePrefab(scene, lines);
            if (fromPrefab != null) return fromPrefab;

            var canvasGO = new GameObject(
                CanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));

            SceneManager.MoveGameObjectToScene(canvasGO, scene);
            Undo.RegisterCreatedObjectUndo(canvasGO, "ESC 안내 문구 배선");

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // GraphicRaycaster를 붙이지 않는다. 글자만 띄우는 캔버스라 받을 입력이 없고,
            // 붙여두면 뒤쪽 보드 클릭을 가로챌 여지만 생긴다.
            lines.Add($"캔버스: '{CanvasName}' 새로 만듦 (sortingOrder {CanvasSortingOrder})");

            return canvasGO.transform;
        }

        /// <summary>
        /// 프리팹을 씬에 놓는다. 없으면 null을 돌려주고 부르는 쪽이 코드로 만든다.
        ///
        /// 연결을 끊지 않고 프리팹 인스턴스로 놓는다. 그래야 프리팹을 고쳤을 때
        /// 놓아둔 씬들이 함께 따라온다.
        /// </summary>
        private static Transform TryPlacePrefab(Scene scene, List<string> lines)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            if (asset == null)
            {
                lines.Add($"캔버스: 프리팹이 없어 코드로 만듦 ({PrefabPath} 에 두면 그것을 쓴다)");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);

            if (instance == null)
            {
                Debug.LogWarning($"[ESC 안내] '{PrefabPath}' 를 놓지 못했습니다. 코드로 만듭니다.");
                return null;
            }

            // 이름이 어긋나면 다음번에 이 캔버스를 못 찾아 하나 더 만들게 된다.
            instance.name = CanvasName;

            Undo.RegisterCreatedObjectUndo(instance, "ESC 안내 문구 배선");

            lines.Add($"캔버스: 프리팹 '{PrefabPath}' 를 놓음");

            return instance.transform;
        }

        /// <summary>
        /// 안내 문구를 찾는다. 없을 때만 만든다.
        ///
        /// 이미 있으면 문구도 색도 손대지 않는다. 씬이나 프리팹에서 고쳐둔 것을
        /// 이 도구가 되돌려버리면 고칠 방법이 없어진다.
        /// </summary>
        private static LSO_HoldTextPrompt EnsurePrompt(Transform canvas, List<string> lines)
        {
            GameObject found = FindChildByName(canvas, PromptName);

            if (found != null)
            {
                var existing = found.GetComponent<LSO_HoldTextPrompt>();

                if (existing == null)
                {
                    Debug.LogWarning(
                        $"[ESC 안내] '{PromptName}' 에 LSO_HoldTextPrompt가 없습니다. " +
                        "안내 문구가 뜨지 않습니다.", found);
                }

                lines.Add($"문구: 기존 '{PromptName}' 사용 (모양은 건드리지 않음)");

                return existing;
            }

            var promptGO = new GameObject(PromptName, typeof(RectTransform), typeof(CanvasGroup));
            promptGO.transform.SetParent(canvas, false);
            Undo.RegisterCreatedObjectUndo(promptGO, "ESC 안내 문구 배선");

            var rect = (RectTransform)promptGO.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 120f); // 손패 위. 옮겨도 배선은 그대로다
            rect.sizeDelta = new Vector2(900f, 80f);

            // 흐린 바탕이 먼저, 선명한 글자가 그 위에. 순서가 뒤집히면 바탕이 앞을 덮는다.
            TMP_Text baseLabel = BuildLabel(promptGO.transform, BaseLabelName, new Color(1f, 1f, 1f, 0.25f));
            TMP_Text fillLabel = BuildLabel(promptGO.transform, FillLabelName, Color.white);

            LSO_HoldTextPrompt prompt = Undo.AddComponent<LSO_HoldTextPrompt>(promptGO);

            var so = new SerializedObject(prompt);
            SetObject(so, "baseLabel", baseLabel);
            SetObject(so, "fillLabel", fillLabel);
            SetString(so, "message", DefaultMessage);
            so.ApplyModifiedProperties();

            lines.Add($"문구: '{PromptName}' 새로 만듦 (\"{DefaultMessage}\")");

            return prompt;
        }

        /// <summary>라벨 한 장. 두 장이 같은 자리에 겹쳐야 글자가 차오르는 것처럼 보인다.</summary>
        private static TMP_Text BuildLabel(Transform parent, string name, Color color)
        {
            var labelGO = new GameObject(name, typeof(RectTransform));
            labelGO.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(labelGO, "ESC 안내 문구 배선");

            Stretch((RectTransform)labelGO.transform);

            var text = labelGO.AddComponent<TextMeshProUGUI>();
            text.text = DefaultMessage;
            text.fontSize = 32f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;

            return text;
        }

        /// <summary>
        /// 남의 컴포넌트가 private [SerializeField]로 들고 있는 값을 세운다.
        /// 필드 이름이 바뀌면 조용히 null이 되는 대신 이름을 짚어준다.
        /// </summary>
        private static void SetObject(SerializedObject so, string fieldName, UnityEngine.Object value)
        {
            SerializedProperty property = Find(so, fieldName);
            if (property == null) return;

            property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject so, string fieldName, string value)
        {
            SerializedProperty property = Find(so, fieldName);
            if (property == null) return;

            property.stringValue = value;
        }

        private static SerializedProperty Find(SerializedObject so, string fieldName)
        {
            SerializedProperty property = so.FindProperty(fieldName);

            if (property == null)
            {
                Debug.LogWarning(
                    $"[ESC 안내] LSO_HoldTextPrompt에 '{fieldName}' 필드가 없습니다 — 이 도구를 고쳐야 합니다.");
            }

            return property;
        }

        private static LDY_GameplayEscapeHandler GetOrCreateHandler(
            Scene scene,
            GameObject[] roots,
            LDY_CardPlacer cardPlacer,
            List<string> lines)
        {
            List<LDY_GameplayEscapeHandler> existing = FindAllInScene<LDY_GameplayEscapeHandler>(roots);

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

            GameObject host = cardPlacer != null ? cardPlacer.gameObject : null;

            if (host == null)
            {
                host = new GameObject(HandlerObjectName);
                SceneManager.MoveGameObjectToScene(host, scene);
                Undo.RegisterCreatedObjectUndo(host, "ESC 안내 문구 배선");
            }

            lines.Add($"핸들러: '{host.name}'에 새로 붙임");

            return Undo.AddComponent<LDY_GameplayEscapeHandler>(host);
        }

        // ── 씬 훑기 ──────────────────────────────────────────

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static GameObject FindRootByName(GameObject[] roots, string name)
        {
            foreach (GameObject root in roots)
                if (root.name == name) return root;

            return null;
        }

        private static GameObject FindChildByName(Transform parent, string name)
        {
            foreach (Transform child in parent)
                if (child.name == name) return child.gameObject;

            return null;
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
