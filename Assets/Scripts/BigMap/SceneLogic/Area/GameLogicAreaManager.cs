using cfg.demo;
using Map.Entity;
using Map.Logic.Events;
using My.Config;
using My.Dungeon;
using My.Map;
using My.Map.Entity;
using My.MapExport;
using My.Quest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static My.MapExport.MapExportDatabase;
using static My.UI.FishingMiniGamePanel;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.Rendering.VolumeComponent;

namespace My.Map.Logic
{

    public struct ChunkCoord
    {
        public int X;
        public int Y;
        public ChunkCoord(int x, int y) { X = x; Y = y; }

        public override string ToString()
        {
            return X.ToString() + "," + Y.ToString();
        }
    }


    /// <summary>
    /// 运行时房间信息（占位，后续可接导出数据）。
    /// </summary>
    public class LogicRoomInfo
    {
        public string RoomId = string.Empty;
        //public RoomExportInfo rawData;

    }

    /// <summary>
    /// 
    /// </summary>
    public partial class GameLogicAreaManager
    {
        public GameLogicManager LogicManager => logicManager;
        public event Action<GameLogicAreaManager, LogicEntityRecord4Npc> EventOnNpcRefreshRecordCreated;
        //public int ChunkCellSize = 32;  // ����������ؿ��ڴ����ø��Ӵ�С
        private readonly Settings settings;



        public float GridCellSize = 16f;
        public UniformGridIndex<long> UnitGridIndex;
        public UniformGridIndex<string> RoomGridIndex;


        public LogicEntityRepository Repo;
        public LongLivedRegistry LongLived { get; } = new();
        public SummonedEntityRegistry Summons { get; }
        public NpcRoutineSystem NpcRoutine { get; }

        public string AreaOverlayId = string.Empty;
        public AreaOverlayStateInfo cacheMapOverlayCfg { get; private set; }

        public MapExportDatabase cacheDatabase;
        public MapChunkDatabase cacheChunkDatabase;

        // 当前 Variant 场景名，与 MapChunk 资源 key 一致（AreaVariantInfo.scene_name）
        public string MapChunkSceneKey { get; private set; } = string.Empty;

        public Dictionary<string, LogicRoomInfo> RuntimeRoomInfos = new();
        

        private GameLogicManager logicManager;

        public InnerListener innerListener;
        public List<DynamicEntityRefreshInfo> EntityRefreshInfo = new List<DynamicEntityRefreshInfo>();

        // StaticId -> 地图导出刷新项；用于 SavePoint 判定与 LinkedRefreshInfo 补链
        private Dictionary<int, DynamicEntityRefreshInfo> _refreshInfoByStaticId;

        private DungeonAreaRuntime _dungeonRuntime;
        public BossEncounterAreaRuntime BossEncounters { get; private set; }

        public List<int> DialogForceStaticIds = new();

        public Dictionary<string, int> StaticName2RefreshIdMap = new();

        public int GetStaticIdByUniqName(string name)
        {
            StaticName2RefreshIdMap.TryGetValue(name, out var id);
            return id;
        }

        public GameLogicAreaManager(GameLogicManager logicManager, Settings settings)
        {
            this.settings = settings;
            this.logicManager = logicManager;

            UnitGridIndex = new UniformGridIndex<long>(GridCellSize);
            RoomGridIndex = new UniformGridIndex<string>(GridCellSize);

            innerListener = new(this);
            Summons = new SummonedEntityRegistry(this);
            NpcRoutine = new NpcRoutineSystem(this);
        }

        public class InnerListener : IMapLogicEventHandler
        {
            private GameLogicAreaManager gameLogicAreaManager;
            public InnerListener(GameLogicAreaManager gameLogicAreaManager)
            {
                this.gameLogicAreaManager = gameLogicAreaManager;
            }

            public void Handle(in IMapLogicEvent evt)
            {
                gameLogicAreaManager.OnMapLogicEvent(evt);
            }
        }


        private List<MapLogicSubscription> subs = new();
        /// <summary>
        /// 初始化地图：加载导出数据、刷新列表、仓库与兴趣点等。
        /// </summary>
        public void InitilizeMap(string mapOVerlayId)
        {
            NpcRoutine.Clear();
            this.AreaOverlayId = mapOVerlayId;
            cacheMapOverlayCfg = CfgMgr.Cfgs.TbAreaOverlayStateInfo.GetOrDefault(mapOVerlayId);

            UnitGridIndex.Clear();
            RoomGridIndex.Clear();

            interestPoints.Clear();
            runtimeStates.Clear();

            spawnEntityQ.Clear();
            despawnEntityQ.Clear();
            wakeEntityQ.Clear();
            sleepEntityQ.Clear();

            foreach(var sub in subs)
            {
                logicManager.LogicEventBus.Unsubscribe(sub);
            }
            subs.Clear();

            {
                var sub = logicManager.LogicEventBus.Subscribe(EMapLogicEventType.Common, innerListener);
                subs.Add(sub);
            }
            {
                var sub = logicManager.LogicEventBus.Subscribe(EMapLogicEventType.Attract, innerListener);
                subs.Add(sub);
            }
            {
                var sub = logicManager.LogicEventBus.Subscribe(EMapLogicEventType.VariableChange, innerListener);
                subs.Add(sub);
            }
            {
                var sub = logicManager.LogicEventBus.Subscribe(EMapLogicEventType.UnitDie, innerListener);
                subs.Add(sub);
            }
            {
                var sub = logicManager.LogicEventBus.Subscribe(EMapLogicEventType.UnitCantAlert, innerListener);
                subs.Add(sub);
            }
            Repo = null;


            if (cacheMapOverlayCfg == null)
            {
                Debug.LogError($"InitilizeMap overlay cfg null: {mapOVerlayId}");
                return;
            }

            cacheDatabase = null;
            cacheChunkDatabase = null;
            MapChunkSceneKey = string.Empty;
            if (!string.IsNullOrEmpty(cacheMapOverlayCfg.ProceduralDefId))
            {
                if (!DungeonMapLoader.TryLoad(cacheMapOverlayCfg, mapOVerlayId, out var procDb, out var genResult))
                {
                    _dungeonRuntime?.Dispose();
                    _dungeonRuntime = null;
                    Debug.LogError(
                        $"InitilizeMap procedural map generate failed: {cacheMapOverlayCfg.ProceduralDefId}");
                    return;
                }

                cacheDatabase = procDb;
                cacheChunkDatabase = null;
                _dungeonRuntime?.Dispose();
                _dungeonRuntime = DungeonAreaRuntime.Create(this, logicManager, genResult);
            }
            else if (!string.IsNullOrEmpty(cacheMapOverlayCfg.MapDataName))
            {
                cacheDatabase = Resources.Load<MapExportDatabase>(
                    $"{MapVariantMapResources.MapExportFolder}/{cacheMapOverlayCfg.MapDataName}");
                MapChunkSceneKey = MapVariantMapResources.ResolveMapChunkKey(cacheMapOverlayCfg) ?? string.Empty;
                cacheChunkDatabase = MapVariantMapResources.LoadMapChunkDatabase(cacheMapOverlayCfg);
                _dungeonRuntime?.Dispose();
                _dungeonRuntime = null;
            }

            if (cacheDatabase == null)
            {
                Debug.LogError($"InitilizeMap cacheDatabase null for overlay {mapOVerlayId}");
                return;
            }

            DialogForceStaticIds.Clear();
            EntityRefreshInfo.Clear();
            EntityRefreshInfo.AddRange(cacheDatabase.EntityRefreshInfo);

            StaticName2RefreshIdMap.Clear();
            foreach (var oneStaticInfo in EntityRefreshInfo)
            {
                if(string.IsNullOrEmpty(oneStaticInfo.UniqName))
                {
                    continue;
                }
                StaticName2RefreshIdMap[oneStaticInfo.UniqName] = oneStaticInfo.StaticId;
            }

            // 初始化实体仓库
            if (Repo == null)
            {
                Repo = new();
            }

            RebuildRefreshInfoByStaticId();

            BuildIndexFromRecords();

            InitDigPoints();
            InitGuardSpawnPoints();
            InitWalkerPath();

            checkRefreshTimer = LogicTime.time;

            logicManager.ApplyPendingMapRuntimeAfterMapInit(mapOVerlayId);

            NpcRoutine.EnsureConfiguredNpcsCreated();

            SetupDesireCrystalSession(mapOVerlayId);

            BossEncounters?.Dispose();
            BossEncounters = new BossEncounterAreaRuntime(this, cacheDatabase);

        }

        public void CleanArea()
        {
            NpcRoutine.Clear();
            Summons.ClearAll();
            BossEncounters?.Dispose();
            BossEncounters = null;
            logicManager?.MapMicroPlot?.AbortForMapChange();

            _dungeonRuntime?.Dispose();
            _dungeonRuntime = null;

            UnitGridIndex.Clear();
            RoomGridIndex.Clear();

            interestPoints.Clear();
            runtimeStates.Clear();

            spawnEntityQ.Clear();
            despawnEntityQ.Clear();
            wakeEntityQ.Clear();
            sleepEntityQ.Clear();

            foreach (var sub in subs)
            {
                logicManager.LogicEventBus.Unsubscribe(sub);
            }
            subs.Clear();
            DialogForceStaticIds.Clear();

            ClearDesireCrystalSession();

            EntityRefreshInfo.Clear();

            SpiritMonster_OnAreaCleanBeforeRepoClear();

            if(Repo != null)
            {
                Repo.Clear();
            }
            LongLived.Clear();

            RefreshInfoRuntimes.Clear();
            Type2EntityList.Clear();

            NewCreateEntityMark.Clear();
            Record2RefreshInfo.Clear();
            _refreshInfoByStaticId?.Clear();

            DungeonSession.ClearLastResult();
        }


        public void SaveRecords()
        {
            if(Repo == null)
            {
                return;
            }

            foreach(var rec in Repo.Records.Values)
            {
                runtimeStates.TryGetValue(rec.Id, out var st);
                if(st != null && st.IsMarkDestroy)
                {
                    continue;
                }

                // keyi cun
            }
        }


        private float checkRefreshTimer;
        private int tickDynamicObjIdx = 0; 

        public void OnMapLogicEvent(IMapLogicEvent ev)
        {
            if(ev.Ctx.IsMapLocal)
            {
                var pos = ev.Ctx.HappenPos;
                float range = GridCellSize;
                if(ev.Ctx.InterestRange > 0)
                {
                    range = ev.Ctx.InterestRange; 
                }
                UnitGridIndex.Query(pos, range, queryBufInt);

                if (ev.Ctx.TargetId != 0)
                {
                    queryBufInt.Add(ev.Ctx.TargetId);
                }

                foreach (var id in queryBufInt)
                {
                    var entity = GetLogicEntiy(id, false);
                    if (entity != null)
                    {
                        entity.OnMapLogicEvent(ev);
                    }
                }
            }
            else
            {
                foreach(var e in Repo.Loaded.Values)
                {
                    e.OnMapLogicEvent(ev);
                }
            }

            switch(ev.Type)
            {
                case EMapLogicEventType.UnitCantAlert:
                    {
                        var realEv = (MLEUnitCantAlert)ev;
                        ClearUnitRelateAlert(realEv.EntityId);
                    }
                    break;

                case EMapLogicEventType.UnitDie:
                    {
                        var realEv = (MLEUnitDie)ev;

                        if (_runningSpriteEntites.Contains(realEv.EntityId))
                        {
                            OnHSpiritClear(realEv.EntityId);
                        }
                    }
                    break;
            }
        }

        
        // 根据 Record 重建 AOI 网格索引
        public void BuildIndexFromRecords()
        {
            foreach (var kv in Repo.Records)
                UnitGridIndex.AddOrMove(kv.Key, kv.Value.Position);

            Summons.RebuildFromRecords();
        }

        // 兴趣点：注册 / 移除
        public void AddInterestPoint(InterestPoint ip) => interestPoints[ip.Id] = ip;
        public void RemoveInterestPoint(int id) => interestPoints.Remove(id);


        public LogicRoomInfo GetRoomByPos(Vector2 logicPos)
        {
            return _dungeonRuntime?.TryGetRoomByPos(logicPos);
        }

        #region AOI 与兴趣相关

        #endregion


        

        public ILogicEntity GetLogicEntiy(long instId, bool ensureExist = true)
        {
            // 1) 无 Record 则不存在
            if (!Repo.Records.TryGetValue(instId, out var rec)) return null;

            // 2) 已加载则直接返回，并处理从 Cooldown 回到 Active
            if (Repo.IsLoaded(instId))
            {
                var ent = Repo.GetLoaded(instId);

                // 有运行时状态时，从 Cooldown 拉回 Active
                if (runtimeStates.TryGetValue(instId, out var st))
                {
                    if(st.State == LogicLifeState.Cooldown)
                    {
                        st.State = LogicLifeState.Active;
                        st.Timer = 0;
                    }
                }

                return ent;
            }

            if(ensureExist)
            {
                Debug.Log("GetLogicEntiy ensure exist " + instId);
                return ImmediateSpawnAndWake(instId);
            }

            return null;
        }

        // 区域每帧驱动：刷新、实体生命周期、邪恶警戒等
        // AI Idle 进入时的通用钩子；Area 决定转发给哪些子系统，Brain 不点名具体 feature
        public void NotifyNpcEnteredIdle(NpcUnitLogicEntity npc)
        {
            NpcRoutine?.SyncOnEnteredIdle(npc);
        }

        public void Tick(float dt)
        {
            NpcRoutine.Tick(dt);
            // 动态刷新出现/消失
            CheckRefreshAppearAndDisappear(dt);

            _dungeonRuntime?.Tick(dt);

            TickEntityLifeCycle(dt);

            Summons.Tick();

            BossEncounters?.Tick(dt);

            TickRefreshWalker();

            TickLowFreqTickRecord();

            // 区域邪恶警戒等
            TickEvilAlerts();

            TickRefreshSpiritMonster(dt);
        }

        private float _lowFreqTickTimer = 0;
        private void TickLowFreqTickRecord()
        {
            float interval = 5.0f;
            if(_lowFreqTickTimer + interval > LogicTime.time)
            {
                return;
            }

            _lowFreqTickTimer = LogicTime.time;

            foreach(var rec in Repo.Records.Values)
            {
                if(Repo.IsLoaded(rec.Id))
                {
                    continue;
                }

                if(rec is LogicEntityRecord4Npc npcRecord)
                {
                    if(npcRecord.MoveBehaveType != UnitMoveBehaveInfo.EMoveBehaveType.MovePath)
                    {
                        continue;
                    }

                    var path = GetRuntimePath(npcRecord.MovePath);
                    if (path == null || path.PointList.Count < 2)
                    {
                        continue;
                    }

                    if(npcRecord.CurrPathIdx >= path.GetEndIndex())
                    {
                        continue;
                    }

                    var p0 = path.PointList[npcRecord.CurrPathIdx];
                    var p1 = path.PointList[npcRecord.CurrPathIdx + 1];

                    var dist = (p1 - p0).magnitude;
                    var spd = 2.0f;

                    float addProgress = (spd * interval) / dist;
                    npcRecord.CurrPathProgress += addProgress;

                    if(npcRecord.CurrPathProgress >= 1)
                    {
                        npcRecord.CurrPathProgress = 0;
                        npcRecord.CurrPathIdx += 1;
                    }
                }
            }
        }

        private void StepStateMachine(OneEntityRuntimeState st, float dt)
        {
            if (st.IsMarkDestroy)
            {
                // 已标记销毁的实体不再参与状态机推进（由销毁队列处理）
                return;
            }

            if(st.ForceActiveUntil != 0 && LogicTime.time < st.ForceActiveUntil)
            {
                return;
            }

            bool hasRec = Repo.Records.TryGetValue(st.Id, out var rec);
            bool activeFlag = IsRecordAlwaysActive(rec);

            if (activeFlag)
            {
                // AlwaysActive：保证已 Spawn 并 Wake，不依赖兴趣圈
                if (!Repo.IsLoaded(st.Id))
                {
                    // 尚未加载则先创建逻辑实体
                    var ent = SpawnEntity(rec.Id);
                }
                // 保持唤醒
                Repo.GetLoaded(st.Id)?.OnWake();

                st.State = LogicLifeState.Active;
                st.Timer = 0f;
                return;
            }

            switch (st.State)
            {
                case LogicLifeState.NotLoaded:
                    if (hasRec)
                    {
                        if (activeFlag)
                        {
                            // AlwaysActive 在 NotLoaded 时也应进入 Warmup 并最终 Active
                            EnqueueSpawn(st.Id);
                            st.State = LogicLifeState.Warmup;
                            break;
                        }
                        if (st.NearAnyWarmup)
                        {
                            EnqueueSpawn(st.Id);
                            st.State = LogicLifeState.Warmup;
                        }
                    }
                    break;

                case LogicLifeState.Warmup:
                    if (Repo.IsLoaded(st.Id) && st.InterestRefCount > 0)
                    {
                        EnqueueWake(st.Id);
                        st.State = LogicLifeState.Active;
                        st.Timer = 0f;
                    }
                    else if (!st.NearAnyWarmup && !activeFlag)
                    {
                        // 无兴趣且非 AlwaysActive：取消加载
                        EnqueueDespawn(st.Id);
                        st.State = LogicLifeState.NotLoaded;
                    }
                    // AlwaysActive：Warmup 后若已 Loaded 可先 Sleep，避免长期 Despawn
                    else if (!st.NearAnyWarmup && activeFlag)
                    {
                        // 已加载则先入 Sleep，再由 Sleep 分支决定是否 Despawn
                        if (Repo.IsLoaded(st.Id))
                        {
                            EnqueueSleep(st.Id);
                            st.State = LogicLifeState.Sleep;
                            st.Timer = settings.SleepToDespawnDelay;
                        }
                    }
                    break;

                case LogicLifeState.Active:
                    if (st.InterestRefCount > 0)
                    {
                        st.Timer = 0f;
                        break;
                    }

                    Repo.Loaded.TryGetValue(st.Id, out var loadedEntity);
                    if (loadedEntity.LifeBindEntityId != 0)
                    {
                        runtimeStates.TryGetValue(loadedEntity.LifeBindEntityId, out var lifeBindSt);
                        if(lifeBindSt != null && lifeBindSt.State == LogicLifeState.Active)
                        {
                            st.Timer = 0f;
                            break;
                        }
                    }

                    bool busy = CheckLogicBusyAlive(loadedEntity);
                    if(busy)
                    {
                        break;
                    }


                    st.State = LogicLifeState.Cooldown;
                    st.Timer = settings.ExitCooldown;
                    break;

                case LogicLifeState.Cooldown:
                    if (st.InterestRefCount > 0)
                    {
                        st.State = LogicLifeState.Active;
                        st.Timer = 0f;
                    }
                    else
                    {
                        st.Timer -= dt;
                        if (st.Timer <= 0f)
                        {
                            if (st.NearAnyWarmup)
                            {
                                EnqueueSleep(st.Id);
                                st.State = LogicLifeState.Sleep;
                                st.Timer = settings.SleepToDespawnDelay;
                            }
                            else
                            {
                                if (!activeFlag)
                                {
                                    EnqueueDespawn(st.Id);
                                    st.State = LogicLifeState.NotLoaded;
                                }
                                else
                                {
                                    // AlwaysActive：不 Despawn，改为 Sleep，保留 Loaded 以便再次 Wake
                                    EnqueueSleep(st.Id);
                                    st.State = LogicLifeState.Sleep;
                                    st.Timer = settings.SleepToDespawnDelay;
                                }
                            }
                        }
                    }
                    break;

                case LogicLifeState.Sleep:
                    if (st.InterestRefCount > 0)
                    {
                        EnqueueWake(st.Id);
                        st.State = LogicLifeState.Active;
                        st.Timer = 0f;
                    }
                    else if (!st.NearAnyWarmup)
                    {
                        st.Timer -= dt;
                        if (st.Timer <= 0f)
                        {
                            if (!activeFlag)
                            {
                                EnqueueDespawn(st.Id);
                                st.State = LogicLifeState.NotLoaded;
                            }
                            else
                            {
                                // AlwaysActive：延长 Sleep 计时，避免立刻 Despawn
                                st.Timer = settings.SleepToDespawnDelay; // 再次进入睡眠前的间隔
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 交互点等是否在忙碌中（例如正在对话），用于延迟 Sleep/Despawn。
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private bool CheckLogicBusyAlive(ILogicEntity entity)
        {
            if(entity is LogicEntityInteractPoint intPoint)
            {
                if(intPoint.IsInteracting)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnqueueSpawn(long id)
        {
            if (!spawnEntityQ.Contains(id)) spawnEntityQ.Enqueue(id);
        }
        private void EnqueueDespawn(long id)
        {
            if (!despawnEntityQ.Contains(id)) despawnEntityQ.Enqueue(id);
        }
        private void EnqueueWake(long id)
        {
            if (!wakeEntityQ.Contains(id)) wakeEntityQ.Enqueue(id);
        }
        private void EnqueueSleep(long id)
        {
            if (!sleepEntityQ.Contains(id)) sleepEntityQ.Enqueue(id);
        }

        private void ProcessQueues( float dt)
        {
            int n;

            // Despawn 每帧预算
            n = settings.MaxDespawnPerFrame;
            while (despawnEntityQ.Count > 0 && n-- > 0)
            {
                var id = despawnEntityQ.Dequeue();
                DespawnEntity(id);
            }

            // Spawn 每帧预算（与 StepStateMachine、AlwaysActive 策略配合）
            n = settings.MaxSpawnPerFrame;
            while (spawnEntityQ.Count > 0 && n-- > 0)
            {
                var id = spawnEntityQ.Dequeue();

                var ent = SpawnEntity(id);
            }

            // Sleep / Wake 每帧预算
            n = settings.MaxSleepPerFrame;
            while (sleepEntityQ.Count > 0 && n-- > 0)
            {
                var id = sleepEntityQ.Dequeue();
                var ent = Repo.GetLoaded(id);
                ent?.OnSleep();
            }

            n = settings.MaxWakePerFrame;
            while (wakeEntityQ.Count > 0 && n-- > 0)
            {
                var id = wakeEntityQ.Dequeue();
                var ent = Repo.GetLoaded(id);
                ent?.OnWake();
            }
        }

        //private void ProcessDieQueue()
        //{
        //    int budget = 64; // 每帧销毁预算（已注释）
        //    while (destroyEntityQ.Count > 0 && budget-- > 0)
        //    {
        //        var pair = destroyEntityQ.Dequeue();
        //        DestroyEntity(pair.Item1, pair.Item2);
        //    }
        //}

        private float _corpseTickTimer;

        /// <summary>
        /// 处理死亡残留计时，到期后真正销毁实体。
        /// </summary>
        /// <param name="dt"></param>
        private void ProcessCorpse(float dt)
        {

            float interval = 1.0f;
            if (_corpseTickTimer + interval > LogicTime.time)
            {
                return;
            }

            _corpseTickTimer = LogicTime.time;

            int n = 32; // 每帧处理的尸体清理数量上限
            int count = Math.Min(n, corpseCleanupQ.Count);
            for (int i = 0; i < count; i++)
            {
                (var id, string reason) = corpseCleanupQ.Dequeue();
                // 无 Record 则跳过
                if (!Repo.Records.TryGetValue(id, out var rec)) continue;

                if (!runtimeStates.TryGetValue(id, out var st)) continue;

                // 与本函数触发间隔一致；原先误用帧 dt，导致 DeathRemainTimer 几乎不降、实体长期不真正 Despawn
                st.DeathRemainTimer -= interval;
                if (st.DeathRemainTimer > 0f)
                {
                    // 死亡计时未到，重新入队
                    corpseCleanupQ.Enqueue((id, reason));
                    continue;
                }

                // 计时结束，销毁实体
                DestroyEntity(id, reason);
            }
        }

        private ILogicEntity SpawnEntity(long entityId)
        {
            // AlwaysActive 等路径外，Spawn 前保证未重复加载
            if (Repo.IsLoaded(entityId))
            {
                return null;   
            }

            if (!Repo.Records.TryGetValue(entityId, out var rec)) return null;

            if (rec is LogicEntityRecord4Npc npcRec)
            {
                logicManager.worldPersistState?.NpcCharacters.TryApplyToRecordBeforeSpawn(npcRec);
                DesireDensitySpawnLogic.ApplyOnNpcRecord(npcRec);
                DesireCrystalSpawnLogic.ApplyOnNpcBeforeSpawn(logicManager, this, npcRec);
                JingYuanTypeSpawnLogic.ApplyOnNpcBeforeSpawn(logicManager, this, npcRec);
            }

            logicManager.worldPersistState?.MapInteractPoints.TryApplyToRecordBeforeSpawn(rec);

            var ent = logicManager.CreateEntityByRecord(rec);
            Repo.Loaded[entityId] = ent;
            ent.Initialize();
            ent.OnSpawn(rec);

            if (rec.EntityType == EEntityType.Player)
            {
                if (logicManager.playerDataManager?.EquipmentManager == null)
                {
                    Debug.LogError("[GameLogicAreaManager] SpawnEntity player failed: EquipmentManager is null");
                }
                else
                {
                    logicManager.playerDataManager.EquipmentManager.NotifyPlayerReady(logicManager);
                }

                if (ent is IEntityBuffOwner buffOwner)
                {
                    logicManager.RestorePendingPlayerBuffsIfAny(buffOwner);
                }

                PlayerEventBus.Publish(new PlayerEntityReadyEvent());
            }

            return ent;
        }

        private void DespawnEntity(long id)
        {
            if (!Repo.IsLoaded(id)) return;

            var record = Repo.Records[id];
            var ent = Repo.GetLoaded(id);

            if (record.EntityType == EEntityType.Player)
            {
                PlayerEventBus.Publish(new PlayerEntityDespawnEvent());
            }

            ent.OnDespawn(ref record);
            Repo.Loaded.Remove(id);

            //ent.EventOnDestroyed -= OnEventEntityDestroyed;
            logicManager.RecycleEntity(ent);
        }


        private void OnEventEntityDestroyed(long entityId)
        {

        }

        /// <summary>
        /// 请求销毁实体（进入死亡延迟队列，非立即移除 Record）。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        public void RequestEntityDestroy(long id, string reason)
        {
            // 无 Record 则忽略
            if (!Repo.HasRecord(id))
            {
                Debug.Log($"RequestEntityDestroy id:{id} not found {reason}");
                return;
            }

            // 确保存在运行时状态条目
            if (!runtimeStates.TryGetValue(id, out var st))
            {
                st = new OneEntityRuntimeState { Id = id, State = LogicLifeState.NotLoaded };
                runtimeStates[id] = st;
            }

            st.IsMarkDestroy = true;
            st.DeathRemainTimer = 5.0f; // 死亡后延迟一段时间再真正销毁（秒）

            corpseCleanupQ.Enqueue((id, reason));
        }

        /// <summary>
        /// 立即移除逻辑实体与 Record（不走死亡延迟队列）；用于刷新条件显隐等。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool ForceDestroyEntityNow(long id, string reason)
        {
            return DestroyEntity(id, reason);
        }

        private bool DestroyEntity(long id, string reason)
        {
            // 无 Record 则失败
            if(!Repo.HasRecord(id))
            {
                Debug.Log($"DestroyEntity id:{id} reason:{reason}");
                return false;
            }

            // 若已加载则先 Despawn 逻辑层
            if (Repo.IsLoaded(id))
            {
                var ent = Repo.GetLoaded(id);
                DespawnEntity(id); // 回收逻辑实体
                //if (!runtimeStates.TryGetValue(id, out var st))
                //{
                //    st = new OneEntityRuntimeState { Id = id, State = LogicLifeState.NotLoaded };
                //    runtimeStates[id] = st;
                //}
                //st.IsDeadRuntime = true;
                //if(reason == 0)
                //{
                //    st.DeathRemainTimer = 0f;
                //}
                //else
                //{
                //    st.DeathRemainTimer = 0.5f;
                //}

                //// 4) 旧版：按死亡计时决定 Sleep 或直接 Despawn（已废弃）
                //if (st.DeathRemainTimer > 0f)
                //{
                //    // 旧版：先入 Sleep（已废弃）
                //    EnqueueSleep(id);
                //    // 旧版：尸体队列（已废弃）
                //    if (!corpseCleanupQ.Contains(id)) corpseCleanupQ.Enqueue(id);
                //}
                //else
                //{
                //    // 旧版：直接 Despawn（已废弃）
                //    DespawnEntity(id);

                //    RemoveLogicRecord(id);
                //}
            }


            UnregisterEntityRecord(id);

            return true;
        }


        /// <summary>
        /// 立即 Spawn 并 Wake（用于 GetLogicEntiy ensureExist 等路径）。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="rec"></param>
        /// <returns></returns>
        private ILogicEntity? ImmediateSpawnAndWake(long id)
        {
            // 已加载则直接返回
            if (Repo.IsLoaded(id)) return Repo.GetLoaded(id);

            var newEnt = SpawnEntity(id);
            // 创建后立刻唤醒
            newEnt.OnWake();

            // 同步 AOI 网格位置
            UnitGridIndex.AddOrMove(id, newEnt.Pos);

            // 标记为 Active 且视为在兴趣范围内（短时保活）
            var st = runtimeStates.ContainsKey(id) ? runtimeStates[id] : (runtimeStates[id] = new OneEntityRuntimeState { Id = id });
            st.State = LogicLifeState.Active;
            st.Timer = 0f;
            st.NearAnyWarmup = true;

            //IssueKeepAliveToken(id, ttlSec: 0.2f);

            return newEnt;
        }

        public bool HasPendingAreaLifecycleQueues()
        {
            return spawnEntityQ.Count > 0 || despawnEntityQ.Count > 0 || wakeEntityQ.Count > 0 || sleepEntityQ.Count > 0;
        }

        public void AdvanceLifecycleForTeleportPrewarm(float dt, int iterations)
        {
            for (int i = 0; i < iterations; i++)
                TickEntityLifeCycle(dt);
        }


        public void OnWantedBehaviourHappend(EWantedBehaveType behaveType, Vector2? center, bool onlyPeace = false)
        {
            logicManager.WantedManager.AddWantedForBehavior(behaveType);

            long playerId = logicManager.playerLogicEntity != null ? logicManager.playerLogicEntity.Id : 0L;

            if(center != null)
            {
                foreach (var ent in logicManager.AreaManager.FindEntityInRange(center.Value, 4.0f))
                {
                    if (ent == null || ent.MarkDestroyed)
                    {
                        continue;
                    }

                    if (ent.Id == playerId)
                    {
                        continue;
                    }

                    if (ent is not NpcUnitLogicEntity npc)
                    {
                        continue;
                    }

                    if (onlyPeace && !npc.NpcConfig.IsPeace)
                    {
                        continue;
                    }

                    if (playerId != 0)
                    {
                        npc.EnmitySystem.AddTempEnmity(100);
                    }
                }
            }
            
        }

        public void ApplyMapVariantPresentation(WorldAreaRoot root)
        {
            if (root == null)
            {
                return;
            }

            root.Initialize(cacheChunkDatabase);
        }

    }
}

