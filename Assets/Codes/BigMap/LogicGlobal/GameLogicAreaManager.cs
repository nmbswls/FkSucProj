using Map.Entity;
using Map.Logic.Events;
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

        public string AreaId = string.Empty;
        public MapExportDatabase cacheDatabase;

        public Dictionary<string, LogicRoomInfo> RuntimeRoomInfos = new();
        public Dictionary<int, long> RefreshInfo2Record = new();

        private GameLogicManager logicManager;

        public InnerListener innerListener;
        public List<DynamicEntityRefreshInfo> EntityRefreshInfo = new List<DynamicEntityRefreshInfo>();
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
        public async Task InitilizeArea(string areaId)
        {
            this.AreaId = areaId;

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

            // 加载 cacheDatabase
            cacheDatabase = Resources.Load<MapExportDatabase>($"MapExport/{areaId}");

            EntityRefreshInfo.Clear();
            EntityRefreshInfo.AddRange(cacheDatabase.EntityRefreshInfo);
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

            if(areaId == "home")
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
                UnitGridIndex.Query(pos, GridCellSize, queryBufInt);

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

            
        }

        

        public bool IsRecordAlwaysActive(LogicEntityRecord rec)
        {
            switch (rec.EntityType)
            {
                case EEntityType.Player:
                case EEntityType.PatrolGroup:
                case EEntityType.HomePlacement:
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rec"></param>
        /// <returns></returns>
        public void RegisterEntityRecord(LogicEntityRecord rec)
        {
            // 交由仓库管理
            Repo.RegisterRecord(rec);
            // 注册到 AOI
            UnitGridIndex.AddOrMove(rec.Id, rec.Position);

            // 长生命周期对象
            if(IsRecordAlwaysActive(rec))
            {
                var ent = SpawnEntity(rec.Id);
                if(ent == null)
                {
                    Debug.LogError("RegisterEntityRecord create null ");
                }
                else
                {
                    // 特殊容器
                    LongLived.Register(ent);

                    // 初始状态：可选择 Active 或 Sleep
                    runtimeStates[rec.Id] = new OneEntityRuntimeState { Id = rec.Id, State = LogicLifeState.Active };
                }
            }
        }


        /// <summary>
        /// 更新实体位置
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="newPos"></param>
        public void UpdatePosition(long entityId, Vector2 newPos)
        {
            UnitGridIndex.AddOrMove(entityId, newPos);
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


        public enum LogicLifeState
        {
            NotLoaded,   // 未加载，仅有Record
            Warmup,      // 预热加载中（Spawn中/完成但未Wake）
            Active,      // 完全活跃
            Cooldown,    // 冷却计时（离开后延迟降级）
            Sleep        // 休眠（轻量态）
        }


        [Serializable]
        public class Settings
        {
            public float WarmupToActiveRadiusBias = -10f; // 例：在距兴趣点半径减10m时转Active
            public float ExitCooldown = 2.0f;             // 离开后保持Active的时间
            public float SleepToDespawnDelay = 8.0f;      // 休眠保持多久后可真正卸载（可选）
            public int MaxSpawnPerFrame = 8;              // 每帧最大重建数
            public int MaxDespawnPerFrame = 6;            // 每帧最大卸载数
            public int MaxWakePerFrame = 16;              // 每帧最大Wake数
            public int MaxSleepPerFrame = 16;             // 每帧最大Sleep数
        }

        // 实体运行态
        private class OneEntityRuntimeState
        {
            public long Id;
            public LogicLifeState State;
            public float Timer;            // 冷却/延迟计时
            public int InterestRefCount;   // 当前落入任一兴趣半径的引用计数
            public bool NearAnyWarmup;     // 是否落入任一Warmup半径
            public float ForceActiveUntil;

            public bool IsMarkDestroy;        // 运行时死亡标记
            public float DeathRemainTimer;    // 尸体/残留计时
        }

        private readonly Dictionary<long, OneEntityRuntimeState> runtimeStates = new();
        private readonly Dictionary<int, InterestPoint> interestPoints = new();

        // 工作队列（限流）
        private readonly Queue<long> spawnEntityQ = new();
        private readonly Queue<long> despawnEntityQ = new();
        private readonly Queue<long> wakeEntityQ = new();
        private readonly Queue<long> sleepEntityQ = new();
        //private readonly Queue<(long, int)> destroyEntityQ = new();
        private readonly Queue<(long, string)> corpseCleanupQ = new();

        // 复用容器
        private readonly List<long> queryBufInt = new(256);

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

            // 1) 重新评估AOI：计算每个实体与兴趣点关系（按区域近似）
            // 做法：对每个兴趣点查询 Warmup/Active 两种半径并合并标记，避免O(N*M)
            var warmIds = new HashSet<long>();
            var activeIds = new HashSet<long>();

            foreach (var ip in interestPoints.Values)
            {
                // 预热查询
                UnitGridIndex.Query(ip.Pos(), ip.WarmupRadius,  queryBufInt);
                foreach (var id in queryBufInt) warmIds.Add(id);

                // Active半径（可用Bias或单独半径）
                float activeR = Mathf.Max(0.1f, ip.LogicRadius);
                UnitGridIndex.Query(ip.Pos(), activeR,  queryBufInt);
                foreach (var id in queryBufInt) activeIds.Add(id);
            }

            // 2) 根据集合更新每个实体状态
            // 为了避免遍历全库，这里仅对“受影响集合”以及“已加载/已有状态”的实体进行处理。
            // 简易实现：合并三类集合
            var affected = new HashSet<long>(warmIds);
            foreach (var id in activeIds) affected.Add(id);
            foreach (var id in runtimeStates.Keys) affected.Add(id);

            foreach (var id in affected)
            {
                bool inWarm = warmIds.Contains(id);
                bool inActive = activeIds.Contains(id);

                if (!runtimeStates.TryGetValue(id, out var st))
                {
                    st = new OneEntityRuntimeState { Id = id, State = LogicLifeState.NotLoaded };
                    runtimeStates[id] = st;
                }

                // 记录兴趣关系
                st.NearAnyWarmup = inWarm;
                st.InterestRefCount = inActive ? 1 : 0;

                StepStateMachine(st, dt);
            }

            //// 处理死亡状态
            //ProcessDieQueue();

            // 处理其他加载卸载等
            ProcessQueues(dt);

            // 处理尸体回收
            ProcessCorpse(dt);

            TickRefreshWalker();

            TickLowFreqTickRecord();
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

                if(rec is LogicEntityRecord4UnitBase unitRecord)
                {
                    if(unitRecord.MoveBehaveType != UnitMoveBehaveInfo.EMoveBehaveType.MovePath)
                    {
                        continue;
                    }

                    var path = GetRuntimePath(unitRecord.MovePath);
                    if(unitRecord.CurrPathIdx == path.PointList.Count - 1)
                    {
                        continue;
                    }

                    var p0 = path.PointList[unitRecord.CurrPathIdx];
                    var p1 = path.PointList[unitRecord.CurrPathIdx + 1];

                    var dist = (p1 - p0).magnitude;
                    var spd = 2.0f;

                    float addProgress = (spd * interval) / dist;
                    unitRecord.CurrPathProgress += addProgress;

                    if(unitRecord.CurrPathProgress >= 1)
                    {
                        unitRecord.CurrPathProgress = 0;
                        unitRecord.CurrPathIdx += 1;
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
            ent.OnSpawn(rec);
            Repo.Loaded[entityId] = ent;

            return ent;
        }

        private void DespawnEntity(long id)
        {
            if (!Repo.IsLoaded(id)) return;
            
            var ent = Repo.GetLoaded(id);

            ent.OnDespawn(out var snap);
            Repo.Loaded.Remove(id);
            if (snap != null) Repo.Records[id] = snap;

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


            RemoveLogicRecord(id);

            return true;
        }


        private void RemoveLogicRecord(long recId)
        {
            Repo.Records.TryGetValue(recId, out var rec);
            if (rec != null && IsRecordAlwaysActive(rec))
            {
                LongLived.Unregister(recId);
            }
            Repo.RemoveRecord(recId);
            UnitGridIndex.Remove(recId);
            runtimeStates.Remove(recId);
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

