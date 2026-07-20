using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using _Scripts.LDY;

namespace _Scripts.LDY.Editor
{
    // 사용법: 유니티 상단 메뉴 "LDY > Build Test Scene" 클릭.
    // 큐브 기반 8x8 보드, 테스트용 기물 6개, 시스템 오브젝트(LDY_GameSystems)를 전부 자동 생성/배선한다.
    // 재실행하면 이전에 생성한 LDY_AutoScene을 지우고 다시 만들기 때문에 여러 번 눌러도 안전하다.
    public static class LDY_SceneBuilder
    {
        private const int Size = 8;
        private const float CellSize = 1f;
        private const string RootName = "LDY_AutoScene";
        private const string BoardLayerName = "LDY_Board";

        private static readonly string[] KeepNames = { "Main Camera", "Directional Light" };

        private class Systems
        {
            public LDY_BoardManager boardManager;
            public LDY_MoveSystem moveSystem;
            public LDY_AttackSystem attackSystem;
            public LDY_TileHighlighter highlighter;
            public LDY_SelectionController selection;
            public LDY_EnemyAI enemyAI;
            public LDY_TurnManager turnManager;
            public LDY_ActionPointManager actionPoints;
        }

        [MenuItem("LDY/Build Test Scene")]
        public static void BuildScene()
        {
            ClearScene();

            int boardLayer = EnsureLayer(BoardLayerName);

            var root = new GameObject(RootName);

            var boardRoot = BuildBoard(root.transform, boardLayer);
            var systems = BuildSystems(root.transform, boardRoot, boardLayer);
            BuildHighlightPrefabs(systems);
            BuildPieces(root.transform, boardRoot);
            BuildTurnUI(root.transform, systems.turnManager, systems.actionPoints);
            FrameCamera(boardRoot);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("LDY 테스트 씬 생성 완료 (LDY_AutoScene). Play를 눌러 확인하세요.");
        }

        // 메뉴 재실행 시 이전에 씬에 있던 오브젝트(직접 배치한 체스판/기물 포함)를 전부 지우고 새로 만든다.
        // Main Camera / Directional Light만 남겨둔다.
        private static void ClearScene()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
            {
                if (System.Array.IndexOf(KeepNames, go.name) >= 0) continue;
                Object.DestroyImmediate(go);
            }
        }

        private static int EnsureLayer(string layerName)
        {
            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            var tagManager = new SerializedObject(tagManagerAssets[0]);
            var layersProp = tagManager.FindProperty("layers");

            for (int i = 8; i < layersProp.arraySize; i++)
            {
                if (layersProp.GetArrayElementAtIndex(i).stringValue == layerName)
                    return i;
            }

            for (int i = 8; i < layersProp.arraySize; i++)
            {
                var sp = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }

            Debug.LogWarning("빈 레이어 슬롯이 없어 Default 레이어를 사용합니다.");
            return 0;
        }

        private static Transform BuildBoard(Transform parent, int boardLayer)
        {
            var boardRoot = new GameObject("LDY_Board");
            boardRoot.transform.SetParent(parent);
            boardRoot.transform.position = Vector3.zero;

            var whiteMat = CreateMaterial(new Color(0.85f, 0.85f, 0.85f));
            var blackMat = CreateMaterial(new Color(0.1f, 0.1f, 0.1f));

            for (int x = 0; x < Size; x++)
            {
                for (int z = 0; z < Size; z++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Tile_{x}_{z}";
                    tile.transform.SetParent(boardRoot.transform);
                    tile.transform.localPosition = new Vector3(x * CellSize, -0.05f, z * CellSize);
                    tile.transform.localScale = new Vector3(CellSize * 0.98f, 0.1f, CellSize * 0.98f);
                    tile.layer = boardLayer;
                    tile.GetComponent<MeshRenderer>().sharedMaterial = (x + z) % 2 == 0 ? blackMat : whiteMat;
                }
            }

            return boardRoot.transform;
        }

        private static Systems BuildSystems(Transform parent, Transform boardRoot, int boardLayer)
        {
            var go = new GameObject("LDY_GameSystems");
            go.transform.SetParent(parent);

            var systems = new Systems
            {
                boardManager = go.AddComponent<LDY_BoardManager>(),
                moveSystem = go.AddComponent<LDY_MoveSystem>(),
                attackSystem = go.AddComponent<LDY_AttackSystem>(),
                highlighter = go.AddComponent<LDY_TileHighlighter>(),
                selection = go.AddComponent<LDY_SelectionController>(),
                enemyAI = go.AddComponent<LDY_EnemyAI>(),
                turnManager = go.AddComponent<LDY_TurnManager>(),
                actionPoints = go.AddComponent<LDY_ActionPointManager>()
            };

            SetPrivateField(systems.boardManager, "boardOrigin", boardRoot);
            SetPrivateField(systems.boardManager, "cellSize", CellSize);

            SetPrivateField(systems.moveSystem, "board", systems.boardManager);
            SetPrivateField(systems.moveSystem, "actionPoints", systems.actionPoints);

            SetPrivateField(systems.attackSystem, "board", systems.boardManager);
            SetPrivateField(systems.attackSystem, "actionPoints", systems.actionPoints);

            SetPrivateField(systems.highlighter, "board", systems.boardManager);

            SetPrivateField(systems.selection, "board", systems.boardManager);
            SetPrivateField(systems.selection, "moveSystem", systems.moveSystem);
            SetPrivateField(systems.selection, "attackSystem", systems.attackSystem);
            SetPrivateField(systems.selection, "highlighter", systems.highlighter);
            SetPrivateField(systems.selection, "boardLayerMask", (LayerMask)(1 << boardLayer));
            SetPrivateField(systems.selection, "turnManager", systems.turnManager);

            SetPrivateField(systems.enemyAI, "board", systems.boardManager);
            SetPrivateField(systems.enemyAI, "moveSystem", systems.moveSystem);
            SetPrivateField(systems.enemyAI, "attackSystem", systems.attackSystem);
            SetPrivateField(systems.enemyAI, "actionPoints", systems.actionPoints);

            SetPrivateField(systems.turnManager, "enemyAI", systems.enemyAI);
            SetPrivateField(systems.turnManager, "moveSystem", systems.moveSystem);
            SetPrivateField(systems.turnManager, "attackSystem", systems.attackSystem);
            SetPrivateField(systems.turnManager, "actionPoints", systems.actionPoints);

            return systems;
        }

        private static void BuildHighlightPrefabs(Systems systems)
        {
            EnsureFolder("Assets/_Prefabs/LDY");

            // 기존 프리팹/머티리얼이 남아있으면(특히 이전에 깨진 머티리얼) 완전히 지우고 새로 만든다.
            DeleteAssetIfExists("Assets/_Prefabs/LDY/LDY_MoveHighlight.prefab");
            DeleteAssetIfExists("Assets/_Prefabs/LDY/LDY_AttackHighlight.prefab");
            DeleteAssetIfExists("Assets/_Prefabs/LDY/LDY_MoveHighlight_Mat.mat");
            DeleteAssetIfExists("Assets/_Prefabs/LDY/LDY_AttackHighlight_Mat.mat");
            AssetDatabase.Refresh();

            // 근접 기물처럼 이동 범위와 공격 범위가 완전히 겹치는 경우, 이동(작은 크기)이 위에 떠도
            // 공격(큰 크기) 하이라이트의 테두리가 가려지지 않도록 이동 쪽을 더 작게 만든다.
            var moveTemplate = CreateHighlightTemplate("LDY_MoveHighlight", new Color(0.65f, 0.25f, 0.95f),
                0.55f, "Assets/_Prefabs/LDY/LDY_MoveHighlight_Mat.mat");
            var attackTemplate = CreateHighlightTemplate("LDY_AttackHighlight", new Color(1f, 0.85f, 0f),
                0.9f, "Assets/_Prefabs/LDY/LDY_AttackHighlight_Mat.mat");

            var movePrefab = PrefabUtility.SaveAsPrefabAsset(moveTemplate, "Assets/_Prefabs/LDY/LDY_MoveHighlight.prefab");
            var attackPrefab = PrefabUtility.SaveAsPrefabAsset(attackTemplate, "Assets/_Prefabs/LDY/LDY_AttackHighlight.prefab");

            Object.DestroyImmediate(moveTemplate);
            Object.DestroyImmediate(attackTemplate);

            SetPrivateField(systems.highlighter, "moveHighlightPrefab", movePrefab);
            SetPrivateField(systems.highlighter, "attackHighlightPrefab", attackPrefab);
        }

        private static void DeleteAssetIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }

        private static GameObject CreateHighlightTemplate(string name, Color color, float sizeRatio, string materialPath)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.localScale = new Vector3(CellSize * sizeRatio, 0.02f, CellSize * sizeRatio);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = CreateHighlightMaterial(color, materialPath);
            return go;
        }

        private static void BuildPieces(Transform parent, Transform boardRoot)
        {
            var piecesRoot = new GameObject("LDY_Pieces");
            piecesRoot.transform.SetParent(parent);

            var playerMat = CreateMaterial(Color.white);
            var enemyMat = CreateMaterial(new Color(0.6f, 0.05f, 0.05f));

            CreatePiece(piecesRoot.transform, boardRoot, playerMat, "Player_Melee",
                new Vector3Int(1, 0, 0), LDY_Team.Player, LDY_RangeType.Melee, 2, 6);
            CreatePiece(piecesRoot.transform, boardRoot, playerMat, "Player_Ranged",
                new Vector3Int(3, 0, 0), LDY_Team.Player, LDY_RangeType.Ranged, 2, 5);
            CreatePiece(piecesRoot.transform, boardRoot, playerMat, "Player_Jump",
                new Vector3Int(5, 0, 1), LDY_Team.Player, LDY_RangeType.Jump, 3, 4);

            CreatePiece(piecesRoot.transform, boardRoot, enemyMat, "Enemy_Melee",
                new Vector3Int(2, 0, 1), LDY_Team.Enemy, LDY_RangeType.Melee, 2, 5);
            CreatePiece(piecesRoot.transform, boardRoot, enemyMat, "Enemy_Ranged",
                new Vector3Int(4, 0, 5), LDY_Team.Enemy, LDY_RangeType.Ranged, 2, 6);
            CreatePiece(piecesRoot.transform, boardRoot, enemyMat, "Enemy_Jump",
                new Vector3Int(6, 0, 7), LDY_Team.Enemy, LDY_RangeType.Jump, 3, 4);
        }

        private static void CreatePiece(Transform parent, Transform boardRoot, Material mat, string name,
            Vector3Int pos, LDY_Team team, LDY_RangeType rangeType, int atk, int hp)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localScale = new Vector3(CellSize * 0.6f, CellSize * 0.6f, CellSize * 0.6f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var animal = go.AddComponent<LDY_Animal>();
            animal.pos = pos;
            animal.team = team;
            animal.rangeType = rangeType;
            animal.baseAtk = atk;
            animal.hp = hp;
            animal.modelTransform = go.transform;

            // 에디터에서 미리 보기용 배치. 실제 등록/정확한 스냅은 Play 시 LDY_BoardManager.Awake()가 수행한다.
            go.transform.position = boardRoot.position + new Vector3(pos.x * CellSize, CellSize * 0.3f, pos.z * CellSize);
        }

        private static void BuildTurnUI(Transform parent, LDY_TurnManager turnManager, LDY_ActionPointManager actionPoints)
        {
            var canvasGO = new GameObject("LDY_UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(parent);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var textGO = new GameObject("TurnLabel", typeof(Text));
            textGO.transform.SetParent(canvasGO.transform);

            var text = textGO.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 48;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.UpperCenter;
            text.text = "Player Turn";

            var rect = textGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -30f);
            rect.sizeDelta = new Vector2(600f, 80f);

            var indicator = textGO.AddComponent<LDY_TurnIndicatorUI>();
            SetPrivateField(indicator, "turnManager", turnManager);
            SetPrivateField(indicator, "actionPoints", actionPoints);
            SetPrivateField(indicator, "label", text);
        }

        private static void FrameCamera(Transform boardRoot)
        {
            var cam = Camera.main;
            if (cam == null) return;

            float center = (Size - 1) * CellSize * 0.5f;
            cam.transform.position = boardRoot.position + new Vector3(center, 8f, -4f);
            cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        }

        private static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(shader) { color = color };
        }

        // URP의 투명(Transparent) 서페이스 키워드 조합이 프로젝트 셰이더 변형에 따라 마젠타(셰이더 오류)로
        // 나오는 경우가 있어, 하이라이트는 안전하게 불투명 Unlit 재질로 만든다.
        // 프리팹에 임베드된 서브에셋 대신 독립된 .mat 파일로 저장해 캐시/오버라이트 문제를 피한다.
        private static Material CreateHighlightMaterial(Color color, string assetPath)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");
            var opaque = new Color(color.r, color.g, color.b, 1f);
            var mat = new Material(shader) { color = opaque };
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", opaque);

            AssetDatabase.CreateAsset(mat, assetPath);
            return AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"{target.GetType().Name}에서 필드 '{fieldName}'를 찾지 못했습니다.");
                return;
            }

            switch (value)
            {
                case UnityEngine.Object objValue:
                    prop.objectReferenceValue = objValue;
                    break;
                case float floatValue:
                    prop.floatValue = floatValue;
                    break;
                case LayerMask layerMaskValue:
                    prop.intValue = layerMaskValue.value;
                    break;
                default:
                    Debug.LogWarning($"지원하지 않는 값 타입: {value.GetType()}");
                    break;
            }

            so.ApplyModifiedProperties();
        }
    }
}
