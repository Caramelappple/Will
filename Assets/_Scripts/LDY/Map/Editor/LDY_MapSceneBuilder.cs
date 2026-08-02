using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// 맵 씬을 만드는 데 필요한 오브젝트/애셋(EventSystem, Canvas, 노드/라인 프리팹,
// 테마 애셋, MapManager, MapUIController)을 한 번에 자동 생성하고 서로 연결해주는 툴.
// 실행 후에는 별자리 맵 에디터(LDY_ConstellationMapEditorWindow)로 노드 좌표만 배치하면 됨.
public static class LDY_MapSceneBuilder
{
    private const string RootFolder = "Assets/01.Scripts/LDY/Map";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string ThemeAssetPath = RootFolder + "/LDY_MapTheme.asset";
    private const string NodePrefabPath = PrefabFolder + "/LDY_MapNodeView.prefab";
    private const string LinePrefabPath = PrefabFolder + "/LDY_MapLine.prefab";

    [MenuItem("LDY/Map/맵 씬 자동 생성")]
    private static void BuildScene()
    {
        if (GameObject.Find("MapManager") != null)
        {
            bool proceed = EditorUtility.DisplayDialog(
                "맵 씬 자동 생성",
                "씬에 이미 'MapManager' 오브젝트가 있습니다. 그래도 새로 생성할까요?\n(기존 오브젝트는 지우지 않고 새 세트를 추가로 만듭니다)",
                "계속", "취소");
            if (!proceed) return;
        }

        EnsureFolder(PrefabFolder);

        LDY_MapTheme theme = FindOrCreateTheme();
        EnsureEventSystem();

        Canvas canvas = CreateCanvas(out Image background, out RectTransform nodeContainer, out RectTransform lineContainer, theme);

        LDY_MapNodeView nodeViewPrefab = CreateNodePrefab();
        Image linePrefab = CreateLinePrefab();
        LDY_MapPlayerToken playerTokenPrefab = CreatePlayerTokenPrefab();

        GameObject managerGO = new GameObject("MapManager");
        Undo.RegisterCreatedObjectUndo(managerGO, "Build Map Scene");
        LDY_MapManager mapManager = managerGO.AddComponent<LDY_MapManager>();

        GameObject controllerGO = new GameObject("MapUIController");
        Undo.RegisterCreatedObjectUndo(controllerGO, "Build Map Scene");
        LDY_MapUIController controller = controllerGO.AddComponent<LDY_MapUIController>();

        WireController(controller, theme, mapManager, nodeContainer, lineContainer, nodeViewPrefab, linePrefab, background, playerTokenPrefab);
        SetupCameraController(canvas, background, mapManager, nodeContainer, lineContainer);
        EnsureSceneTransition();

        Selection.activeGameObject = managerGO;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[LDY_MapSceneBuilder] 맵 씬 기본 구조 생성 완료. " +
            "MapManager 인스펙터의 '별자리 맵 에디터 열기'로 노드를 배치하고, " +
            "노드 프리팹(Prefabs/LDY_MapNodeView)의 아이콘 스프라이트와 LDY_MapTheme의 폰트를 채워 넣어주세요.");

        EditorUtility.DisplayDialog("맵 씬 자동 생성 완료",
            "Canvas / EventSystem / MapManager / MapUIController / 카메라(팬·줌) 컨트롤러 / 노드·라인 프리팹 / 테마 애셋이 생성되었습니다.\n\n" +
            "남은 작업:\n" +
            "1) MapManager에서 별자리 맵 에디터로 노드/연결 배치\n" +
            "2) LDY_MapNodeView 프리팹에 타입별 아이콘 스프라이트 연결\n" +
            "3) LDY_MapTheme 애셋에 헤더/본문 폰트 연결",
            "확인");
    }

    // 이미 LDY_SceneTransition은 있는데(옛날 버전으로 만들어져) 카운트다운만 없는 경우, 그 오브젝트에 붙여줌
    [MenuItem("LDY/Map/카운트다운만 추가")]
    private static void AddCountdownOnly()
    {
        LDY_SceneTransition transition = Object.FindFirstObjectByType<LDY_SceneTransition>();
        if (transition == null)
        {
            EditorUtility.DisplayDialog("카운트다운 추가", "씬에서 LDY_SceneTransition을 찾을 수 없습니다. 먼저 '씬 전환 연출만 추가'를 실행하세요.", "확인");
            return;
        }

        SerializedObject existingSo = new SerializedObject(transition);
        if (existingSo.FindProperty("countdown").objectReferenceValue != null)
        {
            EditorUtility.DisplayDialog("카운트다운 추가", "이미 카운트다운이 연결되어 있습니다.", "확인");
            return;
        }

        LDY_BattleCountdown countdown = CreateCountdownText(transition.transform);

        existingSo.FindProperty("countdown").objectReferenceValue = countdown;
        existingSo.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[LDY_MapSceneBuilder] 카운트다운을 LDY_SceneTransition에 연결했습니다.");
    }

    // 반투명 딤 배경 + "3, 2, 1, START!" 텍스트 + LDY_BattleCountdown을 parent(SceneTransitionCanvas) 밑에 만들어줌.
    // 아이리스가 이미 사라진 뒤(=씬이 보이는 상태)에 재생되므로, 카운트다운 자체가 딤 배경과 입력 차단을 들고 있어야 함
    private static LDY_BattleCountdown CreateCountdownText(Transform parent)
    {
        GameObject dimGO = new GameObject("CountdownDim", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(dimGO, "Add Countdown");
        dimGO.transform.SetParent(parent, false);

        RectTransform dimRt = (RectTransform)dimGO.transform;
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;

        Image dimImg = dimGO.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.45f); // 보드가 살짝 비쳐 보이는 반투명 - 미리 훑어볼 수 있게
        dimImg.raycastTarget = true; // 카운트다운 중엔 클릭 막기
        dimGO.SetActive(false);

        GameObject countdownTextGO = new GameObject("CountdownText", typeof(RectTransform));
        countdownTextGO.transform.SetParent(dimGO.transform, false);

        RectTransform countdownRt = (RectTransform)countdownTextGO.transform;
        countdownRt.anchorMin = countdownRt.anchorMax = new Vector2(0.5f, 0.5f);
        countdownRt.pivot = new Vector2(0.5f, 0.5f);
        countdownRt.anchoredPosition = Vector2.zero;
        countdownRt.sizeDelta = new Vector2(600f, 300f);

        TextMeshProUGUI countdownText = countdownTextGO.AddComponent<TextMeshProUGUI>();
        countdownText.text = "3";
        countdownText.fontSize = 140f;
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.color = Color.black;
        countdownText.fontStyle = FontStyles.Bold;
        countdownText.raycastTarget = false;

        LDY_BattleCountdown countdown = parent.gameObject.AddComponent<LDY_BattleCountdown>();
        SerializedObject countdownSo = new SerializedObject(countdown);
        countdownSo.FindProperty("dimBackground").objectReferenceValue = dimGO;
        countdownSo.FindProperty("countdownText").objectReferenceValue = countdownText;
        countdownSo.ApplyModifiedProperties();

        return countdown;
    }

    // 씬 전환(아이리스) 연출용 오브젝트가 없으면 만들어줌. 다른 씬에서 로드돼도 유지되는 DontDestroyOnLoad 싱글톤
    private static void EnsureSceneTransition()
    {
        if (Object.FindFirstObjectByType<LDY_SceneTransition>() != null) return;

        GameObject canvasGO = new GameObject("SceneTransitionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Map Scene");

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 항상 다른 UI보다 위에

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject overlayGO = new GameObject("IrisOverlay", typeof(RectTransform), typeof(RawImage));
        overlayGO.transform.SetParent(canvasGO.transform, false);

        RectTransform overlayRt = (RectTransform)overlayGO.transform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        RawImage rawImage = overlayGO.GetComponent<RawImage>();
        rawImage.raycastTarget = false;

        // "3, 2, 1, START!" 텍스트 - IrisOverlay 위(형제 순서상 뒤)에 그려지도록 같은 Canvas 밑에 둠
        LDY_BattleCountdown countdown = CreateCountdownText(canvasGO.transform);

        LDY_SceneTransition transition = canvasGO.AddComponent<LDY_SceneTransition>();
        SerializedObject so = new SerializedObject(transition);
        so.FindProperty("overlay").objectReferenceValue = rawImage;
        so.FindProperty("countdown").objectReferenceValue = countdown;
        so.ApplyModifiedProperties();

        // 에디터에서 생성할 때는 Awake()가 실행되지 않아 평소 숨김 처리가 안 먹으므로 여기서 직접 꺼줌
        overlayGO.SetActive(false);
    }

    // Background를 드래그/스크롤 입력을 받는 대상으로 삼아 LDY_MapCameraController를 붙이고 연결
    private static void SetupCameraController(Canvas canvas, Image background, LDY_MapManager mapManager,
        RectTransform nodeContainer, RectTransform lineContainer)
    {
        background.raycastTarget = true;

        LDY_MapCameraController camController = background.gameObject.AddComponent<LDY_MapCameraController>();
        SerializedObject so = new SerializedObject(camController);
        so.FindProperty("mapManager").objectReferenceValue = mapManager;
        so.FindProperty("nodeContainer").objectReferenceValue = nodeContainer;
        so.FindProperty("lineContainer").objectReferenceValue = lineContainer;
        so.FindProperty("viewportRect").objectReferenceValue = canvas.GetComponent<RectTransform>();
        so.FindProperty("canvas").objectReferenceValue = canvas;
        so.ApplyModifiedProperties();
    }

    // 기존 씬에 플레이어 토큰만 추가하고 싶을 때 (씬을 통째로 다시 만들 필요 없음).
    // MapUIController를 찾아서 playerTokenPrefab 필드만 연결해줌
    [MenuItem("LDY/Map/플레이어 토큰만 추가")]
    private static void AddPlayerTokenOnly()
    {
        LDY_MapUIController controller = Object.FindFirstObjectByType<LDY_MapUIController>();
        if (controller == null)
        {
            EditorUtility.DisplayDialog("플레이어 토큰 추가", "씬에서 'LDY_MapUIController'를 찾을 수 없습니다.", "확인");
            return;
        }

        EnsureFolder(PrefabFolder);
        LDY_MapPlayerToken prefab = CreatePlayerTokenPrefab();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("playerTokenPrefab").objectReferenceValue = prefab;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[LDY_MapSceneBuilder] 플레이어 토큰을 MapUIController에 연결했습니다. Play 하면 자동으로 생성됩니다.");
    }

    // 기존 씬에 별가루 배경만 추가하고 싶을 때 (씬을 통째로 다시 만들 필요 없음).
    // MapCanvas를 찾아서 그 밑에 Starfield를 Background 바로 뒤(=화면상 그 위)에 넣어줌.
    [MenuItem("LDY/Map/별가루 배경만 추가")]
    private static void AddStarfieldOnly()
    {
        GameObject canvasGO = GameObject.Find("MapCanvas");
        if (canvasGO == null)
        {
            EditorUtility.DisplayDialog("별가루 배경 추가", "'MapCanvas' 오브젝트를 찾을 수 없습니다.", "확인");
            return;
        }

        if (canvasGO.transform.Find("Starfield") != null)
        {
            EditorUtility.DisplayDialog("별가루 배경 추가", "이미 'Starfield'가 존재합니다.", "확인");
            return;
        }

        LDY_MapTheme theme = FindOrCreateTheme();
        CreateStarfield(canvasGO.transform, theme);

        Transform background = canvasGO.transform.Find("Background");
        if (background != null)
        {
            GameObject starfieldGO = canvasGO.transform.Find("Starfield").gameObject;
            starfieldGO.transform.SetSiblingIndex(background.GetSiblingIndex() + 1);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[LDY_MapSceneBuilder] Starfield를 MapCanvas 아래에 추가했습니다.");
    }

    // 기존 씬에 씬 전환(아이리스) 연출만 추가하고 싶을 때
    [MenuItem("LDY/Map/씬 전환 연출만 추가")]
    private static void AddSceneTransitionOnly()
    {
        if (Object.FindFirstObjectByType<LDY_SceneTransition>() != null)
        {
            EditorUtility.DisplayDialog("씬 전환 연출 추가", "이미 LDY_SceneTransition이 씬에 존재합니다.", "확인");
            return;
        }

        EnsureSceneTransition();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[LDY_MapSceneBuilder] 씬 전환 연출을 추가했습니다. Battle/Boss 노드를 클릭하면 자동으로 재생됩니다.");
    }

    // 씬 구조(Canvas/MapManager/MapUIController)는 그대로 두고 노드·라인 프리팹만 최신 구조로 다시 생성.
    // 프리팹 경로가 같으므로 MapUIController의 참조는 끊어지지 않지만, 프리팹에 직접 넣어둔
    // 아이콘 스프라이트 등 커스터마이징 값은 초기화되니 다시 넣어야 함.
    [MenuItem("LDY/Map/노드·라인 프리팹만 다시 생성")]
    private static void RebuildPrefabsOnly()
    {
        bool proceed = EditorUtility.DisplayDialog(
            "노드·라인 프리팹 재생성",
            "LDY_MapNodeView / LDY_MapLine 프리팹을 최신 구조로 다시 만듭니다.\n" +
            "Canvas, MapManager(노드 좌표/연결), MapUIController는 건드리지 않습니다.\n\n" +
            "단, 프리팹에 직접 넣어둔 타입별 아이콘 스프라이트 등은 초기화되니 다시 넣어야 합니다. 계속할까요?",
            "계속", "취소");
        if (!proceed) return;

        EnsureFolder(PrefabFolder);
        CreateNodePrefab();
        CreateLinePrefab();

        AssetDatabase.SaveAssets();
        Debug.Log("[LDY_MapSceneBuilder] 노드·라인 프리팹을 최신 구조로 재생성했습니다. 아이콘 스프라이트를 다시 연결해주세요.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string newFolderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, newFolderName);
    }

    private static LDY_MapTheme FindOrCreateTheme()
    {
        string[] guids = AssetDatabase.FindAssets("t:LDY_MapTheme");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<LDY_MapTheme>(AssetDatabase.GUIDToAssetPath(guids[0]));

        LDY_MapTheme theme = ScriptableObject.CreateInstance<LDY_MapTheme>();
        AssetDatabase.CreateAsset(theme, ThemeAssetPath);
        AssetDatabase.SaveAssets();
        return theme;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject go = new GameObject("EventSystem", typeof(EventSystem));
        Undo.RegisterCreatedObjectUndo(go, "Build Map Scene");

#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    private static Canvas CreateCanvas(out Image background, out RectTransform nodeContainer, out RectTransform lineContainer, LDY_MapTheme theme)
    {
        GameObject canvasGO = new GameObject("MapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Map Scene");

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        background = CreateStretchImage("Background", canvasGO.transform);
        background.color = theme != null ? theme.navyBackground : Color.black;
        background.raycastTarget = false;

        CreateStarfield(canvasGO.transform, theme);

        lineContainer = CreateCenterAnchor("LineContainer", canvasGO.transform);
        nodeContainer = CreateCenterAnchor("NodeContainer", canvasGO.transform);

        return canvas;
    }

    private static void CreateStarfield(Transform parent, LDY_MapTheme theme)
    {
        GameObject go = new GameObject("Starfield", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        LDY_UIStarfield starfield = go.AddComponent<LDY_UIStarfield>();
        SerializedObject so = new SerializedObject(starfield);
        so.FindProperty("theme").objectReferenceValue = theme;
        so.FindProperty("container").objectReferenceValue = rt;
        so.ApplyModifiedProperties();

        LDY_UIShootingStars shootingStars = go.AddComponent<LDY_UIShootingStars>();
        SerializedObject shootingSo = new SerializedObject(shootingStars);
        shootingSo.FindProperty("container").objectReferenceValue = rt;
        shootingSo.ApplyModifiedProperties();
    }

    private static Image CreateStretchImage(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return go.GetComponent<Image>();
    }

    private static RectTransform CreateCenterAnchor(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        return rt;
    }

    private static LDY_MapNodeView CreateNodePrefab()
    {
        // 루트에는 Image를 두지 않음: 자식은 항상 부모(root)의 그래픽보다 나중에 그려지므로,
        // Glow가 Background보다 "뒤"에 보이려면 둘 다 자식으로 두고 형제 순서로 그리기 순서를 제어해야 함
        GameObject root = new GameObject("LDY_MapNodeView", typeof(RectTransform), typeof(Button));
        RectTransform rootRt = (RectTransform)root.transform;
        rootRt.anchorMin = rootRt.anchorMax = rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.sizeDelta = new Vector2(80f, 80f);

        Image glow = CreateStretchImage("Glow", root.transform);
        glow.raycastTarget = false;
        RectTransform glowRt = glow.rectTransform;
        glowRt.anchorMin = new Vector2(-0.4f, -0.4f);
        glowRt.anchorMax = new Vector2(1.4f, 1.4f);
        glowRt.offsetMin = Vector2.zero;
        glowRt.offsetMax = Vector2.zero;

        Image background = CreateStretchImage("Background", root.transform);
        background.color = Color.white;

        Image border = CreateStretchImage("Border", root.transform);
        border.raycastTarget = false;

        Image icon = CreateStretchImage("Icon", root.transform);
        icon.raycastTarget = false;
        RectTransform iconRt = icon.rectTransform;
        iconRt.anchorMin = new Vector2(0.2f, 0.2f);
        iconRt.anchorMax = new Vector2(0.8f, 0.8f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;

        Image cornerStar = CreateStretchImage("CornerStar", root.transform);
        cornerStar.raycastTarget = false;
        RectTransform starRt = cornerStar.rectTransform;
        starRt.anchorMin = starRt.anchorMax = new Vector2(1f, 1f);
        starRt.pivot = new Vector2(0.5f, 0.5f);
        starRt.sizeDelta = new Vector2(16f, 16f);
        starRt.anchoredPosition = new Vector2(-4f, -4f);

        Button button = root.GetComponent<Button>();
        button.targetGraphic = background;

        LDY_MapNodeView view = root.AddComponent<LDY_MapNodeView>();
        SerializedObject so = new SerializedObject(view);
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("glowImage").objectReferenceValue = glow;
        so.FindProperty("backgroundImage").objectReferenceValue = background;
        so.FindProperty("borderImage").objectReferenceValue = border;
        so.FindProperty("iconImage").objectReferenceValue = icon;
        so.FindProperty("cornerStarImage").objectReferenceValue = cornerStar;
        so.ApplyModifiedProperties();

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, NodePrefabPath);
        Object.DestroyImmediate(root);

        return prefabAsset.GetComponent<LDY_MapNodeView>();
    }

    private static Image CreateLinePrefab()
    {
        GameObject root = new GameObject("LDY_MapLine", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(100f, 2f);

        Image image = root.GetComponent<Image>();
        image.raycastTarget = false;

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, LinePrefabPath);
        Object.DestroyImmediate(root);

        return prefabAsset.GetComponent<Image>();
    }

    private const string PlayerTokenPrefabPath = PrefabFolder + "/LDY_MapPlayerToken.prefab";

    private static LDY_MapPlayerToken CreatePlayerTokenPrefab()
    {
        GameObject root = new GameObject("LDY_MapPlayerToken", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(28f, 28f);

        Image image = root.GetComponent<Image>();
        image.sprite = LDY_ProceduralSprite.Sparkle;
        image.color = new Color32(0xE9, 0xE2, 0xD6, 0xFF);
        image.raycastTarget = false;

        root.AddComponent<LDY_MapPlayerToken>();

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, PlayerTokenPrefabPath);
        Object.DestroyImmediate(root);

        return prefabAsset.GetComponent<LDY_MapPlayerToken>();
    }

    private static void WireController(LDY_MapUIController controller, LDY_MapTheme theme, LDY_MapManager manager,
        RectTransform nodeContainer, RectTransform lineContainer, LDY_MapNodeView nodeViewPrefab, Image linePrefab,
        Image background, LDY_MapPlayerToken playerTokenPrefab)
    {
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("theme").objectReferenceValue = theme;
        so.FindProperty("mapManager").objectReferenceValue = manager;
        so.FindProperty("nodeContainer").objectReferenceValue = nodeContainer;
        so.FindProperty("lineContainer").objectReferenceValue = lineContainer;
        so.FindProperty("nodeViewPrefab").objectReferenceValue = nodeViewPrefab;
        so.FindProperty("linePrefab").objectReferenceValue = linePrefab;
        so.FindProperty("backgroundPanel").objectReferenceValue = background;
        so.FindProperty("playerTokenPrefab").objectReferenceValue = playerTokenPrefab;
        so.ApplyModifiedProperties();
    }
}
