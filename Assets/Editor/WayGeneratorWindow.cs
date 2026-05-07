#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
    // ステージ一括生成用
    private Transform stageParent;
    private bool connectConsecutive = true;
    private bool closeLoop = false;
    // 接続リスト入力 (例: "A->B", "A<->B", 複数はカンマ区切り: "A->B,C")
    private string connectionsText = "";
    // ルート一括生成用
    private List<Intersection> route = new List<Intersection>();
    // 道路プレハブ自動添付
    private GameObject roadPrefab;
    private float roadPrefabBaseLength = 1f;
    private bool attachRoadPrefab = false;
    // Sceneクリックでルート追加
    private bool sceneClickMode = false;
    // 交差点アセットを自動配置
    private bool attachIntersectionAsset = false;
    // 登録済み道路アセットの管理
    private RoadAssetRegistry roadRegistry;
    private const string RoadRegistryAssetPath = "Assets/Editor/RoadAssetRegistry.asset";
    private string newRegistryTag = "";
    private string tagFilter = "All";
    // Editor window scroll position
    private Vector2 scrollPos = Vector2.zero;

    private struct LaneLinkSeed
    {
        public TurnDirection TurnDirection;
        public Lane NextLane;
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
        // Begin scrollable area
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

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

        // ステージ一括生成用設定
        stageParent = (Transform)EditorGUILayout.ObjectField(
            "Stage Parent",
            stageParent,
            typeof(Transform),
            true
        );

        connectConsecutive = EditorGUILayout.Toggle("Connect Consecutive", connectConsecutive);
        closeLoop = EditorGUILayout.Toggle("Close Loop", closeLoop);

        EditorGUILayout.Space();
        GUILayout.Label("Connections (one per line). Format: From->To or From<->To. Multiple targets: From->A,B", EditorStyles.wordWrappedLabel);
        connectionsText = EditorGUILayout.TextArea(connectionsText, GUILayout.Height(80f));

        EditorGUILayout.Space();

        // 入力が有効ならボタン有効
        GUI.enabled = intersectionA != null && intersectionB != null && intersectionA != intersectionB;

        if (GUILayout.Button("双方向のWayを生成してLaneLinkまで設定"))
        {
            // Way/Lane/LaneLinkを一括生成
            CreateTwoWayWays(intersectionA, intersectionB, waysParent, wayPrefab, lanePrefab, laneCount, roadPrefab, roadPrefabBaseLength, attachRoadPrefab, attachIntersectionAsset);
        }

        // ステージ一括生成ボタン
        GUI.enabled = stageParent != null && connectConsecutive;
        if (GUILayout.Button("Stage Parent から一括生成 (隣接接続)"))
        {
            CreateStageFromParent(stageParent, waysParent, wayPrefab, lanePrefab, laneCount, connectConsecutive, closeLoop, roadPrefab, roadPrefabBaseLength, attachRoadPrefab, attachIntersectionAsset);
        }
        GUI.enabled = true;

        // Connections テキストからの生成
        GUI.enabled = stageParent != null && !string.IsNullOrEmpty(connectionsText.Trim());
        if (GUILayout.Button("Connections から一括生成"))
        {
            CreateFromConnections(stageParent, waysParent, wayPrefab, lanePrefab, laneCount, connectionsText, roadPrefab, roadPrefabBaseLength, attachRoadPrefab, attachIntersectionAsset);
        }
        GUI.enabled = true;

        EditorGUILayout.Space();
        GUILayout.Label("ルート生成", EditorStyles.boldLabel);

        // ルート生成用の道路アセット設定（ここで登録できるようにする）
        EditorGUILayout.LabelField("Route Road Asset", EditorStyles.miniBoldLabel);
        roadPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Road Prefab (for route)",
            roadPrefab,
            typeof(GameObject),
            false
        );
        roadPrefabBaseLength = EditorGUILayout.FloatField("Road Prefab Base Length (for route)", roadPrefabBaseLength);
        attachRoadPrefab = EditorGUILayout.Toggle("Attach Road Prefab (route)", attachRoadPrefab);

        EditorGUILayout.BeginHorizontal();
        newRegistryTag = EditorGUILayout.TextField("Registry Tag", newRegistryTag);
        if (GUILayout.Button("Add Road Prefab to Registry", GUILayout.Width(200)))
        {
            if (roadPrefab != null)
            {
                if (roadRegistry == null) LoadOrCreateRoadRegistry(true);
                if (!roadRegistry.entries.Any(x => x.prefab == roadPrefab))
                {
                    roadRegistry.entries.Add(new RoadAssetEntry { prefab = roadPrefab, tag = newRegistryTag });
                    SaveRoadRegistry();
                    Debug.Log($"Added {roadPrefab.name} to Road Registry with tag '{newRegistryTag}'");
                }
                else
                {
                    Debug.LogWarning("This prefab is already registered.");
                }
            }
            else
            {
                Debug.LogWarning("Select a Road Prefab (for route) before adding to registry.");
            }
        }
        if (GUILayout.Button("Add Selected to Registry", GUILayout.Width(180)))
        {
            var sel = Selection.activeObject as GameObject;
            if (sel != null)
            {
                if (roadRegistry == null) LoadOrCreateRoadRegistry(true);
                if (!roadRegistry.entries.Any(x => x.prefab == sel))
                {
                    roadRegistry.entries.Add(new RoadAssetEntry { prefab = sel, tag = newRegistryTag });
                    SaveRoadRegistry();
                    Debug.Log($"Added {sel.name} to Road Registry with tag '{newRegistryTag}'");
                }
                else
                {
                    Debug.LogWarning("Selected prefab is already registered.");
                }
            }
            else
            {
                Debug.LogWarning("Select a prefab in Project window before using Add Selected to Registry.");
            }
        }
        EditorGUILayout.EndHorizontal();

        // Sceneクリックモード
        sceneClickMode = EditorGUILayout.Toggle("Scene Click Mode (add points)", sceneClickMode);
        EditorGUILayout.HelpBox("SceneViewで左クリックすると Intersection を順にルートに追加します。", MessageType.Info);

        int count = Mathf.Max(2, EditorGUILayout.IntField("ポイント数", route.Count));
        while (route.Count < count) route.Add(null);
        while (route.Count > count) route.RemoveAt(route.Count - 1);

        for (int i = 0; i < route.Count; ++i)
        {
            route[i] = (Intersection)EditorGUILayout.ObjectField(
                $"Point {i}",
                route[i],
                typeof(Intersection),
                true
            );
        }

        GUI.enabled = route.Count >= 2 && route.All(r => r != null);
        if (GUILayout.Button("ルートを一括生成"))
        {
            for (int i = 0; i < route.Count - 1; ++i)
            {
                CreateTwoWayWays(route[i], route[i + 1], waysParent, wayPrefab, lanePrefab, laneCount, roadPrefab, roadPrefabBaseLength, attachRoadPrefab, attachIntersectionAsset);
            }
            Debug.Log($"ルート生成完了: points={route.Count}");
        }
        GUI.enabled = true;

        // End scrollable area
        EditorGUILayout.EndScrollView();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView _sceneView)
    {
        if (!sceneClickMode)
            return;

        Event e = Event.current;
        if (e == null) return;

        // 左クリックで Intersection を追加
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            // PickGameObject expects GUI point
            GameObject picked = HandleUtility.PickGameObject(e.mousePosition, false);
            if (picked != null)
            {
                Intersection inter = picked.GetComponent<Intersection>();
                if (inter != null)
                {
                    route.Add(inter);
                    Repaint();
                    e.Use();
                }
            }
        }

        // Scene 上にラベル・ハンドルを描画
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        // 線で接続を表示
        Handles.color = Color.green;
        for (int i = 0; i < route.Count - 1; ++i)
        {
            Intersection a = route[i];
            Intersection b = route[i + 1];
            if (a == null || b == null) continue;
            Handles.DrawLine(a.transform.position, b.transform.position);
        }

        // 各ポイントにマーカーとラベル、削除ボタンを表示
        for (int i = 0; i < route.Count; ++i)
        {
            Intersection inter = route[i];
            if (inter == null) continue;

            Vector3 pos = inter.transform.position;

            // マーカー
            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.25f, EventType.Repaint);

            // ラベル
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.normal.textColor = Color.white;
            Handles.Label(pos + Vector3.up * 0.5f, $"[{i}] {inter.name}", labelStyle);

            // 削除ボタン
            Vector3 btnPos = pos + Vector3.up * 0.95f;
            if (Handles.Button(btnPos, Quaternion.identity, 0.12f, 0.14f, Handles.CubeHandleCap))
            {
                route.RemoveAt(i);
                Repaint();
                break; // 変更したのでループを抜ける
            }
        }
    }

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

    /// <summary>
    /// 指定した親配下の直下子に存在する Intersection を順に読み取り、隣接する交差点間で Way を一括生成する
    /// </summary>
    private static void CreateStageFromParent(
        Transform _stageParent,
        Transform _waysParent,
        GameObject _wayPrefab,
        GameObject _lanePrefab,
        int _laneCount,
        bool _connectConsecutive,
        bool _closeLoop,
        GameObject _roadPrefab,
        float _roadPrefabBaseLength,
        bool _attachRoadPrefab,
        bool _attachIntersectionAsset
    )
    {
        if (_stageParent == null)
        {
            Debug.LogWarning("Stage Parent が指定されていません。");
            return;
        }

        // 直下の Intersection を順に取得
        List<Intersection> intersections = new();
        for (int i = 0; i < _stageParent.childCount; ++i)
        {
            Transform child = _stageParent.GetChild(i);
            if (child == null) continue;
            Intersection inter = child.GetComponent<Intersection>();
            if (inter != null)
            {
                intersections.Add(inter);
            }
        }

        // 子順(Hierarchy)は必ずしも安定したルート順ではないため、名前順でソートして安定化する
        intersections.Sort((a, b) => a.name.CompareTo(b.name));

        if (intersections.Count < 2)
        {
            Debug.LogWarning("Stage Parent 配下に 2 つ以上の Intersection が必要です。");
            return;
        }

        int count = Mathf.Max(1, _laneCount);

        // 隣接接続
        if (_connectConsecutive)
        {
            for (int i = 0; i < intersections.Count - 1; ++i)
            {
                Intersection a = intersections[i];
                Intersection b = intersections[i + 1];
                // CreateTwoWayWays を使って双方向を安全に生成する（内部で incoming links 再構築を行う）
                CreateTwoWayWays(a, b, _waysParent, _wayPrefab, _lanePrefab, count, _roadPrefab, _roadPrefabBaseLength, _attachRoadPrefab, _attachIntersectionAsset);
            }

            // ループで最後と最初を繋ぐ
            if (_closeLoop && intersections.Count >= 2)
            {
                Intersection first = intersections[0];
                Intersection last = intersections[intersections.Count - 1];
                CreateTwoWayWays(last, first, _waysParent, _wayPrefab, _lanePrefab, count, _roadPrefab, _roadPrefabBaseLength, _attachRoadPrefab, _attachIntersectionAsset);
            }
        }

        // すべての交差点の incoming link を再構築
        for (int i = 0; i < intersections.Count; ++i)
        {
            RebuildIncomingLaneLinksAtIntersection(intersections[i]);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Stage 一括生成が完了しました: intersections={intersections.Count}");
    }

    /// <summary>
    /// Connections テキストを解析して Way を生成する
    /// サポート形式:
    ///   From->To
    ///   From<->To  (双方向)
    ///   From->A,B,C (カンマで複数)
    /// コメント行は # で始める
    /// </summary>
    private static void CreateFromConnections(
        Transform _stageParent,
        Transform _waysParent,
        GameObject _wayPrefab,
        GameObject _lanePrefab,
        int _laneCount,
        string _connectionsText,
        GameObject _roadPrefab,
        float _roadPrefabBaseLength,
        bool _attachRoadPrefab,
        bool _attachIntersectionAsset
    )
    {
        if (_stageParent == null)
        {
            Debug.LogWarning("Stage Parent が指定されていません。");
            return;
        }

        // 子から Intersection を収集 (名前で検索できるよう辞書化)
        var dict = new Dictionary<string, Intersection>();
        for (int i = 0; i < _stageParent.childCount; ++i)
        {
            Transform child = _stageParent.GetChild(i);
            if (child == null) continue;
            Intersection inter = child.GetComponent<Intersection>();
            if (inter != null)
            {
                dict[child.name] = inter;
            }
        }

        if (dict.Count == 0)
        {
            Debug.LogWarning("Stage Parent 配下に Intersection が見つかりませんでした。");
            return;
        }

        int laneCount = Mathf.Max(1, _laneCount);

        // 片方向で作成したものがある場合、最終的に再構築を行うために集める
        var touchedIntersections = new HashSet<Intersection>();

        string[] lines = _connectionsText.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            bool bidir = false;
            string[] parts = null;

            if (line.Contains("<->"))
            {
                bidir = true;
                parts = line.Split(new[] { "<->" }, System.StringSplitOptions.RemoveEmptyEntries);
            }
            else if (line.Contains("->"))
            {
                parts = line.Split(new[] { "->" }, System.StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                Debug.LogWarning($"無効な形式の行をスキップします: {line}");
                continue;
            }

            if (parts == null || parts.Length < 2)
            {
                Debug.LogWarning($"無効な接続指定をスキップします: {line}");
                continue;
            }

            string fromName = parts[0].Trim();
            string toPart = parts[1].Trim();
            // 宛先はカンマ区切りで複数指定可能
            string[] toNames = toPart.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (!dict.TryGetValue(fromName, out Intersection fromInter))
            {
                Debug.LogWarning($"From Intersection が見つかりません: {fromName}");
                continue;
            }

            foreach (string toRaw in toNames)
            {
                string toName = toRaw.Trim();
                if (!dict.TryGetValue(toName, out Intersection toInter))
                {
                    Debug.LogWarning($"To Intersection が見つかりません: {toName}");
                    continue;
                }

                    if (bidir)
                    {
                        // 双方向は既存の安全なAPIを使用
                        CreateTwoWayWays(fromInter, toInter, _waysParent, _wayPrefab, _lanePrefab, laneCount, _roadPrefab, _roadPrefabBaseLength, _attachRoadPrefab, _attachIntersectionAsset);
                    }
                else
                {
                    // 片方向は作成して、後でまとめて incoming link を再構築する
                        CreateWayObject(fromInter, toInter, _waysParent, _wayPrefab, _lanePrefab, laneCount, _roadPrefab, _roadPrefabBaseLength, _attachRoadPrefab);
                    touchedIntersections.Add(fromInter);
                    touchedIntersections.Add(toInter);
                }
            }
        }

        // 片方向作成分の再構築
        foreach (var inter in touchedIntersections)
        {
            RebuildIncomingLaneLinksAtIntersection(inter);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Connections からの生成が完了しました。");
    }

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
        GameObject existing = GameObject.Find(wayName);
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
        for (int i = 0; i < _laneCount; ++i)
        {
            // Laneを生成
            Lane lane = CreateLaneObject(lanesRoot, _lanePrefab, $"Lane_{wayName}_{i}");
            // Start/Endを取得（なければ作成）
            Transform startPoint = FindOrCreateChild(lane.transform, "StartPoint");
            Transform endPoint = FindOrCreateChild(lane.transform, "EndPoint");

            // 始終点を配置
            startPoint.position = fromPos;
            endPoint.position = toPos;

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
            AttachRoadPrefabToWay(wayObj, fromPos, toPos, _roadPrefab, Mathf.Max(1e-6f, _roadPrefabBaseLength));
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

    private static void AttachRoadPrefabToWay(GameObject _wayObj, Vector3 _fromPos, Vector3 _toPos, GameObject _roadPrefab, float _prefabBaseLength)
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

        roadInst.transform.position = (_fromPos + _toPos) * 0.5f;
        roadInst.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        // スケール z を長さに合わせる。Prefab の基準長さが _prefabBaseLength と仮定
        Vector3 baseScale = roadInst.transform.localScale;
        float scaleZ = len / _prefabBaseLength;
        roadInst.transform.localScale = new Vector3(baseScale.x, baseScale.y, baseScale.z * scaleZ);

        EditorUtility.SetDirty(roadInst);
    }
}
#endif