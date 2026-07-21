using Config.Unit;
using Config;
using UnityEngine;
using Config.Map;
using System.Collections.Generic;
using System;
using Map.Logic;
using My.Map.Logic;
using static My.GameLogicManager;
using System.Linq;
using System.Security.Principal;
using My.Map.Entity;
using My.Player;
using Unity.VisualScripting;


namespace My.Map
{
    public class EventGroupLogicEntity : LogicEntityInteractPoint
    {
        public enum ETerminalState
        {
            Active,
            Completed,
            Expired,
            Failed,
        }

        public MapEventGroupConfig CacheEventGroupCfg { get { return (MapEventGroupConfig)cacheCfg; } }


        public EventGroupLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var groupRecord = (LogicEntityRecord4EventGroup)BindingRecord;
            foreach (var pair in groupRecord.MemberId2EntityMap) 
            {
                MemberId2EntityMap[pair.Key] = pair.Value;
            }

            foreach(var id in groupRecord.CurrActiveMembers)
            {
                CurrActiveMemberSet.Add(id);
            }

            if (groupRecord.MemberStatusById != null)
            {
                MemberStatusById.AddRange(groupRecord.MemberStatusById);
            }
            if (groupRecord.DefeatedMemberIds != null)
            {
                DefeatedMemberSet.AddRange(groupRecord.DefeatedMemberIds);
            }
            if (groupRecord.TriggerTimesById != null)
            {
                TriggerTimesById.AddRange(groupRecord.TriggerTimesById);
            }
            if (groupRecord.TriggerLastFireTimeById != null)
            {
                TriggerLastFireTimeById.AddRange(groupRecord.TriggerLastFireTimeById);
            }
            TerminalState = (ETerminalState)groupRecord.TerminalState;
            OutcomeClaimed = groupRecord.OutcomeClaimed;
        }

        protected override void LoadCfg()
        {
            cacheCfg = MapEventGroupCfgLoader.Get(CfgId);
        }


        ///// <summary>
        ///// 缓存
        ///// 危险 logicentity可能被创建为另一个
        ///// </summary>
        //public Dictionary<int, ILogicEntity> GroupMemberDict = new();

        private float _lastCheckTimer;

        public Dictionary<int, long> MemberId2EntityMap = new();
        protected HashSet<int> CurrActiveMemberSet = new();
        protected List<int> _tmpMemberList = new();
        readonly Dictionary<int, int> MemberStatusById = new();
        readonly HashSet<int> DefeatedMemberSet = new();
        readonly Dictionary<int, Action<MapInteractPointConfig.StateChangeView>> _memberStatusHandlers = new();
        readonly Dictionary<int, Action<long>> _memberDefeatHandlers = new();
        readonly Dictionary<int, Action<long>> _memberEnmityHandlers = new();
        readonly Queue<int> _pendingTriggerIds = new();
        readonly HashSet<int> _queuedOneShotTriggers = new();
        readonly Dictionary<int, int> TriggerTimesById = new();
        readonly Dictionary<int, float> TriggerLastFireTimeById = new();
        int _pendingCompletionStageId = int.MinValue;
        int _pendingTriggerId = int.MinValue;
        float _nextCompletionRetryTime;

        public ETerminalState TerminalState { get; private set; }
        public bool OutcomeClaimed { get; private set; }

        bool UsesStageFlow => CacheEventGroupCfg?.Stages != null && CacheEventGroupCfg.Stages.Count > 0;
        /// <summary>
        /// 存储各触发器
        /// </summary>
        public class GroupInnerTriggerState
        {
            public int TriggerId = 0;
            public int TriggerTimes = 0;
            public float LastTriggerTime = 0;

            public MapEventGroupConfig.GroupInnerTrigger TriggerCfg;
        }
        protected Dictionary<int, GroupInnerTriggerState> InnerTriggers  = new();


        public override EEntityType Type => EEntityType.EventGroup;

        public override void Initialize()
        {
            base.Initialize();
            InteractComp.EventOnInteractEnded += OnGroupInteractEnded;

            // 初始化内部触发器
            foreach(var eventTrigger in CacheEventGroupCfg.InnerTriggers)
            {
                var info = new GroupInnerTriggerState()
                {
                    TriggerId = eventTrigger.TriggerId,
                };
                info.TriggerCfg = eventTrigger;

                InnerTriggers.Add(info.TriggerId, info);
            }

            EnsureStageEntities();
            BindCurrentStageMemberEvents();
            if (UsesStageFlow)
            {
                QueueStageEnteredTriggers(CurrStatusId, initialEntry: true);
            }
            EvaluateStageCompletion();
        }
        private float _lastEnsureMemberTimer = 0;

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            if (UsesStageFlow)
            {
                ProcessPendingTriggers();
                EvaluateStageCompletion();
            }
            else
            {
                TickAllMemberStatus();
            }

            do
            {
                if(LogicTime.time - _lastEnsureMemberTimer < 1.0f)
                {
                    break;
                }

                _lastEnsureMemberTimer = LogicTime.time;

                EnsureStageEntities();

            } while (false);
        }

        /// <summary>
        /// 确保当前阶段的member都存在且都刷新为entity
        /// 对于子entity 只有在group实例化后才会实例化
        ///              且只要group实例化 子对象保证实例化
        /// </summary>
        protected void EnsureStageEntities()
        {
            var ensureMemberIds = GetCurrentEnsureMemberIds();
            if (ensureMemberIds == null)
            {
                return;
            }


            _tmpMemberList.Clear();
            foreach (var mId in CurrActiveMemberSet)
            {
                if (!ensureMemberIds.Contains(mId))
                {
                    UnbindMemberEvents(mId);
                    MemberId2EntityMap.TryGetValue(mId, out var entityId);
                    if (entityId != 0)
                    {
                        var entity = LogicManager.GetLogicEntity(entityId, false) as LogicEntityBase;
                        entity?.DoEntityDestroyed("event_group_remove");
                        _tmpMemberList.Add(mId);
                    }
                }
            }

            foreach(var id in _tmpMemberList)
            {
                CurrActiveMemberSet.Remove(id);
                MemberId2EntityMap.Remove(id);
            }

            foreach (var mId in ensureMemberIds)
            {
                var mInfo = CacheEventGroupCfg.GroupMemberInfos.Find(item => item.MemberId == mId);
                if (mInfo == null) continue;

                if (MemberId2EntityMap.TryGetValue(mId, out var existingEntityId)
                    && (!LogicManager.AreaManager.Repo.Records.TryGetValue(existingEntityId, out var existingRecord)
                        || existingRecord.MarkDestroyed))
                {
                    UnbindMemberEvents(mId);
                    MemberId2EntityMap.Remove(mId);
                    CurrActiveMemberSet.Remove(mId);
                }

                // A defeated stage member stays defeated even if its corpse record was cleaned up.
                if (DefeatedMemberSet.Contains(mId) && !MemberId2EntityMap.ContainsKey(mId))
                {
                    continue;
                }

                if (!MemberId2EntityMap.ContainsKey(mId))
                {
                    var record = LogicManager.AreaManager.CreateEntityRecordFromInitInfo(mInfo.InitInfo);
                    if(record == null)
                    {
                        Debug.Log($"event group:{Id} create member:{mId} cfgId:{mInfo.InitInfo.CfgId} fail.");
                        continue;
                    }

                    record.LifeBindEntityId = this.Id;
                    record.Position = ResolveMemberPosition(mInfo);
                    if (record is LogicEntityRecord4InteractPoint interactRecord
                        && MemberStatusById.TryGetValue(mId, out var savedStatus))
                    {
                        interactRecord.Status = savedStatus;
                    }
                    MemberId2EntityMap[mId] = record.Id;
                    LogicManager.AddNewEntityRecord(record);

                    Debug.Log($"event group:{Id} create member:{mId} entity:{record.Id}");
                    CurrActiveMemberSet.Add(mId);
                }

                // 强制激活一次
                LogicManager.GetLogicEntity(MemberId2EntityMap[mId]);
                BindMemberEvents(mId);
            }

        }

        List<int> GetCurrentEnsureMemberIds()
        {
            if (UsesStageFlow)
            {
                return CacheEventGroupCfg.Stages.Find(item => item.StageId == CurrStatusId)?.EnsureMemberIds;
            }

            return CacheEventGroupCfg.EventGroupStateInfos
                .Find(item => item.StateId == CurrStatusId)?.EnsureMemberIds;
        }

        Vector2 ResolveMemberPosition(MapEventGroupConfig.MemberInfo member)
        {
            if (member.PlacementMode == MapEventGroupConfig.MemberInfo.EPlacementMode.NamedPoint
                && !string.IsNullOrEmpty(member.NamedPointName))
            {
                var point = LogicManager.AreaManager.cacheDatabase.FindNamedPointByName(member.NamedPointName);
                if (point != null)
                {
                    return point.Value.Position;
                }

                Debug.LogWarning($"EventGroup {CfgId} member {member.MemberId} missing named point {member.NamedPointName}.");
            }

            return Pos + member.InitInfo.Position;
        }

        void BindCurrentStageMemberEvents()
        {
            foreach (var memberId in CurrActiveMemberSet)
            {
                BindMemberEvents(memberId);
            }
        }

        void BindMemberEvents(int memberId)
        {
            if (!MemberId2EntityMap.TryGetValue(memberId, out var entityId))
            {
                return;
            }

            var entity = LogicManager.GetLogicEntity(entityId, false);
            if (entity is LogicEntityInteractPoint interactPoint)
            {
                MemberStatusById[memberId] = interactPoint.CurrStatusId;
                if (!_memberStatusHandlers.ContainsKey(memberId))
                {
                    Action<MapInteractPointConfig.StateChangeView> handler = _ =>
                    {
                        MemberStatusById[memberId] = interactPoint.CurrStatusId;
                        if (UsesStageFlow)
                        {
                            QueueMemberTriggers(
                                MapEventGroupConfig.GroupInnerTrigger.ETriggerType.MemberStatusChanged,
                                memberId,
                                interactPoint.CurrStatusId);
                        }
                    };
                    _memberStatusHandlers[memberId] = handler;
                    interactPoint.EventOnStatusChange += handler;
                }
            }

            if (entity is BaseUnitLogicEntity unit)
            {
                if (unit.IsDead)
                {
                    DefeatedMemberSet.Add(memberId);
                }

                if (!_memberDefeatHandlers.ContainsKey(memberId))
                {
                    Action<long> handler = _ =>
                    {
                        DefeatedMemberSet.Add(memberId);
                        if (UsesStageFlow)
                        {
                            QueueMemberTriggers(
                                MapEventGroupConfig.GroupInnerTrigger.ETriggerType.MemberDefeated,
                                memberId,
                                0);
                        }
                    };
                    _memberDefeatHandlers[memberId] = handler;
                    unit.EventOnDie += handler;
                }

                if (UsesStageFlow && !_memberEnmityHandlers.ContainsKey(memberId))
                {
                    Action<long> handler = _ => QueueMemberTriggers(
                        MapEventGroupConfig.GroupInnerTrigger.ETriggerType.AnyEnmity,
                        memberId,
                        0);
                    _memberEnmityHandlers[memberId] = handler;
                    unit.EventOnEnmityBehave += handler;
                }
            }
        }

        void UnbindMemberEvents(int memberId)
        {
            if (!MemberId2EntityMap.TryGetValue(memberId, out var entityId))
            {
                _memberStatusHandlers.Remove(memberId);
                _memberDefeatHandlers.Remove(memberId);
                return;
            }

            var entity = LogicManager.GetLogicEntity(entityId, false);
            if (entity is LogicEntityInteractPoint interactPoint
                && _memberStatusHandlers.TryGetValue(memberId, out var statusHandler))
            {
                interactPoint.EventOnStatusChange -= statusHandler;
            }

            if (entity is BaseUnitLogicEntity unit
                && _memberDefeatHandlers.TryGetValue(memberId, out var defeatHandler))
            {
                unit.EventOnDie -= defeatHandler;
            }

            if (entity is BaseUnitLogicEntity enmityUnit
                && _memberEnmityHandlers.TryGetValue(memberId, out var enmityHandler))
            {
                enmityUnit.EventOnEnmityBehave -= enmityHandler;
            }

            _memberStatusHandlers.Remove(memberId);
            _memberDefeatHandlers.Remove(memberId);
            _memberEnmityHandlers.Remove(memberId);
        }

        IEnumerable<int> GetCurrentStageTriggerIds()
        {
            if (!UsesStageFlow)
            {
                return Array.Empty<int>();
            }

            var stage = CacheEventGroupCfg.Stages.Find(item => item.StageId == CurrStatusId);
            return stage?.ActiveTriggerIds ?? (IEnumerable<int>)Array.Empty<int>();
        }

        IEnumerable<MapEventGroupConfig.GroupInnerTrigger> GetCurrentStageTriggers()
        {
            var activeIds = new HashSet<int>(GetCurrentStageTriggerIds());
            foreach (var trigger in CacheEventGroupCfg.InnerTriggers)
            {
                if (trigger != null && activeIds.Contains(trigger.TriggerId))
                {
                    yield return trigger;
                }
            }
        }

        List<int> ResolveTriggerMemberIds(MapEventGroupConfig.GroupInnerTrigger trigger)
        {
            if (trigger.MemberIds != null && trigger.MemberIds.Count > 0)
            {
                return trigger.MemberIds;
            }

            var result = new List<int>();
            if (!string.IsNullOrEmpty(trigger.MemberTag))
            {
                foreach (var member in CacheEventGroupCfg.GroupMemberInfos)
                {
                    if (member.Tags != null && member.Tags.Contains(trigger.MemberTag))
                    {
                        result.Add(member.MemberId);
                    }
                }
            }
            return result;
        }

        bool TriggerMatchesMember(MapEventGroupConfig.GroupInnerTrigger trigger, int memberId, int statusId)
        {
            var memberIds = ResolveTriggerMemberIds(trigger);
            if (memberIds.Count > 0 && !memberIds.Contains(memberId))
            {
                return false;
            }

            return trigger.TriggerType != MapEventGroupConfig.GroupInnerTrigger.ETriggerType.MemberStatusChanged
                || statusId == trigger.RequiredStatusId;
        }

        bool CanQueueTrigger(MapEventGroupConfig.GroupInnerTrigger trigger)
        {
            var times = TriggerTimesById.TryGetValue(trigger.TriggerId, out var count) ? count : 0;
            if (trigger.FirePolicy == MapEventGroupConfig.GroupInnerTrigger.EFirePolicy.Once && times > 0)
            {
                return false;
            }

            if (trigger.MaxTriggerCnt > 0 && times >= trigger.MaxTriggerCnt)
            {
                return false;
            }

            if (trigger.FirePolicy == MapEventGroupConfig.GroupInnerTrigger.EFirePolicy.Cooldown
                && TriggerLastFireTimeById.TryGetValue(trigger.TriggerId, out var lastTime)
                && LogicTime.time - lastTime < Mathf.Max(0, trigger.MinTriggerInterval))
            {
                return false;
            }

            return trigger.FirePolicy == MapEventGroupConfig.GroupInnerTrigger.EFirePolicy.EveryOccurrence
                || !_queuedOneShotTriggers.Contains(trigger.TriggerId);
        }

        void QueueMemberTriggers(
            MapEventGroupConfig.GroupInnerTrigger.ETriggerType eventType,
            int memberId,
            int statusId)
        {
            if (!UsesStageFlow || TerminalState != ETerminalState.Active)
            {
                return;
            }

            foreach (var trigger in GetCurrentStageTriggers())
            {
                if (trigger.TriggerType != eventType
                    || !TriggerMatchesMember(trigger, memberId, statusId)
                    || !CanQueueTrigger(trigger))
                {
                    continue;
                }

                _pendingTriggerIds.Enqueue(trigger.TriggerId);
                if (trigger.FirePolicy != MapEventGroupConfig.GroupInnerTrigger.EFirePolicy.EveryOccurrence)
                {
                    _queuedOneShotTriggers.Add(trigger.TriggerId);
                }
            }
        }

        void QueueStageEnteredTriggers(int stageId, bool initialEntry = false)
        {
            if (!UsesStageFlow || TerminalState != ETerminalState.Active || stageId != CurrStatusId)
            {
                return;
            }

            foreach (var trigger in GetCurrentStageTriggers())
            {
                if (trigger.TriggerType != MapEventGroupConfig.GroupInnerTrigger.ETriggerType.StageEntered
                    || (initialEntry && trigger.FirePolicy != MapEventGroupConfig.GroupInnerTrigger.EFirePolicy.Once)
                    || !CanQueueTrigger(trigger))
                {
                    continue;
                }

                _pendingTriggerIds.Enqueue(trigger.TriggerId);
                if (trigger.FirePolicy != MapEventGroupConfig.GroupInnerTrigger.EFirePolicy.EveryOccurrence)
                {
                    _queuedOneShotTriggers.Add(trigger.TriggerId);
                }
            }
        }

        void ProcessPendingTriggers()
        {
            if (!UsesStageFlow
                || _pendingTriggerId != int.MinValue
                || _pendingTriggerIds.Count == 0
                || InteractComp == null
                || InteractComp.IsInteracting)
            {
                return;
            }

            var triggerId = _pendingTriggerIds.Dequeue();
            _queuedOneShotTriggers.Remove(triggerId);
            var trigger = CacheEventGroupCfg.InnerTriggers.Find(item => item.TriggerId == triggerId);
            if (trigger == null
                || !GetCurrentStageTriggerIds().Contains(triggerId)
                || trigger.ActionInteractId <= 0
                || !CanQueueTrigger(trigger))
            {
                return;
            }

            _pendingTriggerId = triggerId;
            if (!InteractComp.TryTriggerInteract(trigger.ActionInteractId, GamePlayerIds.Local))
            {
                _pendingTriggerId = int.MinValue;
            }
        }

        List<int> ResolveConditionMemberIds(MapEventGroupConfig.StageCondition condition)
        {
            if (condition.MemberIds != null && condition.MemberIds.Count > 0)
            {
                return condition.MemberIds;
            }

            var result = new List<int>();
            if (string.IsNullOrEmpty(condition.MemberTag))
            {
                return result;
            }

            foreach (var member in CacheEventGroupCfg.GroupMemberInfos)
            {
                if (member.Tags != null && member.Tags.Contains(condition.MemberTag))
                {
                    result.Add(member.MemberId);
                }
            }

            return result;
        }

        bool CheckStageCondition(MapEventGroupConfig.StageCondition condition)
        {
            var memberIds = ResolveConditionMemberIds(condition);
            if (memberIds.Count == 0)
            {
                return false;
            }

            foreach (var memberId in memberIds)
            {
                if (condition.ConditionType == MapEventGroupConfig.EStageConditionType.AllMembersInteractStatus)
                {
                    if (!MemberStatusById.TryGetValue(memberId, out var statusId)
                        || statusId != condition.RequiredStatusId)
                    {
                        return false;
                    }
                }
                else if (condition.ConditionType == MapEventGroupConfig.EStageConditionType.AllMembersDefeated)
                {
                    if (!IsMemberDefeated(memberId))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        bool IsMemberDefeated(int memberId)
        {
            if (DefeatedMemberSet.Contains(memberId))
            {
                return true;
            }

            if (!MemberId2EntityMap.TryGetValue(memberId, out var entityId))
            {
                return false;
            }

            if (LogicManager.GetLogicEntity(entityId, false) is BaseUnitLogicEntity unit && unit.IsDead)
            {
                DefeatedMemberSet.Add(memberId);
                return true;
            }

            if (LogicManager.AreaManager.Repo.Records.TryGetValue(entityId, out var record)
                && record is LogicEntityRecord4UnitBase unitRecord
                && unitRecord.MarkDefeated)
            {
                DefeatedMemberSet.Add(memberId);
                return true;
            }

            return false;
        }

        void EvaluateStageCompletion()
        {
            if (!UsesStageFlow
                || TerminalState != ETerminalState.Active
                || _pendingCompletionStageId != int.MinValue
                || LogicTime.time < _nextCompletionRetryTime
                || InteractComp == null
                || InteractComp.IsInteracting)
            {
                return;
            }

            var stage = CacheEventGroupCfg.Stages.Find(item => item.StageId == CurrStatusId);
            if (stage?.CompletionConditions == null || stage.CompletionConditions.Count == 0)
            {
                return;
            }

            var satisfied = stage.ConditionMode == MapEventGroupConfig.EConditionMode.All;
            foreach (var condition in stage.CompletionConditions)
            {
                var conditionMet = CheckStageCondition(condition);
                if (stage.ConditionMode == MapEventGroupConfig.EConditionMode.All)
                {
                    satisfied &= conditionMet;
                }
                else
                {
                    satisfied |= conditionMet;
                }
            }

            if (!satisfied)
            {
                return;
            }

            if (stage.CompleteInteractId > 0)
            {
                _pendingCompletionStageId = stage.StageId;
                if (!InteractComp.TryTriggerInteract(stage.CompleteInteractId, GamePlayerIds.Local))
                {
                    _pendingCompletionStageId = int.MinValue;
                }
                return;
            }

            CommitStageCompletion(stage);
        }

        void OnGroupInteractEnded(int interactId, bool succeeded)
        {
            if (_pendingTriggerId != int.MinValue)
            {
                var trigger = CacheEventGroupCfg.InnerTriggers.Find(item => item.TriggerId == _pendingTriggerId);
                if (trigger != null && trigger.ActionInteractId == interactId)
                {
                    var triggerId = _pendingTriggerId;
                    _pendingTriggerId = int.MinValue;
                    if (succeeded)
                    {
                        TriggerTimesById[triggerId] = TriggerTimesById.TryGetValue(triggerId, out var count)
                            ? count + 1
                            : 1;
                        TriggerLastFireTimeById[triggerId] = LogicTime.time;
                    }
                    return;
                }
            }

            if (_pendingCompletionStageId == int.MinValue)
            {
                return;
            }

            var stage = CacheEventGroupCfg.Stages.Find(item => item.StageId == _pendingCompletionStageId);
            if (stage == null || stage.CompleteInteractId != interactId)
            {
                return;
            }

            _pendingCompletionStageId = int.MinValue;
            if (succeeded && CurrStatusId == stage.StageId)
            {
                CommitStageCompletion(stage);
            }
            else if (!succeeded)
            {
                _nextCompletionRetryTime = LogicTime.time + 5f;
            }
        }

        void CommitStageCompletion(MapEventGroupConfig.StageInfo stage)
        {
            if (stage.CompleteEvent)
            {
                OutcomeClaimed = true;
                TerminalState = ETerminalState.Completed;
                if (stage.DestroyOnComplete)
                {
                    DoEntityDestroyed("event_group_completed");
                }
                return;
            }

            if (stage.NextStageId >= 0)
            {
                TryAdvanceToStage(stage.NextStageId);
            }
        }

        public bool TryAdvanceToStage(int stageId)
        {
            if (!UsesStageFlow
                || TerminalState != ETerminalState.Active
                || CacheEventGroupCfg.Stages.Find(item => item.StageId == stageId) == null)
            {
                return false;
            }

            _pendingCompletionStageId = int.MinValue;
            ChangeSelfStatus(stageId);
            return true;
        }

        public override void DoEntityDestroyed(string reason)
        {
            foreach (var memberId in CurrActiveMemberSet.ToList())
            {
                UnbindMemberEvents(memberId);
                if (!MemberId2EntityMap.TryGetValue(memberId, out var entityId))
                {
                    continue;
                }

                var member = LogicManager.GetLogicEntity(entityId, false) as LogicEntityBase;
                if (member != null && !member.MarkDestroyed)
                {
                    member.DoEntityDestroyed($"event_group_owner_{reason}");
                }
                else if (LogicManager.AreaManager.Repo.Records.TryGetValue(entityId, out var record))
                {
                    record.MarkDestroyed = true;
                }
            }

            base.DoEntityDestroyed(reason);
        }

        /// <summary>
        /// 监控每个成员
        /// </summary>
        public void TickAllMemberStatus()
        {
            if(LogicTime.time < _lastCheckTimer)
            {
                return;
            }

            _lastCheckTimer = LogicTime.time + 1f;


            var stateExtraInfo = CacheEventGroupCfg.EventGroupStateInfos.Find(item => item.StateId == CurrStatusId);
            if (stateExtraInfo == null)
            {
                return;
            }

            foreach (var triggerId in stateExtraInfo.ActiveTriggerIds)
            {
                InnerTriggers.TryGetValue(triggerId, out var state);
                if (state == null)
                {
                    continue;
                }

                if (state.TriggerCfg.MaxTriggerCnt != 0 && state.TriggerTimes >= state.TriggerCfg.MaxTriggerCnt)
                {
                    continue;
                }

                if (state.TriggerCfg.TriggerType == MapEventGroupConfig.GroupInnerTrigger.ETriggerType.MemberCleared)
                {
                    var idStrs = state.TriggerCfg.Param3.Split(",");
                    bool allCleared = true;
                    foreach (var idStr in idStrs)
                    {
                        int.TryParse(idStr, out var memberId);
                        if (memberId == 0) continue;

                        MemberId2EntityMap.TryGetValue(memberId, out var entityId);
                        if (entityId == 0)
                        {
                            Debug.LogError($"Entity not exist for member {memberId}");
                            allCleared = false;
                            break;
                        }

                        var realLogic = LogicManager.GetLogicEntity(entityId, false);
                        if (realLogic == null || realLogic is not BaseUnitLogicEntity unitEntity)
                        {
                            allCleared = false;
                            Debug.LogError($"entity not exist");
                            break;
                        }

                        if(!unitEntity.IsDead)
                        {
                            allCleared = false;
                        }
                    }

                    if (!allCleared)
                    {
                        continue;
                    };
                }
                else if(state.TriggerCfg.TriggerType == MapEventGroupConfig.GroupInnerTrigger.ETriggerType.GroupInteractableStatus)
                {
                    int memberId = (int)state.TriggerCfg.Param1;
                    int stateId = (int)state.TriggerCfg.Param2;

                    MemberId2EntityMap.TryGetValue(memberId, out var entityId);
                    if (entityId == 0)
                    {
                        Debug.LogError($"Entity not exist for member {memberId}");
                        continue;
                    }

                    var realLogic = LogicManager.GetLogicEntity(entityId, false);
                    if (realLogic == null || realLogic is not LogicEntityInteractPoint intObj)
                    {
                        continue;
                    }

                    if(intObj.CurrStatusId != stateId)
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }

                Debug.Log($"TickAllMemberStatus trigger something triggerId:{state.TriggerId} {state.TriggerCfg.TriggerType}");

                int interactId = (int)state.TriggerCfg.Param2;

                var success = InteractComp.TryTriggerInteract(interactId, GamePlayerIds.Local);
                if(!success)
                {
                    continue;
                }

                state.TriggerTimes += 1;
            }
        }

        /// <summary>
        /// 监听触发事件
        /// </summary>
        protected override void OnStatusChange(int preStage)
        {
            if (UsesStageFlow)
            {
                EnsureStageEntities();
                BindCurrentStageMemberEvents();
                QueueStageEnteredTriggers(CurrStatusId);
                EvaluateStageCompletion();
                return;
            }

            var stateExtraInfo = CacheEventGroupCfg.EventGroupStateInfos.Find(item => item.StateId == CurrStatusId);
            if (stateExtraInfo == null) 
            {
                return;
            }

            // 确保entity正常
            EnsureStageEntities();

            foreach (var triggerId in stateExtraInfo.ActiveTriggerIds)
            {
                InnerTriggers.TryGetValue(triggerId, out var state);
                if(state == null)
                {
                    continue;
                }

                if(state.TriggerCfg.TriggerType != MapEventGroupConfig.GroupInnerTrigger.ETriggerType.SelfStatus)
                {
                    continue;
                }

                int needState = (int)state.TriggerCfg.Param1;
                if(CurrStatusId != needState)
                {
                    continue;
                }

                if(state.TriggerCfg.MaxTriggerCnt != 0 && state.TriggerTimes >= state.TriggerCfg.MaxTriggerCnt)
                {
                    continue;
                }

                int interactId = (int)state.TriggerCfg.Param2;
                bool success = InteractComp.TryTriggerInteract(interactId, GamePlayerIds.Local);
                if(!success)
                {
                    Debug.LogError("change state trigger can not be blocked");
                    continue;
                }
            }

        }


        /// <summary>
        /// 激活沉睡成员
        /// </summary>
        public void ActivateSleepyMembers()
        {
            foreach(var mId in CurrActiveMemberSet)
            {
                MemberId2EntityMap.TryGetValue(mId, out var entityId);
                if(entityId == 0)
                {
                    continue;
                }

                var entity = LogicManager.GetLogicEntity(entityId);
                if(entity == null)
                {
                    continue;
                }

                if(entity is not BaseUnitLogicEntity unitEntity)
                {
                    continue;
                }

                Debug.Log($"ActivateSleepyMembers active entity:{unitEntity.Id}");
                unitEntity.IsActive = true;
                unitEntity.MarkNoLogic = false;
                LogicManager.globalBuffManager.RemoveAllBuffById(unitEntity.Id, "system_no_logic");
            }
        }

        public override void OnSpawn(LogicEntityRecord data)
        {
            base.OnSpawn(data);

            if (UsesStageFlow)
            {
                BindCurrentStageMemberEvents();
                EvaluateStageCompletion();
                return;
            }


            var stateExtraInfo = CacheEventGroupCfg.EventGroupStateInfos.Find(item => item.StateId == CurrStatusId);
            if(stateExtraInfo != null)
            {
                foreach (var mId in CurrActiveMemberSet)
                {
                    // 不是当前状态该有的成员 跳过
                    if(!stateExtraInfo.EnsureMemberIds.Contains(mId))
                    {
                        continue;
                    }
                    MemberId2EntityMap.TryGetValue(mId, out var entityId);
                    if (entityId == 0)
                    {
                        Debug.LogError($"OnSpawn add member lisnter fail for member {mId} not create correct");
                        continue;
                    }

                    var member = LogicManager.GetLogicEntity(entityId);

                    if (member is BaseUnitLogicEntity unitEntity)
                    {
                        //unitEntity.EventOnDie += OnMemberUnitDead;
                        unitEntity.EventOnEnmityBehave -= OnMemberEntityEnmityBehaved;
                        unitEntity.EventOnEnmityBehave += OnMemberEntityEnmityBehaved;
                    }
                }
            }
            
        }

        public override void OnDespawn(ref LogicEntityRecord snapshot)
        {
            if (InteractComp != null)
            {
                InteractComp.EventOnInteractEnded -= OnGroupInteractEnded;
            }

            foreach (var memberId in CurrActiveMemberSet.ToList())
            {
                UnbindMemberEvents(memberId);
            }

            base.OnDespawn(ref snapshot);
        }


        protected override void RefreshEntityRecordInfo(LogicEntityRecord input)
        {
            base.RefreshEntityRecordInfo(input);

            var realRecord = input as LogicEntityRecord4EventGroup;
            if (realRecord != null)
            {
                realRecord.TriggerTimesById ??= new();
                realRecord.TriggerLastFireTimeById ??= new();
                realRecord.MemberId2EntityMap.Clear();
                realRecord.CurrActiveMembers.Clear();
                realRecord.MemberStatusById.Clear();
                realRecord.DefeatedMemberIds.Clear();
                realRecord.TriggerTimesById.Clear();
                realRecord.TriggerLastFireTimeById.Clear();
                realRecord.MemberId2EntityMap.AddRange(MemberId2EntityMap);
                realRecord.CurrActiveMembers.AddRange(CurrActiveMemberSet);
                realRecord.MemberStatusById.AddRange(MemberStatusById);
                realRecord.DefeatedMemberIds.AddRange(DefeatedMemberSet);
                realRecord.TriggerTimesById.AddRange(TriggerTimesById);
                realRecord.TriggerLastFireTimeById.AddRange(TriggerLastFireTimeById);
                realRecord.TerminalState = (int)TerminalState;
                realRecord.OutcomeClaimed = OutcomeClaimed;
            }
        }


        protected void OnMemberEntityEnmityBehaved(long enmitiedId)
        {
            var stateExtraInfo = CacheEventGroupCfg.EventGroupStateInfos.Find(item => item.StateId == CurrStatusId);
            if (stateExtraInfo == null)
            {
                return;
            }

            foreach (var triggerId in stateExtraInfo.ActiveTriggerIds)
            {
                InnerTriggers.TryGetValue(triggerId, out var state);
                if (state == null)
                {
                    continue;
                }

                if (state.TriggerCfg.TriggerType != MapEventGroupConfig.GroupInnerTrigger.ETriggerType.AnyEnmity)
                {
                    continue;
                }

                if (state.TriggerCfg.MaxTriggerCnt != 0 && state.TriggerTimes >= state.TriggerCfg.MaxTriggerCnt)
                {
                    continue;
                }

                int interactId = (int)state.TriggerCfg.Param1;
                bool success = InteractComp.TryTriggerInteract(interactId, GamePlayerIds.Local);
                if (!success)
                {
                    Debug.LogError("change state trigger can not be blocked");
                    continue;
                }
            }
        }

        protected void OnMemberUnitDead(long deadEntityId)
        {
            //int markMemberId = 1;
            //foreach (var kv in RealRecord.MemberEntityMap)
            //{
            //    if (kv.Value == deadEntityId)
            //    {
            //        markMemberId = kv.Key;
            //    }
            //}

            //RealRecord.MemberEntityMap.Remove(markMemberId);
            //RealRecord.DestroyedMemberIds.Add(markMemberId);
        }

    }

}

