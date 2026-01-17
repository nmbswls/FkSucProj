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
using static UnityEditor.Rendering.CameraUI;
using Unity.VisualScripting;


namespace My.Map
{
    public class EventGroupLogicEntity : LogicEntityInteractPoint
    {

        public MapEventGroupConfig CacheEventGroupCfg { get { return (MapEventGroupConfig)cacheCfg; } }


        public EventGroupLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            cacheCfg = MapEventGroupCfgLoader.Get(cfgId);

            var groupRecord = (LogicEntityRecord4EventGroup)BindingRecord;
            foreach (var pair in groupRecord.MemberId2EntityMap) 
            {
                MemberId2EntityMap[pair.Key] = pair.Value;
            }

            foreach(var id in groupRecord.CurrActiveMembers)
            {
                CurrActiveMemberSet.Add(id);
            }
        }

        ///// <summary>
        ///// 缓存
        ///// 危险 logicentity可能被创建为另一个
        ///// </summary>
        //public Dictionary<int, ILogicEntity> GroupMemberDict = new();

        private float _lastCheckTimer;

        public Dictionary<int, long> MemberId2EntityMap = new();
        protected HashSet<int> CurrActiveMemberSet = new();

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
        }
        private float _lastEnsureMemberTimer = 0;

        public override void Tick(float dt)
        {
            base.Tick(dt);

            TickAllMemberStatus();

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
            var stateExtraInfo = CacheEventGroupCfg.EventGroupStateInfos.Find(item => item.StateId == CurrStatusId);
            if (stateExtraInfo == null)
            {
                return;
            }

            foreach (var mId in CurrActiveMemberSet)
            {
                if (!stateExtraInfo.EnsureMemberIds.Contains(mId))
                {
                    MemberId2EntityMap.TryGetValue(mId, out var entityId);
                    if (entityId != 0)
                    {
                        LogicManager.AreaManager.RequestEntityDestroy(entityId, "event_group_remove");
                        CurrActiveMemberSet.Remove(mId);
                    }
                }
            }


            foreach (var mId in stateExtraInfo.EnsureMemberIds)
            {
                var mInfo = CacheEventGroupCfg.GroupMemberInfos.Find(item => item.MemberId == mId);
                if (mInfo == null) continue;

                if (!MemberId2EntityMap.ContainsKey(mId))
                {
                    var record = LogicManager.AreaManager.CreateEntityRecordFromInitInfo(mInfo.InitInfo);
                    record.LifeBindEntityId = this.Id;
                    MemberId2EntityMap[mId] = record.Id;
                    LogicManager.AddNewEntityRecord(record);

                    Debug.LogError($"event group:{Id} create member:{mId} entity:{record.Id}");
                    

                    CurrActiveMemberSet.Add(mId);
                }

                // 强制激活一次
                LogicManager.GetLogicEntity(MemberId2EntityMap[mId]);
            }

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

                var success = InteractComp.TryTriggerInteract(interactId);
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
                bool success = InteractComp.TryTriggerInteract(interactId);
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

                var entity = LogicManager.GetLogicEntity(mId);
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
            }
        }

        public override void OnSpawn(LogicEntityRecord data)
        {
            base.OnSpawn(data);


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

        public override void OnDespawn(out LogicEntityRecord? snapshot)
        {
            base.OnDespawn(out snapshot);
        }

        protected override LogicEntityRecord CreateRecordByType()
        {
            return new LogicEntityRecord4EventGroup();
        }

        protected override void FillInteractPointRecord(LogicEntityRecord4InteractPoint input)
        {
            base.FillInteractPointRecord(input);

            var realRecord = input as LogicEntityRecord4EventGroup;
            if (realRecord != null)
            {
                realRecord.MemberId2EntityMap.AddRange(MemberId2EntityMap);
                realRecord.CurrActiveMembers.AddRange(CurrActiveMemberSet);
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
                bool success = InteractComp.TryTriggerInteract(interactId);
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

