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

    [MenuItem("Tools/Stage Graph Editor")]
    public static void Open()
    {
        GetWindow<StageGraphEditorWindow>(
            "Stage Graph Editor"
        );
    }

    private void OnGUI()
    {
        GUILayout.Label(
            "Stage Graph Editor",
            EditorStyles.boldLabel
        );

        stageTexture =
            (Texture2D)EditorGUILayout.ObjectField(
                "Stage Texture",
                stageTexture,
                typeof(Texture2D),
                false
            );

        intersectionPrefab =
            (GameObject)EditorGUILayout.ObjectField(
                "Intersection Prefab",
                intersectionPrefab,
                typeof(GameObject),
                false
            );

        wayPrefab =
            (GameObject)EditorGUILayout.ObjectField(
                "Way Prefab",
                wayPrefab,
                typeof(GameObject),
                false
            );

        lanePrefab =
            (GameObject)EditorGUILayout.ObjectField(
                "Lane Prefab",
                lanePrefab,
                typeof(GameObject),
                false
            );

        GUILayout.Space(6);
        mode = (EditMode)GUILayout.Toolbar((int)mode, new string[] { "Select", "Add Node", "Connect" });

        GUILayout.BeginHorizontal();
        GUILayout.Label("Display Size", GUILayout.Width(80));
        displaySize = EditorGUILayout.FloatField(displaySize, GUILayout.Width(80));
        GUILayout.Label("Save Path", GUILayout.Width(60));
        savePath = EditorGUILayout.TextField(savePath);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Graph to JSON", GUILayout.Width(150)))
        {
            SaveGraph();
        }
        if (GUILayout.Button("Load Graph from JSON", GUILayout.Width(150)))
        {
            LoadGraph();
        }
        if (GUILayout.Button("Clear", GUILayout.Width(80)))
        {
            graph = new StageGraph();
            selectedNodeId = null;
        }
        if (GUILayout.Button("Generate Scene + RoadNetworkAsset", GUILayout.Width(220)))
        {
            GenerateNetworkAsset();
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("World Width", GUILayout.Width(80));
        worldWidth = EditorGUILayout.FloatField(worldWidth, GUILayout.Width(80));
        GUILayout.Label("World Height", GUILayout.Width(80));
        worldHeight = EditorGUILayout.FloatField(worldHeight, GUILayout.Width(80));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Network Asset Path", GUILayout.Width(120));
        networkAssetPath = EditorGUILayout.TextField(networkAssetPath);
        GUILayout.EndHorizontal();
        if (selectedRoadIndex >= 0 && selectedRoadIndex < graph.roads.Count)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Lane Count", GUILayout.Width(80));
            int newLaneCount = EditorGUILayout.IntField(graph.roads[selectedRoadIndex].laneCount, GUILayout.Width(60));
            newLaneCount = Mathf.Max(1, newLaneCount);
            graph.roads[selectedRoadIndex].laneCount = newLaneCount;
            GUILayout.EndHorizontal();
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

        Event e = Event.current;

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
                    Vector2 normalized = new Vector2(local.x / rect.width, local.y / rect.height);
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
