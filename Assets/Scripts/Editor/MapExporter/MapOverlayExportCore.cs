using System;
using System.Collections.Generic;
using System.IO;
using My.Map;
using My.Map.Scene;
using My.MapExport;
using UnityEditor;
using UnityEngine;
using static My.MapExport.MapExportDatabase;

// Overlay 级 MapExport 扫描与写出
public static class MapOverlayExportCore
{
    public const string CommonFolderName = "Common";
    public const string StaticOverlayFolderName = "StaticOverlay";

    public struct OverlayScanResult
    {
        public string OverlayId;
        public int StaticCount;
        public int DynamicCount;
    }

    public struct ScanSummary
    {
        public List<OverlayScanResult> Overlays;
        public int NamedPointCount;
        public int NamedPathCount;
        public int PortalNetworkCount;
    }

    public struct ExportResult
    {
        public bool Success;
        public string Message;
        public int ExportedCount;
    }

    struct ScanData
    {
        public Dictionary<(int x, int y), List<StaticPrefabItem>> ChunkBuckets;
        public Dictionary<(int x, int y), List<Segment2D>> ChunkSegments;
        public List<DynamicEntityExportGenerator> DynamicGenerators;
        public Dictionary<string, NamedPoint> NamedPoints;
        public Dictionary<string, NamedPath> NamedPaths;
        public List<PortalNetworkExport> PortalNetworks;
    }

    public static ScanSummary ScanAll(GameObject areaRoot, MapChunkEditorRoot chunkEditor, string variantSceneName)
    {
        var summary = new ScanSummary { Overlays = new List<OverlayScanResult>() };
        if (areaRoot == null)
        {
            return summary;
        }

        var chunkSize = chunkEditor != null
            ? chunkEditor.ChunkWorldSize
            : MapChunkEditorSettings.GetOrCreate().EffectiveChunkWorldSize;
        var chunkOrigin = chunkEditor != null ? chunkEditor.ChunkOrigin : Vector2.zero;

        var shared = ScanSharedVariantData(areaRoot, chunkSize, chunkOrigin);
        summary.NamedPointCount = shared.NamedPoints.Count;
        summary.NamedPathCount = shared.NamedPaths.Count;
        summary.PortalNetworkCount = shared.PortalNetworks.Count;

        var overlays = MapExporterConfigReader.GetOverlaysForVariantScene(variantSceneName);
        if (overlays.Count == 0)
        {
            var legacy = ScanOverlay(areaRoot, null, chunkSize, chunkOrigin);
            summary.Overlays.Add(new OverlayScanResult
            {
                OverlayId = "(legacy)",
                StaticCount = CountStaticItems(legacy.ChunkBuckets),
                DynamicCount = legacy.DynamicGenerators.Count,
            });
            return summary;
        }

        foreach (var overlay in overlays)
        {
            var data = ScanOverlay(areaRoot, overlay.Id, chunkSize, chunkOrigin);
            summary.Overlays.Add(new OverlayScanResult
            {
                OverlayId = overlay.Id,
                StaticCount = CountStaticItems(data.ChunkBuckets),
                DynamicCount = data.DynamicGenerators.Count,
            });
        }

        return summary;
    }

    public static ExportResult ExportOverlay(
        GameObject areaRoot,
        MapChunkEditorRoot chunkEditor,
        string overlayId,
        string mapDataName)
    {
        if (areaRoot == null)
        {
            return Fail("AreaRoot is null.");
        }

        if (string.IsNullOrWhiteSpace(mapDataName))
        {
            return Fail("Map data name is empty.");
        }

        var chunkSize = chunkEditor != null
            ? chunkEditor.ChunkWorldSize
            : MapChunkEditorSettings.GetOrCreate().EffectiveChunkWorldSize;
        var chunkOrigin = chunkEditor != null ? chunkEditor.ChunkOrigin : Vector2.zero;

        var overlayData = ScanOverlay(areaRoot, overlayId, chunkSize, chunkOrigin);
        var shared = ScanSharedVariantData(areaRoot, chunkSize, chunkOrigin);

        var fishingError = ValidateFishingSpots(overlayData.DynamicGenerators);
        if (fishingError != null)
        {
            return Fail(fishingError);
        }

        var asset = BuildDatabase(overlayData, shared, mapDataName);
        var folder = $"Assets/Resources/{MapVariantMapResources.MapExportFolder}";
        EnsureFolder("Assets/Resources");
        EnsureFolder(folder);
        var path = $"{folder}/{mapDataName}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<MapExportDatabase>(path);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        WritePortalNetworksJson(folder, mapDataName, asset.AreaId, shared.PortalNetworks);

        return new ExportResult
        {
            Success = true,
            Message = $"Exported {path} (static buckets={asset.Buckets.Count}, entities={asset.EntityRefreshInfo.Count})",
            ExportedCount = 1,
        };
    }

    public static ExportResult ExportAllOverlays(GameObject areaRoot, MapChunkEditorRoot chunkEditor, string variantSceneName)
    {
        var overlays = MapExporterConfigReader.GetOverlaysForVariantScene(variantSceneName);
        if (overlays.Count == 0)
        {
            return Fail($"No overlays found for variant scene '{variantSceneName}'.");
        }

        int count = 0;
        var messages = new List<string>();
        foreach (var overlay in overlays)
        {
            var result = ExportOverlay(areaRoot, chunkEditor, overlay.Id, overlay.MapDataName);
            if (!result.Success)
            {
                return result;
            }

            count += result.ExportedCount;
            messages.Add(result.Message);
        }

        return new ExportResult
        {
            Success = true,
            ExportedCount = count,
            Message = string.Join("\n", messages),
        };
    }

    static ScanData ScanSharedVariantData(GameObject areaRoot, float chunkSize, Vector2 chunkOrigin)
    {
        var data = NewScanData();
        var namedPointRoot = areaRoot.transform.Find("NamedPoint");
        if (namedPointRoot != null)
        {
            CollectNamedPointLeaves(namedPointRoot, data.NamedPoints);
        }

        var namedPathRoot = areaRoot.transform.Find("NamedPath");
        if (namedPathRoot != null)
        {
            CollectNamedPaths(namedPathRoot, data.NamedPaths);
        }

        ScanPortalNetworks(areaRoot.transform, data.PortalNetworks);
        ScanFovSegments(areaRoot.transform.Find("StaticRoot"), chunkSize, chunkOrigin, data.ChunkSegments);
        return data;
    }

    static ScanData ScanOverlay(GameObject areaRoot, string overlayId, float chunkSize, Vector2 chunkOrigin)
    {
        var data = NewScanData();
        int nextItemId = 100;
        foreach (var root in ResolveStaticScanRoots(areaRoot.transform, overlayId))
        {
            ScanStaticPrefabs(root, chunkSize, chunkOrigin, data.ChunkBuckets, ref nextItemId);
        }

        foreach (var root in ResolveDynamicScanRoots(areaRoot.transform, overlayId))
        {
            ScanDynamicEntities(root, data.DynamicGenerators);
        }

        return data;
    }

    static IEnumerable<Transform> ResolveStaticScanRoots(Transform areaRoot, string overlayId)
    {
        var staticRoot = areaRoot.Find("StaticRoot");
        if (staticRoot == null)
        {
            yield break;
        }

        var overlayRoot = staticRoot.Find(StaticOverlayFolderName);
        if (overlayRoot != null)
        {
            var common = overlayRoot.Find(CommonFolderName);
            if (common != null)
            {
                yield return common;
            }

            if (!string.IsNullOrEmpty(overlayId))
            {
                var specific = overlayRoot.Find(overlayId);
                if (specific != null)
                {
                    yield return specific;
                }
            }

            yield break;
        }

        // 旧结构：StaticRoot 下除 GridRoot 外全部扫描
        yield return staticRoot;
    }

    static IEnumerable<Transform> ResolveDynamicScanRoots(Transform areaRoot, string overlayId)
    {
        var dynamicRoot = areaRoot.Find("DynamicRoot");
        if (dynamicRoot == null)
        {
            yield break;
        }

        if (string.IsNullOrEmpty(overlayId))
        {
            yield return dynamicRoot;
            yield break;
        }

        var common = dynamicRoot.Find(CommonFolderName);
        if (common != null)
        {
            yield return common;
        }

        var specific = dynamicRoot.Find(overlayId);
        if (specific != null)
        {
            yield return specific;
            yield break;
        }

        // 无 overlay 子文件夹时回退整棵 DynamicRoot
        if (common == null)
        {
            yield return dynamicRoot;
        }
    }

    static void ScanStaticPrefabs(
        Transform root,
        float chunkSize,
        Vector2 chunkOrigin,
        Dictionary<(int x, int y), List<StaticPrefabItem>> buckets,
        ref int nextItemId)
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
            if (!t.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (t.name == "GridRoot")
            {
                continue;
            }

            var prefabProvider = t.GetComponent<MapScenePrefabProvider>();
            if (prefabProvider != null)
            {
                var ck = WorldToChunk(prefabProvider.transform.position, chunkSize, chunkOrigin);
                if (!buckets.TryGetValue(ck, out var list))
                {
                    list = new List<StaticPrefabItem>();
                    buckets[ck] = list;
                }

                list.Add(new StaticPrefabItem
                {
                    ItemId = ++nextItemId,
                    Key = prefabProvider.Key,
                    Position = prefabProvider.transform.position,
                    Rotation = prefabProvider.transform.rotation,
                    Scale = prefabProvider.transform.localScale,
                    AppearCond = prefabProvider.AppearCond,
                });
                continue;
            }

            for (int i = 0; i < t.childCount; i++)
            {
                stack.Push(t.GetChild(i));
            }
        }
    }

    static void ScanDynamicEntities(Transform root, List<DynamicEntityExportGenerator> generators)
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
            if (!t.gameObject.activeInHierarchy)
            {
                continue;
            }

            var generator = t.GetComponent<DynamicEntityExportGenerator>();
            if (generator != null)
            {
                generators.Add(generator);
                continue;
            }

            for (int i = 0; i < t.childCount; i++)
            {
                stack.Push(t.GetChild(i));
            }
        }
    }

    static void ScanFovSegments(
        Transform staticRoot,
        float chunkSize,
        Vector2 chunkOrigin,
        Dictionary<(int x, int y), List<Segment2D>> chunkSegments)
    {
        if (staticRoot == null)
        {
            return;
        }

        var fovLayer = LayerMask.NameToLayer("MapViewObc");
        int segmentIdx = 0;
        var stack = new Stack<Transform>();
        stack.Push(staticRoot);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (!t.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (t.gameObject.layer == fovLayer)
            {
                var cols = t.GetComponentsInChildren<Collider2D>();
                var outList = new List<Segment2D>();
                SegmentColliderExtractor.ExtractFromColliders(cols, fovLayer, outList, ref segmentIdx);
                var ck = WorldToChunk(t.position, chunkSize, chunkOrigin);
                if (!chunkSegments.TryGetValue(ck, out var list))
                {
                    list = new List<Segment2D>();
                    chunkSegments[ck] = list;
                }

                list.AddRange(outList);
            }

            for (int i = 0; i < t.childCount; i++)
            {
                stack.Push(t.GetChild(i));
            }
        }
    }

    static void CollectNamedPointLeaves(Transform root, Dictionary<string, NamedPoint> cache)
    {
        var stack = new Stack<Transform>();
        for (int i = 0; i < root.childCount; i++)
        {
            stack.Push(root.GetChild(i));
        }

        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (!t.gameObject.activeInHierarchy)
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

            cache[pInfo.Name] = pInfo;
        }
    }

    static void CollectNamedPaths(Transform namedPathRoot, Dictionary<string, NamedPath> cache)
    {
        for (int i = 0; i < namedPathRoot.childCount; i++)
        {
            var t = namedPathRoot.GetChild(i);
            var comp = t.GetComponent<NamePathProvider>();
            if (comp == null)
            {
                continue;
            }

            var path = new NamedPath
            {
                Name = comp.Name,
                Tag = comp.Tag,
                Points = new List<string>(),
            };
            foreach (var p in comp.NamedPoints)
            {
                path.Points.Add(p.gameObject.name);
            }

            cache[t.name] = path;
        }
    }

    static void ScanPortalNetworks(Transform root, List<PortalNetworkExport> cache)
    {
        var stack = new Stack<Transform>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (!t.gameObject.activeInHierarchy)
            {
                continue;
            }

            var prov = t.GetComponent<PortalNetworkProvider>();
            if (prov != null)
            {
                cache.Add(BuildPortalExport(prov));
            }

            for (int i = 0; i < t.childCount; i++)
            {
                stack.Push(t.GetChild(i));
            }
        }
    }

    static MapExportDatabase BuildDatabase(ScanData overlayData, ScanData sharedData, string mapDataName)
    {
        var asset = ScriptableObject.CreateInstance<MapExportDatabase>();
        asset.AreaId = mapDataName;
        asset.Buckets = new List<ChunkExportItem>();

        var bucketMap = new Dictionary<(int x, int y), ChunkExportItem>();
        MergeBuckets(overlayData.ChunkBuckets, bucketMap);
        MergeSegments(overlayData.ChunkSegments, bucketMap);
        MergeSegments(sharedData.ChunkSegments, bucketMap);

        foreach (var item in bucketMap.Values)
        {
            asset.Buckets.Add(item);
        }

        int staticIdCounter = 100;
        var uniqNames = new HashSet<string>();
        foreach (var dynamicGen in overlayData.DynamicGenerators)
        {
            var refreshInfo = DynamicEntityRefreshInfoExportUtil.CloneForExport(dynamicGen.RefreshInfo);
            if (refreshInfo == null)
            {
                Debug.LogWarning($"[MapExport] Skip dynamic generator with empty RefreshInfo: {dynamicGen.gameObject.name}");
                continue;
            }

            if (!string.IsNullOrEmpty(refreshInfo.UniqName) && uniqNames.Contains(refreshInfo.UniqName))
            {
                Debug.LogError($"[MapExport] Duplicate UniqName '{refreshInfo.UniqName}' on {dynamicGen.gameObject.name}");
                continue;
            }

            refreshInfo.StaticId = staticIdCounter++;
            if (refreshInfo.InitInfo != null)
            {
                refreshInfo.InitInfo.Position = dynamicGen.transform.position;
            }

            if (!string.IsNullOrEmpty(refreshInfo.UniqName))
            {
                uniqNames.Add(refreshInfo.UniqName);
            }

            asset.EntityRefreshInfo.Add(refreshInfo);
        }

        foreach (var p in sharedData.NamedPoints.Values)
        {
            asset.NamedPoints.Add(p);
        }

        foreach (var p in sharedData.NamedPaths.Values)
        {
            asset.NamedPaths.Add(p);
        }

        asset.PortalNetworks.AddRange(sharedData.PortalNetworks);
        return asset;
    }

    static void MergeBuckets(
        Dictionary<(int x, int y), List<StaticPrefabItem>> source,
        Dictionary<(int x, int y), ChunkExportItem> target)
    {
        foreach (var kv in source)
        {
            if (!target.TryGetValue(kv.Key, out var chunk))
            {
                chunk = new ChunkExportItem
                {
                    Chunk = new ChunkKey { X = kv.Key.x, Y = kv.Key.y },
                };
                target[kv.Key] = chunk;
            }

            chunk.StaticItems.AddRange(kv.Value);
        }
    }

    static void MergeSegments(
        Dictionary<(int x, int y), List<Segment2D>> source,
        Dictionary<(int x, int y), ChunkExportItem> target)
    {
        foreach (var kv in source)
        {
            if (!target.TryGetValue(kv.Key, out var chunk))
            {
                chunk = new ChunkExportItem
                {
                    Chunk = new ChunkKey { X = kv.Key.x, Y = kv.Key.y },
                };
                target[kv.Key] = chunk;
            }

            chunk.FovStaticSegments.AddRange(kv.Value);
        }
    }

    static string ValidateFishingSpots(List<DynamicEntityExportGenerator> generators)
    {
        var missing = new List<string>();
        foreach (var dynamicGen in generators)
        {
            var ri = dynamicGen.RefreshInfo;
            if (ri?.InitInfo != null &&
                ri.InitInfo.EntityType == EEntityType.FishingSpot &&
                string.IsNullOrEmpty(ri.UniqName))
            {
                missing.Add(dynamicGen.gameObject.name);
            }
        }

        if (missing.Count == 0)
        {
            return null;
        }

        return "FishingSpot requires UniqName. Missing on: " + string.Join(", ", missing);
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
                continue;
            }

            var lo = string.CompareOrdinal(na, nb) < 0 ? na : nb;
            var hi = string.CompareOrdinal(na, nb) < 0 ? nb : na;
            if (!edgeKeys.Add((lo, hi)))
            {
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

    static void WritePortalNetworksJson(
        string folder,
        string mapDataName,
        string areaId,
        List<PortalNetworkExport> networks)
    {
        if (networks == null || networks.Count == 0)
        {
            return;
        }

        var jsonName = mapDataName + "_portal_networks.json";
        var jsonPath = Path.Combine(folder, jsonName).Replace("\\", "/");
        var jsonText = JsonUtility.ToJson(BuildPortalNetworksJsonRoot(areaId, networks), true);
        File.WriteAllText(jsonPath, jsonText);
        AssetDatabase.Refresh();
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

    static ScanData NewScanData()
    {
        return new ScanData
        {
            ChunkBuckets = new Dictionary<(int x, int y), List<StaticPrefabItem>>(),
            ChunkSegments = new Dictionary<(int x, int y), List<Segment2D>>(),
            DynamicGenerators = new List<DynamicEntityExportGenerator>(),
            NamedPoints = new Dictionary<string, NamedPoint>(),
            NamedPaths = new Dictionary<string, NamedPath>(),
            PortalNetworks = new List<PortalNetworkExport>(),
        };
    }

    static int CountStaticItems(Dictionary<(int x, int y), List<StaticPrefabItem>> buckets)
    {
        int count = 0;
        foreach (var kv in buckets)
        {
            count += kv.Value.Count;
        }

        return count;
    }

    static (int x, int y) WorldToChunk(Vector3 pos, float chunkSize, Vector2 chunkOrigin)
    {
        float px = pos.x - chunkOrigin.x;
        float py = pos.y - chunkOrigin.y;
        int cx = Mathf.FloorToInt(px / chunkSize);
        int cy = Mathf.FloorToInt(py / chunkSize);
        return (cx, cy);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent ?? "Assets", name);
    }

    static ExportResult Fail(string message)
    {
        Debug.LogError("[MapExport] " + message);
        return new ExportResult { Success = false, Message = message };
    }
}
