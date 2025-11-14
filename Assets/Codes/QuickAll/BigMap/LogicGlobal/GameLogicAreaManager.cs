using Map.Entity;
using Map.Logic.Events;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static ChunkMapExportDatabase;

namespace My.Map.Logic
{

    // 逻辑实体的轻量描述（可存持久化）
    [Serializable]
    public class LogicEntityRecord
    {
        public long Id;               // 全局唯一ID
        public EEntityType EntityType;
        public string CfgId;     
        public Vector2 Position;
        public Vector2 FaceDir;

        public EFactionId FactionId;

        public bool DeadMark;
        public float LifeTime;

        public string BelongRoomId;
        public bool AlwaysActive;
    }

    [Serializable]
    public class LogicEntityRecord4LootPoint : LogicEntityRecord
    {
        public string DynamicDropId;
    }

    // 逻辑实体的轻量描述（可存持久化）
    [Serializable]
    public class LogicEntityRecord4UnitBase : LogicEntityRecord
    {
        public bool IsPeace;

        public BaseUnitLogicEntity.EMoveBehaveType MoveBehaveType;

        public string EnmityConfId;
        public List<string> MoveWayPoints;

        public long PatrolFollowId;
        public Vector2 PatrolGroupRelativePos;

        // 仅保存特殊状态 buff丢弃
        public bool Unsensored;
    }


    // 逻辑实体的轻量描述（可存持久化）
    [Serializable]
    public class LogicEntityRecord4PatrolGroup : LogicEntityRecord
    {
        public float MoveSpeed;
        public int WayPointIdx = 0;
        public float WayPointDistance;
        public List<string> WayPointList = new();
        public bool IsBack = false;

        public List<long> PatrolUnitIds = new();
    }


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

    //public static class ChunkConfig
    //{
    //    public static IEnumerable<ChunkMapExportDatabase.StaticItem> GetPrefabs(ChunkCoord c)
    //    {
    //        var db = Resources.Load<ChunkMapExportDatabase>("Area/1");

    //        var it = db.GetChunkStaticItems(c.X, c.Y);
    //        return it;
    //    }
    //}

    /// <summary>
    /// 房间信息 临时数据 考虑放在哪边
    /// </summary>
    public class LogicRoomInfo
    {
        public string RoomId = string.Empty;
        public RoomExportInfo rawData;

    }

    // InterestPoint：兴趣点（玩家、本地AI、相机锚点等）
    public class LogicAreaInterestPoint
    {
        public int Id;            // 唯一ID
        public Func<Vector3> Pos; // 实时位置获取委托
        public float LogicRadius; // 逻辑活跃半径（进入即唤醒）
        public float WarmupRadius;// 预热半径（在更远处预加载，进入Active半径更近）
    }

    public class LogicEntityRepository
    {
        public readonly Dictionary<long, LogicEntityRecord> Records = new();
        // 已加载的运行时实体
        public readonly Dictionary<long, ILogicEntity> Loaded = new();

        public bool HasRecord(long id) => Records.ContainsKey(id);
        public bool IsLoaded(long id) => Loaded.ContainsKey(id);

        public ILogicEntity GetLoaded(long id) => Loaded.TryGetValue(id, out var e) ? e : null;

        public void RegisterRecord(LogicEntityRecord r) => Records[r.Id] = r;
        public void RemoveRecord(long id) => Records.Remove(id);
    }

    public class LongLivedRegistry
    {
        private readonly Dictionary<long, ILogicEntity> _map = new();
        public void Register(ILogicEntity ent) => _map[ent.Id] = ent;
        public void Unregister(long id) => _map.Remove(id);
        public bool TryGet(long id, out ILogicEntity ent) => _map.TryGetValue(id, out ent);
        public IEnumerable<ILogicEntity> All => _map.Values;
    }

    public class UniformGridIndex<TKey> where TKey : IEquatable<TKey>
    {
        private readonly float cellSize;
        private readonly Dictionary<(int x, int y), List<TKey>> cellToIds = new();
        private readonly Dictionary<TKey, (int x, int y)> idToCell = new();

        public UniformGridIndex(float cellSize) { this.cellSize = Mathf.Max(1f, cellSize); }

        public static (int x, int y) PosToCell(Vector2 p, float cellSize)
        {
            int x = Mathf.FloorToInt(p.x / cellSize);
            int y = Mathf.FloorToInt(p.y / cellSize);
            return (x, y);
        }

        public void AddOrMove(TKey id, Vector2 pos)
        {
            var cell = PosToCell(pos, cellSize);
            if (idToCell.TryGetValue(id, out var old) && old.Equals(cell)) return;

            if (idToCell.TryGetValue(id, out var oldCell))
            {
                if (cellToIds.TryGetValue(oldCell, out var lst))
                    lst.Remove(id);
            }

            idToCell[id] = cell;
            if (!cellToIds.TryGetValue(cell, out var list))
                cellToIds[cell] = list = new List<TKey>(8);
            if (!list.Contains(id)) list.Add(id);
        }

        public void Remove(TKey id)
        {
            if (idToCell.TryGetValue(id, out var cell))
            {
                if (cellToIds.TryGetValue(cell, out var lst)) lst.Remove(id);
                idToCell.Remove(id);
            }
        }

        // 简易范围查询（方形近似）
        public void Query(Vector2 center, float radius, List<TKey> result)
        {
            if(result == null)
            {
                ;
            }
            result.Clear();
            int r = Mathf.CeilToInt(radius / cellSize);
            var c0 = PosToCell(center, cellSize);
            for (int y = c0.y - r; y <= c0.y + r; y++)
                for (int x = c0.x - r; x <= c0.x + r; x++)
                {
                    if (!cellToIds.TryGetValue((x, y), out var lst)) continue;
                    foreach (var id in lst) result.Add(id);
                }
        }

        public void Clear()
        {
            cellToIds.Clear();
            idToCell.Clear();
        }
    }


    // InterestPoint：兴趣点（玩家、本地AI、相机锚点等）
    public class InterestPoint
    {
        public int Id;            // 唯一ID
        public Func<Vector3> Pos; // 实时位置获取委托
        public float LogicRadius; // 逻辑活跃半径（进入即唤醒）
        public float WarmupRadius;// 预热半径（在更远处预加载，进入Active半径更近）
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
        public ChunkMapExportDatabase cacheDatabase;

        public Dictionary<string, LogicRoomInfo> RuntimeRoomInfos = new();

        private GameLogicManager logicManager;

        public InnerListener innerListener;
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

            // 加载 cacheDatabase
            cacheDatabase = Resources.Load<ChunkMapExportDatabase>($"Area/{areaId}");

            // 加载repo
            if (Repo == null)
            {
                Repo = new();
                // fake repo
                //  goujian 房间列表
                int cnt = 0;
                foreach (var refreshInfo in cacheDatabase.EntityRefreshInfo)
                {
                    cnt++;
                    HandleOneRefreshInfo(refreshInfo);

                    if(cnt > 100)
                    {
                        cnt = 0;
                        await Task.Yield();
                    }
                }
            }

            //  goujian 房间列表
            foreach(var chunk in cacheDatabase.Buckets)
            {
                foreach(var room in chunk.RoomExportInfos)
                {
                    LogicRoomInfo runtimeInfo = new LogicRoomInfo();
                    runtimeInfo.RoomId = room.RoomId;
                    runtimeInfo.rawData = room;

                    RuntimeRoomInfos[runtimeInfo.RoomId] = runtimeInfo;
                }
            }

            BuildIndexFromRecords();

            InitDigPoints();
        }


        private float checkRefreshTimer;
        private int tickDynamicObjIdx = 0; 

        public void OnMapLogicEvent(IMapLogicEvent ev)
        {
            var pos = ev.Ctx.HappenPos;
            UnitGridIndex.Query(pos, GridCellSize, queryBufInt);

            if(ev.Ctx.TargetId != 0)
            {
                queryBufInt.Add(ev.Ctx.TargetId);
            }

            foreach(var id in queryBufInt)
            {
                var entity = GetLogicEntiy(id, false);
                if(entity != null)
                {
                    entity.OnMapLogicEvent(ev);
                }
            }
        }


        protected bool CheckRefreshAppearCond(DynamicEntityAppearCond cond)
        {
            return false;
        }

        public void CheckRefreshAppearAndDisappear(float dt)
        {
            if(LogicTime.time < checkRefreshTimer + 1)
            {
                return;
            }

            int tickCnt = 100;
            while(tickCnt-- > 0)
            {
                tickDynamicObjIdx += 1;
                tickDynamicObjIdx = tickDynamicObjIdx % cacheDatabase.EntityRefreshInfo.Count;

                HandleOneRefreshInfo(cacheDatabase.EntityRefreshInfo[tickDynamicObjIdx]);
            }
            
        }

        public void HandleOneRefreshInfo(DynamicEntityRefreshInfo refreshInfo)
        {
            // 检查条件
            if (refreshInfo.AppearCond != null && refreshInfo.AppearCond.Type != 0)
            {
                if (!CheckRefreshAppearCond(refreshInfo.AppearCond))
                {
                    return;
                }
            }

            LogicEntityRecord record = null;
            var id = GameLogicManager.LogicEntityIdInst++;
            switch (refreshInfo.EntityType)
            {
                case EEntityType.PatrolGroup:
                    {
                        var patrolGroupRecord = new LogicEntityRecord4PatrolGroup();

                        var initInfo = (DynamicEntityInitInfo4PatrolGroup)refreshInfo.InitInfo;

                        patrolGroupRecord.WayPointIdx = 0;
                        patrolGroupRecord.WayPointDistance = 0;

                        patrolGroupRecord.MoveSpeed = initInfo.MoveSpeed;
                        patrolGroupRecord.WayPointList.AddRange(initInfo.Waypoints);

                        patrolGroupRecord.AlwaysActive = true;

                        var pName = patrolGroupRecord.WayPointList[patrolGroupRecord.WayPointIdx];
                        Vector2 point = cacheDatabase.FindNamedPointByName(pName).Position;
                        // 初始化巡逻兵
                        foreach (var one in initInfo.GroupUnits)
                        {
                            var oneRecrord = new LogicEntityRecord4UnitBase();

                            oneRecrord.Id = GameLogicManager.LogicEntityIdInst++;
                            oneRecrord.EntityType = one.EntityType;
                            oneRecrord.CfgId = one.CfgId;

                            oneRecrord.Position = point + one.RelativePos;
                            oneRecrord.FaceDir = refreshInfo.FaceDir;



                            oneRecrord.MoveBehaveType = BaseUnitLogicEntity.EMoveBehaveType.InPatrolGroup;
                            oneRecrord.PatrolFollowId = id;
                            oneRecrord.PatrolGroupRelativePos = one.RelativePos;


                            patrolGroupRecord.PatrolUnitIds.Add(oneRecrord.Id);

                            Repo.RegisterRecord(oneRecrord);
                        }
                        record = patrolGroupRecord;
                        break;
                    }
                case EEntityType.Npc:
                case EEntityType.Monster:
                    {
                        var unitRecord = new LogicEntityRecord4UnitBase();

                        var initInfo = (DynamicEntityInitInfo4Unit)refreshInfo.InitInfo;

                        unitRecord.IsPeace = initInfo.IsPeace;
                        unitRecord.MoveBehaveType = initInfo.MoveMode;
                        unitRecord.EnmityConfId = initInfo.EnmityConfId;
                        unitRecord.Unsensored = initInfo.InitUnsensored;

                        record = unitRecord;
                        break;
                    }
            }

            if(record != null)
            {
                record.Id = id;
                record.EntityType = refreshInfo.EntityType;
                record.CfgId = refreshInfo.CfgId;
                record.Position = refreshInfo.Position;
                record.BelongRoomId = refreshInfo.BindRoomId;
                record.FactionId = refreshInfo.OrgFactionId;

                Repo.RegisterRecord(record);
            }
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
            if(rec.AlwaysActive)
            {
                var ent = logicManager.CreateEntityByRecord(rec);
                ent.OnSpawn(rec);
                Repo.Loaded[rec.Id] = ent;

                // 特殊容器
                LongLived.Register(ent);

                // 初始状态：可选择 Active 或 Sleep
                runtimeStates[rec.Id] = new OneEntityRuntimeState { Id = rec.Id, State = LogicLifeState.Active };
            }
            else
            {

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

            // 新增：死亡态处理
            public bool IsDeadRuntime;        // 运行时死亡标记
            public float DeathRemainTimer;    // 尸体/残留计时
        }

        private readonly Dictionary<long, OneEntityRuntimeState> runtimeStates = new();
        private readonly Dictionary<int, InterestPoint> interestPoints = new();

        // 工作队列（限流）
        private readonly Queue<long> spawnEntityQ = new();
        private readonly Queue<long> despawnEntityQ = new();
        private readonly Queue<long> wakeEntityQ = new();
        private readonly Queue<long> sleepEntityQ = new();
        private readonly Queue<long> dieEntityQ = new();
        private readonly Queue<long> corpseCleanupQ = new();

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
                return ent;
            }

            if(ensureExist)
            {
                return ImmediateSpawnAndWake(instId, rec);
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

            // 处理死亡状态
            ProcessDieQueue();

            // 处理其他加载卸载等
            ProcessQueues(dt);

            // 处理尸体回收
            ProcessCorpse(dt);
        }

        private void StepStateMachine(OneEntityRuntimeState st, float dt)
        {
            if (st.IsDeadRuntime)
            {
                // 死亡后交给尸体处理管线清理状态
                return;
            }

            if(st.ForceActiveUntil != 0 && LogicTime.time < st.ForceActiveUntil)
            {
                return;
            }

            bool hasRec = Repo.Records.TryGetValue(st.Id, out var rec);
            bool activeFlag = rec.AlwaysActive;

            if (activeFlag)
            {
                // 保持Active：若未加载则立刻Spawn+Wake（不走限流）或走优先级更高的队列
                if (!Repo.IsLoaded(st.Id))
                {
                    // 直接创建避免队列延迟，或使用优先队列
                    var ent = logicManager.CreateEntityByRecord(rec);
                    ent.OnSpawn(rec);
                    Repo.Loaded[st.Id] = ent;
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
                    }
                    else
                    {
                        st.State = LogicLifeState.Cooldown;
                        st.Timer = settings.ExitCooldown;
                    }
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
                DespawnEntity(id, false);
            }

            // Spawn：对长生命周期可给“优先队列”或即时创建（你已在 StepStateMachine 中处理 AlwaysActive）
            n = settings.MaxSpawnPerFrame;
            while (spawnEntityQ.Count > 0 && n-- > 0)
            {
                var id = spawnEntityQ.Dequeue();
                if (Repo.IsLoaded(id)) continue;
                if (!Repo.Records.TryGetValue(id, out var rec)) continue;

                var ent = logicManager.CreateEntityByRecord(rec);
                ent.OnSpawn(rec);
                Repo.Loaded[id] = ent;

                if (rec.AlwaysActive)
                {
                    LongLived.Register(ent);
                }
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

        private void ProcessDieQueue()
        {
            int budget = 64; // 可配置
            while (dieEntityQ.Count > 0 && budget-- > 0)
            {
                var id = dieEntityQ.Dequeue();
                KillEntity(id);
            }
        }

        private void ProcessCorpse(float dt)
        {
            int n = 32; // 每帧检查上限
            int count = Math.Min(n, corpseCleanupQ.Count);
            for (int i = 0; i < count; i++)
            {
                var id = corpseCleanupQ.Dequeue();
                if (!runtimeStates.TryGetValue(id, out var st)) continue;
                if (!Repo.Records.TryGetValue(id, out var rec)) continue;

                st.DeathRemainTimer -= dt;
                if (st.DeathRemainTimer > 0f)
                {
                    // 尚未到时间，回队列
                    corpseCleanupQ.Enqueue(id);
                    continue;
                }

                if (rec.AlwaysActive)
                {
                }

                DespawnEntity(id, true);

                // 从 AOI 移除（如果之前没移除）
                UnitGridIndex.Remove(id); 

                st.State = LogicLifeState.NotLoaded;
                st.Timer = 0;

                //EnqueueDespawn(id);

                runtimeStates.Remove(id);
            }
        }

        public void DespawnEntity(long id, bool isDead)
        {
            if (!Repo.IsLoaded(id)) return;
            if (Repo.Records.TryGetValue(id, out var rec) && rec.AlwaysActive)
            {
                return;
            }
            var ent = Repo.GetLoaded(id);

            ent.OnDespawn(out var snap);
            Repo.Loaded.Remove(id);
            if (snap != null) Repo.Records[id] = snap;

            logicManager.RecycleEntity(ent);

            // 因死亡而移除
            if(isDead)
            {
                UnitGridIndex.Remove(id);
                runtimeStates.Remove(id);
            }
        }

        // 异步请求：下一帧/本帧队列处理
        public void RequestEntityDie(long id)
        {
            if (!dieEntityQ.Contains(id)) dieEntityQ.Enqueue(id);
        }

        /// <summary>
        /// entity
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool KillEntity(long id)
        {
            // 1) 校验是否已加载
            if (!Repo.IsLoaded(id)) return false;

            var ent = Repo.GetLoaded(id);
            if (ent == null) return false;

            // 2) 调用实体死亡逻辑
            ent.MarkDead = true;

            // 3) 标记运行态和记录
            if (!runtimeStates.TryGetValue(id, out var st))
            {
                st = new OneEntityRuntimeState { Id = id, State = LogicLifeState.NotLoaded };
                runtimeStates[id] = st;
            }
            st.IsDeadRuntime = true;
            st.DeathRemainTimer = 0.5f;

            // 4) 死亡后立即退出活跃：可先 Sleep，随后走尸体清理流程
            if (st.DeathRemainTimer > 0f)
            {
                // 让实体先 Sleep（停逻辑）
                EnqueueSleep(id);
                // 将尸体加入清理队列
                if (!corpseCleanupQ.Contains(id)) corpseCleanupQ.Enqueue(id);
            }
            else
            {
                // 立即卸载
                DespawnEntity(id, true);
            }
            return true;
        }

        /// <summary>
        /// 立即创建保活
        /// </summary>
        /// <param name="id"></param>
        /// <param name="rec"></param>
        /// <returns></returns>
        private ILogicEntity? ImmediateSpawnAndWake(long id, LogicEntityRecord rec)
        {
            // 防重入：若刚被加载，直接返回
            if (Repo.IsLoaded(id)) return Repo.GetLoaded(id);

            // 创建实例
            var ent = logicManager.CreateEntityByRecord(rec);
            if (ent == null) return null;

            // OnSpawn
            ent.OnSpawn(rec);

            // 写 Loaded
            Repo.Loaded[id] = ent;

            // OnWake（如需要）
            ent.OnWake();

            // 统一 AOI 索引（避免后续查询不到）
            UnitGridIndex.AddOrMove(id, rec.Position);

            // 更新运行态
            var st = runtimeStates.ContainsKey(id) ? runtimeStates[id] : (runtimeStates[id] = new OneEntityRuntimeState { Id = id });
            st.State = LogicLifeState.Active;
            st.Timer = 0f;
            st.NearAnyWarmup = true;

            //IssueKeepAliveToken(id, ttlSec: 0.2f);

            return ent;
        }
    }
}

