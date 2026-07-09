using cfg.demo;
using My;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using My.Map.Scene;
using My.MapExport;
using Map.Logic.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEditor.Progress;
using static UnityEngine.Rendering.DebugUI.Table;


public class SceneAOIManager : MonoBehaviour
{
    public static SceneAOIManager Instance { get { return MainGameManager.Instance.AOIManager; } }

    // Presenter 已 Bind 且 SetVisible(true) 之后触发；UI 等可订阅，避免在 AOI 内写业务分支
    public event Action<IScenePresentation, ILogicEntity> AfterPresentationShown;
    private MapLogicSubscription _refreshGroupSwapEventSub;
    private MapLogicEventAdapter _refreshGroupSwapEventAdapter;
    private MapLogicEventBus _refreshGroupSwapEventBus;

    public string MapName
    {
        get { return MainGameManager.Instance.gameLogicManager.AreaManager.AreaOverlayId; }
    }

    [Header("Player & AOI")]
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
    [SerializeField] private MapChunkManager mapChunkManager;

    public MapChunkManager MapChunkManager => mapChunkManager;

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

    private sealed class PendingSwapRetire
    {
        public string SwapId;
        public string GroupKey;
        public long OldEntityId;
        public long NewEntityId;
        public float ExpireTime;
        public IScenePresentation RetainedPresentation;
        public readonly List<Collider2D> DisabledColliders = new();
    }

    private readonly Dictionary<long, AOIEntry> _aoiStates = new(); // id -> entry
    private readonly Dictionary<long, PendingSwapRetire> _pendingSwapByOldEntity = new();
    private readonly Dictionary<long, PendingSwapRetire> _pendingSwapByNewEntity = new();

    struct VisualFocusSession
    {
        public Vector2 Pos;
        public float Radius;
        public float UntilTime;
    }

    struct AoiCenter
    {
        public Vector2 Pos;
        public float InnerRadius;
        public float OuterRadius;
    }

    readonly List<VisualFocusSession> _visualFocuses = new();
    readonly HashSet<long> _pinnedPresentationIds = new();
    readonly List<AoiCenter> _aoiCenterScratch = new(4);
    readonly List<Vector3> _chunkCenterScratch = new(4);

    public void InitMapArea(string mapName)
    {
    }

    public async Task CleanupAllAsync()
    {
        // 1) 防止 Update 继续推进状态机（可选：设置标志位或直接禁用组件）
        enabled = false;

        // 2) 动态 AOI：取消创建、回收所有展示
        try
        {
            var entries = new List<AOIEntry>(_aoiStates.Values);

            // 标记所有正在创建的任务取消
            foreach (var entry in entries)
            {
                entry.canceledDuringCreate = true;
            }

            // 回收已存在的展示
            foreach (var entry in entries)
            {
                if (entry.pres != null)
                {
                    // 与 HideAndRecyclePresentation 的语义一致，但这里等待回收完成
                    try
                    {
                        entry.pres.SetVisible(false);
                        entry.pres.Unbind();
                        await _presentationFactory.RecycleAsync(entry.pres);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                    entry.pres = null;
                    // 退出地图的清理不需要触发逻辑层的 OnExitAOI，可按需求决定：
                    // entry.entity.OnExitAOI();
                }
                entry.creating = false;
            }

            _aoiStates.Clear();
            _buckets.Clear();
            foreach (var pending in new List<PendingSwapRetire>(_pendingSwapByOldEntity.Values))
            {
                RetirePendingSwap(pending);
            }
            _pendingSwapByOldEntity.Clear();
            _pendingSwapByNewEntity.Clear();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }

        // 3) 静态 Chunk：释放所有实例并清空记录
        try
        {
            if (mapChunkManager != null)
            {
                await mapChunkManager.CleanupAllAsync();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }

        // 4) 清理地图相关引用
        try
        {


        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }

        // 5) 可选：解绑玩家与其他外部引用（按项目需求）
        // player = null;

        // 6) 恢复组件可用性（如果需要在同场景内重新 InitArea 后复用）
        enabled = true;
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

        if (mapChunkManager == null)
        {
            mapChunkManager = GetComponent<MapChunkManager>();
        }

        if (mapChunkManager == null)
        {
            mapChunkManager = gameObject.AddComponent<MapChunkManager>();
        }

        mapChunkManager.Initialize(_asset, _assetAsync, () => CanRefreshStaticChunksNow());
        EnsureRefreshGroupSwapEventSubscription();
    }

    private void OnDestroy()
    {
        UnsubscribeRefreshGroupSwapEvent();
    }

    private void Update()
    {
        EnsureRefreshGroupSwapEventSubscription();

        if (MainGameManager.Instance == null || !MainGameManager.Instance.Initialized)
        {
            return;
        }

        if(!WorldAreaManager.Instance.IsWorldLoaded)
        {
            return;
        }

        if(MainGameManager.Instance.gameLogicManager.MainStage == GameLogicManager.EMainGameStage.UnInitialized)
        {
            return;
        }

        if (MainGameManager.Instance.gameLogicManager.playerLogicEntity == null) return;
        if (string.IsNullOrEmpty(MapName)) return;

        var playerPos = MainGameManager.Instance.gameLogicManager.playerLogicEntity.Pos;
        BuildAoiCenters(playerPos, _aoiCenterScratch);

        CleanupExpiredPendingSwaps();
        RefreshDynamicAOI(_aoiCenterScratch, LogicTime.deltaTime);

        _chunkCenterScratch.Clear();
        _chunkCenterScratch.Add(playerPos);
        for (int i = 0; i < _visualFocuses.Count; i++)
        {
            _chunkCenterScratch.Add(_visualFocuses[i].Pos);
        }

        mapChunkManager?.RefreshChunksUnion(_chunkCenterScratch, chunkRing);
        mapChunkManager?.ProcessPendingVisibleChunkRefresh();
    }

    private void EnsureRefreshGroupSwapEventSubscription()
    {
        var logicEventBus = MainGameManager.Instance?.gameLogicManager?.LogicEventBus;
        if (logicEventBus == null)
        {
            return;
        }

        if (_refreshGroupSwapEventSub != null)
        {
            if (ReferenceEquals(_refreshGroupSwapEventBus, logicEventBus))
            {
                return;
            }

            UnsubscribeRefreshGroupSwapEvent();
        }

        _refreshGroupSwapEventAdapter = new MapLogicEventAdapter(OnRefreshGroupSwapEvent);
        _refreshGroupSwapEventBus = logicEventBus;
        _refreshGroupSwapEventSub = logicEventBus.Subscribe(
            EMapLogicEventType.RefreshGroupSwap,
            _refreshGroupSwapEventAdapter);
    }

    private void UnsubscribeRefreshGroupSwapEvent()
    {
        if (_refreshGroupSwapEventSub == null)
        {
            return;
        }

        _refreshGroupSwapEventBus?.Unsubscribe(_refreshGroupSwapEventSub);
        _refreshGroupSwapEventSub = null;
        _refreshGroupSwapEventAdapter = null;
        _refreshGroupSwapEventBus = null;
    }

    private void OnRefreshGroupSwapEvent(IMapLogicEvent evt)
    {
        if (!(evt is MLERefreshGroupSwapEvent swapEvent))
        {
            return;
        }

        if (swapEvent.IsBindNewEntity)
        {
            BindRefreshGroupSwapNewEntity(swapEvent.OldEntityId, swapEvent.NewEntityId);
            return;
        }

        if (swapEvent.MaxRetainSeconds > 0f)
        {
            BeginRefreshGroupSwap(swapEvent.OldEntityId, swapEvent.GroupKey, swapEvent.MaxRetainSeconds);
            return;
        }

        BeginRefreshGroupSwap(swapEvent.OldEntityId, swapEvent.GroupKey);
    }

    void BuildAoiCenters(Vector2 playerPos, List<AoiCenter> centers)
    {
        centers.Clear();
        float innerR = aoiRadius;
        float outerR = aoiRadius + Mathf.Max(0f, radiusHysteresis);
        centers.Add(new AoiCenter
        {
            Pos = playerPos,
            InnerRadius = innerR,
            OuterRadius = outerR,
        });

        float now = LogicTime.time;
        for (int i = 0; i < _visualFocuses.Count; i++)
        {
            var focus = _visualFocuses[i];
            if (now > focus.UntilTime)
            {
                continue;
            }

            float focusInner = Mathf.Max(0.1f, focus.Radius);
            float focusOuter = focusInner + Mathf.Max(0f, radiusHysteresis);
            centers.Add(new AoiCenter
            {
                Pos = focus.Pos,
                InnerRadius = focusInner,
                OuterRadius = focusOuter,
            });
        }
    }

    public void AddVisualFocus(Vector2 logicPos, float radius, float untilTime)
    {
        _visualFocuses.Add(new VisualFocusSession
        {
            Pos = logicPos,
            Radius = radius,
            UntilTime = untilTime,
        });
    }

    public void RemoveVisualFocus(Vector2 logicPos)
    {
        for (int i = _visualFocuses.Count - 1; i >= 0; i--)
        {
            if ((_visualFocuses[i].Pos - logicPos).sqrMagnitude < 0.01f)
            {
                _visualFocuses.RemoveAt(i);
            }
        }
    }

    public void RemoveExpiredVisualFocuses(float now)
    {
        for (int i = _visualFocuses.Count - 1; i >= 0; i--)
        {
            if (now > _visualFocuses[i].UntilTime)
            {
                _visualFocuses.RemoveAt(i);
            }
        }
    }

    public void ClearAllVisualFocusAndPins()
    {
        _visualFocuses.Clear();
        _pinnedPresentationIds.Clear();
    }

    public void PinPresentation(long entityId)
    {
        if (entityId != 0)
        {
            _pinnedPresentationIds.Add(entityId);
        }
    }

    public void UnpinPresentation(long entityId)
    {
        _pinnedPresentationIds.Remove(entityId);
    }

    public bool IsFocusAreaReady(Vector2 logicPos, long pinEntityId)
    {
        if (mapChunkManager != null)
        {
            var worldPos = MainGameManager.Instance.GetWorldPosFromLogicPos(logicPos);
            if (!mapChunkManager.IsWorldPosChunkLoaded(worldPos))
            {
                return false;
            }

            mapChunkManager.ProcessPendingVisibleChunkRefresh();
        }

        if (pinEntityId != 0 && !IsPinnedPresentationShown(pinEntityId))
        {
            return false;
        }

        return true;
    }

    public bool IsPinnedPresentationShown(long entityId)
    {
        if (entityId == 0)
        {
            return true;
        }

        if (!_aoiStates.TryGetValue(entityId, out var entry) || entry.entity == null)
        {
            return false;
        }

        if (entry.entity.MarkDestroyed)
        {
            return false;
        }

        return entry.isShown && entry.pres != null;
    }

    public void PrewarmTickAtFocusOnce(Vector2 focusPos, float dt, long pinEntityId = 0)
    {
        if (!CanRefreshStaticChunksNow())
        {
            return;
        }

        BuildAoiCenters(MainGameManager.Instance.gameLogicManager.playerLogicEntity.Pos, _aoiCenterScratch);
        RefreshDynamicAOI(_aoiCenterScratch, dt, noEnterGrace: true);

        _chunkCenterScratch.Clear();
        _chunkCenterScratch.Add(MainGameManager.Instance.gameLogicManager.playerLogicEntity.Pos);
        _chunkCenterScratch.Add(focusPos);
        mapChunkManager?.RefreshChunksUnion(_chunkCenterScratch, chunkRing);
        mapChunkManager?.ProcessPendingVisibleChunkRefresh();

        if (mapChunkManager != null)
        {
            var worldPos = MainGameManager.Instance.GetWorldPosFromLogicPos(focusPos);
            if (mapChunkManager.IsWorldPosChunkLoaded(worldPos))
            {
                mapChunkManager.ForceUpdateOneChunk(mapChunkManager.WorldToChunk(worldPos));
            }
        }

        if (pinEntityId != 0
            && _pinnedPresentationIds.Contains(pinEntityId)
            && _aoiStates.TryGetValue(pinEntityId, out var pinnedEntry))
        {
            EnsurePinnedPresentationVisible(pinnedEntry, dt, noEnterGrace: true);
        }
    }

    public void TickPinnedPresentation(long entityId, float dt)
    {
        if (!_pinnedPresentationIds.Contains(entityId))
        {
            return;
        }

        if (!_aoiStates.TryGetValue(entityId, out var entry) || entry.entity == null)
        {
            return;
        }

        EnsurePinnedPresentationVisible(entry, dt);
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

    public bool CheckNoLoading()
    {
        foreach (var aoiState in _aoiStates.Values)
        {
            if (aoiState.creating)
            {
                return false;
            }
        }

        if (mapChunkManager != null && mapChunkManager.HasPendingVisibleLoads())
        {
            return false;
        }

        return true;
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
            if (TryRetainPresentationForPendingSwap(entity.Id, entry))
            {
                entry.entity.OnExitAOI();
                entry.pres = null;
            }
            else
            {
                HideAndRecyclePresentation(entry); // === 修改: 使用 entry 版本 ===
            }
        }
        // === 新增结束 ===

        _aoiStates.Remove(entity.Id);
    }

    public void BeginRefreshGroupSwap(long oldEntityId, string groupKey, float maxRetainSeconds = 2f)
    {
        if (oldEntityId == 0 || string.IsNullOrEmpty(groupKey))
        {
            return;
        }

        if (_pendingSwapByOldEntity.TryGetValue(oldEntityId, out var existing))
        {
            RetirePendingSwap(existing);
        }

        _pendingSwapByOldEntity[oldEntityId] = new PendingSwapRetire
        {
            SwapId = $"{groupKey}:{oldEntityId}:{Time.frameCount}",
            GroupKey = groupKey,
            OldEntityId = oldEntityId,
            ExpireTime = Time.time + Mathf.Max(0.1f, maxRetainSeconds),
        };
    }

    public void BindRefreshGroupSwapNewEntity(long oldEntityId, long newEntityId)
    {
        if (oldEntityId == 0 || newEntityId == 0)
        {
            return;
        }

        if (!_pendingSwapByOldEntity.TryGetValue(oldEntityId, out var pending))
        {
            return;
        }

        pending.NewEntityId = newEntityId;
        _pendingSwapByNewEntity[newEntityId] = pending;
    }

    private bool TryRetainPresentationForPendingSwap(long oldEntityId, AOIEntry entry)
    {
        if (!_pendingSwapByOldEntity.TryGetValue(oldEntityId, out var pending) || entry?.pres == null)
        {
            return false;
        }

        var pres = entry.pres;
        pres.Unbind();
        pres.SetVisible(true);
        pending.RetainedPresentation = pres;

        if (pres is Component comp)
        {
            var colliders = comp.GetComponentsInChildren<Collider2D>(true);
            foreach (var col in colliders)
            {
                if (col == null || !col.enabled)
                {
                    continue;
                }

                col.enabled = false;
                pending.DisabledColliders.Add(col);
            }
        }

        return true;
    }

    private void CompleteRefreshGroupSwapIfAny(long newEntityId)
    {
        if (_pendingSwapByNewEntity.TryGetValue(newEntityId, out var pending))
        {
            RetirePendingSwap(pending);
        }
    }

    private void CleanupExpiredPendingSwaps()
    {
        if (_pendingSwapByOldEntity.Count == 0)
        {
            return;
        }

        List<PendingSwapRetire> expired = null;
        foreach (var pending in _pendingSwapByOldEntity.Values)
        {
            if (Time.time <= pending.ExpireTime)
            {
                continue;
            }

            expired ??= new List<PendingSwapRetire>();
            expired.Add(pending);
        }

        if (expired == null)
        {
            return;
        }

        foreach (var pending in expired)
        {
            RetirePendingSwap(pending);
        }
    }

    private void RetirePendingSwap(PendingSwapRetire pending)
    {
        if (pending == null)
        {
            return;
        }

        _pendingSwapByOldEntity.Remove(pending.OldEntityId);
        if (pending.NewEntityId != 0)
        {
            _pendingSwapByNewEntity.Remove(pending.NewEntityId);
        }

        for (int i = 0; i < pending.DisabledColliders.Count; i++)
        {
            if (pending.DisabledColliders[i] != null)
            {
                pending.DisabledColliders[i].enabled = true;
            }
        }

        if (pending.RetainedPresentation != null)
        {
            pending.RetainedPresentation.SetVisible(false);
            _ = _presentationFactory.RecycleAsync(pending.RetainedPresentation);
            pending.RetainedPresentation = null;
        }
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

    // AOI 的 enter/exit 计时器只是视觉防抖，不需要跟随真实物理时间。
    // 将 dt 限制在合理上限，防止切后台/长加载时第一帧的巨大 unscaledDeltaTime
    // 在一帧内把所有 exitTimer 打爆，导致正在异步创建中的 Presenter 全部被 cancel。
    private const float _aoiDtCap = 0.1f;

    private void RefreshDynamicAOI(List<AoiCenter> centers, float dt, bool noEnterGrace = false)
    {
        dt = Mathf.Min(dt, _aoiDtCap);

        if (centers == null || centers.Count == 0)
        {
            return;
        }

        var candidate = new HashSet<ILogicEntity>();
        for (int ci = 0; ci < centers.Count; ci++)
        {
            var center = centers[ci];
            var min = center.Pos - new Vector2(center.OuterRadius, center.OuterRadius);
            var max = center.Pos + new Vector2(center.OuterRadius, center.OuterRadius);
            var cMin = ToDynamicCell(min);
            var cMax = ToDynamicCell(max);

            for (int cx = cMin.Item1; cx <= cMax.Item1; cx++)
            {
                for (int cy = cMin.Item2; cy <= cMax.Item2; cy++)
                {
                    if (_buckets.TryGetValue((cx, cy), out var set))
                    {
                        foreach (var e in set)
                        {
                            candidate.Add(e);
                        }
                    }
                }
            }
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
                    creating = false,
                    canceledDuringCreate = false,
                    pres = null
                };
                _aoiStates[e.Id] = entry;
            }

            Vector2 pos = ExtractPosition(e);
            bool insideInner = false;
            bool insideOuter = false;
            bool isPinned = _pinnedPresentationIds.Contains(e.Id);
            for (int ci = 0; ci < centers.Count; ci++)
            {
                var center = centers[ci];
                float d2 = (pos - center.Pos).sqrMagnitude;
                if (d2 <= center.InnerRadius * center.InnerRadius)
                {
                    insideInner = true;
                }

                if (d2 <= center.OuterRadius * center.OuterRadius)
                {
                    insideOuter = true;
                }
            }

            if (isPinned)
            {
                insideInner = true;
                insideOuter = true;
            }

            if (!entry.isShown)
            {
                // 未显示：连续处于内圈累计 enterTimer
                if (insideInner)
                {
                    entry.enterTimer += dt;
                    if (noEnterGrace || entry.enterTimer >= enterGraceSeconds)
                    {
                        // === 修改: 进入后如无展示且不在创建中则异步创建 ===
                        entry.isShown = true;
                        entry.exitTimer = 0f;

                        if (entry.pres != null)
                        {
                            ShowPresentation(entry); // === 修改 ===
                        }
                        else if (!entry.creating && !entry.entity.MarkDestroyed)
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
                        entry.isShown = false;
                        entry.enterTimer = 0f;

                        if (entry.pres != null)
                        {
                            HideAndRecyclePresentation(entry);
                        }
                        else if (entry.creating)
                        {
                            entry.canceledDuringCreate = true;
                        }
                    }
                }
                else
                {
                    entry.exitTimer = 0f;

                    // isShown=true 但 pres 丢失（SpawnAsync 曾抛异常）且未在创建中时重试。
                    // 若不重试，实体会永久卡在 isShown=true/pres=null/creating=false 的死状态。
                    if (entry.pres == null && !entry.creating && !entry.entity.MarkDestroyed)
                    {
                        entry.creating = true;
                        entry.canceledDuringCreate = false;
                        _ = SpawnPresentationAsync(entry);
                    }
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

        foreach (var pinId in _pinnedPresentationIds)
        {
            visited.Add(pinId);
            if (_aoiStates.TryGetValue(pinId, out var pinnedEntry))
            {
                EnsurePinnedPresentationVisible(pinnedEntry, dt, noEnterGrace);
            }
        }

        var keys = new List<long>(_aoiStates.Keys);
        foreach (var id in keys)
        {
            if (visited.Contains(id))
            {
                continue;
            }

            if (_pinnedPresentationIds.Contains(id))
            {
                continue;
            }

            var entry = _aoiStates[id];
            if (entry.isShown)
            {
                entry.exitTimer += dt;
                if (entry.exitTimer >= exitGraceSeconds)
                {
                    entry.isShown = false;
                    entry.enterTimer = 0f;
                    if (entry.pres != null)
                    {
                        HideAndRecyclePresentation(entry);
                    }
                    else if (entry.creating)
                    {
                        entry.canceledDuringCreate = true;
                    }
                }
            }
        }
    }

    void EnsurePinnedPresentationVisible(AOIEntry entry, float dt, bool noEnterGrace = false)
    {
        if (entry == null || entry.entity == null || entry.entity.MarkDestroyed)
        {
            return;
        }

        entry.exitTimer = 0f;
        if (entry.isShown)
        {
            return;
        }

        entry.enterTimer += dt;
        if (!noEnterGrace && entry.enterTimer < enterGraceSeconds)
        {
            return;
        }

        entry.isShown = true;
        entry.enterTimer = 0f;
        if (entry.pres != null)
        {
            ShowPresentation(entry);
        }
        else if (!entry.creating)
        {
            entry.creating = true;
            entry.canceledDuringCreate = false;
            _ = SpawnPresentationAsync(entry);
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
            if(MainGameManager.Instance.gameLogicManager.AreaManager.NewCreateEntityMark.Contains(logic.Id))
            {
                MainGameManager.Instance.gameLogicManager.AreaManager.NewCreateEntityMark.Remove(logic.Id);

                MainGameManager.Instance.ShowFakeFxEffect("创建", logic.Pos);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AOIManager SpawnAsync failed for {logic.Id}: {ex.Message} {ex.StackTrace}");
            entry.creating = false;
            entry.canceledDuringCreate = false;
            return;
        }

        // 创建完成：更新状态
        entry.pres = pres;
        entry.creating = false;
        if(entry.entity.Type == EEntityType.Player)
        {
            MainGameManager.Instance.playerScenePresenter = pres as PlayerScenePresenter;
            MainGameManager.Instance.EnsureOpenWorldVcamFollow();
            MainGameManager.Instance.gameLogicManager?.playerDataManager?.HumanQuickBar?.ApplyWeaponToRuntime();
        }

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
        AfterPresentationShown?.Invoke(entry.pres, entry.entity);
        entry.entity.OnEnterAOI();
        CompleteRefreshGroupSwapIfAny(entry.entity.Id);

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

    public void RequestVisibleChunkRefresh()
    {
        mapChunkManager?.RequestVisibleChunkRefresh();
    }

    static string GetStaticChunkRefreshBlockReason()
    {
        if (MainGameManager.Instance == null)
        {
            return "MainGameManager.Instance is null";
        }

        if (!MainGameManager.Instance.Initialized)
        {
            var stage = MainGameManager.Instance.gameLogicManager?.MainStage;
            if (stage != GameLogicManager.EMainGameStage.SwitchingMap)
            {
                return "MainGameManager not initialized";
            }
        }

        if (WorldAreaManager.Instance == null)
        {
            return "WorldAreaManager.Instance is null";
        }

        if (!WorldAreaManager.Instance.IsWorldLoaded)
        {
            return "World not loaded or WorldAreaRoot missing";
        }

        var logic = MainGameManager.Instance.gameLogicManager;
        if (logic == null)
        {
            return "GameLogicManager is null";
        }

        if (logic.MainStage == GameLogicManager.EMainGameStage.UnInitialized)
        {
            return "GameLogicManager not initialized";
        }

        if (logic.playerLogicEntity == null)
        {
            return "Player entity not ready";
        }

        if (string.IsNullOrEmpty(MainGameManager.Instance.gameLogicManager.AreaManager.AreaOverlayId))
        {
            return "MapName is empty";
        }

        return null;
    }

    bool CanRefreshStaticChunksNow(bool logFailure = false, string context = null)
    {
        var reason = GetStaticChunkRefreshBlockReason();
        if (reason == null)
        {
            return true;
        }

        if (logFailure)
        {
            var prefix = string.IsNullOrEmpty(context) ? "[SceneAOIManager]" : $"[SceneAOIManager] {context}";
            Debug.LogError($"{prefix} static chunk refresh blocked: {reason}");
        }

        return false;
    }

    public void UnloadAllResource()
    {
    }

    public ChunkCoord WorldToChunk(Vector3 pos)
    {
        if (mapChunkManager != null)
        {
            return mapChunkManager.WorldToChunk(pos);
        }

        var logicPos = MainGameManager.Instance.GetLogicPosFromWorldPos(pos);
        return MapChunkUtility.WorldToChunk(logicPos, Vector2.zero, GameConsts.ChunkCellSize);
    }

    // 本地传送黑屏期间：立即按当前玩家位置跑一轮动态/静态 AOI，不等待下一帧 Update。
    public void PrewarmTickAtPlayerOnce(float dt)
    {
        if (!CanRefreshStaticChunksNow())
        {
            return;
        }

        var pos = MainGameManager.Instance.gameLogicManager.playerLogicEntity.Pos;
        BuildAoiCenters(pos, _aoiCenterScratch);
        RefreshDynamicAOI(_aoiCenterScratch, dt, noEnterGrace: true);
        mapChunkManager?.RefreshChunks(pos, chunkRing);
        mapChunkManager?.ProcessPendingVisibleChunkRefresh();
    }

}
