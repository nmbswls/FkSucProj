using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static ChunkMapExportDatabase;

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

    // 分桶（Chunk）设置
    [SerializeField] private float chunkCellSize = 16f;
    [SerializeField] private Vector2 chunkOrigin = Vector2.zero; // 坐标原点偏移

    // 扫描结果缓存
    private Dictionary<(int x, int y), List<ChunkMapExportDatabase.StaticItem>> chunkBuckets =
        new Dictionary<(int x, int y), List<ChunkMapExportDatabase.StaticItem>>();

    // 扫描结果缓存
    private Dictionary<(int x, int y), List<DynamicEntityExportGenerator>> chunkDynamicGenerator =
        new Dictionary<(int x, int y), List<DynamicEntityExportGenerator>>();

    private Dictionary<string, Transform> namedPointCache = new();

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
        EditorGUILayout.LabelField("Chunking", EditorStyles.boldLabel);

        chunkCellSize = EditorGUILayout.FloatField("Chunk Cell Size", chunkCellSize);
        chunkOrigin = EditorGUILayout.Vector2Field("Chunk Origin", chunkOrigin);

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

        EditorGUILayout.LabelField($"Geenetor:{chunkDynamicGenerator.Count}");
    }

    private void DrawRootsList()
    {
        sceneRoot =(GameObject) EditorGUILayout.ObjectField(sceneRoot, typeof(GameObject), true); 
        if (GUILayout.Button("Clear")) sceneRoot = null;
    }

    private void ScanScene()
    {
        chunkBuckets.Clear();
        chunkDynamicGenerator.Clear();
        namedPointCache.Clear();

        if (sceneRoot == null)
        {
            EditorUtility.DisplayDialog("Static Export", "Please add root GameObjects.", "OK");
            return;
        }

        int count = 0;

        var staticRoot = sceneRoot.transform.Find("StaticRoot");
        var prefabRoot = staticRoot.transform.Find("Prefabs"); 


        for (int i = 0; i < prefabRoot.childCount; i++)
        {
            var t = prefabRoot.GetChild(i);
            // 过滤
            if (!includeInactive && !t.gameObject.activeInHierarchy) continue;
            if (filterByTag && !t.CompareTag(tagFilter)) continue;
            if (filterByLayer && t.gameObject.layer != layerFilter) continue;

            // 可扩展：只导出带特定组件
            // if (t.GetComponent<MeshRenderer>() == null) continue;

            // 构造 Item
            var item = MakeItemFromTransform(t);
            if (item.HasValue)
            {
                var val = item.Value;
                count++;

                var ck = WorldToChunk(val.Position);
                var key = (ck.x, ck.y);
                if (!chunkBuckets.TryGetValue(key, out var list))
                {
                    list = new List<ChunkMapExportDatabase.StaticItem>();
                    chunkBuckets[key] = list;
                }
                list.Add(new ChunkMapExportDatabase.StaticItem
                {
                    Key = val.Key,
                    Position = val.Position,
                    Rotation = val.Rotation,
                    Scale = val.Scale
                });
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
                    if (!chunkDynamicGenerator.TryGetValue(key, out var list))
                    {
                        list = new List<DynamicEntityExportGenerator>();
                        chunkDynamicGenerator[key] = list;
                    }
                    list.Add(generator);
                    continue;
                }

                // 遍历子节点
                for (int i = 0; i < t.childCount; i++)
                    stack.Push(t.GetChild(i));
            }
        }

        var namedPoint = sceneRoot.transform.Find("NamedPoint");
        for (int i = 0; i < namedPoint.childCount; i++)
        {
            var t = namedPoint.GetChild(i);
            namedPointCache[t.name] = t; 
        }
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
        float py = pos.z - chunkOrigin.y; // 常见做法：用 x-z 平面分块
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

        var asset = ScriptableObject.CreateInstance<ChunkMapExportDatabase>();
        asset.Buckets = new List<ChunkMapExportDatabase.ChunkItems>();

        Dictionary<(int x, int y), ChunkMapExportDatabase.ChunkItems> infos1 = new();
        

        foreach (var kv in chunkBuckets)
        {
            if(!infos1.TryGetValue(kv.Key, out var chunkItems))
            {
                chunkItems = new ChunkMapExportDatabase.ChunkItems
                {
                    Chunk = new ChunkMapExportDatabase.ChunkKey { X = kv.Key.x, Y = kv.Key.y },
                };
                infos1.Add(kv.Key, chunkItems);
            }

            chunkItems.StaticItems = kv.Value;
        }

        foreach(var kv in chunkDynamicGenerator)
        {
            if (!infos1.TryGetValue(kv.Key, out var chunkItems))
            {
                chunkItems = new ChunkMapExportDatabase.ChunkItems
                {
                    Chunk = new ChunkMapExportDatabase.ChunkKey { X = kv.Key.x, Y = kv.Key.y },
                };
                infos1.Add(kv.Key, chunkItems);
            }
            int unitId = 100;
            foreach(var dynamicGen in kv.Value)
            {
                var refreshInfo = new DynamicEntityRefreshInfo()
                {
                    UniqId = unitId++,
                    EntityType = dynamicGen.EntityType,
                    CfgId = dynamicGen.CfgId,
                    Position = dynamicGen.transform.position,
                    FaceDir = dynamicGen.transform.right,


                    BindRoomId = dynamicGen.BindRoomId,
                    AppearCond = dynamicGen.AppearCond,
                };

                if (dynamicGen is DynamicNpcExportGenerator unitEntity)
                {
                    var initInfo = new DynamicEntityInitInfo4Unit();
                    initInfo.EnmityMode = unitEntity.EnmityMode;
                    initInfo.MoveMode = unitEntity.MoveMode;
                    initInfo.IsPeace = unitEntity.IsPeace;

                    refreshInfo.InitInfo = initInfo;
                }
                else if(dynamicGen is DynamicPatrolGroupExportGenerator patrolGroupGen)
                {
                    var initInfo = new DynamicEntityInitInfo4PatrolGroup();

                    initInfo.MoveSpeed = patrolGroupGen.MoveSpeed;
                    initInfo.Waypoints.AddRange(patrolGroupGen.Waypoints);
                    initInfo.LoopMode = patrolGroupGen.LoopMode;
                    initInfo.GroupUnits = patrolGroupGen.GroupUnits;

                    refreshInfo.InitInfo = initInfo;
                }

                chunkItems.EntityRefreshInfo.Add(refreshInfo);

            }
        }

        foreach(var info in infos1)
        {
            asset.Buckets.Add(info.Value);
        }

        foreach(var p in namedPointCache)
        {
            asset.NamedPoints.Add(new NamedPoint()
            {
                Name = p.Key,
                Rotation = p.Value.rotation,
                Position = p.Value.position,
            });
        }

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(asset);
        Debug.Log($"Exported ChunkStaticDatabase: {asset.Buckets.Count} buckets -> {path}");
    }
}