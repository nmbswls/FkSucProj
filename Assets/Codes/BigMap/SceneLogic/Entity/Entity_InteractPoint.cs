using Config;
using Config.Map;
using Map.Logic.Events;
using My.Config;
using My.Map.Logic;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Config.Map.MapInteractPointConfig;
using static UnityEditor.Rendering.CameraUI;

namespace My.Map.Entity
{


    public class LogicEntityInteractPoint : LogicEntityBase, IWithInteract, IEntityInteractable
    {
        // ×´Ì¬
        //public bool Appear = false;
        public int CurrStatusId = 0;
        private bool IsSwitching = false;
        public Dictionary<string, string> DynamicVariables = new();

        public MapInteractPointConfig cacheCfg;

        public EntityInteractComp InteractComp;

        public event Action<StateChangeView> EventOnStatusChange;


        public LogicEntityInteractPoint(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            cacheCfg = MapInteractPointLoader.Get(CfgId);

            var realRecord = bindingRecord as LogicEntityRecord4InteractPoint;
            CurrStatusId = realRecord.Status;
            DynamicVariables.AddRange(realRecord.DynamicVariables);
        }

        public override EEntityType Type => EEntityType.InteractPoint;

        public override void Initialize()
        {
            base.Initialize();

            InteractComp = new(this);

            var curState = GetCurrentStatusInfo();
            if (curState != null)
            {
                InteractComp.RefreshInteractInfo(curState.InteractInfos);
            }

            CheckStatusCondition();
        }

        public string GetRuntimeVariable(string paramName)
        {
            DynamicVariables.TryGetValue(paramName, out var value);
            return value;
        }

        public StatusInfo GetCurrentStatusInfo()
        {
            if(CurrStatusId == 0)
            {
                return cacheCfg.MainStatusInfo;
            }

            var findIt = cacheCfg.ExtraStatusInfos.Find((item)=>item.StatusId == CurrStatusId);
            return findIt;

        }

        /// <summary>
        /// ¼ì²é×´Ì¬ÇÐ»»
        /// </summary>
        public void CheckStatusCondition()
        {
            foreach(var rule in cacheCfg.StateChangeRules)
            {
                if(rule.FromStatus != CurrStatusId)
                {
                    continue;
                }

                var poassed = true;
                foreach(var cond in rule.CommonConds)
                {
                    if (!LogicManager.CheckCommonCond(cond))
                    {
                        poassed = false;
                        break;
                    }
                }

                //foreach (var needFlag in rule.NeedSelfFlag)
                //{
                //    if(!this.RealRecord.CustomFlags.Contains(needFlag))
                //    {
                //        poassed = false;
                //        break;
                //    }
                //}

                if (poassed)
                {
                    ChangeSelfStatus(rule.ToStatus, rule.ChangeView);
                    break;
                }
            }
        }

        /// <summary>
        /// ÊÇ·ñÕýÔÚ½»»¥
        /// </summary>
        public bool IsInteracting { get { return InteractComp.IsInteracting; } }

        public List<MapInteractInfo> InteractInfos { get { return InteractComp.InteractInfos; } }

        public bool TryTriggerInteract(int interactId)
        {
            return InteractComp.TryTriggerInteract(interactId);
        }

        public bool CheckTriggerInteract(int interactId)
        {
            return InteractComp.CheckTriggerInteract(interactId);
        }


        public override void OnMapLogicEvent(IMapLogicEvent evt)
        {
            base.OnMapLogicEvent(evt);

            CheckStatusCondition(); 
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            // µÍÆµ¼ì²é×´Ì¬ÇÐ»»
            LowFreqCheckStatusChange();

            InteractComp?.TickInteract(dt);
        }

        private float _lowFreqCheckStatusTimer = 0;
        /// <summary>
        /// µÍÆµ¼ì²é
        /// </summary>
        protected void LowFreqCheckStatusChange()
        {
            if(LogicTime.time < _lowFreqCheckStatusTimer)
            {
                return;
            }

            _lowFreqCheckStatusTimer = LogicTime.time + 2f;

            CheckStatusCondition();
        }

        public void ChangeSelfStatus(int newStatus, StateChangeView changeView = null)
        {
            int oldStat = CurrStatusId;
            CurrStatusId = newStatus;

            var curState = GetCurrentStatusInfo();
            if (curState != null)
            {
                InteractComp.RefreshInteractInfo(curState.InteractInfos);
            }
            else
            {
                InteractComp.RefreshInteractInfo(new());
            }

            OnStatusChange(oldStat);
            EventOnStatusChange?.Invoke(changeView);
        }

        public void DoAnimation(string animName)
        {
            //
            Debug.Log("DoAnimation doanimation");

            AddAnimLayer(animName);
        }

        protected virtual void OnStatusChange(int preStage)
        {

        }

        public override void OnDespawn(ref LogicEntityRecord snapshot)
        {
            base.OnDespawn(ref snapshot);
        }
    

        protected override void RefreshEntityRecordInfo(LogicEntityRecord input)
        {
            base.RefreshEntityRecordInfo(input);

            var realRecord = input as LogicEntityRecord4InteractPoint;
            realRecord.Status = CurrStatusId;
            realRecord.DynamicVariables.AddRange(DynamicVariables);
        }
    }



}


