using cfg.demo;
using Map.Entity;
using Map.Logic.Events;
using My.Config;
using My.Map;
using My.Map.Entity;
using My.MapExport;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    /// ??????? ??????? ???????????
    /// </summary>
    public class LogicRoomInfo
    {
        public string RoomId = string.Empty;
        //public RoomExportInfo rawData;

    }

    /// <summary>
    /// ????????
    /// </summary>
    public partial class GameLogicAreaManager
    {
        //public int ChunkCellSize = 32;  // ?????????
        private readonly Settings settings;



        public float GridCellSize = 16f;
        public UniformGridIndex<long> UnitGridIndex;
        public UniformGridIndex<string> RoomGridIndex;


        public LogicEntityRepository Repo;
        public LongLivedRegistry LongLived { get; } = new();

        public string MapName = string.Empty;
        public MapAreaInfo cacheMapCfg { get; private set; }

        public MapExportDatabase cacheDatabase;

        public Dictionary<string, LogicRoomInfo> RuntimeRoomInfos = new();
        

        private GameLogicManager logicManager;

        public InnerListener innerListener;
        public List<DynamicEntityRefreshInfo> EntityRefreshInfo = new List<DynamicEntityRefreshInfo>();

        // StaticId -> ????????????????????SavePoint ?????????????
        private Dictionary<int, DynamicEntityRefreshInfo> _refreshInfoByStaticId;

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
        /// ???????????
        /// </summary>
        public void InitilizeMap(string mapName)
        {
            this.MapName = mapName;
            cacheMapCfg = CfgMgr.Cfgs.TbMapAreaInfo.GetOrDefault(mapName);

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

            Repo = null;


            // ???? cacheDatabase
            cacheDatabase = Resources.Load<MapExportDatabase>($"MapExport/{mapName}");

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

            // ????repo
            if (Repo == null)
            {
                Repo = new();
            }

            if(cacheMapCfg != null && cacheMapCfg.IsHome)
            {
                var homeRefreshs = logicManager.homeDataManager.GetAllValidLogicEntites();
                EntityRefreshInfo.AddRange(homeRefreshs);
            }

            RebuildRefreshInfoByStaticId();

            BuildIndexFromRecords();

            InitDigPoints();
            InitGuardSpawnPoints();
            InitWalkerPath();

            checkRefreshTimer = LogicTime.time;

            logicManager.ApplyPendingMapRuntimeAfterMapInit(mapName);
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
            DialogForceStaticIds.Clear();

            EntityRefreshInfo.Clear();

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
                case EMapLogicEventType.UnitDie:
                    {
                        var realEv = (MLEUnitDie)ev;
                        AlertOnEntityDie(realEv.EntityId);
                    }
                    break;
            }
        }

        
        // ??????????????????????????
        public void BuildIndexFromRecords()
        {
            foreach (var kv in Repo.Records)
                UnitGridIndex.AddOrMove(kv.Key, kv.Value.Position);
        }

        // ???/????????
        public void AddInterestPoint(InterestPoint ip) => interestPoints[ip.Id] = ip;
        public void RemoveInterestPoint(int id) => interestPoints.Remove(id);


        public LogicRoomInfo GetRoomByPos(Vector2 logicPos)
        {
            return null;
        }

        #region aoi ????????

        #endregion


        

        public ILogicEntity GetLogicEntiy(long instId, bool ensureExist = true)
        {
            // ????????? ?????????
            if (!Repo.Records.TryGetValue(instId, out var rec)) return null;

            // 2) ??????????????
            if (Repo.IsLoaded(instId))
            {
                var ent = Repo.GetLoaded(instId);

                // ????
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

        // ??????????????
        public void Tick(float dt)
        {
            // ??????
            CheckRefreshAppearAndDisappear(dt);

            TickEntityLifeCycle(dt);

            TickRefreshWalker();

            TickLowFreqTickRecord();

            // ??????????
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
                // ??????????????????????????
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
                // ????Active????????????????Spawn+Wake?????????????????????????????
                if (!Repo.IsLoaded(st.Id))
                {
                    // ?????????????????????????????
                    var ent = SpawnEntity(rec.Id);
                }
                // ??????Wake
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
                            // ?????????????always active????????? Warmup -> Active ????????????NotLoaded
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
                        // ????????????????????????
                        EnqueueDespawn(st.Id);
                        st.State = LogicLifeState.NotLoaded;
                    }
                    // isLong ??????? Warmup???????? Warmup??Loaded????Wake?????? Sleep?????? Despawn
                    else if (!st.NearAnyWarmup && activeFlag)
                    {
                        // ?????????? Loaded ?? Sleep???? Loaded ???? Warmup ???????
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
                                    // ??????????????? Despawn??????? Sleep??Loaded ????Wake??
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
                                // ??????????????? Sleep?????? Despawn
                                st.Timer = settings.SleepToDespawnDelay; // ?????????
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// ??????????????????????
        /// ??????????I????????
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

            // Despawn????????????????
            n = settings.MaxDespawnPerFrame;
            while (despawnEntityQ.Count > 0 && n-- > 0)
            {
                var id = despawnEntityQ.Dequeue();
                DespawnEntity(id);
            }

            // Spawn????????????????????????????????????????????? StepStateMachine ?????? AlwaysActive??
            n = settings.MaxSpawnPerFrame;
            while (spawnEntityQ.Count > 0 && n-- > 0)
            {
                var id = spawnEntityQ.Dequeue();

                var ent = SpawnEntity(id);
            }

            // Sleep/Wake ????
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
        //    int budget = 64; // ??????
        //    while (destroyEntityQ.Count > 0 && budget-- > 0)
        //    {
        //        var pair = destroyEntityQ.Dequeue();
        //        DestroyEntity(pair.Item1, pair.Item2);
        //    }
        //}

        private float _corpseTickTimer;

        /// <summary>
        /// ???????????
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

            int n = 32; // ?????????
            int count = Math.Min(n, corpseCleanupQ.Count);
            for (int i = 0; i < count; i++)
            {
                (var id, string reason) = corpseCleanupQ.Dequeue();
                // ?????????????????
                if (!Repo.Records.TryGetValue(id, out var rec)) continue;

                if (!runtimeStates.TryGetValue(id, out var st)) continue;

                st.DeathRemainTimer -= dt;
                if (st.DeathRemainTimer > 0f)
                {
                    // ??????????????
                    corpseCleanupQ.Enqueue((id, reason));
                    continue;
                }

                // ???entity
                DestroyEntity(id, reason);
            }
        }

        private ILogicEntity SpawnEntity(long entityId)
        {
            // ????Active????????????????Spawn+Wake?????????????????????????????
            if (Repo.IsLoaded(entityId))
            {
                return null;   
            }

            if (!Repo.Records.TryGetValue(entityId, out var rec)) return null;

            var ent = logicManager.CreateEntityByRecord(rec);
            Repo.Loaded[entityId] = ent;
            ent.Initialize();
            ent.OnSpawn(rec);

            if (rec.EntityType == EEntityType.Player && ent is IEntityBuffOwner buffOwner)
            {
                logicManager.RestorePendingPlayerBuffsIfAny(buffOwner);
            }

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
        /// ????????entity
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        public void RequestEntityDestroy(long id, string reason)
        {
            // ?????????
            if (!Repo.HasRecord(id))
            {
                Debug.Log($"RequestEntityDestroy id:{id} not found {reason}");
                return;
            }

            // ??????runtime???
            if (!runtimeStates.TryGetValue(id, out var st))
            {
                st = new OneEntityRuntimeState { Id = id, State = LogicLifeState.NotLoaded };
                runtimeStates[id] = st;
            }

            st.IsMarkDestroy = true;
            st.DeathRemainTimer = 5.0f; // ??5??????????

            corpseCleanupQ.Enqueue((id, reason));
        }

        /// <summary>
        /// ???????? 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <summary>
        /// 立即移除逻辑实体与 Record（不经过死亡延迟队列），用于刷新条件驱动的显隐。
        /// </summary>
        public bool ForceDestroyEntityNow(long id, string reason)
        {
            return DestroyEntity(id, reason);
        }

        private bool DestroyEntity(long id, string reason)
        {
            // ????????record
            if(!Repo.HasRecord(id))
            {
                Debug.Log($"DestroyEntity id:{id} reason:{reason}");
                return false;
            }

            // ??????????logic ?????????
            if (Repo.IsLoaded(id))
            {
                var ent = Repo.GetLoaded(id);
                DespawnEntity(id); // ??????logic???
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

                //// 4) ?????????????????????? Sleep??????????????????
                //if (st.DeathRemainTimer > 0f)
                //{
                //    // ??????? Sleep????????
                //    EnqueueSleep(id);
                //    // ????????????????
                //    if (!corpseCleanupQ.Contains(id)) corpseCleanupQ.Enqueue(id);
                //}
                //else
                //{
                //    // ????????
                //    DespawnEntity(id);

                //    RemoveLogicRecord(id);
                //}
            }


            UnregisterEntityRecord(id);

            return true;
        }


        /// <summary>
        /// ????????????
        /// </summary>
        /// <param name="id"></param>
        /// <param name="rec"></param>
        /// <returns></returns>
        private ILogicEntity? ImmediateSpawnAndWake(long id)
        {
            // ???????????????????????
            if (Repo.IsLoaded(id)) return Repo.GetLoaded(id);

            var newEnt = SpawnEntity(id);
            // OnWake??????????
            newEnt.OnWake();

            // ?? AOI ????????????????????????
            UnitGridIndex.AddOrMove(id, newEnt.Pos);

            // ???????????
            var st = runtimeStates.ContainsKey(id) ? runtimeStates[id] : (runtimeStates[id] = new OneEntityRuntimeState { Id = id });
            st.State = LogicLifeState.Active;
            st.Timer = 0f;
            st.NearAnyWarmup = true;

            //IssueKeepAliveToken(id, ttlSec: 0.2f);

            return newEnt;
        }



    }
}

