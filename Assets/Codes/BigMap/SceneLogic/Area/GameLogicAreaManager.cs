using cfg.demo;
using Map.Entity;
using Map.Logic.Events;
using My.Config;
using My.Map.Entity;
using My.MapExport;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static My.MapExport.MapExportDatabase;
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
    /// 房间信息 临时数据 考虑放在哪边
    /// </summary>
    public class LogicRoomInfo
    {
        public string RoomId = string.Empty;
        //public RoomExportInfo rawData;

    }

    /// <summary>
    /// 管理区域
    /// </summary>
    public partial class GameLogicAreaManager
    {
        //public int ChunkCellSize = 32;  // 静态分块大小
        private readonly Settings settings;



        public float GridCellSize = 16f;
        public UniformGridIndex<long> UnitGridIndex;
        public UniformGridIndex<string> RoomGridIndex;


        public LogicEntityRepository Repo;
        public LongLivedRegistry LongLived { get; } = new();

        public int AreaId = 0;
        protected MapAreaInfo cacheMapAreaCfg;
        public MapExportDatabase cacheDatabase;

        public Dictionary<string, LogicRoomInfo> RuntimeRoomInfos = new();
        

        private GameLogicManager logicManager;

        public InnerListener innerListener;
        public List<DynamicEntityRefreshInfo> EntityRefreshInfo = new List<DynamicEntityRefreshInfo>();

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
        /// 初始化地区
        /// </summary>
        public void InitilizeArea(int areaId)
        {
            this.AreaId = areaId;
            cacheMapAreaCfg = CfgMgr.Cfgs.TbMapAreaInfo.GetOrDefault(areaId);

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
                var sub = logicManager.LogicEventBus.Subscribe(EMapLogicEventType.OnDie, innerListener);
                subs.Add(sub);
            }
            Repo = null;

            var mapCfg = CfgMgr.Cfgs.TbMapAreaInfo.GetOrDefault(areaId);

            // 加载 cacheDatabase
            cacheDatabase = Resources.Load<MapExportDatabase>($"MapExport/{areaId}");

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

            // 加载repo
            if (Repo == null)
            {
                Repo = new();
                //// fake repo
                ////  goujian 房间列表
                //int cnt = 0;
                //foreach (var refreshInfo in cacheDatabase.EntityRefreshInfo)
                //{
                //    cnt++;
                //    HandleOneRefreshInfo(refreshInfo);

                //    if(cnt > 100)
                //    {
                //        cnt = 0;
                //        await Task.Yield();
                //    }
                //}
            }

            if(mapCfg != null && mapCfg.IsHome)
            {
                var homeRefreshs = logicManager.homeDataManager.GetAllValidLogicEntites();
                EntityRefreshInfo.AddRange(homeRefreshs);
            }


            BuildIndexFromRecords();

            InitDigPoints();
            InitGuardSpawnPoints();
            InitWalkerPath();

            checkRefreshTimer = LogicTime.time;
        }

        public void CleanArea()
        {
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

            EntityRefreshInfo.Clear();

            Repo.Clear();
            LongLived.Clear();

            RefreshInfoRuntimes.Clear();
            Type2EntityList.Clear();

            NewCreateEntityMark.Clear();
            Record2RefreshInfo.Clear();
        }


        public void SaveRecords()
        {
            if(Repo != null)
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
                case EMapLogicEventType.OnDie:
                    {
                        var realEv = (MLEUnitDeadEvent)ev;
                        AlertOnEntityDie(realEv.EntityId);
                    }
                    break;
            }
        }

        
        // 初始化：注册所有记录进索引
        public void BuildIndexFromRecords()
        {
            foreach (var kv in Repo.Records)
                UnitGridIndex.AddOrMove(kv.Key, kv.Value.Position);
        }

        // 注册/移除兴趣点
        public void AddInterestPoint(InterestPoint ip) => interestPoints[ip.Id] = ip;
        public void RemoveInterestPoint(int id) => interestPoints.Remove(id);


        public LogicRoomInfo GetRoomByPos(Vector2 logicPos)
        {
            return null;
        }

        #region aoi 生命周期

        #endregion


        

        public ILogicEntity GetLogicEntiy(long instId, bool ensureExist = true)
        {
            // 记录不存在 一定不存在
            if (!Repo.Records.TryGetValue(instId, out var rec)) return null;

            // 2) 已加载则直接执行
            if (Repo.IsLoaded(instId))
            {
                var ent = Repo.GetLoaded(instId);

                // 续命
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

        // 外部驱动：每帧调用
        public void Tick(float dt)
        {
            // 检查刷新
            CheckRefreshAppearAndDisappear(dt);

            TickEntityLifeCycle(dt);

            TickRefreshWalker();

            TickLowFreqTickRecord();

            // 检查邪恶告警
            TickEvilAlerts();
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
                    if(npcRecord.CurrPathIdx == path.PointList.Count - 1)
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
                // 死亡后交给尸体处理管线清理状态
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
                // 保持Active：若未加载则立刻Spawn+Wake（不走限流）或走优先级更高的队列
                if (!Repo.IsLoaded(st.Id))
                {
                    // 直接创建避免队列延迟，或使用优先队列
                    var ent = SpawnEntity(rec.Id);
                }
                // 确保已Wake
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
                            // 长生命周期但不always active：至少保证 Warmup -> Active 路径，不允许NotLoaded
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
                        // 非长生命周期才允许真正卸载
                        EnqueueDespawn(st.Id);
                        st.State = LogicLifeState.NotLoaded;
                    }
                    // isLong 且离开 Warmup：保留在 Warmup（Loaded但不Wake）或转 Sleep，避免 Despawn
                    else if (!st.NearAnyWarmup && activeFlag)
                    {
                        // 可选：如果已 Loaded 则 Sleep；未 Loaded 则保持 Warmup 等待机会
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
                                    // 长生命周期：不可 Despawn，降级为 Sleep（Loaded 但不Wake）
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
                                // 长生命周期：保持 Sleep，不做 Despawn
                                st.Timer = settings.SleepToDespawnDelay; // 复位或钳制
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 检查物体是否处于逻辑忙状态
        /// 逻辑忙的物体不会被卸载
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

            // Despawn：跳过长生命周期
            n = settings.MaxDespawnPerFrame;
            while (despawnEntityQ.Count > 0 && n-- > 0)
            {
                var id = despawnEntityQ.Dequeue();
                DespawnEntity(id);
            }

            // Spawn：对长生命周期可给“优先队列”或即时创建（你已在 StepStateMachine 中处理 AlwaysActive）
            n = settings.MaxSpawnPerFrame;
            while (spawnEntityQ.Count > 0 && n-- > 0)
            {
                var id = spawnEntityQ.Dequeue();

                var ent = SpawnEntity(id);
            }

            // Sleep/Wake 不变
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
        //    int budget = 64; // 可配置
        //    while (destroyEntityQ.Count > 0 && budget-- > 0)
        //    {
        //        var pair = destroyEntityQ.Dequeue();
        //        DestroyEntity(pair.Item1, pair.Item2);
        //    }
        //}

        private float _corpseTickTimer;

        /// <summary>
        /// 处理尸体销毁
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

            int n = 32; // 每帧检查上限
            int count = Math.Min(n, corpseCleanupQ.Count);
            for (int i = 0; i < count; i++)
            {
                (var id, string reason) = corpseCleanupQ.Dequeue();
                // 未注册或已清理的实例
                if (!Repo.Records.TryGetValue(id, out var rec)) continue;

                if (!runtimeStates.TryGetValue(id, out var st)) continue;

                st.DeathRemainTimer -= dt;
                if (st.DeathRemainTimer > 0f)
                {
                    // 尚未到时间，回队列
                    corpseCleanupQ.Enqueue((id, reason));
                    continue;
                }

                // 摧毁entity
                DestroyEntity(id, reason);
            }
        }

        private ILogicEntity SpawnEntity(long entityId)
        {
            // 保持Active：若未加载则立刻Spawn+Wake（不走限流）或走优先级更高的队列
            if (Repo.IsLoaded(entityId))
            {
                return null;   
            }

            if (!Repo.Records.TryGetValue(entityId, out var rec)) return null;

            var ent = logicManager.CreateEntityByRecord(rec);
            Repo.Loaded[entityId] = ent;
            ent.Initialize();
            ent.OnSpawn(rec);

            return ent;
        }

        private void DespawnEntity(long id)
        {
            if (!Repo.IsLoaded(id)) return;

            var record = Repo.Records[id];
            var ent = Repo.GetLoaded(id);

            ent.OnDespawn(ref record);
            Repo.Loaded.Remove(id);

            //ent.EventOnDestroyed -= OnEventEntityDestroyed;
            logicManager.RecycleEntity(ent);
        }


        private void OnEventEntityDestroyed(long entityId)
        {

        }

        /// <summary>
        /// 请求销毁entity
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        public void RequestEntityDestroy(long id, string reason)
        {
            // 检查是否合法
            if (!Repo.HasRecord(id))
            {
                Debug.Log($"RequestEntityDestroy id:{id} not found {reason}");
                return;
            }

            // 保证拥有runtime状态
            if (!runtimeStates.TryGetValue(id, out var st))
            {
                st = new OneEntityRuntimeState { Id = id, State = LogicLifeState.NotLoaded };
                runtimeStates[id] = st;
            }

            st.IsMarkDestroy = true;
            st.DeathRemainTimer = 5.0f; // 留5秒钟容错时间

            corpseCleanupQ.Enqueue((id, reason));
        }

        /// <summary>
        /// 执行销毁 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool DestroyEntity(long id, string reason)
        {
            // 检查是否由record
            if(!Repo.HasRecord(id))
            {
                Debug.Log($"DestroyEntity id:{id} reason:{reason}");
                return false;
            }

            // 对于已加载的logic 先执行卸载
            if (Repo.IsLoaded(id))
            {
                var ent = Repo.GetLoaded(id);
                DespawnEntity(id); // 先卸载logic实体
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

                //// 4) 死亡后立即退出活跃：可先 Sleep，随后走尸体清理流程
                //if (st.DeathRemainTimer > 0f)
                //{
                //    // 让实体先 Sleep（停逻辑）
                //    EnqueueSleep(id);
                //    // 将尸体加入清理队列
                //    if (!corpseCleanupQ.Contains(id)) corpseCleanupQ.Enqueue(id);
                //}
                //else
                //{
                //    // 立即卸载
                //    DespawnEntity(id);

                //    RemoveLogicRecord(id);
                //}
            }


            UnregisterEntityRecord(id);

            return true;
        }


        /// <summary>
        /// 立即创建保活
        /// </summary>
        /// <param name="id"></param>
        /// <param name="rec"></param>
        /// <returns></returns>
        private ILogicEntity? ImmediateSpawnAndWake(long id)
        {
            // 防重入：若刚被加载，直接返回
            if (Repo.IsLoaded(id)) return Repo.GetLoaded(id);

            var newEnt = SpawnEntity(id);
            // OnWake（如需要）
            newEnt.OnWake();

            // 统一 AOI 索引（避免后续查询不到）
            UnitGridIndex.AddOrMove(id, newEnt.Pos);

            // 更新运行态
            var st = runtimeStates.ContainsKey(id) ? runtimeStates[id] : (runtimeStates[id] = new OneEntityRuntimeState { Id = id });
            st.State = LogicLifeState.Active;
            st.Timer = 0f;
            st.NearAnyWarmup = true;

            //IssueKeepAliveToken(id, ttlSec: 0.2f);

            return newEnt;
        }



    }
}

