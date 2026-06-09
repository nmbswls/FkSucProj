using cfg.demo;
using My;
using My.Map;
using My.Map.Logic;
using My.MapExport;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapChunkManager : MonoBehaviour
{
    public enum LoadState
    {
        Unloaded,
        Loading,
        Loaded,
        Unloading
    }

    public sealed class ChunkRecord
    {
        public readonly ChunkCoord coord;
        public bool desiredVisible;
        public LoadState loadState;
        public float lastBecameDesired;
        public float lastBecameUndesired;
        public float lastBecameLoaded;
        public bool cancelAfterLoad;
        public bool refreshAfterLoad;
        public List<(GameObject go, int itemId)> staticInstances;
        public GameObject backgroundInstance;
        public GameObject tilemapInstance;

        public ChunkRecord(ChunkCoord c)
        {
            coord = c;
            desiredVisible = false;
            loadState = LoadState.Unloaded;
            cancelAfterLoad = false;
            refreshAfterLoad = false;
            staticInstances = null;
            backgroundInstance = null;
            tilemapInstance = null;
            lastBecameDesired = 0f;
            lastBecameUndesired = 0f;
            lastBecameLoaded = 0f;
        }
    }

    [Header("Debounce / Hysteresis")]
    [SerializeField] private float chunkEnterDelay = 0.05f;
    [SerializeField] private float chunkExitDelay = 0.15f;
    [SerializeField] private float chunkMinStay = 0.30f;

    [Header("Load Limits")]
    [SerializeField] private int maxConcurrentLoads = 2;
    [SerializeField] private int batchObjectsPerSlice = 8;
    [SerializeField] private int yieldEveryNObjects = 4;

    readonly Dictionary<ChunkCoord, ChunkRecord> _chunks = new Dictionary<ChunkCoord, ChunkRecord>();
    readonly HashSet<ChunkCoord> _ringScratch = new HashSet<ChunkCoord>();

    int _concurrentLoading;
    float _lastRefreshChunkTime;
    bool _pendingVisibleChunkRefresh;

    IAssetProvider _asset;
    IAssetProviderAsync _assetAsync;
    Func<bool> _canRefreshNow;

    MapExportDatabase ExportDb =>
        MainGameManager.Instance.gameLogicManager.AreaManager.cacheDatabase;

    MapChunkDatabase ChunkDb =>
        MainGameManager.Instance.gameLogicManager.AreaManager.cacheChunkDatabase;

    float ChunkWorldSize =>
        ChunkDb != null && ChunkDb.ChunkWorldSize > 0f ? ChunkDb.ChunkWorldSize : GameConsts.ChunkCellSize;

    Vector2 ChunkOrigin => ChunkDb != null ? ChunkDb.ChunkOrigin : Vector2.zero;

    bool IsChunkInLogicBounds(ChunkCoord coord)
    {
        var db = ChunkDb;
        if (db == null)
        {
            return true;
        }

        return db.IsChunkInLogicBounds(coord);
    }

    public void Initialize(IAssetProvider asset, IAssetProviderAsync assetAsync, Func<bool> canRefreshNow)
    {
        _asset = asset;
        _assetAsync = assetAsync;
        _canRefreshNow = canRefreshNow;
    }

    public void RefreshChunks(Vector3 playerPos, int chunkRing)
    {
        RefreshChunksUnion(new[] { playerPos }, chunkRing);
    }

    public void RefreshChunksUnion(IReadOnlyList<Vector3> centers, int chunkRing)
    {
        if (centers == null || centers.Count == 0)
        {
            return;
        }

        var unionRing = new HashSet<ChunkCoord>();
        for (int i = 0; i < centers.Count; i++)
        {
            var center = WorldToChunk(centers[i]);
            MapChunkUtility.CollectChunkRing(center, chunkRing, _ringScratch);
            foreach (var c in _ringScratch)
            {
                unionRing.Add(c);
            }
        }

        _ringScratch.Clear();
        foreach (var c in unionRing)
        {
            if (!IsChunkInLogicBounds(c))
            {
                continue;
            }

            _ringScratch.Add(c);

            if (!_chunks.TryGetValue(c, out var rec))
            {
                rec = new ChunkRecord(c);
                _chunks.Add(c, rec);
            }

            rec.desiredVisible = true;
        }

        var keys = new List<ChunkCoord>(_chunks.Keys);
        foreach (var c in keys)
        {
            if (!_ringScratch.Contains(c) || !IsChunkInLogicBounds(c))
            {
                _chunks[c].desiredVisible = false;
            }
        }

        int startedLoadsThisFrame = 0;
        int startedUnloadsThisFrame = 0;

        foreach (var c in keys)
        {
            TickChunkUnload(_chunks[c], LogicTime.time, ref startedUnloadsThisFrame);
        }

        foreach (var c in keys)
        {
            TickChunkLoad(_chunks[c], LogicTime.time, ref startedLoadsThisFrame);
        }

        if (LogicTime.time > _lastRefreshChunkTime + 2.0f)
        {
            _lastRefreshChunkTime = LogicTime.time;
        }
    }

    public bool IsWorldPosChunkLoaded(Vector3 worldPos)
    {
        var coord = WorldToChunk(worldPos);
        return _chunks.TryGetValue(coord, out var rec) && rec.loadState == LoadState.Loaded;
    }

    // 可见范围内仍有未完成的静态 chunk 加载/卸载
    public bool HasPendingVisibleLoads()
    {
        if (_concurrentLoading > 0)
        {
            return true;
        }

        foreach (var rec in _chunks.Values)
        {
            if (!rec.desiredVisible)
            {
                continue;
            }

            if (rec.loadState != LoadState.Loaded)
            {
                return true;
            }
        }

        return false;
    }

    public void RequestVisibleChunkRefresh()
    {
        _pendingVisibleChunkRefresh = true;
    }

    public void ProcessPendingVisibleChunkRefresh()
    {
        if (!_pendingVisibleChunkRefresh)
        {
            return;
        }

        _pendingVisibleChunkRefresh = false;
        RefreshVisibleLoadedChunks();
    }

    public void RefreshVisibleLoadedChunks()
    {
        if (!CanRefreshNow(true, "RefreshVisibleLoadedChunks"))
        {
            return;
        }

        var keys = new List<ChunkCoord>(_chunks.Keys);
        foreach (var coord in keys)
        {
            if (!_chunks.TryGetValue(coord, out var rec))
            {
                continue;
            }

            if (!rec.desiredVisible || rec.loadState != LoadState.Loaded)
            {
                continue;
            }

            ForceUpdateOneChunk(coord);
        }
    }

    public void ForceUpdateOneChunk(ChunkCoord coord)
    {
        if (!CanRefreshNow(true, $"ForceUpdateOneChunk({coord})"))
        {
            return;
        }

        if (!_chunks.TryGetValue(coord, out var record))
        {
            Debug.LogError("ForceUpdateOneChunk chunk not loaded");
            return;
        }

        if (record.loadState != LoadState.Loaded)
        {
            if (record.loadState == LoadState.Loading)
            {
                record.refreshAfterLoad = true;
            }

            return;
        }

        var instances = new List<(GameObject, int)>();
        var currentInstances = record.staticInstances ?? new List<(GameObject, int)>();

        foreach (var item in GetChunkStaticPrefabs(coord))
        {
            var existObjInfo = currentInstances.Find(a => a.Item2 == item.ItemId);

            if (item.AppearCond != null &&
                !MainGameManager.Instance.gameLogicManager.CheckCommonCond(item.AppearCond))
            {
                if (existObjInfo.Item1 != null)
                {
                    _asset.Release(existObjInfo.Item1);
                }

                continue;
            }

            if (existObjInfo.Item1 == null)
            {
                GameObject go = null;
                try
                {
                    go = _asset.Instantiate(ResolveStaticPrefabResourceKey(item.Key));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                if (go == null)
                {
                    Debug.LogError($"[MapChunkManager] ForceUpdateOneChunk({coord}) instantiate failed: Prefab/{item.Key}");
                }
                else if (!TryAttachStaticPrefab(go, item.Position, item.Rotation, item.Scale,
                             $"ForceUpdateOneChunk({coord}) item={item.Key}"))
                {
                    _asset.Release(go);
                    continue;
                }
                else
                {
                    instances.Add((go, item.ItemId));
                }
            }
            else
            {
                instances.Add(existObjInfo);
            }
        }

        record.staticInstances = instances;
    }

    public async Task CleanupAllAsync()
    {
        try
        {
            var chunkList = new List<ChunkRecord>(_chunks.Values);

            foreach (var rec in chunkList)
            {
                rec.cancelAfterLoad = true;
                await ReleaseChunkContent(rec);
                rec.loadState = LoadState.Unloaded;
                rec.desiredVisible = false;
                rec.lastBecameDesired = 0f;
                rec.lastBecameUndesired = 0f;
                rec.lastBecameLoaded = 0f;
            }

            _chunks.Clear();
            _concurrentLoading = 0;
            _pendingVisibleChunkRefresh = false;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public ChunkCoord WorldToChunk(Vector3 pos)
    {
        var logicPos = MainGameManager.Instance.GetLogicPosFromWorldPos(pos);
        return MapChunkUtility.WorldToChunk(logicPos, ChunkOrigin, ChunkWorldSize);
    }

    public bool IsWorldPosWalkable(Vector3 worldPos)
    {
        var coord = WorldToChunk(worldPos);
        if (!_chunks.TryGetValue(coord, out var rec) || rec.loadState != LoadState.Loaded)
        {
            return false;
        }

        if (rec.tilemapInstance == null)
        {
            return false;
        }

        var tilemaps = rec.tilemapInstance.GetComponentsInChildren<Tilemap>(true);
        if (tilemaps == null || tilemaps.Length == 0)
        {
            return false;
        }

        foreach (var ground in tilemaps)
        {
            if (ground == null)
            {
                continue;
            }

            var cell = ground.WorldToCell(worldPos);
            if (!ground.cellBounds.Contains(cell))
            {
                continue;
            }

            if (ground.GetTile(cell) != null)
            {
                return true;
            }
        }

        return false;
    }

    static string ResolveStaticPrefabResourceKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        if (key.StartsWith("MapChunk/", StringComparison.Ordinal))
        {
            return key;
        }

        return "Prefab/" + key;
    }

    IEnumerable<StaticPrefabItem> GetChunkStaticPrefabs(ChunkCoord c)
    {
        if (ExportDb == null)
        {
            yield break;
        }

        foreach (var item in ExportDb.GetChunkStaticItems(c.X, c.Y))
        {
            yield return item;
        }
    }

    bool CanRefreshNow(bool logFailure, string context)
    {
        if (_canRefreshNow != null && _canRefreshNow())
        {
            return true;
        }

        if (logFailure)
        {
            Debug.LogError($"[MapChunkManager] {context} chunk refresh blocked");
        }

        return false;
    }

    bool TryAttachStaticPrefab(GameObject go, Vector3 position, Quaternion rotation, Vector3 scale, string context)
    {
        if (go == null)
        {
            Debug.LogError($"[MapChunkManager] {context} attach static prefab failed: GameObject is null");
            return false;
        }

        var staticRoot = MainGameManager.Instance?.GetWorldMapVariantRoot("1");
        if (staticRoot == null)
        {
            Debug.LogError($"[MapChunkManager] {context} attach static prefab failed: MapVariantRoot is null");
            return false;
        }

        go.transform.SetParent(staticRoot, false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = scale;
        go.SetActive(true);
        return true;
    }

    void TickChunkLoad(ChunkRecord rec, float now, ref int startedLoadsThisFrame)
    {
        switch (rec.loadState)
        {
            case LoadState.Unloaded:
                if (!rec.desiredVisible)
                {
                    return;
                }

                if (now - rec.lastBecameDesired < chunkEnterDelay)
                {
                    return;
                }

                if (_concurrentLoading >= maxConcurrentLoads)
                {
                    return;
                }

                startedLoadsThisFrame++;
                StartChunkLoad(rec);
                break;

            case LoadState.Loading:
                if (!rec.desiredVisible)
                {
                    rec.cancelAfterLoad = true;
                    if (rec.lastBecameUndesired == 0f)
                    {
                        rec.lastBecameUndesired = now;
                    }
                }

                break;

            case LoadState.Loaded:
                if (!rec.desiredVisible)
                {
                    if (rec.lastBecameUndesired == 0f)
                    {
                        rec.lastBecameUndesired = now;
                    }

                    if (now - rec.lastBecameLoaded < chunkMinStay)
                    {
                        return;
                    }

                    if (now - rec.lastBecameUndesired < chunkExitDelay)
                    {
                        return;
                    }
                }
                else if (LogicTime.time > _lastRefreshChunkTime + 2.0f)
                {
                    ForceUpdateOneChunk(rec.coord);
                }

                break;

            case LoadState.Unloading:
                break;
        }
    }

    void TickChunkUnload(ChunkRecord rec, float now, ref int startedUnloadsThisFrame)
    {
        switch (rec.loadState)
        {
            case LoadState.Loaded:
                if (!rec.desiredVisible)
                {
                    if (now - rec.lastBecameLoaded < chunkMinStay)
                    {
                        return;
                    }

                    if (rec.lastBecameUndesired == 0f)
                    {
                        rec.lastBecameUndesired = now;
                    }

                    if (now - rec.lastBecameUndesired < chunkExitDelay)
                    {
                        return;
                    }

                    startedUnloadsThisFrame++;
                    StartChunkUnload(rec);
                }

                break;

            case LoadState.Loading:
                if (!rec.desiredVisible)
                {
                    rec.cancelAfterLoad = true;
                    if (rec.lastBecameUndesired == 0f)
                    {
                        rec.lastBecameUndesired = now;
                    }
                }

                break;
        }
    }

    async void StartChunkLoad(ChunkRecord rec)
    {
        if (rec.loadState != LoadState.Unloaded)
        {
            return;
        }

        if (!CanRefreshNow(true, $"StartChunkLoad({rec.coord})"))
        {
            return;
        }

        rec.loadState = LoadState.Loading;
        rec.cancelAfterLoad = false;
        rec.refreshAfterLoad = false;
        rec.lastBecameUndesired = 0f;
        _concurrentLoading++;

        var staticInstances = new List<(GameObject, int)>();
        var batchBuffer = new List<StaticPrefabItem>(batchObjectsPerSlice);
        int objCountSinceYield = 0;

        foreach (var item in GetChunkStaticPrefabs(rec.coord))
        {
            if (item.AppearCond != null && item.AppearCond.Type != ECommonCheckType.None &&
                !MainGameManager.Instance.gameLogicManager.CheckCommonCond(item.AppearCond))
            {
                continue;
            }

            batchBuffer.Add(item);
            if (batchBuffer.Count >= batchObjectsPerSlice)
            {
                objCountSinceYield = await InstantiateStaticBatch(batchBuffer, staticInstances, objCountSinceYield);
                batchBuffer.Clear();
            }
        }

        if (batchBuffer.Count > 0)
        {
            objCountSinceYield = await InstantiateStaticBatch(batchBuffer, staticInstances, objCountSinceYield);
        }

        GameObject backgroundInstance = null;
        GameObject tilemapInstance = null;
        if (!rec.cancelAfterLoad && rec.desiredVisible)
        {
            (backgroundInstance, tilemapInstance) = await LoadMapLayerAsync(rec.coord);
        }

        if (!CanRefreshNow(true, $"StartChunkLoad complete({rec.coord})"))
        {
            await ReleaseInstances(staticInstances);
            await ReleaseMapLayer(backgroundInstance, tilemapInstance);
            if (_chunks.TryGetValue(rec.coord, out var stale) && stale == rec)
            {
                rec.staticInstances = null;
                rec.backgroundInstance = null;
                rec.tilemapInstance = null;
                rec.loadState = LoadState.Unloaded;
            }

            _concurrentLoading = Mathf.Max(0, _concurrentLoading - 1);
            return;
        }

        if (!_chunks.TryGetValue(rec.coord, out var cur) || cur != rec)
        {
            await ReleaseInstances(staticInstances);
            await ReleaseMapLayer(backgroundInstance, tilemapInstance);
            _concurrentLoading = Mathf.Max(0, _concurrentLoading - 1);
            return;
        }

        if (rec.cancelAfterLoad || !rec.desiredVisible)
        {
            await ReleaseInstances(staticInstances);
            await ReleaseMapLayer(backgroundInstance, tilemapInstance);
            rec.staticInstances = null;
            rec.backgroundInstance = null;
            rec.tilemapInstance = null;
            rec.loadState = LoadState.Unloaded;
            _concurrentLoading = Mathf.Max(0, _concurrentLoading - 1);
            return;
        }

        rec.staticInstances = staticInstances;
        rec.backgroundInstance = backgroundInstance;
        rec.tilemapInstance = tilemapInstance;
        rec.loadState = LoadState.Loaded;
        rec.lastBecameLoaded = LogicTime.time;
        _concurrentLoading = Mathf.Max(0, _concurrentLoading - 1);

        if (rec.refreshAfterLoad)
        {
            ForceUpdateOneChunk(rec.coord);
            return;
        }

        if (ExportDb != null && WorldAreaManager.Instance?.SegmentProvider != null)
        {
            var segments = ExportDb.GetChunkSegments(rec.coord.X, rec.coord.Y);
            WorldAreaManager.Instance.SegmentProvider.AddSegments(rec.coord.ToString(), segments);
        }
    }

    async Task<(GameObject background, GameObject tilemap)> LoadMapLayerAsync(ChunkCoord coord)
    {
        var db = ChunkDb;
        if (db == null || !db.HasChunkContent || _assetAsync == null)
        {
            return (null, null);
        }

        var item = db.GetChunkItem(coord);
        if (item == null)
        {
            return (null, null);
        }

        GameObject background = null;
        GameObject tilemap = null;
        var min = MapChunkUtility.ChunkWorldMin(coord, ChunkOrigin, ChunkWorldSize);
        var root = WorldAreaManager.Instance?.currentRoot;

        if (!string.IsNullOrEmpty(item.BackgroundKey))
        {
            try
            {
                background = await _assetAsync.InstantiateAsync(item.BackgroundKey);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            if (background != null)
            {
                var parent = root != null && root.BackgroundChunkRoot != null
                    ? root.BackgroundChunkRoot
                    : root != null ? root.transform : null;
                if (parent != null)
                {
                    background.transform.SetParent(parent, false);
                }

                background.transform.position = min;
                background.SetActive(true);
            }
        }

        if (!string.IsNullOrEmpty(item.TilemapKey))
        {
            try
            {
                tilemap = await _assetAsync.InstantiateAsync(item.TilemapKey);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            if (tilemap != null)
            {
                var parent = root != null && root.TilemapChunkRoot != null
                    ? root.TilemapChunkRoot
                    : root != null ? root.transform : null;
                if (parent != null)
                {
                    tilemap.transform.SetParent(parent, false);
                }

                tilemap.transform.position = min;
                tilemap.SetActive(true);
            }
        }

        return (background, tilemap);
    }

    async Task<int> InstantiateStaticBatch(List<StaticPrefabItem> items, List<(GameObject, int)> instances,
        int objCountSinceYield)
    {
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            GameObject go = null;
            try
            {
                go = await _assetAsync.InstantiateAsync(ResolveStaticPrefabResourceKey(it.Key));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            if (go == null)
            {
                Debug.LogError($"[MapChunkManager] InstantiateBatch failed: Prefab/{it.Key}");
            }
            else if (!TryAttachStaticPrefab(go, it.Position, it.Rotation, it.Scale,
                         $"InstantiateBatch item={it.Key}"))
            {
                await _assetAsync.ReleaseAsync(go);
                continue;
            }
            else
            {
                instances.Add((go, it.ItemId));
            }

            objCountSinceYield++;
            if (objCountSinceYield >= yieldEveryNObjects)
            {
                objCountSinceYield = 0;
                await Task.Yield();
            }
        }

        return objCountSinceYield;
    }

    async void StartChunkUnload(ChunkRecord rec)
    {
        if (rec.loadState == LoadState.Unloaded || rec.loadState == LoadState.Unloading)
        {
            return;
        }

        rec.loadState = LoadState.Unloading;
        await ReleaseChunkContent(rec);

        if (_chunks.TryGetValue(rec.coord, out var cur) && cur == rec)
        {
            rec.loadState = LoadState.Unloaded;
            rec.lastBecameUndesired = 0f;
        }

        if (WorldAreaManager.Instance?.SegmentProvider != null)
        {
            WorldAreaManager.Instance.SegmentProvider.RemoveSource(rec.coord.ToString());
        }
    }

    async Task ReleaseChunkContent(ChunkRecord rec)
    {
        var staticList = rec.staticInstances ?? new List<(GameObject, int)>();
        rec.staticInstances = null;
        await ReleaseInstances(staticList);
        await ReleaseMapLayer(rec.backgroundInstance, rec.tilemapInstance);
        rec.backgroundInstance = null;
        rec.tilemapInstance = null;
    }

    async Task ReleaseInstances(List<(GameObject, int)> list)
    {
        if (_assetAsync == null || list == null)
        {
            return;
        }

        List<(GameObject, int)> slice = new List<(GameObject, int)>(batchObjectsPerSlice);
        for (int i = 0; i < list.Count; i++)
        {
            slice.Add(list[i]);
            if (slice.Count >= batchObjectsPerSlice)
            {
                await ReleaseSlice(slice);
                slice.Clear();
            }
        }

        if (slice.Count > 0)
        {
            await ReleaseSlice(slice);
        }
    }

    async Task ReleaseMapLayer(GameObject background, GameObject tilemap)
    {
        if (_assetAsync == null)
        {
            return;
        }

        if (background != null)
        {
            try
            {
                await _assetAsync.ReleaseAsync(background);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        if (tilemap != null)
        {
            try
            {
                await _assetAsync.ReleaseAsync(tilemap);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    async Task ReleaseSlice(List<(GameObject, int)> slice)
    {
        for (int i = 0; i < slice.Count; i++)
        {
            try
            {
                await _assetAsync.ReleaseAsync(slice[i].Item1);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            if ((i + 1) % yieldEveryNObjects == 0)
            {
                await Task.Yield();
            }
        }

        await Task.Yield();
    }
}
