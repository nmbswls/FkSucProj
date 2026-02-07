

using System.Collections.Generic;
using System.Drawing;
using Config;
using My.MapExport;
using UnityEngine;

namespace My.Map.Logic

{
    public partial class GameLogicAreaManager
    {

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
        }
        public Dictionary<int, SceneRefreshInfoRuntime> RefreshInfoRuntimes = new();
        public Dictionary<long, int> Record2RefreshInfo = new();

        private Dictionary<EEntityType, List<long>> Type2EntityList = new();

        public HashSet<long> NewCreateEntityMark = new();

        /// <summary>
        /// 检查刷新和消失
        /// </summary>
        /// <param name="dt"></param>
        public void CheckRefreshAppearAndDisappear(float dt)
        {
            if (EntityRefreshInfo.Count == 0)
            {
                return;
            }

            if (LogicTime.time < checkRefreshTimer + 1)
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
        /// 处理一次刷新
        /// </summary>
        /// <param name="refreshInfo"></param>
        public void HandleOneRefreshInfo(DynamicEntityRefreshInfo refreshInfo)
        {
            if (RefreshInfoRuntimes.TryGetValue(refreshInfo.StaticId, out var refreshRuntime))
            {
                if(!refreshInfo.WillRespawn)
                {
                    return;
                }

                if(LogicTime.time - refreshRuntime.LastDestroyTime < refreshInfo.RespawnInterval)
                {
                    return;
                }
            }

            // 检查条件
            if (refreshInfo.AppearCond != null && refreshInfo.AppearCond.Type != 0)
            {
                if (!logicManager.CheckCommonCond(refreshInfo.AppearCond))
                {
                    return;
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
            };

            Record2RefreshInfo[record.Id] = refreshInfo.StaticId;
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
                        // 初始化巡逻兵
                        foreach (var one in initInfo4PatrolGroup.GroupUnits)
                        {
                            var groupRecord = CreateEntityRecordFromInitInfo(one.InitInfo);
                            patrolGroupRecord.PatrolUnitIds.Add(groupRecord.Id);
                            Repo.RegisterRecord(groupRecord);
                        }
                        record = patrolGroupRecord;
                        break;
                    }
                case EEntityType.Npc:
                    {
                        var unitRecord = new LogicEntityRecord4Npc();

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
            // 交由仓库管理
            Repo.RegisterRecord(rec);
            // 注册到 AOI
            UnitGridIndex.AddOrMove(rec.Id, rec.Position);

            // 长生命周期对象
            if (IsRecordAlwaysActive(rec))
            {
                var ent = SpawnEntity(rec.Id);
                if (ent == null)
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
                }

                Record2RefreshInfo.Remove(recId);
            }
        }

        public List<ILogicEntity> FindEntityInRange(Vector2 pos, float radius)
        {
            UnitGridIndex.Query(pos, radius, queryBufInt);

            var ret = new List<ILogicEntity>();
            foreach (var id in queryBufInt)
            {
                var entity = GetLogicEntiy(id);
                ret.Add(entity);
            }
            return ret;
        }
    }
}
