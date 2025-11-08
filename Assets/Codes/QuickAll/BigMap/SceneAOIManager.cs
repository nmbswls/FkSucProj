using Map.Entity;
using Map.Logic.Chunk;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI.Table;


public class SceneAOIManager : MonoBehaviour
{
    public static SceneAOIManager Instance;

    public string AreaId { get; set; }

    [Header("Player & AOI")]
    public Transform player;
    public float aoiRadius = 20f;   // 动态对象可见半径（圆或方形）
    public int chunkRing = 1;       // 玩家所在Chunk及周边环数

    [Tooltip("离开AOI迟滞边界（离开判定半径 = aoiRadius + radiusHysteresis）")]
    public float radiusHysteresis = 2f;

    [Header("Debounce & Grace")]
    public float enterGraceSeconds = 0.15f;
    public float exitGraceSeconds = 0.3f;

    [Header("Factories & Assets")]
    [SerializeField] private MonoBehaviour presentationFactorySource; // 赋值为实现 IPresentationFactory 的组件
    [SerializeField] private MonoBehaviour assetProviderSource;       // 赋值为实现 IAssetProvider 的组件

    // 动态实体：网格桶（cellSize 用 chunkCellSize 或更细粒度）
    public int dynamicCellSize = 1;

    // 内部状态
    private IPresentationFactoryAsync _presentationFactory;
    private IAssetProviderAsync _assetAsync;
    private IAssetProvider _asset;

    // 动态实体空间索引：cell坐标 -> 实体集合
    private readonly Dictionary<(int, int), HashSet<ILogicEntity>> _buckets = new();
    // 已在AOI中的实体ID -> Presentation

    //// 静态Chunk已加载实例：Chunk坐标 -> GameObject列表
    //private readonly Dictionary<ChunkCoord, List<GameObject>> _loadedChunks = new();
    //// 当前Chunk集合
    //private HashSet<ChunkCoord> _currentChunks = new();

    // 实体 AOI 状态（定时器 + 是否显示）
    private class AOIEntry
    {
        public ILogicEntity entity;
        public bool isShown;               // 期望显示（满足定时器后显示）
        public float enterTimer;           // 进入延迟累计
        public float exitTimer;            // 离开延迟累计
        public bool lastInsideInner;
        public bool lastInsideOuter;

        // === 新增: 异步创建状态与展示引用 ===
        public bool creating;              // 正在 SpawnAsync
        public bool canceledDuringCreate;  // 创建过程中被取消（离开或卸载）
        public IScenePresentation pres;         // 已创建的展示（可能为 null）
        public Vector2 pos;

    }

    private readonly Dictionary<long, AOIEntry> _aoiStates = new(); // id -> entry

    protected ChunkMapExportDatabase ExportDb;
    public IEnumerable<ChunkMapExportDatabase.StaticItem> GetChunkPrefabs(ChunkCoord c)
    {
        var it = ExportDb.GetChunkStaticItems(c.X, c.Y);
        return it;
    }


    public void InitArea(string areaId)
    {
        this.AreaId = areaId;
        ExportDb = Resources.Load<ChunkMapExportDatabase>($"Area/{areaId}");

        // player  绑定
        if(player == null)
        {
            player = MainGameManager.Instance.playerScenePresenter.transform;
        }
    }


    private void Awake()
    {
        _presentationFactory = presentationFactorySource as IPresentationFactoryAsync;
        _asset = assetProviderSource as IAssetProvider;
        _assetAsync = assetProviderSource as IAssetProviderAsync;
        if (_presentationFactory == null)
            Debug.LogError("AOIManager: presentationFactorySource must implement IPresentationFactory.");
        if (_asset == null)
            Debug.LogError("AOIManager: assetProviderSource must implement IAssetProvider.");

        Instance = this;
    }

    private void Update()
    {
        if (player == null) return;
        if (string.IsNullOrEmpty(AreaId)) return;

        // 1) 动态实体 AOI 刷新（网格桶 + 半径范围）
        RefreshDynamicAOI(player.position, Time.deltaTime);

        // 2) 静态 Chunk AOI 刷新（九宫格/环）
        RefreshStaticChunks(player.position);
    }

    // ===== 动态实体接口 =====

    public IScenePresentation GetActivePresentation(long instId)
    {
        _aoiStates.TryGetValue(instId, out var aoiEntry);
        if (aoiEntry == null) return null;
        return aoiEntry.pres;
    }

    public IEnumerable<IScenePresentation> GetAllActivePresentation()
    {
        foreach(var aoiState in _aoiStates.Values)
        {
            if(aoiState.pres != null)
            {
                yield return aoiState.pres;
            }
        }
    }


    public void RegisterEntity(ILogicEntity entity, Vector2 worldPos)
    {
        var cell = ToDynamicCell(worldPos);
        if (!_buckets.TryGetValue(cell, out var set))
            _buckets[cell] = set = new HashSet<ILogicEntity>();
        set.Add(entity);

        if (!_aoiStates.TryGetValue(entity.Id, out var entry))
        {
            _aoiStates[entity.Id] = new AOIEntry
            {
                entity = entity,
                isShown = false,
                enterTimer = 0f,
                exitTimer = 0f,
                lastInsideInner = false,
                lastInsideOuter = false,
                creating = false,              // === 新增 ===
                canceledDuringCreate = false, // === 新增 ===
                pres = null,                   // === 新增 ===
                pos = worldPos,
            };
        }
        else
        {
            entry.pos = worldPos;
        }
    }

    public void UnregisterEntity(ILogicEntity entity)
    {
        _aoiStates.TryGetValue(entity.Id, out var entry);
        if(entry == null)
        {
            Debug.LogError($"UnregisterEntity not gound:{entity.Id}");
            return;
        }

        var cell = ToDynamicCell(entry.pos);
        if (_buckets.TryGetValue(cell, out var set))
        {
            set.Remove(entity);
        }

        // === 新增: 异步创建取消与展示回收 ===
        if (entry.creating) entry.canceledDuringCreate = true;

        if (entry.pres != null)
        {
            HideAndRecyclePresentation(entry); // === 修改: 使用 entry 版本 ===
        }
        // === 新增结束 ===

        _aoiStates.Remove(entity.Id);
    }

    public void MoveEntity(ILogicEntity entity, Vector2 oldPos, Vector2 newPos)
    {
        var c0 = ToDynamicCell(oldPos);
        var c1 = ToDynamicCell(newPos);
        if (c0 != c1)
        {
            if (_buckets.TryGetValue(c0, out var set0)) set0.Remove(entity);
            RegisterEntity(entity, newPos);
        }
        // 逻辑层可自行触发状态事件；可选：若已在AOI，Presenter位置会通过事件或下一帧刷新
    }

    private void RefreshDynamicAOI(Vector3 playerPos, float dt)
    {
        Vector2 center = playerPos;
        float innerR = aoiRadius;
        float outerR = aoiRadius + Mathf.Max(0f, radiusHysteresis);
        float innerR2 = innerR * innerR;
        float outerR2 = outerR * outerR;

        // 候选集合（按外圈方形包围）
        var min = center - new Vector2(outerR, outerR);
        var max = center + new Vector2(outerR, outerR);
        var cMin = ToDynamicCell(min);
        var cMax = ToDynamicCell(max);

        var candidate = new HashSet<ILogicEntity>();
        for (int cx = cMin.Item1; cx <= cMax.Item1; cx++)
            for (int cy = cMin.Item2; cy <= cMax.Item2; cy++)
            {
                if (_buckets.TryGetValue((cx, cy), out var set))
                    foreach (var e in set) candidate.Add(e);
            }

        var visited = new HashSet<long>();

        foreach (var e in candidate)
        {
            visited.Add(e.Id);
            if (!_aoiStates.TryGetValue(e.Id, out var entry))
            {
                entry = new AOIEntry
                {
                    entity = e,
                    isShown = false,
                    enterTimer = 0f,
                    exitTimer = 0f,
                    lastInsideInner = false,
                    lastInsideOuter = false,
                    creating = false,              // === 新增 ===
                    canceledDuringCreate = false, // === 新增 ===
                    pres = null                   // === 新增 ===
                };
                _aoiStates[e.Id] = entry;
            }

            Vector2 pos = ExtractPosition(e);
            float d2 = (pos - center).sqrMagnitude;

            bool insideInner = d2 <= innerR2; // 进入判定
            bool insideOuter = d2 <= outerR2; // 离开迟滞判定

            if (!entry.isShown)
            {
                // 未显示：连续处于内圈累计 enterTimer
                if (insideInner)
                {
                    entry.enterTimer += dt;
                    if (entry.enterTimer >= enterGraceSeconds)
                    {
                        // === 修改: 进入后如无展示且不在创建中则异步创建 ===
                        entry.isShown = true;
                        entry.exitTimer = 0f;

                        if (entry.pres != null)
                        {
                            ShowPresentation(entry); // === 修改 ===
                        }
                        else if (!entry.creating && !entry.entity.MarkDead)
                        {
                            entry.creating = true;              // === 新增 ===
                            entry.canceledDuringCreate = false; // === 新增 ===
                            _ = SpawnPresentationAsync(entry);  // === 新增: fire-and-forget 异步创建 ===
                        }
                        // === 修改结束 ===
                    }
                }
                else
                {
                    entry.enterTimer = 0f;
                }
            }
            else
            {
                // 已显示：仅在完全超出外圈才累计 exitTimer
                if (!insideOuter)
                {
                    entry.exitTimer += dt;
                    if (entry.exitTimer >= exitGraceSeconds)
                    {
                        // === 修改: 触发离开，若正在创建则标记取消 ===
                        entry.isShown = false;
                        entry.enterTimer = 0f;

                        if (entry.pres != null)
                        {
                            HideAndRecyclePresentation(entry); // === 修改 ===
                        }
                        else if (entry.creating)
                        {
                            entry.canceledDuringCreate = true; // === 新增 ===
                        }
                        // === 修改结束 ===
                    }
                }
                else
                {
                    entry.exitTimer = 0f;
                }
            }

            //if(entry.pres != null)
            //{
            //    // 检查死亡动画完毕
            //    if (entry.pres.CheckValid())
            //    {
            //        HideAndRecyclePresentation(entry); // === 修改 ===
            //    }
            //}

            entry.lastInsideInner = insideInner;
            entry.lastInsideOuter = insideOuter;
        }

        // 未访问到的实体：视为处于外圈之外，若正在显示则推进离开计时
        var keys = new List<long>(_aoiStates.Keys);
        foreach (var id in keys)
        {
            if (visited.Contains(id)) continue;
            var entry = _aoiStates[id];
            if (entry.isShown)
            {
                entry.exitTimer += dt;
                if (entry.exitTimer >= exitGraceSeconds)
                {
                    // === 修改: 离开处理（含创建期取消） ===
                    entry.isShown = false;
                    entry.enterTimer = 0f;
                    if (entry.pres != null)
                        HideAndRecyclePresentation(entry);
                    else if (entry.creating)
                        entry.canceledDuringCreate = true;
                    // === 修改结束 ===
                }
            }
        }
    }

    // === 新增: 异步创建与竞态处理 ===
    private async Task SpawnPresentationAsync(AOIEntry entry) // === 新增 ===
    {
        var logic = entry.entity;
        IScenePresentation pres = null;
        try
        {
            pres = await _presentationFactory.SpawnAsync(logic); // === 新增 ===
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AOIManager SpawnAsync failed for {logic.Id}: {ex.Message}");
            entry.creating = false;
            entry.canceledDuringCreate = false;
            return;
        }

        // 创建完成：更新状态
        entry.pres = pres;
        entry.creating = false;

        if (entry.canceledDuringCreate || !entry.isShown)
        {
            // 已取消或不需显示：直接回收
            entry.canceledDuringCreate = false;
            if (entry.pres != null)
            {
                entry.pres.SetVisible(false);
                entry.pres.Unbind();
                await _presentationFactory.RecycleAsync(entry.pres); // === 新增 ===
                entry.pres = null;
            }
            return;
        }

        // 仍需显示：绑定与显示
        ShowPresentation(entry);
    }
    // === 新增结束 ===

    private void ShowPresentation(AOIEntry entry) // === 修改: 使用 entry 版本 ===
    {
        if (entry.pres == null) return;
        entry.pres.Bind(entry.entity);
        entry.pres.SetVisible(true);
        entry.entity.OnEnterAOI();
    }

    private void HideAndRecyclePresentation(AOIEntry entry) // === 修改: 使用 entry 版本 ===
    {
        if (entry.pres == null) return;
        entry.pres.SetVisible(false);
        entry.pres.Unbind();
        _ = _presentationFactory.RecycleAsync(entry.pres); // === 修改: 异步回收 ===
        entry.pres = null;
        entry.entity.OnExitAOI();
    }

    private (int, int) ToDynamicCell(Vector2 pos)
    {
        int x = Mathf.FloorToInt(pos.x / dynamicCellSize);
        int y = Mathf.FloorToInt(pos.y / dynamicCellSize);
        return (x, y);
    }

    private Vector2 ExtractPosition(ILogicEntity e)
    {
        return e.Pos;
    }

    // ===== 静态 Chunk 管理 =====

    [Header("Debounce / Hysteresis")]
    [SerializeField] private float chunkEnterDelay = 0.05f;  // 进入窗口，秒
    [SerializeField] private float chunkExitDelay = 0.15f;   // 退出窗口，秒
    [SerializeField] private float chunkMinStay = 0.30f;     // 最短驻留，秒（Loaded 后至少保持这么久）

    private readonly Dictionary<ChunkCoord, ChunkRecord> _chunks = new Dictionary<ChunkCoord, ChunkRecord>();
    private int maxConcurrentLoads = 2;     // 同时 Loading 的 chunk 上限
    private int _concurrentLoading = 0;

    private int batchObjectsPerSlice = 8;
    private int yieldEveryNObjects = 4;

    // 每个 chunk 的本地状态
    public sealed class ChunkRecord
    {
        public readonly ChunkCoord coord;

        // 目标意图
        public bool desiredVisible;

        // 进度状态
        public LoadState loadState;

        // 抖动控制的时间戳
        public float lastBecameDesired;    // 最近一次变为 desiredVisible=true 的时间（在外部更新）
        public float lastBecameUndesired;  // 最近一次变为 desiredVisible=false 的时间
        public float lastBecameLoaded;     // 最近一次进入 Loaded 的时间

        // 加载中取消标志
        public bool cancelAfterLoad;

        // 实例
        public List<GameObject> instances;

        public ChunkRecord(ChunkCoord c)
        {
            coord = c;
            desiredVisible = false;
            loadState = LoadState.Unloaded;
            cancelAfterLoad = false;
            instances = null;
            lastBecameDesired = 0f;
            lastBecameUndesired = 0f;
            lastBecameLoaded = 0f;
        }
    }

    public enum LoadState
    {
        Unloaded,
        Loading,
        Loaded,
        Unloading
    }

    private void RefreshStaticChunks(Vector3 playerPos)
    {
        var center = WorldToChunk(playerPos);
        var target = CollectChunkRing(center, chunkRing);

        // 标记 desiredVisible，并维护记录集
        foreach (var c in target)
        {
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
            if (!target.Contains(c))
            {
                var rec = _chunks[c];
                rec.desiredVisible = false;
            }
        }

        // 2) 推进状态机（带限流与抖动窗口）
        int startedLoadsThisFrame = 0;
        int startedUnloadsThisFrame = 0;

        // 优先卸载（控制内存峰值），再加载
        // 2.1 卸载推进
        foreach (var c in keys)
        {
            var rec = _chunks[c];
            TickChunkUnload(rec, Time.time, ref startedUnloadsThisFrame);
        }

        // 2.2 加载推进
        foreach (var c in keys)
        {
            var rec = _chunks[c];
            TickChunkLoad(rec, Time.time, ref startedLoadsThisFrame);
        }
    }

    private void TickChunkLoad(ChunkRecord rec, float now, ref int startedLoadsThisFrame)
    {
        switch (rec.loadState)
        {
            case LoadState.Unloaded:
                if (!rec.desiredVisible) return;

                // 进入窗口判定：需要显示且满足 enterDelay
                if (now - rec.lastBecameDesired < chunkEnterDelay) return;

                // 限流：并发与每帧新开上限
                if (_concurrentLoading >= maxConcurrentLoads) return;

                startedLoadsThisFrame++;
                StartChunkLoad(rec);
                break;

            case LoadState.Loading:
                // 如果中途不再需要显示，标记取消
                if (!rec.desiredVisible)
                {
                    // 但若已加载完成会在回调时处理
                    rec.cancelAfterLoad = true;
                    rec.lastBecameUndesired = (rec.lastBecameUndesired == 0f) ? now : rec.lastBecameUndesired;
                }
                break;

            case LoadState.Loaded:
                // 最短驻留保护：即使不再需要，也要保持 minStay
                if (!rec.desiredVisible)
                {
                    // 退出窗口计时点
                    if (rec.lastBecameUndesired == 0f)
                        rec.lastBecameUndesired = now;

                    // 如果没达到 minStay，不卸
                    if (now - rec.lastBecameLoaded < chunkMinStay) return;

                    // 退出窗口：达到 exitDelay 才卸
                    if (now - rec.lastBecameUndesired < chunkExitDelay) return;

                    // 由卸载推进流程处理
                }
                break;

            case LoadState.Unloading:
                // 若又需要显示，等卸载结束后再加载（避免拉锯）
                break;
        }
    }

    private void TickChunkUnload(ChunkRecord rec, float now, ref int startedUnloadsThisFrame)
    {
        switch (rec.loadState)
        {
            case LoadState.Loaded:
                if (!rec.desiredVisible)
                {
                    // 最短驻留 + 退出窗口判定
                    if (now - rec.lastBecameLoaded < chunkMinStay) return;

                    if (rec.lastBecameUndesired == 0f)
                        rec.lastBecameUndesired = now;

                    if (now - rec.lastBecameUndesired < chunkExitDelay) return;

                    startedUnloadsThisFrame++;
                    StartChunkUnload(rec);
                }
                break;

            case LoadState.Loading:
                // 若不再需要显示，标记取消并准备在 load 完成后直接释放
                if (!rec.desiredVisible)
                {
                    rec.cancelAfterLoad = true;
                    if (rec.lastBecameUndesired == 0f)
                        rec.lastBecameUndesired = now;
                }
                break;

            case LoadState.Unloading:
            case LoadState.Unloaded:
                // 无需处理
                break;
        }
    }

    // 异步加载：分批实例化 + 并发计数
    private async void StartChunkLoad(ChunkRecord rec)
    {
        if (rec.loadState != LoadState.Unloaded) return;

        rec.loadState = LoadState.Loading;
        rec.cancelAfterLoad = false;
        rec.lastBecameUndesired = 0f; // 清理不需要计时
        _concurrentLoading++;

        var instances = new List<GameObject>();
        var batchBuffer = new List<ChunkMapExportDatabase.StaticItem>(batchObjectsPerSlice);
        int objCountSinceYield = 0;

        // 批次枚举（根据你的配置实现 GetPrefabs）
        foreach (var item in GetChunkPrefabs(rec.coord))
        {
            batchBuffer.Add(item);
            if (batchBuffer.Count >= batchObjectsPerSlice)
            {
                objCountSinceYield = await InstantiateBatch(batchBuffer, instances, objCountSinceYield);
                batchBuffer.Clear();

                // 加载过程中若被标记取消，可继续把已加载的回收处理延后到完成阶段
            }
        }
        // 处理剩余的半批
        if (batchBuffer.Count > 0)
        {
            objCountSinceYield = await InstantiateBatch(batchBuffer, instances, objCountSinceYield);
        }

        // 加载完成，回主线程后核验
        if (!_chunks.TryGetValue(rec.coord, out var cur) || cur != rec)
        {
            // 记录已被替换，安全释放
            foreach (var go in instances) _ = _assetAsync.ReleaseAsync(go);
            _concurrentLoading = Mathf.Max(0, _concurrentLoading - 1);
            return;
        }

        // 若不再需要显示或被标记取消，则直接释放
        if (rec.cancelAfterLoad || !rec.desiredVisible)
        {
            foreach (var go in instances)
                _ = _assetAsync.ReleaseAsync(go);

            rec.instances = null;
            rec.loadState = LoadState.Unloaded;
            _concurrentLoading = Mathf.Max(0, _concurrentLoading - 1);
            return;
        }

        rec.instances = instances;
        rec.loadState = LoadState.Loaded;
        rec.lastBecameLoaded = Time.time;
        _concurrentLoading = Mathf.Max(0, _concurrentLoading - 1);
    }

    private async Task<int> InstantiateBatch(List<ChunkMapExportDatabase.StaticItem> items, List<GameObject> instances, int objCountSinceYield)
    {
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            GameObject go = null;
            try
            {
                go = await _assetAsync.InstantiateAsync("Prefab/" + AreaId + "/" +  it.Key);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            if (go != null)
            {
                var root = MainGameManager.Instance.GetWorldStaticPrefabRoot("1");
                if(root != null)
                {
                    go.transform.SetParent(root);
                }
                go.transform.SetPositionAndRotation(it.Position, it.Rotation);
                go.transform.localScale = it.Scale;
                go.SetActive(true);
                instances.Add(go);
            }

            objCountSinceYield++;
            if (objCountSinceYield >= yieldEveryNObjects)
            {
                objCountSinceYield = 0;
                await Task.Yield(); // 切片，避免卡顿
            }
        }

        return objCountSinceYield;
    }

    // 异步卸载：分批释放
    private async void StartChunkUnload(ChunkRecord rec)
    {
        if (rec.loadState == LoadState.Unloaded) return;
        if (rec.loadState == LoadState.Unloading) return;

        rec.loadState = LoadState.Unloading;

        // 取出现有实例并立即清空，防止重复操作
        var list = rec.instances ?? new List<GameObject>();
        rec.instances = null;

        int count = 0;
        List<GameObject> slice = new List<GameObject>(batchObjectsPerSlice);

        // 切片释放
        for (int i = 0; i < list.Count; i++)
        {
            slice.Add(list[i]);
            if (slice.Count >= batchObjectsPerSlice)
            {
                await ReleaseSlice(slice);
                slice.Clear();
            }
            count++;
        }
        if (slice.Count > 0)
        {
            await ReleaseSlice(slice);
            slice.Clear();
        }

        // 卸载完成，若此时又需要显示，交由下一帧的 Tick 决定是否重新加载
        if (_chunks.TryGetValue(rec.coord, out var cur) && cur == rec)
        {
            rec.loadState = LoadState.Unloaded;
            rec.lastBecameUndesired = 0f; // 退出窗口计时完成，重置
        }
    }

    private async Task ReleaseSlice(List<GameObject> slice)
    {
        for (int i = 0; i < slice.Count; i++)
        {
            var go = slice[i];
            try { await _assetAsync.ReleaseAsync(go); }
            catch (System.Exception ex) { Debug.LogException(ex); }
            if ((i + 1) % yieldEveryNObjects == 0)
                await Task.Yield();
        }
        await Task.Yield();
    }

    /// <summary>
    /// 世界坐标转chunk
    /// 检查玩家是否处于sub空间
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private ChunkCoord WorldToChunk(Vector3 pos)
    {
        var logicPos = MainGameManager.Instance.GetLogicPosFromWorldPos(pos);
        int x = Mathf.FloorToInt(logicPos.x / GameConsts.ChunkCellSize);
        int y = Mathf.FloorToInt(logicPos.y / GameConsts.ChunkCellSize);
        return new ChunkCoord(x, y);
    }



    private HashSet<ChunkCoord> CollectChunkRing(ChunkCoord center, int r)
    {
        var set = new HashSet<ChunkCoord>();
        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
                set.Add(new ChunkCoord(center.X + dx, center.Y + dy));
        return set;
    }

    
}