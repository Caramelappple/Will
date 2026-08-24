using System.Collections.Generic;
using System.Text;
using _Scripts.LSO.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Scripts.LDY.Editor
{
    /// <summary>
    /// 사용법: 전투·맵 씬을 열고 상단 메뉴 "LDY > 일시정지 오버레이 만들기" 클릭.
    ///
    /// 열려 있는 모든 씬에 LDY_GameplayEscapeHandler와 최소 오버레이를 놓고 배선까지 채운다.
    /// 여러 번 눌러도 같은 결과가 나오므로 씬을 고친 뒤 다시 눌러도 된다.
    ///
    /// ── 그림 에셋을 쓰지 않는 이유 ──────────────────────────────
    /// 일시정지 화면에 쓸 이미지가 아직 없다. 이미지를 기다리는 동안 기능이 멈추지 않도록
    /// 반투명 검정 + 텍스트 + 내장 Knob 스프라이트만으로 만든다.
    /// 나중에 그림이 나오면 여기서 만든 오브젝트의 Image/Text만 갈아끼우면 된다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    public static class LDY_PauseOverlaySetup
    {
        private const string CanvasName = "LDY_PauseCanvas";
        private const string OverlayName = "LDY_PauseOverlay";
        private const string DimName = "Dim";
        private const string LabelName = "Label";
        private const string GaugeName = "LDY_HoldGauge";
        private const string HandlerObjectName = "LDY_GameplayEscapeHandler";

        /// <summary>맵의 아이리스 연출 캔버스(1000)보다는 아래, 보통 UI보다는 위.</summary>
        private const int CanvasSortingOrder = 900;

        [MenuItem("LDY/일시정지 오버레이 만들기")]
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
                Debug.LogWarning("[일시정지] 열려 있는 씬이 없습니다.");
                return;
            }

            Debug.Log($"[일시정지] 씬 {builtScenes}개 완료.\n{report}");
        }

        private static void BuildScene(Scene scene, StringBuilder report)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            var lines = new List<string>();

            LDY_CardPlacer cardPlacer = FindInScene<LDY_CardPlacer>(roots);
            LDY_MoveSystem moveSystem = FindInScene<LDY_MoveSystem>(roots);
            LDY_AttackSystem attackSystem = FindInScene<LDY_AttackSystem>(roots);

            Transform canvas = EnsureCanvas(scene, roots, lines);
            GameObject overlay = EnsureOverlay(canvas, lines);
            Image gauge = EnsureGauge(canvas, lines);

            LDY_GameplayEscapeHandler handler = GetOrCreateHandler(scene, roots, cardPlacer, lines);

            Undo.RecordObject(handler, "일시정지 오버레이 배선");
            handler.EditorApplyWiring(cardPlacer, moveSystem, attackSystem, overlay, gauge);
            EditorUtility.SetDirty(handler);
            EditorSceneManager.MarkSceneDirty(scene);

            WarnAboutOldHandler(roots, lines);

            lines.Add(
                $"전투 참조 — 배치: {(cardPlacer != null ? "연결됨" : "없음(맵 씬이면 정상)")}" +
                $" / 이동: {(moveSystem != null ? "연결됨" : "없음")}" +
                $" / 공격: {(attackSystem != null ? "연결됨" : "없음")}");

            report.AppendLine($"● {scene.name}");
            foreach (string line in lines)
                report.AppendLine($"    {line}");
        }

        /// <summary>
        /// 한 씬에 ESC를 보는 컴포넌트가 둘이면 한 번의 입력을 양쪽이 각자 처리한다.
        /// 컴파일도 되고 에러도 없이 "ESC 한 번에 두 가지가 일어나는" 상태라 눈으로 알아채기 어렵다.
        /// </summary>
        private static void WarnAboutOldHandler(GameObject[] roots, List<string> lines)
        {
            LDY_EscapeKeyHandler old = FindInScene<LDY_EscapeKeyHandler>(roots);
            if (old == null) return;

            lines.Add(
                $"⚠ '{old.name}'에 LDY_EscapeKeyHandler가 남아 있습니다. 지우거나 비활성화하세요 — " +
                "ESC 한 번을 두 컴포넌트가 각자 처리합니다.");
        }

        // ── 만들기 ──────────────────────────────────────────

        /// <summary>
        /// 전용 캔버스를 따로 만든다. 기존 캔버스에 얹으면 그 캔버스의 정렬 순서에 끌려다녀
        /// 다른 UI 밑에 깔리는 일이 생긴다. 정지 화면은 언제나 맨 위여야 한다.
        /// </summary>
        private static Transform EnsureCanvas(Scene scene, GameObject[] roots, List<string> lines)
        {
            GameObject existing = FindRootByName(roots, CanvasName);
            if (existing != null)
            {
                lines.Add($"캔버스: 기존 '{CanvasName}' 사용");
                return existing.transform;
            }

            var canvasGO = new GameObject(
                CanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            SceneManager.MoveGameObjectToScene(canvasGO, scene);
            Undo.RegisterCreatedObjectUndo(canvasGO, "일시정지 오버레이 배선");

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            lines.Add($"캔버스: '{CanvasName}' 새로 만듦 (sortingOrder {CanvasSortingOrder})");

            return canvasGO.transform;
        }

        private static GameObject EnsureOverlay(Transform canvas, List<string> lines)
        {
            GameObject overlay = FindChildByName(canvas, OverlayName);
            bool created = overlay == null;

            if (created)
            {
                overlay = new GameObject(OverlayName, typeof(RectTransform));
                overlay.transform.SetParent(canvas, false);
                Undo.RegisterCreatedObjectUndo(overlay, "일시정지 오버레이 배선");
            }

            Stretch((RectTransform)overlay.transform);
            BuildDim(overlay.transform);
            BuildLabel(overlay.transform);
            ApplyFadePanel(overlay);

            // LSO_FadePanel은 비활성 오브젝트에서 Awake가 돌지 않으므로 켜둔 채로 저장한다.
            // 대신 편집 중에 게임 뷰를 가리지 않도록 알파를 0으로 눕혀둔다.
            // 플레이를 누르면 Awake의 ApplyInstant(false)가 알아서 꺼준다.
            overlay.SetActive(true);

            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            lines.Add($"오버레이: '{OverlayName}' {(created ? "새로 만듦" : "기존 것 갱신")} (LSO_FadePanel, ignoreTimeScale)");

            return overlay;
        }

        private static void BuildDim(Transform overlay)
        {
            GameObject dim = FindChildByName(overlay, DimName);
            if (dim == null)
            {
                dim = new GameObject(DimName, typeof(RectTransform), typeof(Image));
                dim.transform.SetParent(overlay, false);
                Undo.RegisterCreatedObjectUndo(dim, "일시정지 오버레이 배선");
            }

            Stretch((RectTransform)dim.transform);

            Image image = dim.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.6f);
            image.raycastTarget = true; // 정지 중에 뒤쪽 보드가 눌리면 안 된다
        }

        private static void BuildLabel(Transform overlay)
        {
            GameObject label = FindChildByName(overlay, LabelName);
            if (label == null)
            {
                label = new GameObject(LabelName, typeof(RectTransform));
                label.transform.SetParent(overlay, false);
                Undo.RegisterCreatedObjectUndo(label, "일시정지 오버레이 배선");
            }

            var rect = (RectTransform)label.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(900f, 240f);

            TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = label.AddComponent<TextMeshProUGUI>();

            text.text = "일시정지";
            text.fontSize = 96f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.raycastTarget = false;
        }

        private static void ApplyFadePanel(GameObject overlay)
        {
            LSO_FadePanel panel = overlay.GetComponent<LSO_FadePanel>();
            if (panel == null)
                panel = Undo.AddComponent<LSO_FadePanel>(overlay); // CanvasGroup은 RequireComponent로 따라온다

            var so = new SerializedObject(panel);
            SetBool(so, "ignoreTimeScale", true); // 정지 중이라 scaled time으로는 페이드가 안 돈다
            SetBool(so, "applyOnAwake", true);
            SetBool(so, "openOnStart", false);
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// 남의 컴포넌트가 private [SerializeField]로 들고 있는 값을 세운다.
        /// 필드 이름이 바뀌면 조용히 null이 되어 NullReference로 죽는 대신 이름을 짚어준다.
        /// </summary>
        private static void SetBool(SerializedObject so, string fieldName, bool value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogWarning(
                    $"[일시정지] LSO_FadePanel에 '{fieldName}' 필드가 없습니다 — 이 도구를 고쳐야 합니다.");
                return;
            }

            property.boolValue = value;
        }

        /// <summary>
        /// 롱프레스 진행률. 오버레이 안이 아니라 캔버스 바로 아래에 둔다 —
        /// 정지돼 있지 않을 때도 차오르는 게 보여야 한다.
        /// </summary>
        private static Image EnsureGauge(Transform canvas, List<string> lines)
        {
            GameObject gaugeGO = FindChildByName(canvas, GaugeName);
            bool created = gaugeGO == null;

            if (created)
            {
                gaugeGO = new GameObject(GaugeName, typeof(RectTransform), typeof(Image));
                gaugeGO.transform.SetParent(canvas, false);
                Undo.RegisterCreatedObjectUndo(gaugeGO, "일시정지 오버레이 배선");
            }

            var rect = (RectTransform)gaugeGO.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 220f); // 손패 위. 자리가 마음에 안 들면 옮겨도 배선은 그대로다
            rect.sizeDelta = new Vector2(120f, 120f);

            Image gauge = gaugeGO.GetComponent<Image>();
            if (gauge == null)
                gauge = gaugeGO.AddComponent<Image>();

            // 내장 원형 스프라이트. 없으면 흰 사각형으로 그려지는데 그래도 진행률은 보인다.
            gauge.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            gauge.color = new Color(1f, 1f, 1f, 0.85f);
            gauge.type = Image.Type.Filled;
            gauge.fillMethod = Image.FillMethod.Radial360;
            gauge.fillOrigin = (int)Image.Origin360.Top;
            gauge.fillClockwise = true;
            gauge.fillAmount = 0f; // 0이면 아무것도 안 그려진다. 별도로 켜고 끌 필요가 없다
            gauge.raycastTarget = false;

            // 오버레이가 뜬 뒤에도 게이지가 보여야 한다(정지 중에 길게 눌러 나가는 흐름).
            gaugeGO.transform.SetAsLastSibling();

            lines.Add($"게이지: '{GaugeName}' {(created ? "새로 만듦" : "기존 것 갱신")} (Filled/Radial360)");

            return gauge;
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
                Undo.RegisterCreatedObjectUndo(host, "일시정지 오버레이 배선");
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
