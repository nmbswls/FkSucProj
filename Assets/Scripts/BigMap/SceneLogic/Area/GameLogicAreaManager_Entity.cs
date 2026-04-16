

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using cfg.demo;
using Config;
using My.Config;
using My.Map;
using My.MapExport;
using UnityEngine;

namespace My.Map.Logic

{
    public partial class GameLogicAreaManager
    {

        public class LogicEntityRepository
        {
            public readonly Dictionary<long, LogicEntityRecord> Records = new();
            // ??????????????
            public readonly Dictionary<long, ILogicEntity> Loaded = new();

            public bool HasRecord(long id) => Records.ContainsKey(id);
            public bool IsLoaded(long id) => Loaded.ContainsKey(id);

            public ILogicEntity GetLoaded(long id) => Loaded.TryGetValue(id, out var e) ? e : null;

            public void RegisterRecord(LogicEntityRecord r) => Records[r.Id] = r;
            public void RemoveRecord(long id) => Records.Remove(id);

            public void Clear()
            {

                Records.Clear();
                Loaded.Clear();
            }
        }

        public class LongLivedRegistry
        {
            private readonly Dictionary<long, ILogicEntity> _map = new();
            public void Register(ILogicEntity ent) => _map[ent.Id] = ent;
            public void Unregister(long id) => _map.Remove(id);
            public bool TryGet(long id, out ILogicEntity ent) => _map.TryGetValue(id, out ent);
            public IEnumerable<ILogicEntity> All => _map.Values;

            public void Clear()
            {
                _map.Clear();
            }
        }

        public class SceneRefreshInfoRuntime
        {
            public long EntityInstId;
            public float LastRespawnTime;
            public float LastDestroyTime;

            // ???????????????????????/????????????????????????????????????????????????????????????????
            public bool LastRemovalWasVisibilityCond;
        }
        public Dictionary<int, SceneRefreshInfoRuntime> RefreshInfoRuntimes = new();

        public Dictionary<long, int> Record2RefreshInfo = new();

        private Dictionary<EEntityType, List<long>> Type2EntityList = new();

        public HashSet<long> NewCreateEntityMark = new();

        /// <summary>
        /// ??????????
        /// </summary>
        /// <param name="dt"></param>
        public void CheckRefreshAppearAndDisappear(float dt)
        {
            if (EntityRefreshInfo.Count == 0)
            {
                return;
            }

            if (LogicTime.time < checkRefreshTimer + 0.5)
            {
                return;
            }

            checkRefreshTimer = LogicTime.time;

            int tickCnt = 100;
            while (tickCnt-- > 0)
            {
                tickDynamicObjIdx += 1;
                tickDynamicObjIdx = tickDynamicObjIdx % EntityRefreshInfo.Count;

                HandleOneRefreshInfo(EntityRefreshInfo[tickDynamicObjIdx]);
            }
        }

        /// <summary>
        /// ?????????
        /// </summary>
        public void ForceCheckRefreshInfos()
        {
            foreach(var refreshInfo in EntityRefreshInfo)
            {
                if(!DialogForceStaticIds.Contains(refreshInfo.StaticId))
                {
                    continue;
                }

                HandleOneRefreshInfo(refreshInfo);
            }
        }

        /// <summary>
        /// ??????????
        /// </summary>
        /// <param name="refreshInfo"></param>
        public void HandleOneRefreshInfo(DynamicEntityRefreshInfo refreshInfo)
        {
            RefreshInfoRuntimes.TryGetValue(refreshInfo.StaticId, out var refreshRuntime);

            if (refreshRuntime != null && refreshRuntime.EntityInstId != 0 &&
                Repo.Records.TryGetValue(refreshRuntime.EntityInstId, out var liveRec) &&
                !liveRec.MarkDestroyed)
            {
                if (!DialogForceStaticIds.Contains(refreshInfo.StaticId) &&
                    ShouldHideByRefreshConditions(refreshInfo))
                {
                    ForceDestroyEntityNow(refreshRuntime.EntityInstId, "RefreshCondHide");
                    if (RefreshInfoRuntimes.TryGetValue(refreshInfo.StaticId, out var rtVis))
                    {
                        rtVis.LastRemovalWasVisibilityCond = true;
                    }

                    RefreshInfoRuntimes.TryGetValue(refreshInfo.StaticId, out refreshRuntime);
                }
                else
                {
                    return;
                }
            }

            if (RefreshInfoRuntimes.TryGetValue(refreshInfo.StaticId, out refreshRuntime))
            {
                if (refreshRuntime.EntityInstId != 0 &&
                    Repo.Records.TryGetValue(refreshRuntime.EntityInstId, out var existingRec) &&
                    !existingRec.MarkDestroyed)
                {
                    return;
                }

                if (refreshInfo.WillRespawn)
                {
                    if (LogicTime.time - refreshRuntime.LastDestroyTime < refreshInfo.RespawnInterval)
                    {
                        return;
                    }
                }
                else if (!IsSavePointRefreshInfo(refreshInfo))
                {
                    if (!HasDynamicVisibilityCond(refreshInfo) || !refreshRuntime.LastRemovalWasVisibilityCond)
                    {
                        return;
                    }
                }
            }

            // ???????????
            // todo ????
            if(!DialogForceStaticIds.Contains(refreshInfo.StaticId))
            {
                // ???????
                if (refreshInfo.AppearCond != null && refreshInfo.AppearCond.Type != ECommonCheckType.None)
                {
                    if (!logicManager.CheckCommonCond(refreshInfo.AppearCond))
                    {
                        return;
                    }
                }
            }
            
            LogicEntityRecord record = CreateEntityRecordFromInitInfo(refreshInfo.InitInfo);

            if(record == null)
            {
                Debug.Log("HandleOneRefreshInfo err not good");
                return;
            }

            RegisterEntityRecord(record);
            RefreshInfoRuntimes[refreshInfo.StaticId] = new SceneRefreshInfoRuntime()
            {
                EntityInstId = record.Id,
                LastRespawnTime = LogicTime.time,
                LastRemovalWasVisibilityCond = false,
            };

            Record2RefreshInfo[record.Id] = refreshInfo.StaticId;
        }

        private void RebuildRefreshInfoByStaticId()
        {
            if (_refreshInfoByStaticId == null)
            {
                _refreshInfoByStaticId = new Dictionary<int, DynamicEntityRefreshInfo>(EntityRefreshInfo.Count);
            }
            else
            {
                _refreshInfoByStaticId.Clear();
            }

            foreach (var ri in EntityRefreshInfo)
            {
                _refreshInfoByStaticId[ri.StaticId] = ri;
            }
        }

        internal bool TryGetRefreshInfoByStaticId(int staticId, out DynamicEntityRefreshInfo info)
        {
            if (_refreshInfoByStaticId == null)
            {
                info = null;
                return false;
            }

            return _refreshInfoByStaticId.TryGetValue(staticId, out info);
        }

        internal static bool IsSavePointRefreshInfo(DynamicEntityRefreshInfo ri)
        {
            return ri?.InitInfo != null && ri.InitInfo.EntityType == EEntityType.SavePoint;
        }

        private static bool HasDynamicVisibilityCond(DynamicEntityRefreshInfo ri)
        {
            bool hasAppear = ri.AppearCond != null && ri.AppearCond.Type != ECommonCheckType.None;
            bool hasDisappear = ri.DisappearCond != null && ri.DisappearCond.Type != ECommonCheckType.None;
            return hasAppear || hasDisappear;
        }

        private bool ShouldHideByRefreshConditions(DynamicEntityRefreshInfo refreshInfo)
        {
            if (refreshInfo.AppearCond != null && refreshInfo.AppearCond.Type != ECommonCheckType.None)
            {
                if (!logicManager.CheckCommonCond(refreshInfo.AppearCond))
                {
                    return true;
                }
            }

            if (refreshInfo.DisappearCond != null && refreshInfo.DisappearCond.Type != ECommonCheckType.None)
            {
                if (logicManager.CheckCommonCond(refreshInfo.DisappearCond))
                {
                    return true;
                }
            }

            return false;
        }


        public LogicEntityRecord CreateEntityRecordFromInitInfo(EntityInitInfo initInfo)
        {
            LogicEntityRecord record = null;
            var id = GameLogicManager.LogicEntityIdInst++;

            switch (initInfo.EntityType)
            {
                case EEntityType.PatrolGroup:
                    {
                        var patrolGroupRecord = new LogicEntityRecord4PatrolGroup();

                        var initInfo4PatrolGroup = (EntityInitInfo4PatrolGroup)initInfo;

                        patrolGroupRecord.WayPointIdx = 0;
                        patrolGroupRecord.WayPointDistance = 0;

                        patrolGroupRecord.MoveSpeed = initInfo4PatrolGroup.MoveSpeed;
                        patrolGroupRecord.WayPointList.AddRange(initInfo4PatrolGroup.Waypoints);

                        var pName = patrolGroupRecord.WayPointList[patrolGroupRecord.WayPointIdx];
                        Vector2 point = cacheDatabase.FindNamedPointByName(pName)?.Position ?? Vector3.zero;
                        // ?????????
                        foreach (var one in initInfo4PatrolGroup.GroupUnits)
                        {
                            var groupRecord = CreateEntityRecordFromInitInfo(one.InitInfo);
                            if(groupRecord == null)
                            {
                                Debug.Log("PatrolGroup group unit create fail " + one.GroupIdx);
                                continue;
                            }
                            patrolGroupRecord.PatrolUnitIds.Add(groupRecord.Id);
                            Repo.RegisterRecord(groupRecord);
                        }
                        record = patrolGroupRecord;
                        break;
                    }
                case EEntityType.Npc:
                    {
                        var unitRecord = new LogicEntityRecord4Npc();

                        var npcId = initInfo.CfgId;
                        var npcCfg = CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(npcId);
                        if(npcCfg == null)
                        {
                            break;
                        }
                        var initInfo4Unit = (EntityInitInfo4Unit)initInfo;

                        unitRecord.IsPeace = initInfo4Unit.IsPeace;
                        unitRecord.MoveBehaveType = initInfo4Unit.MoveMode;
                        unitRecord.EnmityConfId = initInfo4Unit.EnmityConfId;
                        unitRecord.Unsensored = initInfo4Unit.InitUnsensored;
                        unitRecord.MarkNoLogic = initInfo4Unit.InitNoLogic;

                        record = unitRecord;
                        break;
                    }
                case EEntityType.InteractPoint:
                    {
                        var realRecord = new LogicEntityRecord4InteractPoint();
                        realRecord.Status = 0;

                        var initInfo4IP = (EntityInitInfo4InteractPoint) initInfo;
                        for(int i=0;i<initInfo4IP.Variables.keys.Count;i++)
                        {
                            realRecord.DynamicVariables.Add(initInfo4IP.Variables.keys[i], initInfo4IP.Variables.values[i]);
                        }
                        
                        record = realRecord;
                        break;
                    }
                case EEntityType.LootPoint:
                    {
                        var realRecord = new LogicEntityRecord4LootPoint();
                        record = realRecord;
                        break;
                    }
                case EEntityType.EventGroup:
                    {
                        var egRecord = new LogicEntityRecord4EventGroup();
                        var cfg = MapEventGroupCfgLoader.Get(initInfo.CfgId);
                        var initInfo4IP = (EntityInitInfo4InteractPoint)initInfo;
                        for (int i = 0; i < initInfo4IP.Variables.keys.Count; i++)
                        {
                            egRecord.DynamicVariables.Add(initInfo4IP.Variables.keys[i], initInfo4IP.Variables.values[i]);
                        }
                        record = egRecord;
                    }
                    break;
                case EEntityType.HomeFacility:
                    {
                        var realRecord = new LogicEntityRecord4HomeFacility();

                        var initInfo4HomeFacility = (EntityInitInfo4HomePlacement)initInfo;
                        realRecord.BindingFacilityId = initInfo4HomeFacility.BindingFacilityId;
                        record = realRecord;
                        break;
                    }
                case EEntityType.Teleporter:
                    {
                        var realRecord = new LogicEntityRecord4Teleporter();

                        var initInfo4Teleporter = (EntityInitInfo4Teleporter)initInfo;
                        realRecord.TargetMap = initInfo4Teleporter.TargetMapName;
                        realRecord.TargetNamedPoint = initInfo4Teleporter.TargetNamedPoint;
                        record = realRecord;
                        break;
                    }
                case EEntityType.SimpleBlock:
                    {
                        var realRecord = new LogicEntityRecord4SimpleBlock();

                        var initInfo4SimpleBlock = (EntityInitInfo4SimpleBlock)initInfo;
                        realRecord.SizeX = initInfo4SimpleBlock.SizeX;
                        realRecord.SizeY = initInfo4SimpleBlock.SizeY;
                        record = realRecord;
                        break;
                    }
                default:
                    {
                        record = new LogicEntityRecord();
                    }
                    break;
            }

            if (record != null)
            {
                record.Id = id;
                record.EntityType = initInfo.EntityType;
                record.CfgId = initInfo.CfgId;
                record.Position = initInfo.Position;
                record.BelongRoomId = initInfo.BindRoomId;
                record.FactionId = initInfo.OrgFactionId;
            }
            return record;
        }


        public bool IsRecordAlwaysActive(LogicEntityRecord rec)
        {
            switch (rec.EntityType)
            {
                case EEntityType.Player:
                case EEntityType.PatrolGroup:
                case EEntityType.HomeFacility:
                    {
                        return true;
                    }
                    break;
                case EEntityType.InteractPoint:
                    {
                        var cfg = MapInteractPointLoader.Get(rec.CfgId);
                        if(cfg != null && cfg.IsAlwaysActive)
                        {
                            return true;
                        }
                        return false;
                    }
            }
            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rec"></param>
        /// <returns></returns>
        public void RegisterEntityRecord(LogicEntityRecord rec, bool isCreate = false)
        {
            // ?????????
            Repo.RegisterRecord(rec);
            // ??? AOI
            UnitGridIndex.AddOrMove(rec.Id, rec.Position);

            // ?????????????
            if (IsRecordAlwaysActive(rec))
            {
                var ent = SpawnEntity(rec.Id);
                if (ent == null)
                {
                    Debug.LogError("RegisterEntityRecord create null ");
                }
                else
                {
                    // ????????
                    LongLived.Register(ent);

                    // ???????????? Active ?? Sleep
                    runtimeStates[rec.Id] = new OneEntityRuntimeState { Id = rec.Id, State = LogicLifeState.Active };
                }
            }

            if(isCreate)
            {
                NewCreateEntityMark.Add(rec.Id);
            }
        }



        private void UnregisterEntityRecord(long recId)
        {
            Repo.Records.TryGetValue(recId, out var rec);
            if (rec != null && IsRecordAlwaysActive(rec))
            {
                LongLived.Unregister(recId);
            }
            Repo.RemoveRecord(recId);
            UnitGridIndex.Remove(recId);
            runtimeStates.Remove(recId);

            if(Record2RefreshInfo.TryGetValue(recId, out var refreshId))
            {
                RefreshInfoRuntimes.TryGetValue(refreshId, out var refreshRuntime);
                if(refreshRuntime != null)
                {
                    refreshRuntime.EntityInstId = 0;
                    refreshRuntime.LastDestroyTime = LogicTime.time;
                    refreshRuntime.LastRemovalWasVisibilityCond = false;
                }

                Record2RefreshInfo.Remove(recId);
            }
        }
        private List<ILogicEntity> _cacheEntityList = new();
        public IEnumerable<ILogicEntity> FindEntityInRange(Vector2 pos, float radius)
        {
            UnitGridIndex.Query(pos, radius, queryBufInt);

            _cacheEntityList.Clear();
            foreach (var id in queryBufInt)
            {
                var entity = GetLogicEntiy(id);
                _cacheEntityList.Add(entity);
            }
            return _cacheEntityList;
        }
    }
}
