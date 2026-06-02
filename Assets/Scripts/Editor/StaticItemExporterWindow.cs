using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using My.Map;
using My.Map.Scene;
using My.MapExport;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static My.MapExport.MapExportDatabase;

public class StaticItemExporterWindow : EditorWindow
{
    // 输入
    [SerializeField] private GameObject sceneRoot;
    [SerializeField] private Transform namedPointRoot;
    [SerializeField] private bool includeInactive = false;
    [SerializeField] private bool filterByTag = false;
    [SerializeField] private string tagFilter = "Untagged";
    [SerializeField] private bool filterByLayer = false;
    [SerializeField] private int layerFilter = 0;

    // Key 生成
    private enum KeyMode { PrefabName, AssetGUID, Path }
    [SerializeField] private KeyMode keyMode = KeyMode.PrefabName;
    [SerializeField] private string keyPrefix = ""; // 可选前缀
    [SerializeField] private bool stripInstanceSuffix = true; // 去掉 "(Clone)" 之类

    // 变换处理
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private bool roundToGrid = false;
    [SerializeField] private float gridUnit = 0.1f;

    // 分桶（Chunk）设置，与 MapChunkEditorRoot 对齐
    [SerializeField] private float chunkCellSize = 32f;
    [SerializeField] private Vector2 chunkOrigin = Vector2.zero;

    // 扫描结果缓存
    private Dictionary<(int x, int y), List<StaticPrefabItem>> chunkBuckets =
        new Dictionary<(int x, int y), List<StaticPrefabItem>>();

    // 扫描结果缓存
    private List<DynamicEntityExportGenerator> dynamicGenerator =
        new();

    private Dictionary<string, NamedPoint> namedPointCache = new();
    private Dictionary<string, NamedPath> namedPathCache = new();

    private Dictionary<(int x, int y), List<Segment2D>> chunkSegments =
        new Dictionary<(int x, int y), List<Segment2D>>();

    private readonly List<PortalNetworkExport> portalNetworkCache = new();

    [MenuItem("Window/Static Item Exporter")]
    public static void Open()
    {
        GetWindow<StaticItemExporterWindow>("Static Item Exporter");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scene Roots", EditorStyles.boldLabel);
        DrawRootsList();

        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        filterByTag = EditorGUILayout.Toggle("Filter By Tag", filterByTag);
        if (filterByTag) tagFilter = EditorGUILayout.TagField("Tag", tagFilter);
        filterByLayer = EditorGUILayout.Toggle("Filter By Layer", filterByLayer);
        if (filterByLayer) layerFilter = EditorGUILayout.LayerField("Layer", layerFilter);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Key Generation", EditorStyles.boldLabel);
        keyMode = (KeyMode)EditorGUILayout.EnumPopup("Key Mode", keyMode);
        keyPrefix = EditorGUILayout.TextField("Key Prefix", keyPrefix);
        stripInstanceSuffix = EditorGUILayout.Toggle("Strip Instance Suffix", stripInstanceSuffix);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Transform Processing", EditorStyles.boldLabel);
        positionOffset = EditorGUILayout.Vector3Field("Position Offset", positionOffset);
        roundToGrid = EditorGUILayout.Toggle("Round To Grid", roundToGrid);
        if (roundToGrid) gridUnit = EditorGUILayout.FloatField("Grid Unit", gridUnit);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Chunking (sync with Map Chunk Exporter)", EditorStyles.boldLabel);
        chunkCellSize = EditorGUILayout.FloatField("Chunk Cell Size", chunkCellSize);
        chunkOrigin = EditorGUILayout.Vector2Field("Chunk Origin", chunkOrigin);
        if (GUILayout.Button("Sync From MapChunkEditorRoot"))
        {
            SyncChunkSettingsFromScene();
        }

        var chunkEditor = MapChunkEditorUtility.Resolve(sceneRoot);
        if (chunkEditor == null)
        {
            EditorGUILayout.HelpBox("未找到 MapChunkEditorRoot，chunk 参数需与 Map Chunk Exporter 手动保持一致。", MessageType.Info);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Scan"))
        {
            ScanScene();
        }
        if (GUILayout.Button("Scan & Export"))
        {
            ScanScene();
            Export();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Result Preview", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Buckets: {chunkBuckets.Count}");

        EditorGUILayout.LabelField($"Geenetor:{dynamicGenerator.Count} points:{namedPointCache.Count} path:{namedPathCache.Count}");
        EditorGUILayout.LabelField($"Portal networks: {portalNetworkCache.Count}");
    }

    private void DrawRootsList()
    {
        sceneRoot = (GameObject)EditorGUILayout.ObjectField("Scene Root", sceneRoot, typeof(GameObject), true);
        namedPointRoot = (Transform)EditorGUILayout.ObjectField("NamedPoint Root (optional)", namedPointRoot, typeof(Transform), true);
        if (GUILayout.Button("Clear")) sceneRoot = null;
    }

    void CollectNamedPointLeaves(Transform root)
    {
        if (root == null)
        {
            return;
        }

        var stack = new Stack<Transform>();
        for (int i = 0; i < root.childCount; i++)
        {
            stack.Push(root.GetChild(i));
        }

        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (!includeInactive && !t.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (t.childCount > 0)
            {
                for (int i = 0; i < t.childCount; i++)
                {
                    stack.Push(t.GetChild(i));
                }

                continue;
            }

            var comp = t.GetComponent<NamePointGenerator>();
            var pInfo = new NamedPoint
            {
                Name = t.gameObject.name,
                Position = t.position,
                Rotation = t.rotation,
                PointType = ENamedPointType.Normal,
            };

            if (comp != null)
            {
                pInfo.PointType = comp.Info.PointType;
            }

            if (namedPointCache.ContainsKey(pInfo.Name))
            {
                Debug.LogWarning($"[Static Export] Duplicate named point '{pInfo.Name}', overwritten.");
            }

            namedPointCache[pInfo.Name] = pInfo;
        }
    }

    void SyncChunkSettingsFromScene()
    {
        MapChunkEditorUtility.SyncChunkSettings(
            MapChunkEditorUtility.Resolve(sceneRoot),
            ref chunkCellSize,
            ref chunkOrigin);
    }

    private void ScanScene()
    {
        chunkBuckets.Clear();
        dynamicGenerator.Clear();
        chunkSegments.Clear();

        namedPointCache.Clear();
        namedPathCache.Clear();
        portalNetworkCache.Clear();

        SyncChunkSettingsFromScene();

        if (sceneRoot == null)
        {
            EditorUtility.DisplayDialog("Static Export", "Please add root GameObjects.", "OK");
            return;
        }

        int count = 0;

        var staticRoot = sceneRoot.transform.Find("StaticRoot");
        int statidId = 100;
        {

            var stack = new Stack<Transform>();
            stack.Push(staticRoot);

            while (stack.Count > 0)
            {
                var t = stack.Pop();

                // 过滤
                if (!includeInactive && !t.gameObject.activeInHierarchy) continue;
                if (filterByTag && !t.CompareTag(tagFilter)) continue;
                if (filterByLayer && t.gameObject.layer != layerFilter) continue;

                // 可扩展：只导出带特定组件
                // if (t.GetComponent<MeshRenderer>() == null) continue;
                var prefabProvider = t.GetComponent<MapScenePrefabProvider>();
                if (prefabProvider != null)
                {

                    count++;

                    var ck = WorldToChunk(prefabProvider.transform.position);
                    var key = (ck.x, ck.y);
                    if (!chunkBuckets.TryGetValue(key, out var list))
                    {
                        list = new List<StaticPrefabItem>();
                        chunkBuckets[key] = list;
                    }
                    list.Add(new StaticPrefabItem
                    {
                        ItemId = ++statidId,
                        Key = prefabProvider.Key,
                        Position = prefabProvider.transform.position,
                        Rotation = prefabProvider.transform.rotation,
                        Scale = prefabProvider.transform.localScale,
                        AppearCond = prefabProvider.AppearCond,
                    });

                    continue;
                }

                // 遍历子节点
                for (int i = 0; i < t.childCount; i++)
                    stack.Push(t.GetChild(i));
            }
        }
        

        Debug.Log($"Static Export Scan finished. Collected {count} items.");
        var dynamicRoot = sceneRoot.transform.Find("DynamicRoot");

        {
            var stack = new Stack<Transform>();
            stack.Push(dynamicRoot);

            while (stack.Count > 0)
            {
                var t = stack.Pop();

                // 过滤
                if (!includeInactive && !t.gameObject.activeInHierarchy) continue;
                if (filterByTag && !t.CompareTag(tagFilter)) continue;
                if (filterByLayer && t.gameObject.layer != layerFilter) continue;

                // 可扩展：只导出带特定组件
                // if (t.GetComponent<MeshRenderer>() == null) continue;
                var generator = t.GetComponent<DynamicEntityExportGenerator>();
                if (generator != null)
                {
                    var ck = WorldToChunk(generator.transform.position);
                    var key = (ck.x, ck.y);
                    dynamicGenerator.Add(generator);
                    continue;
                }

                // 遍历子节点
                for (int i = 0; i < t.childCount; i++)
                    stack.Push(t.GetChild(i));
            }
        }

        Transform namedPointRootTransform = namedPointRoot != null
            ? namedPointRoot
            : sceneRoot.transform.Find("NamedPoint");
        if (namedPointRootTransform == null)
        {
            Debug.LogWarning("[Static Export] NamedPoint root not found, skip named points.");
        }
        else
        {
            CollectNamedPointLeaves(namedPointRootTransform);
        }

        var namedPathRoot = sceneRoot.transform.Find("NamedPath");
        if(namedPathRoot != null)
        {
            for (int i = 0; i < namedPathRoot.childCount; i++)
            {
                var t = namedPathRoot.GetChild(i);
                var comp = t.GetComponent<NamePathProvider>();
                if (comp != null)
                {
                    var path = new NamedPath();
                    path.Name = comp.Name;
                    path.Tag = comp.Tag;
                    path.Points = new();
                    foreach (var p in comp.NamedPoints)
                    {
                        path.Points.Add(p.gameObject.name);
                    }
                    namedPathCache[t.name] = path;
                }
            }
        }

        {

            var fovObstacleRoot = sceneRoot.transform.Find("StaticRoot");
            var fovObcLayer = LayerMask.NameToLayer("MapViewObc");
            int segmentIdx = 0;


            var stack = new Stack<Transform>();
            stack.Push(fovObstacleRoot);

            while (stack.Count > 0)
            {
                var t = stack.Pop();

                // 过滤
                if (!includeInactive && !t.gameObject.activeInHierarchy) continue;

                // 对于指定层的碰撞体生成视线数据
                if (t.gameObject.layer == fovObcLayer)
                {
                    var cols = t.GetComponentsInChildren<Collider2D>();
                    List<Segment2D> outList = new();
                    SegmentColliderExtractor.ExtractFromColliders(cols, fovObcLayer, outList, ref segmentIdx);
                    count += outList.Count;

                    var ck = WorldToChunk(t.position);
                    var key = (ck.x, ck.y);
                    if (!chunkSegments.TryGetValue(key, out var list))
                    {
                        list = new();
                        chunkSegments[key] = list;
                    }

                    list.AddRange(outList);
                }

                // 遍历子节点
                for (int i = 0; i < t.childCount; i++)
                    stack.Push(t.GetChild(i));
            }
        }

        foreach (var chunk1 in chunkSegments)
        {
            Debug.Log($"Geenetor: segment:{chunk1.Key} count:{chunk1.Value.Count}");
        }

        ScanPortalNetworks(sceneRoot.transform);
    }

    static PortalNetworkExport BuildPortalExport(PortalNetworkProvider prov)
    {
        var export = new PortalNetworkExport { NetworkId = prov.NetworkId };
        var seenNames = new HashSet<string>();
        foreach (var t in prov.Nodes)
        {
            if (t == null)
            {
                continue;
            }

            var id = t.gameObject.name;
            if (!seenNames.Add(id))
            {
                Debug.LogError(
                    $"[PortalNetwork] Duplicate node_id \"{id}\" in network \"{export.NetworkId}\" ({prov.gameObject.name})");
                continue;
            }

            export.Nodes.Add(new PortalNetworkNodeExport
            {
                NodeId = id,
                Position = t.position,
                Rotation = t.rotation,
            });
        }

        var edgeKeys = new HashSet<(string a, string b)>();
        foreach (var eb in prov.Edges)
        {
            if (eb == null || eb.a == null || eb.b == null)
            {
                continue;
            }

            var na = eb.a.gameObject.name;
            var nb = eb.b.gameObject.name;
            if (string.Equals(na, nb, StringComparison.Ordinal))
            {
                Debug.LogWarning($"[PortalNetwork] Self-edge ignored: {na} in \"{export.NetworkId}\"");
                continue;
            }

            if (!seenNames.Contains(na) || !seenNames.Contains(nb))
            {
                Debug.LogWarning(
                    $"[PortalNetwork] Edge ({na},{nb}) references transform not listed in Nodes of \"{export.NetworkId}\". Still exported.");
            }

            var lo = string.CompareOrdinal(na, nb) < 0 ? na : nb;
            var hi = string.CompareOrdinal(na, nb) < 0 ? nb : na;
            var key = (lo, hi);
            if (!edgeKeys.Add(key))
            {
                Debug.LogWarning($"[PortalNetwork] Duplicate undirected edge ({lo},{hi}) in \"{export.NetworkId}\"");
                continue;
            }

            Vector2 pa = eb.a.transform.position;
            Vector2 pb = eb.b.transform.position;

            float weight = eb.weight;
            if (weight == 0)
            {
                weight = (pa - pb).magnitude;
            }

            export.Edges.Add(new PortalNetworkEdgeExport
            {
                NodeA = lo,
                NodeB = hi,
                Weight = weight,
            });
        }

        return export;
    }

    void ScanPortalNetworks(Transform root)
    {
        if (root == null)
        {
            return;
        }

        var stack = new Stack<Transform>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (!includeInactive && !t.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (filterByTag && !t.CompareTag(tagFilter))
            {
                continue;
            }

            if (filterByLayer && t.gameObject.layer != layerFilter)
            {
                continue;
            }

            var prov = t.GetComponent<PortalNetworkProvider>();
            if (prov != null)
            {
                portalNetworkCache.Add(BuildPortalExport(prov));
            }

            for (int i = 0; i < t.childCount; i++)
            {
                stack.Push(t.GetChild(i));
            }
        }
    }

    static PortalNetworksJsonRoot BuildPortalNetworksJsonRoot(string areaId, List<PortalNetworkExport> list)
    {
        var entries = new PortalNetworkJsonEntry[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            var src = list[i];
            var nodes = new PortalNetworkNodeJson[src.Nodes.Count];
            for (int j = 0; j < src.Nodes.Count; j++)
            {
                var n = src.Nodes[j];
                nodes[j] = new PortalNetworkNodeJson
                {
                    node_id = n.NodeId,
                    position = n.Position,
                    rotation = n.Rotation,
                };
            }

            var edges = new PortalNetworkEdgeJson[src.Edges.Count];
            for (int j = 0; j < src.Edges.Count; j++)
            {
                var e = src.Edges[j];
                edges[j] = new PortalNetworkEdgeJson
                {
                    node_a = e.NodeA,
                    node_b = e.NodeB,
                    weight = e.Weight,
                };
            }

            entries[i] = new PortalNetworkJsonEntry
            {
                network_id = src.NetworkId,
                nodes = nodes,
                edges = edges,
            };
        }

        return new PortalNetworksJsonRoot { area_id = areaId ?? string.Empty, networks = entries };
    }

    private (string Key, Vector3 Position, Quaternion Rotation, Vector3 Scale)? MakeItemFromTransform(Transform t)
    {
        // 确定 Key
        string key = GenerateKey(t.gameObject);
        if (string.IsNullOrEmpty(key))
            return null;

        // 位置/旋转/缩放
        var pos = t.position + positionOffset;
        if (roundToGrid && gridUnit > 0f)
        {
            pos.x = Mathf.Round(pos.x / gridUnit) * gridUnit;
            pos.y = Mathf.Round(pos.y / gridUnit) * gridUnit;
            pos.z = Mathf.Round(pos.z / gridUnit) * gridUnit;
        }

        var rot = t.rotation;
        var scl = t.lossyScale; // 使用世界缩放

        return (key, pos, rot, scl);
    }

    private string GenerateKey(GameObject go)
    {
        string baseKey = "";
        switch (keyMode)
        {
            case KeyMode.PrefabName:
                baseKey = PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab
                    ? PrefabUtility.GetCorrespondingObjectFromSource(go)?.name
                    : go.name;
                break;
            case KeyMode.AssetGUID:
                {
                    var prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    if (prefab != null)
                    {
                        string path = AssetDatabase.GetAssetPath(prefab);
                        baseKey = AssetDatabase.AssetPathToGUID(path);
                    }
                    else
                    {
                        baseKey = "";
                    }
                }
                break;
            case KeyMode.Path:
                {
                    var prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    baseKey = prefab != null ? AssetDatabase.GetAssetPath(prefab) : "";
                }
                break;
        }

        if (string.IsNullOrEmpty(baseKey)) return null;

        if (stripInstanceSuffix && baseKey.EndsWith("(Clone)"))
            baseKey = baseKey.Replace("(Clone)", "").Trim();

        if (!string.IsNullOrEmpty(keyPrefix))
            baseKey = keyPrefix + baseKey;

        return baseKey;
    }

    private (int x, int y) WorldToChunk(Vector3 pos)
    {
        float px = pos.x - chunkOrigin.x;
        float py = pos.y - chunkOrigin.y;
        int cx = Mathf.FloorToInt(px / chunkCellSize);
        int cy = Mathf.FloorToInt(py / chunkCellSize);
        return (cx, cy);
    }

    private void Export()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Static Database",
            "ChunkStaticDatabase.asset",
            "asset",
            "Choose save location for static database.");

        if (string.IsNullOrEmpty(path)) return;

        var fishingMissingUniq = new List<string>();
        foreach (var dynamicGen in dynamicGenerator)
        {
            var ri = dynamicGen.RefreshInfo;
            if (ri?.InitInfo != null &&
                ri.InitInfo.EntityType == EEntityType.FishingSpot &&
                string.IsNullOrEmpty(ri.UniqName))
            {
                fishingMissingUniq.Add(dynamicGen != null ? dynamicGen.gameObject.name : "(null generator)");
            }
        }

        if (fishingMissingUniq.Count > 0)
        {
            var detail = string.Join("\n", fishingMissingUniq);
            const string title = "Map export blocked";
            var body = "FishingSpot refresh entries require a non-empty UniqName (player save key). Missing on:\n\n" + detail;
            EditorUtility.DisplayDialog(title, body, "OK");
            Debug.LogError("[MapExport] " + body.Replace("\n", " | "));
            return;
        }

        var asset = ScriptableObject.CreateInstance<MapExportDatabase>();
        asset.Buckets = new List<MapExportDatabase.ChunkExportItem>();

        Dictionary<(int x, int y), MapExportDatabase.ChunkExportItem> infos1 = new();
        

        foreach (var kv in chunkBuckets)
        {
            if(!infos1.TryGetValue(kv.Key, out var chunkItems))
            {
                chunkItems = new MapExportDatabase.ChunkExportItem
                {
                    Chunk = new MapExportDatabase.ChunkKey { X = kv.Key.x, Y = kv.Key.y },
                };
                infos1.Add(kv.Key, chunkItems);
            }

            chunkItems.StaticItems = kv.Value;
        }

        foreach (var kv in chunkSegments)
        {
            if (!infos1.TryGetValue(kv.Key, out var chunkItems))
            {
                chunkItems = new MapExportDatabase.ChunkExportItem
                {
                    Chunk = new MapExportDatabase.ChunkKey { X = kv.Key.x, Y = kv.Key.y },
                };
                infos1.Add(kv.Key, chunkItems);
            }

            chunkItems.FovStaticSegments = kv.Value;
        }

        int staticIdCounter = 100;
        HashSet<string> uniqNames = new();
        foreach (var dynamicGen in dynamicGenerator)
        {
            var refreshInfo = DynamicEntityRefreshInfoExportUtil.CloneForExport(dynamicGen.RefreshInfo);
            if (refreshInfo == null)
            {
                Debug.LogWarning($"[MapExport] Skip dynamic generator with empty RefreshInfo: {dynamicGen.gameObject.name}");
                continue;
            }

            if(!string.IsNullOrEmpty(refreshInfo.UniqName) && uniqNames.Contains(refreshInfo.UniqName))
            {
                Debug.LogError($"duplicate key {refreshInfo.UniqName} in {dynamicGen.gameObject.name}");
                continue;
            }
            refreshInfo.StaticId = staticIdCounter++;
            if (refreshInfo.InitInfo != null)
            {
                refreshInfo.InitInfo.Position = dynamicGen.transform.position;
            }

            if (refreshInfo.InitInfo is EntityInitInfo4InteractPoint initInfo4Ip &&
                initInfo4Ip.Variables != null &&
                initInfo4Ip.Variables.keys.Count > 0)
            {
                Debug.Log(
                    $"[MapExport] {dynamicGen.gameObject.name} export Variables: " +
                    $"{initInfo4Ip.Variables.keys.Count} entries");
            }

            uniqNames.Add(refreshInfo.UniqName);
            asset.EntityRefreshInfo.Add(refreshInfo);
        }

        foreach(var info in infos1)
        {
            asset.Buckets.Add(info.Value);
        }

        foreach(var p in namedPointCache)
        {
            asset.NamedPoints.Add(p.Value);
        }

        foreach (var p in namedPathCache)
        {
            asset.NamedPaths.Add(p.Value);
        }

        asset.PortalNetworks.Clear();
        asset.PortalNetworks.AddRange(portalNetworkCache);

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(asset);

        var jsonName = Path.GetFileNameWithoutExtension(path) + "_portal_networks.json";
        var jsonPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, jsonName);
        jsonPath = jsonPath.Replace("\\", "/");
        var jsonText = JsonUtility.ToJson(BuildPortalNetworksJsonRoot(asset.AreaId, portalNetworkCache), true);
        File.WriteAllText(jsonPath, jsonText);
        AssetDatabase.Refresh();
        Debug.Log($"Exported ChunkStaticDatabase: {asset.Buckets.Count} buckets -> {path}. Portal JSON: {jsonPath} ({portalNetworkCache.Count} networks)");
    }
}