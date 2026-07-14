

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using cfg.demo;
using Config;
using My.Config;
using My.Map;
using My.MapExport;
using Map.Logic.Events;
using UnityEngine;

namespace My.Map.Logic
{
    // 刷新槽位最后一次移除实例的原因（与 Record 生命周期、是否允许再生成配合使用）。
    public enum ERefreshSlotRemovalReason : byte
    {
        None = 0,
        // 因 Appear/Disappear 条件隐藏而移除；与 Destructive（玩法破坏）区分。
        VisibilityCondition = 1,
        // 破坏、拾取、击杀等玩法移除。
        Destructive = 2,
        // 剧情等永久清场；除非重置地图状态，否则不再生成。
        PermanentClear = 3,
    }

    public partial class GameLogicAreaManager
    {

        public class LogicEntityRepository
        {
            public readonly Dictionary<long, LogicEntityRecord> Records = new();
            // 已加载的运行时逻辑实体
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

            public ERefreshSlotRemovalReason LastRemovalReason;

            // 绑定的地图导出刷新项；减少 TryGetRefreshInfoByStaticId 的字典查询。
            public DynamicEntityRefreshInfo LinkedRefreshInfo;
        }
        public Dictionary<int, SceneRefreshInfoRuntime> RefreshInfoRuntimes = new();

        public Dictionary<long, int> Record2RefreshInfo = new();

        private Dictionary<EEntityType, List<long>> Type2EntityList = new();

        public HashSet<long> NewCreateEntityMark = new();

        /// <summary>
        /// 按时间片检查动态刷新的出现与消失。
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
        /// 检查对话等强制刷新列表中的条目（DialogForceStaticIds）。
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

        public void ForceCheckAllRefreshInfos()
        {
            HashSet<string> handledGroups = null;
            foreach(var refreshInfo in EntityRefreshInfo)
            {
                if (refreshInfo == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(refreshInfo.RefreshGroupKey))
                {
                    handledGroups ??= new HashSet<string>();
                    if (!handledGroups.Add(refreshInfo.RefreshGroupKey))
                    {
                        continue;
                    }
                }

                HandleOneRefreshInfo(refreshInfo);
            }
        }

        /// <summary>
        /// 处理单条动态实体刷新配置（创建、条件隐藏、重生间隔等）。
        /// </summary>
        /// <param name="refreshInfo"></param>
        public void HandleOneRefreshInfo(DynamicEntityRefreshInfo refreshInfo)
        {
            if (refreshInfo == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(refreshInfo.RefreshGroupKey))
            {
                HandleRefreshGroup(refreshInfo.RefreshGroupKey);
                return;
            }

            RefreshInfoRuntimes.TryGetValue(refreshInfo.StaticId, out var refreshRuntime);
            if (refreshRuntime != null)
            {
                refreshRuntime.LinkedRefreshInfo = refreshInfo;
            }

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
                        rtVis.LastRemovalReason = ERefreshSlotRemovalReason.VisibilityCondition;
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
                    if (refreshRuntime.LastRemovalReason == ERefreshSlotRemovalReason.PermanentClear)
                    {
                        return;
                    }

                    if (!HasDynamicVisibilityCond(refreshInfo) ||
                        refreshRuntime.LastRemovalReason != ERefreshSlotRemovalReason.VisibilityCondition)
                    {
                        return;
                    }
                }
            }

            // 非对话强制刷新时，再校验出现条件（强制项在 DialogForceStaticIds 分支中单独处理）
            // todo: 抽象统一条件入口
            if (_dungeonRuntime != null && !_dungeonRuntime.ShouldAllowSpawn(refreshInfo))
            {
                return;
            }

            if(!DialogForceStaticIds.Contains(refreshInfo.StaticId))
            {
                // 检查出现条件
                if (refreshInfo.AppearCond != null && refreshInfo.AppearCond.Type != ECommonCheckType.None)
                {
                    if (!logicManager.CheckCommonCond(refreshInfo.AppearCond))
                    {
                        return;
                    }
                }
            }

            TrySpawnRefreshInfo(refreshInfo, null, null, out _);
        }

        private bool TrySpawnRefreshInfo(
            DynamicEntityRefreshInfo refreshInfo,
            Vector2? overridePosition,
            Vector2? overrideFaceDir,
            out LogicEntityRecord record)
        {
            record = null;
            if (refreshInfo?.InitInfo == null)
            {
                return false;
            }

            if (refreshInfo.InitInfo.EntityType == EEntityType.FishingSpot &&
                string.IsNullOrEmpty(refreshInfo.UniqName))
            {
                Debug.LogError(
                    $"[FishingSpot] StaticId={refreshInfo.StaticId}: UniqName is empty. " +
                    "Fishing spots need a non-empty UniqName for save/state; fix the map export or refresh config.");
                return false;
            }

            record = CreateEntityRecordFromInitInfo(refreshInfo.InitInfo);

            if(record == null)
            {
                Debug.Log("HandleOneRefreshInfo err not good");
                return false;
            }

            if (overridePosition.HasValue)
            {
                record.Position = overridePosition.Value;
            }

            if (overrideFaceDir.HasValue && overrideFaceDir.Value.sqrMagnitude > 1e-8f)
            {
                record.FaceDir = overrideFaceDir.Value.normalized;
            }

            if (refreshInfo.DungeonNodeId >= 0)
            {
                record.BelongRoomId = refreshInfo.DungeonNodeId.ToString();
            }

            record.IsFixed = true;
            if(!string.IsNullOrEmpty(refreshInfo.UniqName))
            {
                record.SrcUniqName = refreshInfo.UniqName;
            }

            RegisterEntityRecord(record);
            RefreshInfoRuntimes[refreshInfo.StaticId] = new SceneRefreshInfoRuntime()
            {
                EntityInstId = record.Id,
                LastRespawnTime = LogicTime.time,
                LastRemovalReason = ERefreshSlotRemovalReason.None,
                LinkedRefreshInfo = refreshInfo,
            };

            Record2RefreshInfo[record.Id] = refreshInfo.StaticId;
            return true;
        }

        private void HandleRefreshGroup(string groupKey)
        {
            if (string.IsNullOrEmpty(groupKey))
            {
                return;
            }

            DynamicEntityRefreshInfo desired = null;
            DynamicEntityRefreshInfo currentInfo = null;
            SceneRefreshInfoRuntime currentRuntime = null;
            LogicEntityRecord currentRecord = null;

            for (int i = 0; i < EntityRefreshInfo.Count; i++)
            {
                var info = EntityRefreshInfo[i];
                if (info == null || info.RefreshGroupKey != groupKey)
                {
                    continue;
                }

                if (!RefreshInfoRuntimes.TryGetValue(info.StaticId, out var rt))
                {
                    rt = null;
                }
                else
                {
                    rt.LinkedRefreshInfo = info;
                }

                if (rt != null &&
                    rt.EntityInstId != 0 &&
                    Repo.Records.TryGetValue(rt.EntityInstId, out var rec) &&
                    !rec.MarkDestroyed)
                {
                    if (currentRecord == null)
                    {
                        currentInfo = info;
                        currentRuntime = rt;
                        currentRecord = rec;
                    }
                    else
                    {
                        ForceDestroyEntityNow(rt.EntityInstId, "RefreshGroupExtraActive");
                        if (RefreshInfoRuntimes.TryGetValue(info.StaticId, out var extraRt))
                        {
                            extraRt.LastRemovalReason = ERefreshSlotRemovalReason.VisibilityCondition;
                        }
                    }
                }

                if (!CanRefreshInfoBeDesiredInGroup(info, rt))
                {
                    continue;
                }

                if (desired == null ||
                    info.RefreshGroupPriority > desired.RefreshGroupPriority ||
                    (info.RefreshGroupPriority == desired.RefreshGroupPriority && info.StaticId > desired.StaticId))
                {
                    desired = info;
                }
            }

            if (currentInfo != null && desired != null && currentInfo.StaticId == desired.StaticId)
            {
                return;
            }

            Vector2? handoffPos = null;
            Vector2? handoffFace = null;
            long oldEntityId = currentRuntime?.EntityInstId ?? 0;

            if (currentRecord != null && desired != null &&
                desired.RefreshGroupHandoffPolicy == ERefreshGroupHandoffPolicy.PreserveTransform)
            {
                if (oldEntityId != 0 && Repo.IsLoaded(oldEntityId) && Repo.GetLoaded(oldEntityId) != null)
                {
                    var oldEntity = Repo.GetLoaded(oldEntityId);
                    handoffPos = oldEntity.Pos;
                }
                else
                {
                    handoffPos = currentRecord.Position;
                }

                handoffFace = currentRecord.FaceDir;
            }

            if (currentRuntime != null && oldEntityId != 0)
            {
                if (desired != null && desired.RefreshTransitionMode == ERefreshGroupTransitionMode.HiddenSwap)
                {
                    logicManager.LogicEventBus.Publish(new MLERefreshGroupSwapEvent()
                    {
                        GroupKey = groupKey,
                        OldEntityId = oldEntityId,
                    });
                }

                ForceDestroyEntityNow(oldEntityId, desired == null ? "RefreshGroupHide" : "RefreshGroupSwap");
                if (currentInfo != null && RefreshInfoRuntimes.TryGetValue(currentInfo.StaticId, out var oldRt))
                {
                    oldRt.LastRemovalReason = ERefreshSlotRemovalReason.VisibilityCondition;
                }
            }

            if (desired == null)
            {
                return;
            }

            if (TrySpawnRefreshInfo(desired, handoffPos, handoffFace, out var newRecord) &&
                oldEntityId != 0 &&
                desired.RefreshTransitionMode == ERefreshGroupTransitionMode.HiddenSwap)
            {
                logicManager.LogicEventBus.Publish(new MLERefreshGroupSwapEvent()
                {
                    GroupKey = groupKey,
                    OldEntityId = oldEntityId,
                    NewEntityId = newRecord.Id,
                });
            }
        }

        private bool CanRefreshInfoBeDesiredInGroup(DynamicEntityRefreshInfo refreshInfo, SceneRefreshInfoRuntime refreshRuntime)
        {
            if (refreshInfo == null)
            {
                return false;
            }

            if (_dungeonRuntime != null && !_dungeonRuntime.ShouldAllowSpawn(refreshInfo))
            {
                return false;
            }

            if (refreshInfo.AppearCond != null && refreshInfo.AppearCond.Type != ECommonCheckType.None)
            {
                if (!logicManager.CheckCommonCond(refreshInfo.AppearCond))
                {
                    return false;
                }
            }

            if (refreshInfo.DisappearCond != null && refreshInfo.DisappearCond.Type != ECommonCheckType.None)
            {
                if (logicManager.CheckCommonCond(refreshInfo.DisappearCond))
                {
                    return false;
                }
            }

            if (refreshRuntime != null)
            {
                if (refreshRuntime.LastRemovalReason == ERefreshSlotRemovalReason.PermanentClear)
                {
                    return false;
                }

                if (!refreshInfo.WillRespawn &&
                    refreshRuntime.LastRemovalReason == ERefreshSlotRemovalReason.Destructive)
                {
                    return false;
                }

                if (refreshInfo.WillRespawn &&
                    LogicTime.time - refreshRuntime.LastDestroyTime < refreshInfo.RespawnInterval)
                {
                    return false;
                }
            }

            return true;
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

        internal bool IsSavePointRefreshRuntime(int staticId, SceneRefreshInfoRuntime rt)
        {
            if (rt?.LinkedRefreshInfo != null)
            {
                return IsSavePointRefreshInfo(rt.LinkedRefreshInfo);
            }

            return TryGetRefreshInfoByStaticId(staticId, out var ri) && IsSavePointRefreshInfo(ri);
        }

        internal static bool IsFishingSpotRefreshInfo(DynamicEntityRefreshInfo ri)
        {
            return ri?.InitInfo != null && ri.InitInfo.EntityType == EEntityType.FishingSpot;
        }

        internal bool IsFishingSpotRefreshRuntime(int staticId, SceneRefreshInfoRuntime rt)
        {
            if (rt?.LinkedRefreshInfo != null)
            {
                return IsFishingSpotRefreshInfo(rt.LinkedRefreshInfo);
            }

            return TryGetRefreshInfoByStaticId(staticId, out var ri) && IsFishingSpotRefreshInfo(ri);
        }

        private void EnsureLinkedRefreshOnRuntime(int staticId, SceneRefreshInfoRuntime rt)
        {
            if (rt == null || rt.LinkedRefreshInfo != null)
            {
                return;
            }

            if (TryGetRefreshInfoByStaticId(staticId, out var def))
            {
                rt.LinkedRefreshInfo = def;
            }
        }

        private static ERefreshSlotRemovalReason SanitizePersistedRemovalReason(int raw)
        {
            if (raw < (int)ERefreshSlotRemovalReason.None || raw > (int)ERefreshSlotRemovalReason.PermanentClear)
            {
                return ERefreshSlotRemovalReason.None;
            }

            return (ERefreshSlotRemovalReason)raw;
        }

        /// <summary>
        /// 将指定 StaticId 的刷新槽位标为永久清场（之后 HandleOneRefreshInfo 不再创建实例）。若场上仍有实体请先自行销毁。
        /// </summary>
        public void MarkRefreshSlotPermanentClear(int staticId)
        {
            if (!RefreshInfoRuntimes.TryGetValue(staticId, out var rt))
            {
                rt = new SceneRefreshInfoRuntime();
                RefreshInfoRuntimes[staticId] = rt;
            }

            EnsureLinkedRefreshOnRuntime(staticId, rt);
            rt.LastRemovalReason = ERefreshSlotRemovalReason.PermanentClear;
            rt.LastDestroyTime = LogicTime.time;
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


        public LogicEntityRecord CreateEntityRecordFromInitInfo(EntityInitInfo initInfo, long groupId = 0)
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
                        patrolGroupRecord.PatrolPortalNetworkId = initInfo4PatrolGroup.PatrolPortalNetworkId ?? string.Empty;
                        if (initInfo4PatrolGroup.PatrolCycleNodeIds != null && initInfo4PatrolGroup.PatrolCycleNodeIds.Count > 0)
                        {
                            patrolGroupRecord.PatrolCycleNodeIds = new List<string>(initInfo4PatrolGroup.PatrolCycleNodeIds);
                        }

                        patrolGroupRecord.WayPointList.AddRange(initInfo4PatrolGroup.Waypoints);

                        // 初始化巡逻组各单位 Record
                        foreach (var one in initInfo4PatrolGroup.GroupUnits)
                        {
                            var groupRecord = CreateEntityRecordFromInitInfo(one.InitInfo);
                            if(groupRecord == null)
                            {
                                Debug.Log("PatrolGroup group unit create fail " + one.GroupIdx);
                                continue;
                            }

                            groupRecord.Position = initInfo.Position + one.InitInfo.Position;

                            patrolGroupRecord.PatrolUnitIds.Add(groupRecord.Id);
                            RegisterEntityRecord(groupRecord);

                            if(groupRecord is LogicEntityRecord4Npc rec4Npc)
                            {
                                rec4Npc.PatrolFollowId = id;
                                rec4Npc.PatrolGroupRelativePos = one.InitInfo.Position;
                            }

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
                        unitRecord.CharacterKey = initInfo4Unit.CharacterKey ?? string.Empty;

                        if (initInfo is EntityInitInfo4Npc npcMore)
                        {
                            unitRecord.PatrolPortalNetworkId = npcMore.PatrolPortalNetworkId ?? string.Empty;
                            if (npcMore.PatrolCycleNodeIds != null && npcMore.PatrolCycleNodeIds.Count > 0)
                            {
                                unitRecord.PatrolCycleNodeIds = new List<string>(npcMore.PatrolCycleNodeIds);
                            }
                        }
                        record = unitRecord;
                        break;
                    }
                case EEntityType.InteractPoint:
                    {
                        var realRecord = new LogicEntityRecord4InteractPoint();
                        realRecord.Status = 0;

                        var initInfo4IP = (EntityInitInfo4InteractPoint)initInfo;
                        initInfo4IP.CopyVariablesTo(realRecord.DynamicVariables);

                        record = realRecord;
                        break;
                    }
                case EEntityType.RemovableObstacle:
                    {
                        var realRecord = new LogicEntityRecord4InteractPoint();
                        realRecord.Status = 0;

                        var initInfo4RO = (EntityInitInfo4RemovableObstacle)initInfo;
                        initInfo4RO.CopyVariablesTo(realRecord.DynamicVariables);

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
                        initInfo4IP.CopyVariablesTo(egRecord.DynamicVariables);
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
                case EEntityType.FishingSpot:
                    {
                        record = new LogicEntityRecord4FishingSpot();
                        break;
                    }
                case EEntityType.Trap:
                    {
                        record = new LogicEntityRecord4Trap();
                        break;
                    }
                case EEntityType.SavePoint:
                    {
                        record = new LogicEntityRecord();
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
                if (initInfo.FaceDir.sqrMagnitude > 1e-8f)
                {
                    record.FaceDir = initInfo.FaceDir.normalized;
                }
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
        /// 注册实体 Record 并加入 AOI；AlwaysActive 类型会立即生成逻辑实体。
        /// </summary>
        /// <param name="rec"></param>
        /// <returns></returns>
        public void RegisterEntityRecord(LogicEntityRecord rec, bool isCreate = false)
        {
            if (rec is LogicEntityRecord4Npc npcRec)
            {
                DesireDensitySpawnLogic.ApplyOnNpcRecord(npcRec);
            }

            // 交由仓库管理
            Repo.RegisterRecord(rec);
            Summons.Register(rec, rec.SummonMaxAlivePerOwner);
            // 注册到 AOI 网格
            UnitGridIndex.AddOrMove(rec.Id, rec.Position);

            // 长生命周期实体：始终加载
            if (IsRecordAlwaysActive(rec))
            {
                var ent = SpawnEntity(rec.Id);
                if (ent == null)
                {
                    Debug.LogError("RegisterEntityRecord create null ");
                }
                else
                {
                    // 特殊长生命周期容器
                    LongLived.Register(ent);

                    // 初始生命周期状态：Active（后续可由 Sleep 切换）
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
            Summons.Unregister(recId);
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
                    refreshRuntime.LastRemovalReason = ERefreshSlotRemovalReason.Destructive;
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
