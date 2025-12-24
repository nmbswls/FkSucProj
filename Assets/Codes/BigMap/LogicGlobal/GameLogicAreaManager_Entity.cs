

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
        }

        public class LongLivedRegistry
        {
            private readonly Dictionary<long, ILogicEntity> _map = new();
            public void Register(ILogicEntity ent) => _map[ent.Id] = ent;
            public void Unregister(long id) => _map.Remove(id);
            public bool TryGet(long id, out ILogicEntity ent) => _map.TryGetValue(id, out ent);
            public IEnumerable<ILogicEntity> All => _map.Values;
        }


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
            if (RefreshInfo2Record.TryGetValue(refreshInfo.UniqId, out var recordId))
            {
                return;
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
                return;
            }

            RegisterEntityRecord(record);
            RefreshInfo2Record[refreshInfo.UniqId] = record.Id;
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
                        var cfg = MapEventGroupCfgLoader.Get(egRecord.CfgId);

                        foreach(var memberInfo in cfg.StaticGroupEntites)
                        {
                            int groupId = memberInfo.GroupId;

                            var mRecord = CreateEntityRecordFromInitInfo(memberInfo.InitInfo);
                            mRecord.Position = memberInfo.InitInfo.Position + egRecord.Position;

                            egRecord.MemberEntityMap.Add(groupId, mRecord.Id);

                            mRecord.LifeBindEntityId = id;
                            mRecord.Activated = false;

                            Repo.RegisterRecord(mRecord);
                        }
                    }
                    break;
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
    }
}
