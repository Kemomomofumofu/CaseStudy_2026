using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class EditorNode
{
    public string id;
    // normalized 0..1 relative to texture (x: left->right, y: top->bottom)
    public Vector2 position;
}


[System.Serializable]
public class EditorRoad
{
    public string from;
    public string to;
    public int laneCount = 3;
}

[System.Serializable]
public class StageGraph
{
    public List<EditorNode> intersections = new();
    public List<EditorRoad> roads = new();
}

public class StageGraphEditorWindow : EditorWindow
{
    private Texture2D stageTexture;
    private StageGraph graph = new();
    private string selectedNodeId = null;
    private enum EditMode { Select, AddNode, Connect }
    private EditMode mode = EditMode.AddNode;
    private string savePath = "Assets/StageMaps/Resources/MapJSON/stage_graph.json";
    private float displaySize = 512f;
    // Scene placement scale for converting normalized coords to world units
    private float worldWidth = 10f;
    private float worldHeight = 10f;
    // Output RoadNetworkAsset path
    private string networkAssetPath = "Assets/StageMaps/Resources/MapJSON/stage_network.asset";
    private GameObject intersectionPrefab;
    private GameObject wayPrefab;
    private GameObject lanePrefab;
    private int selectedRoadIndex = -1;

    // グリッド設定
    private bool showGrid = true;
    private bool snapToGrid = true;
    private int gridCellCount = 10;
    private Color gridColor = new Color(0.4f, 0.8f, 1f, 0.8f);
    private float gridOffsetX = 0f;
    private float gridOffsetY = 0f;
    private List<float> customGridX = new List<float>();
    private List<float> customGridY = new List<float>();
    private bool useCustomGrid = false;
    private int draggedLineIndex = -1;
    private bool draggedLineIsX = true;

    [MenuItem("Tools/Stage Graph Editor")]
    public static void Open()
    {
        GetWindow<StageGraphEditorWindow>(
            "Stage Graph Editor"
        );
    }

    private void OnGUI()
    {
        // ── タイトル ──
        EditorGUILayout.LabelField("Stage Graph Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // ── アセット設定 ──
        EditorGUILayout.LabelField("アセット設定", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            stageTexture = (Texture2D)EditorGUILayout.ObjectField("Stage Texture", stageTexture, typeof(Texture2D), false);
            intersectionPrefab = (GameObject)EditorGUILayout.ObjectField("Intersection Prefab", intersectionPrefab, typeof(GameObject), false);
            wayPrefab = (GameObject)EditorGUILayout.ObjectField("Way Prefab", wayPrefab, typeof(GameObject), false);
            lanePrefab = (GameObject)EditorGUILayout.ObjectField("Lane Prefab", lanePrefab, typeof(GameObject), false);
        }


        EditorGUILayout.Space(6);

        // ── 編集モード ──
        EditorGUILayout.LabelField("編集モード", EditorStyles.boldLabel);
        mode = (EditMode)GUILayout.Toolbar((int)mode, new string[] { "Select", "Add Node", "Connect" });
        EditorGUILayout.Space(6);

        // ── グリッド設定 ──
        EditorGUILayout.LabelField("グリッド設定", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                showGrid = EditorGUILayout.ToggleLeft("グリッド表示", showGrid, GUILayout.Width(110));
                snapToGrid = EditorGUILayout.ToggleLeft("スナップ", snapToGrid, GUILayout.Width(90));
            }
            gridCellCount = Mathf.Max(2, EditorGUILayout.IntField("分割数", gridCellCount));
            displaySize = EditorGUILayout.FloatField("表示サイズ (px)", displaySize);
            gridColor = EditorGUILayout.ColorField("グリッド色", gridColor);
            if (!useCustomGrid)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("オフセット", GUILayout.Width(70));
                    EditorGUILayout.LabelField("X", GUILayout.Width(12));
                    gridOffsetX = EditorGUILayout.Slider(gridOffsetX, -1f, 1f);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("", GUILayout.Width(70));
                    EditorGUILayout.LabelField("Y", GUILayout.Width(12));
                    gridOffsetY = EditorGUILayout.Slider(gridOffsetY, -1f, 1f);
                }
                if (GUILayout.Button("カスタムグリッドに変換"))
                {
                    GenerateCustomGridFromUniform();
                    useCustomGrid = true;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Sceneビュー上でグリッド線をドラッグできます。右クリックで削除できます。", MessageType.Info);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("縦線を追加")) { customGridX.Add(0.5f); customGridX.Sort(); SceneView.RepaintAll(); }
                    if (GUILayout.Button("横線を追加")) { customGridY.Add(0.5f); customGridY.Sort(); SceneView.RepaintAll(); }
                }
                if (GUILayout.Button("均等グリッドに戻す"))
                {
                    customGridX.Clear();
                    customGridY.Clear();
                    useCustomGrid = false;
                    draggedLineIndex = -1;
                }
            }
        }
        EditorGUILayout.Space(6);

        // ── ワールド設定 ──
        EditorGUILayout.LabelField("ワールド設定", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            worldWidth = EditorGUILayout.FloatField("World Width", worldWidth);
            worldHeight = EditorGUILayout.FloatField("World Height", worldHeight);
            networkAssetPath = EditorGUILayout.TextField("Network Asset Path", networkAssetPath);
            savePath = EditorGUILayout.TextField("JSON Save Path", savePath);
        }

        EditorGUILayout.Space(6);

        // ── 選択中の道路 ──
        if (selectedRoadIndex >= 0 && selectedRoadIndex < graph.roads.Count)
        {
            EditorGUILayout.LabelField("選択中の道路", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int newLaneCount = EditorGUILayout.IntField("レーン数", graph.roads[selectedRoadIndex].laneCount);
                graph.roads[selectedRoadIndex].laneCount = Mathf.Max(1, newLaneCount);
            }
            EditorGUILayout.Space(4);
        }
        // ── 操作ボタン ──
        EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save JSON")) SaveGraph();
                if (GUILayout.Button("Load JSON")) LoadGraph();
                if (GUILayout.Button("Clear"))
                {
                    graph = new StageGraph();
                    selectedNodeId = null;
                }
            }
            EditorGUILayout.Space(2);
            if (GUILayout.Button("Generate Scene + RoadNetworkAsset", GUILayout.Height(30)))
                GenerateNetworkAsset();
        }
    }
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (stageTexture == null)
            return;

        Handles.BeginGUI();

        // Draw texture at fixed offset
        Rect rect = new Rect(10, 10, displaySize, displaySize);
        GUI.DrawTexture(rect, stageTexture, ScaleMode.ScaleToFit);

        // グリッド描画
        if (showGrid)
        {
            Color borderColor = new Color(1f, 1f, 0f, 1f);
            // 外枠（常に固定）
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), borderColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), borderColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), borderColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), borderColor);

            if (useCustomGrid)
            {
                for (int i = 0; i < customGridX.Count; i++)
                {
                    float x = rect.x + customGridX[i] * rect.width;
                    bool hovered = draggedLineIndex == i && draggedLineIsX;
                    Color col = hovered ? Color.yellow : gridColor;
                    float lw = hovered ? 3f : 2f;
                    EditorGUI.DrawRect(new Rect(x, rect.y, lw, rect.height), col);
                }
                for (int i = 0; i < customGridY.Count; i++)
                {
                    float y = rect.y + customGridY[i] * rect.height;
                    bool hovered = draggedLineIndex == i && !draggedLineIsX;
                    Color col = hovered ? Color.yellow : gridColor;
                    float lw = hovered ? 3f : 2f;
                    EditorGUI.DrawRect(new Rect(rect.x, y, rect.width, lw), col);
                }
            }
            else
            {
                Color axisColor = new Color(gridColor.r, gridColor.g, gridColor.b, 1f);
                float step = 1f / gridCellCount;
                // オフセットを1セル分の範囲に収める（繰り返しパターンなのでfmod）
                float ox = ((gridOffsetX % step) + step) % step;
                float oy = ((gridOffsetY % step) + step) % step;

                // 内部グリッド線（オフセット適用）
                for (int i = 1; i < gridCellCount; i++)
                {
                    float tx = ox + i * step;
                    float ty = oy + i * step;
                    if (tx > 0f && tx < 1f)
                    {
                        float x = rect.x + tx * rect.width;
                        Color col = (Mathf.Abs(tx - 0.5f) < step * 0.1f) ? axisColor : gridColor;
                        float lw = (Mathf.Abs(tx - 0.5f) < step * 0.1f) ? 2f : 1f;
                        EditorGUI.DrawRect(new Rect(x, rect.y, lw, rect.height), col);
                    }
                    if (ty > 0f && ty < 1f)
                    {
                        float y = rect.y + ty * rect.height;
                        Color col = (Mathf.Abs(ty - 0.5f) < step * 0.1f) ? axisColor : gridColor;
                        float lw = (Mathf.Abs(ty - 0.5f) < step * 0.1f) ? 2f : 1f;
                        EditorGUI.DrawRect(new Rect(rect.x, y, rect.width, lw), col);
                    }
                }
            }
        }

        Event e = Event.current;

        if (useCustomGrid && showGrid)
        {
            Vector2 m = e.mousePosition;

            if (e.type == EventType.MouseDown && e.button == 0 && e.alt)
            {
                for (int i = 0; i < customGridX.Count; i++)
                {
                    float x = rect.x + customGridX[i] * rect.width;
                    if (Mathf.Abs(m.x - x) <= 6f && m.y >= rect.y && m.y <= rect.yMax)
                    {
                        draggedLineIndex = i;
                        draggedLineIsX = true;
                        e.Use();
                        break;
                    }
                }

                if (draggedLineIndex == -1)
                {
                    for (int i = 0; i < customGridY.Count; i++)
                    {
                        float y = rect.y + customGridY[i] * rect.height;
                        if (Mathf.Abs(m.y - y) <= 6f && m.x >= rect.x && m.x <= rect.xMax)
                        {
                            draggedLineIndex = i;
                            draggedLineIsX = false;
                            e.Use();
                            break;
                        }
                    }
                }
            }

            if (e.type == EventType.MouseDrag && e.button == 0 && draggedLineIndex != -1)
            {
                float normalized = draggedLineIsX
                    ? Mathf.Clamp01((m.x - rect.x) / rect.width)
                    : Mathf.Clamp01((m.y - rect.y) / rect.height);

                if (draggedLineIsX)
                    customGridX[draggedLineIndex] = normalized;
                else
                    customGridY[draggedLineIndex] = normalized;

                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                draggedLineIndex = -1;
                e.Use();
            }

            if (e.type == EventType.MouseDown && e.button == 1)
            {
                for (int i = customGridX.Count - 1; i >= 0; i--)
                {
                    float x = rect.x + customGridX[i] * rect.width;
                    if (Mathf.Abs(m.x - x) <= 6f && m.y >= rect.y && m.y <= rect.yMax)
                    {
                        customGridX.RemoveAt(i);
                        e.Use();
                        Repaint();
                        break;
                    }
                }

                for (int i = customGridY.Count - 1; i >= 0; i--)
                {
                    float y = rect.y + customGridY[i] * rect.height;
                    if (Mathf.Abs(m.y - y) <= 6f && m.x >= rect.x && m.x <= rect.xMax)
                    {
                        customGridY.RemoveAt(i);
                        e.Use();
                        Repaint();
                        break;
                    }
                }
            }
        }

        // Handle mouse clicks inside texture rect
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Vector2 m = e.mousePosition;
            if (rect.Contains(m))
            {
                Vector2 local = m - rect.position;
                Vector2 normalized = new Vector2(local.x / rect.width, local.y / rect.height);

                if (mode == EditMode.AddNode)
                {
                    normalized = SnapToGrid(normalized);
                    EditorNode n = new EditorNode { id = System.Guid.NewGuid().ToString(), position = normalized };
                    graph.intersections.Add(n);
                    e.Use();
                    Repaint();
                }
                else if (mode == EditMode.Select)
                {
                    string hit = FindNodeUnderMouse(rect, m);
                    if (!string.IsNullOrEmpty(hit))
                    {
                        selectedNodeId = hit;
                        selectedRoadIndex = -1;
                    }
                    else
                    {
                        int roadHit = FindRoadUnderMouse(rect, m);
                        if (roadHit != -1)
                        {
                            selectedRoadIndex = roadHit;
                            selectedNodeId = null;
                        }
                        else
                        {
                            selectedNodeId = null;
                            selectedRoadIndex = -1;
                        }
                    }
                    e.Use();
                    Repaint();
                }
                else if (mode == EditMode.Connect)
                {
                    string hit = FindNodeUnderMouse(rect, m);
                    if (!string.IsNullOrEmpty(hit))
                    {
                        if (string.IsNullOrEmpty(selectedNodeId))
                        {
                            selectedNodeId = hit;
                        }
                        else if (selectedNodeId != hit)
                        {
                            // create road
                            EditorRoad r = new EditorRoad { from = selectedNodeId, to = hit, laneCount = 3 };
                            graph.roads.Add(r);
                            selectedNodeId = null;
                        }
                        e.Use();
                        Repaint();
                    }
                }
            }
        }

        // Drag selected node when in Select mode
        if (mode == EditMode.Select && e.type == EventType.MouseDrag && e.button == 0)
        {
            Vector2 m = e.mousePosition;
            if (rect.Contains(m) && !string.IsNullOrEmpty(selectedNodeId))
            {
                var node = graph.intersections.Find(n => n.id == selectedNodeId);
                if (node != null)
                {
                    Vector2 local = m - rect.position;
                    Vector2 normalized = SnapToGrid(new Vector2(local.x / rect.width, local.y / rect.height));
                    node.position = normalized;
                    e.Use();
                    Repaint();
                }
            }
        }

        // Draw roads
        Handles.color = Color.white;
        foreach (var road in graph.roads)
        {
            var a = graph.intersections.Find(n => n.id == road.from);
            var b = graph.intersections.Find(n => n.id == road.to);
            if (a == null || b == null) continue;
            Vector2 pa = rect.position + new Vector2(a.position.x * rect.width, a.position.y * rect.height);
            Vector2 pb = rect.position + new Vector2(b.position.x * rect.width, b.position.y * rect.height);
            Handles.DrawLine(pa, pb);
        }

        // Draw nodes
        for (int i = 0; i < graph.intersections.Count; ++i)
        {
            var node = graph.intersections[i];
            Vector2 p = rect.position + new Vector2(node.position.x * rect.width, node.position.y * rect.height);
            Color col = node.id == selectedNodeId ? Color.yellow : Color.green;
            Handles.color = col;
            Handles.DrawSolidDisc(p, Vector3.forward, 6f);
            Handles.color = Color.white;
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white } };
            Vector2 labelPos = p + new Vector2(8f, -8f);
            Handles.Label(labelPos, i.ToString(), style);
        }

        Handles.EndGUI();

    }

    private Vector2 SnapToGrid(Vector2 normalized)
    {
        if (!snapToGrid)
            return normalized;

        if (useCustomGrid)
        {
            if (customGridX.Count == 0 && customGridY.Count == 0)
                return normalized;

            float snappedX = normalized.x;
            float snappedY = normalized.y;

            if (customGridX.Count > 0)
                snappedX = FindNearestValue(normalized.x, customGridX);
            if (customGridY.Count > 0)
                snappedY = FindNearestValue(normalized.y, customGridY);

            return new Vector2(snappedX, snappedY);
        }

        if (gridCellCount <= 0) return normalized;
        float step = 1f / gridCellCount;
        float ox = ((gridOffsetX % step) + step) % step;
        float oy = ((gridOffsetY % step) + step) % step;
        return new Vector2(
            Mathf.Round((normalized.x - ox) / step) * step + ox,
            Mathf.Round((normalized.y - oy) / step) * step + oy);
    }

    private static float FindNearestValue(float value, List<float> values)
    {
        float nearest = values[0];
        float nearestDist = Mathf.Abs(value - nearest);

        for (int i = 1; i < values.Count; i++)
        {
            float dist = Mathf.Abs(value - values[i]);
            if (dist < nearestDist)
            {
                nearest = values[i];
                nearestDist = dist;
            }
        }

        return nearest;
    }

    private void GenerateCustomGridFromUniform()
    {
        customGridX.Clear();
        customGridY.Clear();

        float step = 1f / gridCellCount;
        float ox = ((gridOffsetX % step) + step) % step;
        float oy = ((gridOffsetY % step) + step) % step;

        for (int i = 1; i < gridCellCount; i++)
        {
            float tx = ox + i * step;
            float ty = oy + i * step;
            if (tx > 0f && tx < 1f) customGridX.Add(tx);
            if (ty > 0f && ty < 1f) customGridY.Add(ty);
        }

        customGridX.Sort();
        customGridY.Sort();
        SceneView.RepaintAll();
    }

    private string FindNodeUnderMouse(Rect rect, Vector2 mouse)
    {
        for (int i = 0; i < graph.intersections.Count; ++i)
        {
            var n = graph.intersections[i];
            Vector2 p = rect.position + new Vector2(n.position.x * rect.width, n.position.y * rect.height);
            if (Vector2.Distance(p, mouse) <= 8f)
                return n.id;
        }
        return null;
    }

    private int FindRoadUnderMouse(Rect rect, Vector2 mouse)
    {
        for (int i = 0; i < graph.roads.Count; ++i)
        {
            var road = graph.roads[i];
            var a = graph.intersections.Find(n => n.id == road.from);
            var b = graph.intersections.Find(n => n.id == road.to);
            if (a == null || b == null) continue;
            Vector2 pa = rect.position + new Vector2(a.position.x * rect.width, a.position.y * rect.height);
            Vector2 pb = rect.position + new Vector2(b.position.x * rect.width, b.position.y * rect.height);
            Vector2 ab = pb - pa;
            float denom = ab.sqrMagnitude;
            if (denom <= 1e-6f) continue;
            float t = Vector2.Dot(mouse - pa, ab) / denom;
            t = Mathf.Clamp01(t);
            Vector2 proj = pa + ab * t;
            float dist = Vector2.Distance(mouse, proj);
            if (dist <= 8f) return i;
        }
        return -1;
    }


    private bool IsStraightNode(EditorNode node)
    {
        List<EditorRoad> connected = new();

        foreach (var road in graph.roads)
        {
            if (road.from == node.id || road.to == node.id)
            {
                connected.Add(road);
            }
        }

        if (connected.Count != 2)
            return false;

        EditorNode nodeA = null;
        EditorNode nodeB = null;

        foreach (var road in connected)
        {
            string otherId =
                road.from == node.id
                ? road.to
                : road.from;

            var other =
                graph.intersections.Find(x => x.id == otherId);

            if (nodeA == null)
                nodeA = other;
            else
                nodeB = other;
        }

        if (nodeA == null || nodeB == null)
            return false;

        Vector2 dir1 =
            (nodeA.position - node.position).normalized;

        Vector2 dir2 =
            (nodeB.position - node.position).normalized;

        float angle =
            Vector2.Angle(dir1, dir2);

        return Mathf.Abs(angle - 180f) < 10f;
    }

    private void SaveGraph()
    {
        try
        {
            string fullPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length), savePath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string json = JsonUtility.ToJson(graph, true);
            File.WriteAllText(fullPath, json);
            AssetDatabase.Refresh();
            Debug.Log($"Saved graph to {savePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save graph: {ex}");
        }
    }

    private void LoadGraph()
    {
        try
        {
            string fullPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length), savePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"Graph file not found: {savePath}");
                return;
            }
            string json = File.ReadAllText(fullPath);
            graph = JsonUtility.FromJson<StageGraph>(json) ?? new StageGraph();
            Debug.Log($"Loaded graph from {savePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load graph: {ex}");
        }
    }

    private void GenerateNetworkAsset()
    {
        if (graph == null || graph.intersections.Count == 0)
        {
            Debug.LogWarning("Graph is empty. Load or create a graph first.");
            return;
        }

        // Remove any previous generated parents to avoid duplicate/stale objects
        var prevInterParent = GameObject.Find("StageGraph_Intersections");
        if (prevInterParent != null)
        {
            Undo.DestroyObjectImmediate(prevInterParent);
            Debug.Log("Removed existing StageGraph_Intersections before generation.");
        }

        // Create parent object for intersections
        GameObject parent = new GameObject("StageGraph_Intersections");
        Undo.RegisterCreatedObjectUndo(parent, "Create Intersections Parent");

        // Map node id to Intersection component
        var map = new Dictionary<string, Intersection>();

        for (int i = 0; i < graph.intersections.Count; ++i)
        {
            var node = graph.intersections[i];
            // Note: texture Y is normalized top->bottom. Convert to world Z with origin at center
            // by flipping Y so that top of texture maps to positive Z consistently.
            Vector3 pos = new Vector3((node.position.x - 0.5f) * worldWidth, 0f, (0.5f - node.position.y) * worldHeight);
            GameObject go;
            if (intersectionPrefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(intersectionPrefab);
                if (go == null) go = new GameObject($"Intersection_{i}");
                Undo.RegisterCreatedObjectUndo(go, "Create Intersection");
            }
            else
            {
                go = new GameObject($"Intersection_{i}");
                Undo.RegisterCreatedObjectUndo(go, "Create Intersection");
            }
            go.name = $"Intersection_{i}";
            go.transform.SetParent(parent.transform, false);
            go.transform.position = pos;
            Intersection inter = go.GetComponent<Intersection>() ?? go.AddComponent<Intersection>();
            map[node.id] = inter;
        }

        // Remove any previous generated ways parent to avoid duplicate/stale Way objects
        var prevWaysParent = GameObject.Find("StageGraph_Ways");
        if (prevWaysParent != null)
        {
            Undo.DestroyObjectImmediate(prevWaysParent);
            Debug.Log("Removed existing StageGraph_Ways before generation.");
        }

        // Create Ways for each road
        GameObject waysParent = new GameObject("StageGraph_Ways");
        Undo.RegisterCreatedObjectUndo(waysParent, "Create Ways Parent");

        float laneWidth = 3.5f;

        for (int ri = 0; ri < graph.roads.Count; ++ri)
        {
            var edge = graph.roads[ri];
            if (!map.ContainsKey(edge.from) || !map.ContainsKey(edge.to)) continue;
            Intersection fromI = map[edge.from];
            Intersection toI = map[edge.to];

            Vector3 fromPos = fromI.transform.position;
            Vector3 toPos = toI.transform.position;

            string wayName = $"Way_{fromI.name}To{toI.name}_{ri}";
            GameObject wayObj;
            if (wayPrefab != null)
            {
                wayObj = (GameObject)PrefabUtility.InstantiatePrefab(wayPrefab);
                if (wayObj == null) wayObj = new GameObject(wayName);
                Undo.RegisterCreatedObjectUndo(wayObj, "Create Way");
            }
            else
            {
                wayObj = new GameObject(wayName);
                Undo.RegisterCreatedObjectUndo(wayObj, "Create Way");
            }
            wayObj.name = wayName;
            wayObj.transform.SetParent(waysParent.transform, false);
            wayObj.transform.position = (fromPos + toPos) * 0.5f;

            Way way = wayObj.GetComponent<Way>() ?? wayObj.AddComponent<Way>();

            // lanes root
            Transform lanesRoot = new GameObject("Lanes").transform;
            lanesRoot.SetParent(wayObj.transform, false);

            int laneCount = Mathf.Max(1, edge.laneCount);
            Vector3 dir = (toPos - fromPos).normalized;
            Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;

            var createdLanes = new List<Lane>();
            for (int li = 0; li < laneCount; ++li)
            {
                GameObject laneObj;
                if (lanePrefab != null)
                {
                    laneObj = (GameObject)PrefabUtility.InstantiatePrefab(lanePrefab);
                    if (laneObj == null) laneObj = new GameObject($"Lane_{ri}_{li}");
                    Undo.RegisterCreatedObjectUndo(laneObj, "Create Lane");
                }
                else
                {
                    laneObj = new GameObject($"Lane_{ri}_{li}");
                    Undo.RegisterCreatedObjectUndo(laneObj, "Create Lane");
                }
                laneObj.name = $"Lane_{ri}_{li}";
                laneObj.transform.SetParent(lanesRoot, false);

                // start/end point (reuse if prefab already contains them)
                Transform startPoint = laneObj.transform.Find("StartPoint");
                if (startPoint == null)
                {
                    startPoint = new GameObject("StartPoint").transform;
                    startPoint.SetParent(laneObj.transform, false);
                }
                Transform endPoint = laneObj.transform.Find("EndPoint");
                if (endPoint == null)
                {
                    endPoint = new GameObject("EndPoint").transform;
                    endPoint.SetParent(laneObj.transform, false);
                }

                float centerIndex = (laneCount - 1) * 0.5f;
                float offset = (li - centerIndex) * laneWidth;
                startPoint.position = fromPos + perp * offset;
                endPoint.position = toPos + perp * offset;

                Lane lane = laneObj.GetComponent<Lane>() ?? laneObj.AddComponent<Lane>();
                // set serialized fields
                SerializedObject laneSO = new SerializedObject(lane);
                laneSO.FindProperty("parentWay").objectReferenceValue = way;
                laneSO.FindProperty("laneIndex").intValue = li;
                laneSO.FindProperty("startPoint").objectReferenceValue = startPoint;
                laneSO.FindProperty("endPoint").objectReferenceValue = endPoint;
                laneSO.ApplyModifiedPropertiesWithoutUndo();

                createdLanes.Add(lane);
            }


            // assign lanes to way
            SerializedObject waySO = new SerializedObject(way);
            SerializedProperty lanesProp = waySO.FindProperty("lanes");
            lanesProp.ClearArray();
            for (int i = 0; i < createdLanes.Count; ++i)
            {
                lanesProp.InsertArrayElementAtIndex(i);
                lanesProp.GetArrayElementAtIndex(i).objectReferenceValue = createdLanes[i];
            }
            waySO.ApplyModifiedPropertiesWithoutUndo();

            // register with intersections
            Vector3 direction = toPos - fromPos;
            Undo.RecordObject(fromI, $"Assign way on {fromI.name}");
            fromI.SetWayByWorldDirection(direction, way);
            EditorUtility.SetDirty(fromI);

            Undo.RecordObject(toI, $"Register incoming way on {toI.name}");
            toI.RegisterIncomingWay(way);
            EditorUtility.SetDirty(toI);
        }

        // After creating ways, build lane links at each intersection
        foreach (var kv in map)
        {
            RebuildIncomingLaneLinksAtIntersection_Local(kv.Value);
        }

        // Build RoadNetworkAsset
        RoadNetworkAsset existing = AssetDatabase.LoadAssetAtPath<RoadNetworkAsset>(networkAssetPath);
        if (existing == null)
        {
            RoadNetworkAsset asset = ScriptableObject.CreateInstance<RoadNetworkAsset>();
            asset.connections = new List<RoadConnection>();

            // For each from-node collect targets
            foreach (var node in graph.intersections)
            {
                var outs = graph.roads.FindAll(r => r.from == node.id);
                if (outs == null || outs.Count == 0) continue;

                RoadConnection rc = new RoadConnection();
                rc.from = map[node.id];
                rc.to = new List<RoadTarget>();

                foreach (var r in outs)
                {
                    if (!map.ContainsKey(r.to)) continue;
                    RoadTarget rt = new RoadTarget();
                    rt.intersection = map[r.to];
                    rt.directionType = RoadDirectionType.TwoWay;
                    rt.laneCount = Mathf.Max(1, r.laneCount);
                    rt.allowLeftTurn = rt.allowRightTurn = rt.allowStraight = true;
                    rc.to.Add(rt);
                }

                asset.connections.Add(rc);
            }

            string path = networkAssetPath;
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"Created RoadNetworkAsset at {path}");
        }
        else
        {
            // overwrite existing
            existing.connections = new List<RoadConnection>();
            foreach (var node in graph.intersections)
            {
                var outs = graph.roads.FindAll(r => r.from == node.id);
                if (outs == null || outs.Count == 0) continue;

                RoadConnection rc = new RoadConnection();
                rc.from = map[node.id];
                rc.to = new List<RoadTarget>();

                foreach (var r in outs)
                {
                    if (!map.ContainsKey(r.to)) continue;
                    RoadTarget rt = new RoadTarget();
                    rt.intersection = map[r.to];
                    rt.directionType = RoadDirectionType.TwoWay;
                    rt.laneCount = Mathf.Max(1, r.laneCount);
                    rt.allowLeftTurn = rt.allowRightTurn = rt.allowStraight = true;
                    rc.to.Add(rt);
                }

                existing.connections.Add(rc);
            }

            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = existing;
            Debug.Log($"Updated existing RoadNetworkAsset at {networkAssetPath}");
        }
    }

    // --- Local helper methods for lane link rebuilding ---
    private void RebuildIncomingLaneLinksAtIntersection_Local(Intersection _intersection)
    {
        if (_intersection == null) return;

        _intersection.CleanupIncomingWays();

        IReadOnlyList<Way> incomingWays = _intersection.IncomingWays;
        for (int i = 0; i < incomingWays.Count; ++i)
        {
            Way way = incomingWays[i];
            if (way == null) continue;

            List<Lane> lanes = new();
            if (way.Lanes != null)
            {
                for (int li = 0; li < way.Lanes.Count; ++li)
                {
                    var l = way.Lanes[li];
                    if (l != null) lanes.Add(l);
                }
            }

            if (lanes.Count == 0) continue;

            Lane baseLane = null;
            for (int li = 0; li < lanes.Count; ++li)
            {
                if (lanes[li] != null && lanes[li].StartPoint != null && lanes[li].EndPoint != null)
                {
                    baseLane = lanes[li];
                    break;
                }
            }

            if (baseLane == null) continue;

            Vector3 forwardAtTo = baseLane.EndPoint.position - baseLane.StartPoint.position;
            forwardAtTo.y = 0f;
            if (forwardAtTo.sqrMagnitude <= 1e-6f) continue;

            TurnDirection[] turnDirections = { TurnDirection.Straight, TurnDirection.Left, TurnDirection.Right, TurnDirection.Back };

            for (int laneIndex = 0; laneIndex < lanes.Count; ++laneIndex)
            {
                Lane sourceLane = lanes[laneIndex];
                if (sourceLane == null) continue;

                var seeds = new List<(TurnDirection turn, Lane next)>();

                for (int ti = 0; ti < turnDirections.Length; ++ti)
                {
                    TurnDirection turnDirection = turnDirections[ti];
                    // GetWayByTurn expects a forward vector pointing from the intersection
                    // into the incoming way's direction. Our forwardAtTo is from start->end
                    // (toward the intersection), so invert it to match the intersection-local convention.
                    Way targetWay = _intersection.GetWayByTurn((-forwardAtTo).normalized, turnDirection);
                    if (targetWay == null) continue;
                    Lane targetLane = GetLaneByIndexOrDefault_Local(targetWay, sourceLane.LaneIndex);
                    if (targetLane == null) continue;
                    seeds.Add((turnDirection, targetLane));
                }

                ApplyLaneLinks_Local(sourceLane, seeds);
            }
        }
    }

    private Lane GetLaneByIndexOrDefault_Local(Way _way, int _laneIndex)
    {
        if (_way == null) return null;
        Lane byIndex = _way.GetLane(_laneIndex);
        if (byIndex != null) return byIndex;
        return _way.GetDefaultLane();
    }

    private void ApplyLaneLinks_Local(Lane _lane, List<(TurnDirection turn, Lane next)> _seeds)
    {
        SerializedObject laneSO = new SerializedObject(_lane);
        SerializedProperty linksProp = laneSO.FindProperty("nextLaneLinks");
        linksProp.ClearArray();

        for (int i = 0; i < _seeds.Count; ++i)
        {
            linksProp.InsertArrayElementAtIndex(i);
            SerializedProperty linkProp = linksProp.GetArrayElementAtIndex(i);
            linkProp.FindPropertyRelative("turnDirection").enumValueIndex = (int)_seeds[i].turn;
            linkProp.FindPropertyRelative("nextLane").objectReferenceValue = _seeds[i].next;
        }

        laneSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(_lane);
    }

}
