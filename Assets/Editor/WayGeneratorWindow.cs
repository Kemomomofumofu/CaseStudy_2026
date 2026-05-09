#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Flags describing allowed turn directions for a lane. The original code
// referenced `Lane.LaneTurnFlags` which does not exist in this project.
[System.Flags]
public enum LaneTurnFlags
{
    None = 0,
    Straight = 1 << 0,
    Left = 1 << 1,
    Right = 1 << 2
}

/// <summary>
/// 二つの交差点から双方向のWayを生成し、LaneとLaneLinkまで自動設定するツール
/// </summary>
public class WayGeneratorWindow : EditorWindow
{
    private Intersection intersectionA;
    private Intersection intersectionB;
    private Transform waysParent;
    private GameObject wayPrefab;
    private GameObject lanePrefab;
    private int laneCount = 2;
    // 道路プレハブ自動添付
    private GameObject roadPrefab;
    private float roadPrefabBaseLength = 1f;
    private bool attachRoadPrefab = false;
    // 交差点アセットを自動配置
    private bool attachIntersectionAsset = false;
    // 登録済み道路アセットの管理
    private RoadAssetRegistry roadRegistry;
    private const string RoadRegistryAssetPath = "Assets/Editor/RoadAssetRegistry.asset";
    private string newRegistryTag = "";
    private string tagFilter = "All";
    // ネットワーク一括生成用アセット
    private RoadNetworkAsset roadNetworkAsset;

    private struct LaneLinkSeed
    {
        public TurnDirection TurnDirection;
        public Lane NextLane;
    }

    private static GeneratedWayInfo CreateOneWay(
        Intersection _from,
        Intersection _to,
        Transform _parent,
        GameObject _wayPrefab,
        GameObject _lanePrefab,
        int _laneCount,
        GameObject _roadPrefab,
        float _roadPrefabBaseLength,
        bool _attachRoadPrefab,
        bool _attachIntersectionAsset,
        LaneTurnFlags _allowedTurns
    )
    {
        // Use existing creation logic
        GeneratedWayInfo info = CreateWayObject(_from, _to, _parent, _wayPrefab, _lanePrefab, _laneCount, _roadPrefab, _roadPrefabBaseLength, _attachRoadPrefab);

        // Apply allowed turn flags to each created lane
        if (info.Lanes != null)
        {
            for (int i = 0; i < info.Lanes.Count; ++i)
            {
                Lane lane = info.Lanes[i];
                if (lane == null) continue;

                SerializedObject laneSO = new(lane);
                var prop = laneSO.FindProperty("allowedTurns");
                if (prop != null)
                {
                    prop.enumValueIndex = (int)_allowedTurns;
                    laneSO.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(lane);
                }
            }
        }

        // Rebuild incoming links on destination
        RebuildIncomingLaneLinksAtIntersection(_to);

        // Attach intersection asset if requested
        if (_attachIntersectionAsset)
        {
            AttachIntersectionAsset(_to);
        }

        return info;
    }

    private static void CreateRoadNetwork(
        RoadNetworkAsset _network,
        Transform _parent,
        GameObject _wayPrefab,
        GameObject _lanePrefab,
        int _laneCount,
        GameObject _roadPrefab,
        float _roadPrefabBaseLength,
        bool _attachRoadPrefab,
        bool _attachIntersectionAsset
    )
    {
        if (_network == null)
            return;

        foreach (var conn in _network.connections)
        {
            if (conn == null || conn.from == null)
                continue;

            if (conn.to == null || conn.to.Count == 0)
                continue;

            foreach (var target in conn.to)
            {
                if (target == null || target.intersection == null) continue;

                var dest = target.intersection;
                int useLaneCount = Mathf.Max(1, target.laneCount);

                // build allowed turns flags
                LaneTurnFlags allowed = LaneTurnFlags.None;
                if (target.allowStraight) allowed |= LaneTurnFlags.Straight;
                if (target.allowLeftTurn) allowed |= LaneTurnFlags.Left;
                if (target.allowRightTurn) allowed |= LaneTurnFlags.Right;

                if (target.directionType == RoadDirectionType.TwoWay)
                {
                    // create both directions
                    CreateOneWay(conn.from, dest, _parent, _wayPrefab, _lanePrefab, useLaneCount, _roadPrefab, _roadPrefabBaseLength, _attachRoadPrefab, _attachIntersectionAsset, allowed);
                    CreateOneWay(dest, conn.from, _parent, _wayPrefab, _lanePrefab, useLaneCount, _roadPrefab, _roadPrefabBaseLength, _attachRoadPrefab, _attachIntersectionAsset, allowed);
                }
                else
                {
                    // one-way from -> dest
                    CreateOneWay(conn.from, dest, _parent, _wayPrefab, _lanePrefab, useLaneCount, _roadPrefab, _roadPrefabBaseLength, _attachRoadPrefab, _attachIntersectionAsset, allowed);
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Road network generation complete: connections={_network.connections.Count}");
    }

    private void LoadOrCreateRoadRegistry(bool forceCreate = false)
    {
        roadRegistry = AssetDatabase.LoadAssetAtPath<RoadAssetRegistry>(RoadRegistryAssetPath);
        if (roadRegistry == null && forceCreate)
        {
            roadRegistry = ScriptableObject.CreateInstance<RoadAssetRegistry>();
            AssetDatabase.CreateAsset(roadRegistry, RoadRegistryAssetPath);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(roadRegistry);
            Debug.Log($"Created RoadRegistry at {RoadRegistryAssetPath}");
        }
    }

    private void SaveRoadRegistry()
    {
        if (roadRegistry == null) return;
        EditorUtility.SetDirty(roadRegistry);
        AssetDatabase.SaveAssets();
    }

    private static void CreateTwoWayWays(
        Intersection _a,
        Intersection _b,
        Transform _parent,
        GameObject _wayPrefab,
        GameObject _lanePrefab,
        int _laneCount,
        GameObject _roadPrefab,
        float _roadPrefabBaseLength,
        bool _attachRoadPrefab,
        bool _attachIntersectionAsset
    )
    {
        // 交差点が不正なら終了
        if (_a == null || _b == null)
        {
            Debug.LogWarning("交差点の指定が不正");
            return;
        }

        // レーン数を最低1に補正
        int count = Mathf.Max(1, _laneCount);

        // A -> B / B -> A 生成
        GeneratedWayInfo infoAB = CreateWayObject(_a, _b, _parent, _wayPrefab, _lanePrefab, count, _roadPrefab, _roadPrefabBaseLength, _attachRoadPrefab);
        GeneratedWayInfo infoBA = CreateWayObject(_b, _a, _parent, _wayPrefab, _lanePrefab, count, _roadPrefab, _roadPrefabBaseLength, _attachRoadPrefab);

        // 交差点が持つ incomingWays から再構築
        RebuildIncomingLaneLinksAtIntersection(_a);
        RebuildIncomingLaneLinksAtIntersection(_b);

        // 交差点アセットを添付
        if (_attachIntersectionAsset)
        {
            AttachIntersectionAsset(_a);
            AttachIntersectionAsset(_b);
        }

        // 変更を保存
        AssetDatabase.SaveAssets();
    }

    private struct GeneratedWayInfo
    {
        public Intersection From;
        public Intersection To;
        public Way Way;
        public List<Lane> Lanes;
        public Vector3 ForwardAtTo;
    }

    [MenuItem("Tools/Way Generator")]
    public static void Open()
    {
        GetWindow<WayGeneratorWindow>("Way Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("交差点から双方向のWayを生成", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // 生成元交差点を取得
        intersectionA = (Intersection)EditorGUILayout.ObjectField(
            "Intersection A",
            intersectionA,
            typeof(Intersection),
            true
        );

        // 生成先交差点を取得
        intersectionB = (Intersection)EditorGUILayout.ObjectField(
            "Intersection B",
            intersectionB,
            typeof(Intersection),
            true
        );

        // Way親オブジェクトを取得
        waysParent = (Transform)EditorGUILayout.ObjectField(
            "Ways Parent",
            waysParent,
            typeof(Transform),
            true
        );

        // Wayプレハブを取得
        wayPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Way Prefab",
            wayPrefab,
            typeof(GameObject),
            false
        );

        // Laneプレハブを取得
        lanePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Lane Prefab",
            lanePrefab,
            typeof(GameObject),
            false
        );

        // Roadプレハブ関連
        roadPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Road Prefab",
            roadPrefab,
            typeof(GameObject),
            false
        );
        roadPrefabBaseLength = EditorGUILayout.FloatField("Road Prefab Base Length", roadPrefabBaseLength);
        attachRoadPrefab = EditorGUILayout.Toggle("Attach Road Prefab", attachRoadPrefab);
        attachIntersectionAsset = EditorGUILayout.Toggle("Attach Intersection Asset", attachIntersectionAsset);

        // Road アセット登録管理
        if (roadRegistry == null)
        {
            LoadOrCreateRoadRegistry();
        }

        EditorGUILayout.LabelField("Registered Road Assets", EditorStyles.boldLabel);
        if (roadRegistry != null)
        {
            // タグ一覧作成
            var tags = new List<string> { "All" };
            tags.AddRange(roadRegistry.entries.Select(e => string.IsNullOrEmpty(e.tag) ? "Untagged" : e.tag).Distinct());
            int tagIndex = Mathf.Max(0, tags.IndexOf(tagFilter));
            tagIndex = EditorGUILayout.Popup("Filter Tag", tagIndex, tags.ToArray());
            tagFilter = tags[tagIndex];

            EditorGUILayout.BeginHorizontal();
            newRegistryTag = EditorGUILayout.TextField("New Tag", newRegistryTag);
            if (GUILayout.Button("Apply Tag to Selected", GUILayout.Width(140)))
            {
                var sel = Selection.activeObject as GameObject;
                if (sel != null)
                {
                    var existing = roadRegistry.entries.FirstOrDefault(x => x.prefab == sel);
                    if (existing != null)
                    {
                        existing.tag = newRegistryTag;
                    }
                    else
                    {
                        roadRegistry.entries.Add(new RoadAssetEntry { prefab = sel, tag = newRegistryTag });
                    }
                    SaveRoadRegistry();
                }
                else
                {
                    Debug.LogWarning("Project window でプレハブを選択してから Apply Tag を押してください。");
                }
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < roadRegistry.entries.Count; ++i)
            {
                var entry = roadRegistry.entries[i];
                string entryTagDisplay = string.IsNullOrEmpty(entry.tag) ? "Untagged" : entry.tag;
                if (tagFilter != "All" && tagFilter != entryTagDisplay)
                    continue;

                EditorGUILayout.BeginHorizontal();
                entry.prefab = (GameObject)EditorGUILayout.ObjectField(entry.prefab, typeof(GameObject), false);
                entry.tag = EditorGUILayout.TextField(entry.tag, GUILayout.Width(100));
                if (GUILayout.Button("Use", GUILayout.Width(40)))
                {
                    roadPrefab = entry.prefab;
                }
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    roadRegistry.entries.RemoveAt(i);
                    SaveRoadRegistry();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Selected to Registry"))
            {
                var sel = Selection.activeObject as GameObject;
                if (sel != null)
                {
                    if (!roadRegistry.entries.Any(x => x.prefab == sel))
                    {
                        roadRegistry.entries.Add(new RoadAssetEntry { prefab = sel, tag = newRegistryTag });
                        SaveRoadRegistry();
                    }
                }
                else
                {
                    Debug.LogWarning("Project window でプレハブを選択してから Add Selected を押してください。");
                }
            }
            if (GUILayout.Button("Create Registry Asset"))
            {
                LoadOrCreateRoadRegistry(true);
            }
            EditorGUILayout.EndHorizontal();
        }

        // 生成するレーン数を取得
        laneCount = EditorGUILayout.IntField("Lane Count", laneCount);
        // 1未満なら1に補正
        laneCount = Mathf.Max(1, laneCount);

        EditorGUILayout.Space();
        // Network アセットからの一括生成 / エディット
        EditorGUILayout.LabelField("Road Network (Batch)", EditorStyles.boldLabel);
        roadNetworkAsset = (RoadNetworkAsset)EditorGUILayout.ObjectField("Network Asset", roadNetworkAsset, typeof(RoadNetworkAsset), false);

        // If the assigned asset was deleted from the Project window, clear reference so user can recreate
        if (roadNetworkAsset != null && !AssetDatabase.Contains(roadNetworkAsset))
        {
            roadNetworkAsset = null;
        }
        EditorGUILayout.BeginHorizontal();
        if (roadNetworkAsset == null)
        {
            if (GUILayout.Button("Create New Network Asset"))
            {
                RoadNetworkAsset newAsset = ScriptableObject.CreateInstance<RoadNetworkAsset>();
                string path = AssetDatabase.GenerateUniqueAssetPath("Assets/RoadNetwork.asset");
                AssetDatabase.CreateAsset(newAsset, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = newAsset;
                roadNetworkAsset = newAsset;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (roadNetworkAsset != null)
        {
            // Inline 編集 UI
            if (roadNetworkAsset.connections == null)
                roadNetworkAsset.connections = new System.Collections.Generic.List<RoadConnection>();

            if (GUILayout.Button("Add Connection"))
            {
                Undo.RecordObject(roadNetworkAsset, "Add Connection");
                roadNetworkAsset.connections.Add(new RoadConnection());
                EditorUtility.SetDirty(roadNetworkAsset);
            }

            for (int ci = 0; ci < roadNetworkAsset.connections.Count; ++ci)
            {
                var conn = roadNetworkAsset.connections[ci];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                conn.from = (Intersection)EditorGUILayout.ObjectField("From", conn.from, typeof(Intersection), true);
                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    Undo.RecordObject(roadNetworkAsset, "Remove Connection");
                    roadNetworkAsset.connections.RemoveAt(ci);
                    EditorUtility.SetDirty(roadNetworkAsset);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                // Ensure to list exists
                if (conn.to == null)
                    conn.to = new System.Collections.Generic.List<RoadTarget>();

                if (GUILayout.Button("Add Target"))
                {
                    Undo.RecordObject(roadNetworkAsset, "Add Target");
                    conn.to.Add(new RoadTarget());
                    EditorUtility.SetDirty(roadNetworkAsset);
                }

                for (int ti = 0; ti < conn.to.Count; ++ti)
                {
                    var target = conn.to[ti];
                    EditorGUILayout.BeginVertical("helpbox");
                    EditorGUILayout.BeginHorizontal();
                    target.intersection = (Intersection)EditorGUILayout.ObjectField("To", target.intersection, typeof(Intersection), true);
                    if (GUILayout.Button("Remove Target", GUILayout.Width(110)))
                    {
                        Undo.RecordObject(roadNetworkAsset, "Remove Target");
                        conn.to.RemoveAt(ti);
                        EditorUtility.SetDirty(roadNetworkAsset);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    target.directionType = (RoadDirectionType)EditorGUILayout.EnumPopup("Direction", target.directionType);
                    target.laneCount = EditorGUILayout.IntField("Lane Count", Mathf.Max(1, target.laneCount));
                    EditorGUILayout.LabelField("Allowed Turns");
                    EditorGUILayout.BeginHorizontal();
                    target.allowLeftTurn = EditorGUILayout.ToggleLeft("Left", target.allowLeftTurn, GUILayout.Width(60));
                    target.allowStraight = EditorGUILayout.ToggleLeft("Straight", target.allowStraight, GUILayout.Width(80));
                    target.allowRightTurn = EditorGUILayout.ToggleLeft("Right", target.allowRightTurn, GUILayout.Width(70));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Network Asset"))
            {
                EditorUtility.SetDirty(roadNetworkAsset);
                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("Generate Road Network from Asset"))
            {
                CreateRoadNetwork(roadNetworkAsset, waysParent, wayPrefab, lanePrefab, laneCount, roadPrefab, roadPrefabBaseLength, attachRoadPrefab, attachIntersectionAsset);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        // 個別生成
        GUI.enabled = intersectionA != null && intersectionB != null && intersectionA != intersectionB;
        if (GUILayout.Button("双方向のWayを生成してLaneLinkまで設定"))
        {
            CreateTwoWayWays(intersectionA, intersectionB, waysParent, wayPrefab, lanePrefab, laneCount, roadPrefab, roadPrefabBaseLength, attachRoadPrefab, attachIntersectionAsset);
        }
        GUI.enabled = true;
    }

    // SceneView 編集系およびルート一括生成は廃止。個別生成のみを提供します。

    private static void AttachIntersectionAsset(Intersection _intersection)
    {
        if (_intersection == null)
            return;

        // Use the new selector that chooses prefab based on configured shape mappings
        GameObject prefab = _intersection.GetPreferredIntersectionPrefab();
        if (prefab == null)
            return;

        // 既に子として存在するならスキップ
        Transform existing = _intersection.transform.Find("IntersectionAsset");
        if (existing != null)
            return;

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (inst == null)
            return;

        inst.name = "IntersectionAsset";
        Undo.RegisterCreatedObjectUndo(inst, "Create Intersection Asset");
        inst.transform.SetParent(_intersection.transform, false);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        EditorUtility.SetDirty(inst);
    }

    private static void CreateTwoWayWays(
        Intersection _a,
        Intersection _b,
        Transform _parent,
        GameObject _wayPrefab,
        GameObject _lanePrefab,
        int _laneCount
    )
    {
        // 既存のオーバーロードにフォワード（road prefab なし, intersection attach false）
        CreateTwoWayWays(_a, _b, _parent, _wayPrefab, _lanePrefab, _laneCount, null, 1f, false, false);
    }

    // Stage 一括生成は廃止しました。個別生成のみをサポートします。

    // Connections 一括生成は廃止しました。個別生成のみをサポートします。

    private static GeneratedWayInfo CreateWayObject(
        Intersection _from,
        Intersection _to,
        Transform _parent,
        GameObject _wayPrefab,
        GameObject _lanePrefab,
        int _laneCount,
        GameObject _roadPrefab,
        float _roadPrefabBaseLength,
        bool _attachRoadPrefab
    )
    {
        string wayName = $"Way_{_from.name}To{_to.name}";

        // 同名Wayが存在するなら再利用
        GameObject existing = null;
        // まず親配下を検索して重複生成を防ぐ（親が指定されている場合は高速）
        if (_parent != null)
        {
            Transform found = _parent.Find(wayName);
            if (found != null)
                existing = found.gameObject;
        }
        // 見つからなければグローバル検索にフォールバック（後方互換）
        if (existing == null)
        {
            existing = GameObject.Find(wayName);
        }

        if (existing != null)
        {
            Debug.LogWarning($"既に{wayName}が存在しています。既存オブジェクトを利用します。");

            Way existingWay = existing.GetComponent<Way>();

            // Wayが取れたなら交差点に設定
            if (existingWay != null)
            {
                Vector3 existingDirection = _to.transform.position - _from.transform.position;
                Undo.RecordObject(_from, $"Assign existing way on {_from.name}");
                _from.SetWayByWorldDirection(existingDirection, existingWay);
                EditorUtility.SetDirty(_from);

                Undo.RecordObject(_to, $"Register incoming way on {_to.name}");
                _to.RegisterIncomingWay(existingWay);
                EditorUtility.SetDirty(_to);
            }

            return new GeneratedWayInfo
            {
                From = _from,
                To = _to,
                Way = existingWay,
                Lanes = GetOrderedLanes(existingWay),
                ForwardAtTo = (_to.transform.position - _from.transform.position).normalized
            };
        }

        GameObject wayObj;
        // WayプレハブがあるならPrefabから生成
        if (_wayPrefab != null)
        {
            wayObj = (GameObject)PrefabUtility.InstantiatePrefab(_wayPrefab);
            wayObj.name = wayName;
        }
        else
        {
            // ないなら空オブジェクト生成
            wayObj = new GameObject(wayName);
        }

        Undo.RegisterCreatedObjectUndo(wayObj, $"Create {wayName}");

        // 親が指定されているなら親子付け
        if (_parent != null)
        {
            wayObj.transform.SetParent(_parent, true);
        }

        Vector3 fromPos = _from.transform.position;
        Vector3 toPos = _to.transform.position;
        // 中点に配置
        wayObj.transform.position = (fromPos + toPos) * 0.5f;

        // Wayコンポーネントを取得
        Way way = wayObj.GetComponent<Way>();
        // Wayがなければ追加
        if (way == null)
        {
            way = wayObj.AddComponent<Way>();
        }

        // Lanesルートを取得（なければ作成）
        Transform lanesRoot = FindOrCreateChild(wayObj.transform, "Lanes");

        // 既存Lane子を全削除して作り直す
        for (int i = lanesRoot.childCount - 1; i >= 0; --i)
        {
            Undo.DestroyObjectImmediate(lanesRoot.GetChild(i).gameObject);
        }

        List<Lane> createdLanes = new();
        // レーンオフセット（左右にずらして重なりを防ぐ）
        float laneWidth = 3.5f; // デフォルトレーン幅（m）。必要なら引数化する
        Vector3 laneDir = (toPos - fromPos).normalized;
        Vector3 perp = Vector3.Cross(laneDir, Vector3.up).normalized;

        for (int i = 0; i < _laneCount; ++i)
        {
            // Laneを生成
            Lane lane = CreateLaneObject(lanesRoot, _lanePrefab, $"Lane_{wayName}_{i}");
            // Start/Endを取得（なければ作成）
            Transform startPoint = FindOrCreateChild(lane.transform, "StartPoint");
            Transform endPoint = FindOrCreateChild(lane.transform, "EndPoint");

            // オフセット計算: 中央を基準に左右に配置
            float centerIndex = (_laneCount - 1) * 0.5f;
            float offset = (i - centerIndex) * laneWidth;

            // 始終点を配置（道路法線方向にオフセット）
            startPoint.position = fromPos + perp * offset;
            endPoint.position = toPos + perp * offset;

            // Lane参照を設定
            SetupLaneSerializedFields(lane, way, i, startPoint, endPoint);
            createdLanes.Add(lane);

            EditorUtility.SetDirty(lane);
        }

        // WayにLane配列を設定
        SetWayLanes(way, createdLanes);

        // 交差点側にWayを設定
        Vector3 direction = toPos - fromPos;
        Undo.RecordObject(_from, $"Assign way on {_from.name}");
        _from.SetWayByWorldDirection(direction, way);
        EditorUtility.SetDirty(_from);

        // 交差点側にWayを登録
        Undo.RecordObject(_to, $"Register incoming way on {_to.name}");
        _to.RegisterIncomingWay(way);
        EditorUtility.SetDirty(_to);

        EditorUtility.SetDirty(wayObj);
        EditorUtility.SetDirty(way);

        // 道路プレハブを Way に添付 (オプション)
        if (_attachRoadPrefab && _roadPrefab != null)
        {
            // laneCount を渡して幅に応じた調整を行う
            AttachRoadPrefabToWay(wayObj, fromPos, toPos, _roadPrefab, Mathf.Max(1e-6f, _roadPrefabBaseLength), _laneCount);
        }

        // Intersection にアセットを添付するのは呼び出し側で行う

        return new GeneratedWayInfo
        {
            From = _from,
            To = _to,
            Way = way,
            Lanes = createdLanes,
            ForwardAtTo = direction.normalized
        };
    }

    private static Lane CreateLaneObject(Transform _parent, GameObject _lanePrefab, string _laneName)
    {
        GameObject laneObj;
        // LaneプレハブがあるならPrefabから生成
        if (_lanePrefab != null)
        {
            laneObj = (GameObject)PrefabUtility.InstantiatePrefab(_lanePrefab);
            laneObj.name = _laneName;
        }
        else
        {
            // ないなら空オブジェクト生成
            laneObj = new GameObject(_laneName);
        }

        Undo.RegisterCreatedObjectUndo(laneObj, $"Create {_laneName}");
        laneObj.transform.SetParent(_parent, false);

        // Laneコンポーネントを取得
        Lane lane = laneObj.GetComponent<Lane>();
        // Laneがなければ追加
        if (lane == null)
        {
            lane = laneObj.AddComponent<Lane>();
        }

        return lane;
    }

    private static Transform FindOrCreateChild(Transform _parent, string _name)
    {
        // 子を取得
        Transform child = _parent.Find(_name);
        // すでにあるならそれを使う
        if (child != null)
        {
            return child;
        }

        // なければ新規作成
        GameObject childObj = new(_name);
        Undo.RegisterCreatedObjectUndo(childObj, $"Create {_name}");
        childObj.transform.SetParent(_parent, false);
        return childObj.transform;
    }

    private static void SetupLaneSerializedFields(
        Lane _lane,
        Way _way,
        int _laneIndex,
        Transform _startPoint,
        Transform _endPoint
    )
    {
        SerializedObject laneSO = new(_lane);

        // Laneの参照項目を設定
        laneSO.FindProperty("parentWay").objectReferenceValue = _way;
        laneSO.FindProperty("laneIndex").intValue = _laneIndex;
        laneSO.FindProperty("startPoint").objectReferenceValue = _startPoint;
        laneSO.FindProperty("endPoint").objectReferenceValue = _endPoint;

        laneSO.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetWayLanes(Way _way, List<Lane> _lanes)
    {
        SerializedObject waySO = new(_way);
        SerializedProperty lanesProp = waySO.FindProperty("lanes");

        // 既存配列をクリア
        lanesProp.ClearArray();

        // 生成Laneを順に設定
        for (int i = 0; i < _lanes.Count; ++i)
        {
            lanesProp.InsertArrayElementAtIndex(i);
            lanesProp.GetArrayElementAtIndex(i).objectReferenceValue = _lanes[i];
        }

        waySO.ApplyModifiedPropertiesWithoutUndo();
    }

    private static List<Lane> GetOrderedLanes(Way _way)
    {
        List<Lane> lanes = new();
        // Wayが不正なら空を返す
        if (_way == null || _way.Lanes == null)
        {
            return lanes;
        }

        // null以外のLaneを収集
        for (int i = 0; i < _way.Lanes.Count; ++i)
        {
            Lane lane = _way.Lanes[i];
            if (lane != null)
            {
                lanes.Add(lane);
            }
        }

        // LaneIndex順に並べ替え
        lanes.Sort((a, b) => a.LaneIndex.CompareTo(b.LaneIndex));
        return lanes;
    }

    private static Lane GetLaneByIndexOrDefault(Way _way, int _laneIndex)
    {
        // Wayがnullなら接続不可
        if (_way == null)
        {
            return null;
        }

        // 同じLaneIndexを優先取得
        Lane byIndex = _way.GetLane(_laneIndex);
        if (byIndex != null)
        {
            return byIndex;
        }

        // なければデフォルトLaneを返す
        return _way.GetDefaultLane();
    }

    private static void SetupLaneLinksByIntersection(GeneratedWayInfo _info)
    {
        // Lane情報が不正なら終了
        if (_info.Lanes == null || _info.Lanes.Count == 0 || _info.To == null)
        {
            return;
        }

        TurnDirection[] turnDirections =
        {
            TurnDirection.Straight,
            TurnDirection.Left,
            TurnDirection.Right,
            TurnDirection.Back
        };

        // 生成した各LaneごとにLinkを設定
        for (int laneIndex = 0; laneIndex < _info.Lanes.Count; ++laneIndex)
        {
            Lane sourceLane = _info.Lanes[laneIndex];
            // Laneがnullならスキップ
            if (sourceLane == null)
            {
                continue;
            }

            List<LaneLinkSeed> seeds = new();

            for (int i = 0; i < turnDirections.Length; ++i)
            {
                TurnDirection turnDirection = turnDirections[i];
                // 曲がり方向から接続先Wayを取得
                Way targetWay = _info.To.GetWayByTurn(_info.ForwardAtTo, turnDirection);
                // 接続先Wayがないならスキップ
                if (targetWay == null)
                {
                    continue;
                }

                // 同一LaneIndex優先で接続先Laneを取得
                Lane targetLane = GetLaneByIndexOrDefault(targetWay, sourceLane.LaneIndex);
                // 接続先Laneがないならスキップ
                if (targetLane == null)
                {
                    continue;
                }

                // Link候補を追加
                seeds.Add(new LaneLinkSeed
                {
                    TurnDirection = turnDirection,
                    NextLane = targetLane
                });
            }

            // Linkを反映
            ApplyLaneLinks(sourceLane, seeds);
        }
    }

    /// <summary>
    /// LaneLinkSeedのリストを元に、LaneのnextLaneLinksを上書きする
    /// </summary>
    /// <param name="_lane"></param>
    /// <param name="_seeds"></param>
    private static void ApplyLaneLinks(Lane _lane, List<LaneLinkSeed> _seeds)
    {
        SerializedObject laneSO = new(_lane);
        SerializedProperty linksProp = laneSO.FindProperty("nextLaneLinks");

        linksProp.ClearArray();

        for (int i = 0; i < _seeds.Count; ++i)
        {
            linksProp.InsertArrayElementAtIndex(i);
            SerializedProperty linkProp = linksProp.GetArrayElementAtIndex(i);

            linkProp.FindPropertyRelative("turnDirection").enumValueIndex = (int)_seeds[i].TurnDirection;
            linkProp.FindPropertyRelative("nextLane").objectReferenceValue = _seeds[i].NextLane;
        }

        laneSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(_lane);

        Debug.Log($"LaneLink更新: {_lane.name} / count={_seeds.Count}");
    }

    /// <summary>
    /// 交差点のIncomingWaysを元に、接続されているWayのLaneLinkをすべて再構築する
    /// </summary>
    /// <param name="_intersection"></param>
    private static void RebuildIncomingLaneLinksAtIntersection(Intersection _intersection)
    {
        if (_intersection == null)
        {
            return;
        }

        _intersection.CleanupIncomingWays();

        IReadOnlyList<Way> incomingWays = _intersection.IncomingWays;
        for (int i = 0; i < incomingWays.Count; ++i)
        {
            Way way = incomingWays[i];
            if (!TryBuildIncomingWayInfo(way, _intersection, out GeneratedWayInfo info))
            {
                continue;
            }

            SetupLaneLinksByIntersection(info);
        }
    }

    private static bool TryBuildIncomingWayInfo(Way _way, Intersection _to, out GeneratedWayInfo _info)
    {
        _info = default;

        if (_way == null || _to == null)
        {
            return false;
        }

        List<Lane> lanes = GetOrderedLanes(_way);
        if (lanes.Count == 0)
        {
            return false;
        }

        Lane baseLane = null;
        for (int i = 0; i < lanes.Count; ++i)
        {
            Lane lane = lanes[i];
            if (lane != null && lane.StartPoint != null && lane.EndPoint != null)
            {
                baseLane = lane;
                break;
            }
        }

        if (baseLane == null)
        {
            return false;
        }

        Vector3 forwardAtTo = baseLane.EndPoint.position - baseLane.StartPoint.position;
        forwardAtTo.y = 0f;
        if (forwardAtTo.sqrMagnitude <= 1e-6f)
        {
            return false;
        }

        _info = new GeneratedWayInfo
        {
            From = null,
            To = _to,
            Way = _way,
            Lanes = lanes,
            ForwardAtTo = forwardAtTo.normalized
        };

        return true;
    }

    private static void AttachRoadPrefabToWay(
        GameObject _wayObj,
        Vector3 _fromPos,
        Vector3 _toPos,
        GameObject _roadPrefab,
        float _prefabBaseLength,
        int _laneCount = 1,
        float _laneWidth = 3.5f
    )
    {
        if (_wayObj == null || _roadPrefab == null)
            return;

        GameObject roadInst = (GameObject)PrefabUtility.InstantiatePrefab(_roadPrefab);
        if (roadInst == null)
            return;

        roadInst.name = "RoadMesh";
        Undo.RegisterCreatedObjectUndo(roadInst, "Create RoadMesh for Way");
        roadInst.transform.SetParent(_wayObj.transform, true);

        Vector3 dir = _toPos - _fromPos;
        float len = dir.magnitude;
        if (len <= 1e-6f)
        {
            roadInst.transform.position = _wayObj.transform.position;
            roadInst.transform.rotation = Quaternion.identity;
            return;
        }

        Vector3 center = (_fromPos + _toPos) * 0.5f;
        roadInst.transform.position = center;
        roadInst.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        // 道路幅計算
        float roadWidth = Mathf.Max(1, _laneCount) * _laneWidth;

        // ScalableRoot を探してそこだけをスケールする
        Transform scalableRoot = roadInst.transform.Find("ScalableRoot");
        if (scalableRoot == null)
        {
            scalableRoot = roadInst.transform;
        }

        // Prefab の基準幅を推定（ScalableRoot の子メッシュから幅を測定）
        float prefabBaseWidth = 1f;
        try
        {
            var mrs = scalableRoot.GetComponentsInChildren<MeshRenderer>();
            if (mrs != null && mrs.Length > 0)
            {
                // 子メッシュ全体のローカルバウンディングボックスを集計（簡易計算）
                Bounds b = new Bounds(scalableRoot.InverseTransformPoint(mrs[0].bounds.center), Vector3.zero);
                foreach (var mr in mrs)
                {
                    Vector3 localCenter = scalableRoot.InverseTransformPoint(mr.bounds.center);
                    Vector3 size = mr.bounds.size;
                    // サイズはワールド空間なのでおおよそ取り扱う
                    b.Encapsulate(new Bounds(localCenter, size));
                }
                prefabBaseWidth = Mathf.Max(0.0001f, b.size.x);
            }
        }
        catch
        {
            prefabBaseWidth = 1f;
        }

        float lengthScale = len / Mathf.Max(0.0001f, _prefabBaseLength);
        float widthScale = roadWidth / Mathf.Max(0.0001f, prefabBaseWidth);

        Vector3 baseScale = scalableRoot.localScale;
        scalableRoot.localScale = new Vector3(baseScale.x * widthScale, baseScale.y, baseScale.z * lengthScale);

        // UV調整: マテリアルの共有アセットを書き換えないようにインスタンス化して設定
        MeshRenderer[] renderers = scalableRoot.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null) continue;

            Material[] mats = renderer.sharedMaterials;
            // インスタンス化
            for (int i = 0; i < mats.Length; ++i)
            {
                if (mats[i] == null) continue;
                mats[i] = new Material(mats[i]);
                if (mats[i].HasProperty("_MainTex"))
                {
                    mats[i].mainTextureScale = new Vector2(widthScale, lengthScale);
                }
            }
            renderer.materials = mats;
        }

        // PropsRoot があれば位置調整のみ（スケールは変更しない）
        Transform propsRoot = roadInst.transform.Find("PropsRoot");
        if (propsRoot != null)
        {
            // PropsRoot自体のスケールをリセット（ScalableRootのみを伸縮するため）
            propsRoot.localScale = Vector3.one;

            // 各プロップのローカル位置を道路幅に合わせて押し広げる（X方向のみ）
            for (int i = 0; i < propsRoot.childCount; ++i)
            {
                Transform prop = propsRoot.GetChild(i);
                if (prop == null) continue;

                Vector3 localPos = prop.localPosition;
                localPos.x *= widthScale;
                prop.localPosition = localPos;

                // プロップのスケールは保つ（WORLDスケールを保ちたい場合はさらに調整が必要）
            }
        }

        EditorUtility.SetDirty(roadInst);
    }
}
#endif
