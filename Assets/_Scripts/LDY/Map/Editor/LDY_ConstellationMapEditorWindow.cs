using UnityEditor;
using UnityEngine;

// LDY_MapManager의 nodePositions / nodeTypes / connections를 2D 캔버스에서 시각적으로 편집하는 툴
// (좌표는 UI anchoredPosition 기준. 씬 오브젝트 트랜스폼과는 무관한 독립 좌표계)
// 한 노드가 여러 연결을 가지면 분기(두 갈래 이상)가 됨
public class LDY_ConstellationMapEditorWindow : EditorWindow
{
    private enum EditMode { Select, AddNode, Connect }

    private LDY_MapManager manager;

    private Vector2 panOffset;
    private float zoom = 1f;
    private const float NodeRadius = 8f;
    private const float HitRadius = 10f;

    private EditMode mode = EditMode.Select;
    private int selectedIndex = -1;
    private int draggingIndex = -1;
    private int connectFrom = -1;

    private bool isPanning;
    private Vector2 panStartMouse;
    private Vector2 panStartOffset;

    [MenuItem("LDY/Map/Constellation Map Editor")]
    private static void OpenFromMenu()
    {
        LDY_MapManager auto = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<LDY_MapManager>()
            : null;
        Open(auto);
    }

    public static void Open(LDY_MapManager target)
    {
        LDY_ConstellationMapEditorWindow window = GetWindow<LDY_ConstellationMapEditorWindow>("별자리 맵 에디터");
        if (target != null) window.manager = target;
        window.Show();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (manager == null)
        {
            EditorGUILayout.HelpBox("편집할 LDY_MapManager를 지정하세요.", MessageType.Info);
            return;
        }

        SerializedObject so = new SerializedObject(manager);
        so.Update();
        SerializedProperty posProp = so.FindProperty("nodePositions");
        SerializedProperty typeProp = so.FindProperty("nodeTypes");
        SerializedProperty connProp = so.FindProperty("connections");

        EditorGUILayout.BeginHorizontal();
        Rect canvasRect = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawCanvas(canvasRect, posProp, typeProp, connProp);
        DrawSidePanel(posProp, typeProp, connProp);
        EditorGUILayout.EndHorizontal();

        so.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(manager);
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUILayout.LabelField("대상", GUILayout.Width(30));
        manager = (LDY_MapManager)EditorGUILayout.ObjectField(manager, typeof(LDY_MapManager), true, GUILayout.Width(200));

        GUI.backgroundColor = mode == EditMode.AddNode ? new Color(0.85f, 0.66f, 0.3f) : Color.white;
        if (GUILayout.Button(mode == EditMode.AddNode ? "노드 추가 모드 (ON)" : "노드 추가 모드", EditorStyles.toolbarButton, GUILayout.Width(140)))
        {
            mode = mode == EditMode.AddNode ? EditMode.Select : EditMode.AddNode;
            connectFrom = -1;
        }

        GUI.backgroundColor = mode == EditMode.Connect ? new Color(0.72f, 0.48f, 0.49f) : Color.white;
        if (GUILayout.Button(mode == EditMode.Connect ? "연결 모드 (ON)" : "연결 모드 (분기)", EditorStyles.toolbarButton, GUILayout.Width(140)))
        {
            mode = mode == EditMode.Connect ? EditMode.Select : EditMode.Connect;
            connectFrom = -1;
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("뷰 초기화", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            panOffset = Vector2.zero;
            zoom = 1f;
        }

        GUILayout.FlexibleSpace();

        string hint = mode == EditMode.Connect
            ? "연결 모드: 시작 노드 클릭 → 도착 노드 클릭 (같은 노드를 다시 클릭하면 취소)"
            : "좌클릭: 선택/드래그   |   우클릭 드래그: 이동   |   휠: 확대/축소";
        EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCanvas(Rect canvasRect, SerializedProperty posProp, SerializedProperty typeProp, SerializedProperty connProp)
    {
        EditorGUI.DrawRect(canvasRect, new Color(0.07f, 0.06f, 0.14f));

        Handles.BeginGUI();
        DrawGrid(canvasRect);
        DrawConnections(canvasRect, posProp, connProp);
        Handles.EndGUI();

        DrawNodes(canvasRect, posProp, typeProp);
        HandleInput(canvasRect, posProp, typeProp, connProp);
    }

    private void DrawGrid(Rect rect)
    {
        float step = 100f * zoom;
        if (step < 4f) return;

        Handles.color = new Color(1f, 1f, 1f, 0.05f);

        float originX = rect.x + rect.width * 0.5f + panOffset.x;
        for (float x = Mod(originX - rect.x, step) + rect.x; x < rect.xMax; x += step)
            Handles.DrawLine(new Vector3(x, rect.yMin), new Vector3(x, rect.yMax));

        float originY = rect.y + rect.height * 0.5f + panOffset.y;
        for (float y = Mod(originY - rect.y, step) + rect.y; y < rect.yMax; y += step)
            Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));
    }

    private static float Mod(float a, float b)
    {
        float r = a % b;
        return r < 0 ? r + b : r;
    }

    private void DrawConnections(Rect rect, SerializedProperty posProp, SerializedProperty connProp)
    {
        if (connProp.arraySize == 0) return;

        Handles.color = new Color(0.85f, 0.66f, 0.3f, 0.9f);
        for (int i = 0; i < connProp.arraySize; i++)
        {
            SerializedProperty c = connProp.GetArrayElementAtIndex(i);
            int from = c.FindPropertyRelative("fromIndex").intValue;
            int to = c.FindPropertyRelative("toIndex").intValue;
            if (from < 0 || from >= posProp.arraySize || to < 0 || to >= posProp.arraySize) continue;

            Vector2 a = NodeToScreen(posProp.GetArrayElementAtIndex(from).vector2Value, rect);
            Vector2 b = NodeToScreen(posProp.GetArrayElementAtIndex(to).vector2Value, rect);
            Handles.DrawAAPolyLine(2.5f, a, b);
        }
    }

    private void DrawNodes(Rect rect, SerializedProperty posProp, SerializedProperty typeProp)
    {
        for (int i = 0; i < posProp.arraySize; i++)
        {
            Vector2 screenPos = NodeToScreen(posProp.GetArrayElementAtIndex(i).vector2Value, rect);
            if (!rect.Contains(screenPos) && Vector2.Distance(screenPos, rect.center) > rect.width) continue;

            LDY_NodeType type = (LDY_NodeType)typeProp.GetArrayElementAtIndex(i).enumValueIndex;

            Handles.BeginGUI();
            if (i == selectedIndex || i == connectFrom)
            {
                Handles.color = i == connectFrom ? new Color(0.72f, 0.48f, 0.49f) : Color.white;
                Handles.DrawSolidDisc(screenPos, Vector3.forward, NodeRadius + 3f);
            }
            Handles.color = EditorTypeColor(type);
            Handles.DrawSolidDisc(screenPos, Vector3.forward, NodeRadius);
            Handles.EndGUI();

            GUI.color = Color.white;
            GUI.Label(new Rect(screenPos.x - 20, screenPos.y + NodeRadius + 2, 60, 16), $"{i}:{ShortType(type)}", EditorStyles.miniLabel);
        }
    }

    private void HandleInput(Rect canvasRect, SerializedProperty posProp, SerializedProperty typeProp, SerializedProperty connProp)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && canvasRect.Contains(e.mousePosition))
        {
            if (e.button == 0)
            {
                if (mode == EditMode.AddNode)
                {
                    Vector2 nodePos = ScreenToNode(e.mousePosition, canvasRect);
                    PlaceNewNode(posProp, typeProp, connProp, nodePos);
                }
                else if (mode == EditMode.Connect)
                {
                    HandleConnectClick(posProp, connProp, canvasRect, e.mousePosition);
                }
                else
                {
                    int hit = HitTestNode(posProp, canvasRect, e.mousePosition);
                    selectedIndex = hit;
                    draggingIndex = hit;
                    if (hit >= 0) Undo.RecordObject(manager, "Move Constellation Node");
                }
                e.Use();
                Repaint();
            }
            else if (e.button == 1 || e.button == 2)
            {
                isPanning = true;
                panStartMouse = e.mousePosition;
                panStartOffset = panOffset;
                e.Use();
            }
        }
        else if (e.type == EventType.MouseDrag)
        {
            if (draggingIndex >= 0)
            {
                posProp.GetArrayElementAtIndex(draggingIndex).vector2Value = ScreenToNode(e.mousePosition, canvasRect);
                e.Use();
                Repaint();
            }
            else if (isPanning)
            {
                panOffset = panStartOffset + (e.mousePosition - panStartMouse);
                e.Use();
                Repaint();
            }
        }
        else if (e.type == EventType.MouseUp)
        {
            draggingIndex = -1;
            isPanning = false;
        }
        else if (e.type == EventType.ScrollWheel && canvasRect.Contains(e.mousePosition))
        {
            zoom = Mathf.Clamp(zoom - e.delta.y * 0.02f, 0.2f, 4f);
            e.Use();
            Repaint();
        }
    }

    private void HandleConnectClick(SerializedProperty posProp, SerializedProperty connProp, Rect canvasRect, Vector2 mousePos)
    {
        int hit = HitTestNode(posProp, canvasRect, mousePos);

        if (hit < 0)
        {
            connectFrom = -1;
        }
        else if (connectFrom < 0)
        {
            connectFrom = hit;
        }
        else if (hit == connectFrom)
        {
            connectFrom = -1;
        }
        else
        {
            AddConnection(connProp, connectFrom, hit);
            connectFrom = -1;
        }
    }

    private int HitTestNode(SerializedProperty posProp, Rect canvasRect, Vector2 mousePos)
    {
        for (int i = posProp.arraySize - 1; i >= 0; i--)
        {
            Vector2 screenPos = NodeToScreen(posProp.GetArrayElementAtIndex(i).vector2Value, canvasRect);
            if (Vector2.Distance(mousePos, screenPos) <= HitRadius) return i;
        }
        return -1;
    }

    private Vector2 NodeToScreen(Vector2 nodePos, Rect canvasRect)
    {
        Vector2 flipped = new Vector2(nodePos.x, -nodePos.y);
        return canvasRect.center + panOffset + flipped * zoom;
    }

    private Vector2 ScreenToNode(Vector2 screenPos, Rect canvasRect)
    {
        Vector2 local = (screenPos - canvasRect.center - panOffset) / zoom;
        return new Vector2(local.x, -local.y);
    }

    private void DrawSidePanel(SerializedProperty posProp, SerializedProperty typeProp, SerializedProperty connProp)
    {
        GUILayout.BeginVertical(GUILayout.Width(220));

        EditorGUILayout.LabelField("별자리 노드", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"노드 수: {posProp.arraySize}   연결 수: {connProp.arraySize}");
        EditorGUILayout.Space();

        if (selectedIndex >= 0 && selectedIndex < posProp.arraySize)
        {
            SerializedProperty posElem = posProp.GetArrayElementAtIndex(selectedIndex);
            SerializedProperty typeElem = typeProp.GetArrayElementAtIndex(selectedIndex);

            EditorGUILayout.LabelField($"선택된 노드 #{selectedIndex}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Vector2 newPos = EditorGUILayout.Vector2Field("위치", posElem.vector2Value);
            LDY_NodeType newType = (LDY_NodeType)EditorGUILayout.EnumPopup("타입", (LDY_NodeType)typeElem.enumValueIndex);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Edit Constellation Node");
                posElem.vector2Value = newPos;
                typeElem.enumValueIndex = (int)newType;
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(selectedIndex <= 0))
            {
                if (GUILayout.Button("↑ 배열 순서 앞으로")) MoveNode(posProp, typeProp, connProp, selectedIndex, -1);
            }
            using (new EditorGUI.DisabledScope(selectedIndex >= posProp.arraySize - 1))
            {
                if (GUILayout.Button("↓ 배열 순서 뒤로")) MoveNode(posProp, typeProp, connProp, selectedIndex, 1);
            }

            if (GUILayout.Button("여기서 새 분기 노드 추가")) AddBranchFrom(posProp, typeProp, connProp, selectedIndex);

            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("이 노드 삭제")) DeleteNode(posProp, typeProp, connProp, selectedIndex);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();
            DrawOutgoingConnections(posProp, connProp, selectedIndex);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "캔버스에서 노드를 클릭해 선택하세요.\n\n" +
                "· 노드 추가 모드: 캔버스 클릭 → 새 노드 추가 (선택 중이던 노드가 있으면 자동 연결됨)\n" +
                "· 연결 모드: 노드 두 개를 순서대로 클릭 → 연결 생성 (한 노드에서 2개 이상 연결하면 분기)",
                MessageType.Info);
        }

        GUILayout.FlexibleSpace();

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("전체 초기화"))
        {
            if (EditorUtility.DisplayDialog("전체 초기화", "모든 노드와 연결을 삭제하시겠습니까?", "삭제", "취소"))
                ClearAll(posProp, typeProp, connProp);
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndVertical();
    }

    private void DrawOutgoingConnections(SerializedProperty posProp, SerializedProperty connProp, int index)
    {
        EditorGUILayout.LabelField("이 노드에서 나가는 연결", EditorStyles.boldLabel);

        int deleteAt = -1;
        for (int i = 0; i < connProp.arraySize; i++)
        {
            SerializedProperty c = connProp.GetArrayElementAtIndex(i);
            SerializedProperty fromProp = c.FindPropertyRelative("fromIndex");
            SerializedProperty toProp = c.FindPropertyRelative("toIndex");
            if (fromProp.intValue != index) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"→ #{toProp.intValue}");
            if (GUILayout.Button("X", GUILayout.Width(22))) deleteAt = i;
            EditorGUILayout.EndHorizontal();
        }

        if (deleteAt >= 0)
        {
            Undo.RecordObject(manager, "Remove Connection");
            connProp.DeleteArrayElementAtIndex(deleteAt);
        }
    }

    private void PlaceNewNode(SerializedProperty posProp, SerializedProperty typeProp, SerializedProperty connProp, Vector2 pos)
    {
        Undo.RecordObject(manager, "Add Constellation Node");
        int index = posProp.arraySize;
        posProp.arraySize++;
        typeProp.arraySize++;
        posProp.GetArrayElementAtIndex(index).vector2Value = pos;
        typeProp.GetArrayElementAtIndex(index).enumValueIndex = (int)LDY_NodeType.Battle;

        if (selectedIndex >= 0 && selectedIndex < index)
            AddConnection(connProp, selectedIndex, index);

        selectedIndex = index;
    }

    private void AddBranchFrom(SerializedProperty posProp, SerializedProperty typeProp, SerializedProperty connProp, int fromIndex)
    {
        Undo.RecordObject(manager, "Add Branch Node");

        Vector2 basePos = posProp.GetArrayElementAtIndex(fromIndex).vector2Value;
        int outgoing = CountOutgoing(connProp, fromIndex);
        Vector2 offset = outgoing % 2 == 0 ? new Vector2(50f, -40f) : new Vector2(50f, 40f);

        int index = posProp.arraySize;
        posProp.arraySize++;
        typeProp.arraySize++;
        posProp.GetArrayElementAtIndex(index).vector2Value = basePos + offset;
        typeProp.GetArrayElementAtIndex(index).enumValueIndex = (int)LDY_NodeType.Battle;

        AddConnection(connProp, fromIndex, index);
        selectedIndex = index;
    }

    private int CountOutgoing(SerializedProperty connProp, int fromIndex)
    {
        int count = 0;
        for (int i = 0; i < connProp.arraySize; i++)
            if (connProp.GetArrayElementAtIndex(i).FindPropertyRelative("fromIndex").intValue == fromIndex) count++;
        return count;
    }

    private void AddConnection(SerializedProperty connProp, int from, int to)
    {
        for (int i = 0; i < connProp.arraySize; i++)
        {
            SerializedProperty c = connProp.GetArrayElementAtIndex(i);
            if (c.FindPropertyRelative("fromIndex").intValue == from && c.FindPropertyRelative("toIndex").intValue == to)
                return;
        }

        Undo.RecordObject(manager, "Connect Constellation Nodes");
        int index = connProp.arraySize;
        connProp.arraySize++;
        SerializedProperty elem = connProp.GetArrayElementAtIndex(index);
        elem.FindPropertyRelative("fromIndex").intValue = from;
        elem.FindPropertyRelative("toIndex").intValue = to;
    }

    private void DeleteNode(SerializedProperty posProp, SerializedProperty typeProp, SerializedProperty connProp, int index)
    {
        Undo.RecordObject(manager, "Delete Constellation Node");

        RemoveConnectionsForDelete(connProp, index);

        posProp.DeleteArrayElementAtIndex(index);
        typeProp.DeleteArrayElementAtIndex(index);

        selectedIndex = -1;
        draggingIndex = -1;
        connectFrom = -1;
    }

    private void RemoveConnectionsForDelete(SerializedProperty connProp, int deleteIndex)
    {
        for (int i = connProp.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty c = connProp.GetArrayElementAtIndex(i);
            SerializedProperty fromProp = c.FindPropertyRelative("fromIndex");
            SerializedProperty toProp = c.FindPropertyRelative("toIndex");
            int from = fromProp.intValue;
            int to = toProp.intValue;

            if (from == deleteIndex || to == deleteIndex)
            {
                connProp.DeleteArrayElementAtIndex(i);
                continue;
            }

            if (from > deleteIndex) fromProp.intValue = from - 1;
            if (to > deleteIndex) toProp.intValue = to - 1;
        }
    }

    private void MoveNode(SerializedProperty posProp, SerializedProperty typeProp, SerializedProperty connProp, int index, int direction)
    {
        int other = index + direction;
        if (other < 0 || other >= posProp.arraySize) return;

        Undo.RecordObject(manager, "Reorder Constellation Node");

        SerializedProperty posA = posProp.GetArrayElementAtIndex(index);
        SerializedProperty posB = posProp.GetArrayElementAtIndex(other);
        (posA.vector2Value, posB.vector2Value) = (posB.vector2Value, posA.vector2Value);

        SerializedProperty typeA = typeProp.GetArrayElementAtIndex(index);
        SerializedProperty typeB = typeProp.GetArrayElementAtIndex(other);
        (typeA.enumValueIndex, typeB.enumValueIndex) = (typeB.enumValueIndex, typeA.enumValueIndex);

        RemapConnectionsForSwap(connProp, index, other);

        selectedIndex = other;
    }

    private void RemapConnectionsForSwap(SerializedProperty connProp, int a, int b)
    {
        for (int i = 0; i < connProp.arraySize; i++)
        {
            SerializedProperty c = connProp.GetArrayElementAtIndex(i);
            SerializedProperty fromProp = c.FindPropertyRelative("fromIndex");
            SerializedProperty toProp = c.FindPropertyRelative("toIndex");

            if (fromProp.intValue == a) fromProp.intValue = b;
            else if (fromProp.intValue == b) fromProp.intValue = a;

            if (toProp.intValue == a) toProp.intValue = b;
            else if (toProp.intValue == b) toProp.intValue = a;
        }
    }

    private void ClearAll(SerializedProperty posProp, SerializedProperty typeProp, SerializedProperty connProp)
    {
        Undo.RecordObject(manager, "Clear Constellation Nodes");
        posProp.ClearArray();
        typeProp.ClearArray();
        connProp.ClearArray();
        selectedIndex = -1;
        connectFrom = -1;
    }

    private static string ShortType(LDY_NodeType type)
    {
        switch (type)
        {
            case LDY_NodeType.Start: return "St";
            case LDY_NodeType.Battle: return "Ba";
            case LDY_NodeType.Event: return "Ev";
            case LDY_NodeType.Shop: return "Sh";
            case LDY_NodeType.Boss: return "Bo";
            default: return "?";
        }
    }

    // 편집 툴 전용 시인성 색상 (실제 게임 내 노드 색상은 LDY_MapTheme의 상태값을 따름)
    private static Color EditorTypeColor(LDY_NodeType type)
    {
        switch (type)
        {
            case LDY_NodeType.Start: return new Color(0.92f, 0.92f, 0.9f);
            case LDY_NodeType.Battle: return new Color(0.88f, 0.32f, 0.32f);
            case LDY_NodeType.Event: return new Color(0.88f, 0.63f, 0.32f);
            case LDY_NodeType.Shop: return new Color(0.32f, 0.79f, 0.48f);
            case LDY_NodeType.Boss: return new Color(0.69f, 0.32f, 0.88f);
            default: return Color.gray;
        }
    }
}
