using Config;
using Config.Map;
using Map.Logic.Events;
using My.Config;
using My.Map;
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
        // ??
        //public bool Appear = false;
        public int CurrStatusId = 0;
        private bool IsSwitching = false;
        public Dictionary<string, string> DynamicVariables = new();

        public MapInteractPointConfig cacheCfg;

        public EntityInteractComp InteractComp;

        public event Action<StateChangeView> EventOnStatusChange;


        public LogicEntityInteractPoint(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var realRecord = bindingRecord as LogicEntityRecord4InteractPoint;
            CurrStatusId = realRecord.Status;
            DynamicVariables.AddRange(realRecord.DynamicVariables);
        }

        protected virtual void LoadCfg()
        {
            cacheCfg = MapInteractPointLoader.Get(CfgId);
        }

        public override EEntityType Type => EEntityType.InteractPoint;

        public override WorldMapLandmarkKind WorldMapLandmark
        {
            get
            {
                if (cacheCfg == null) return base.WorldMapLandmark;
                if (cacheCfg.ShowOnWorldMap) return WorldMapLandmarkKind.MajorInteract;
                if (WorldMapRuntime.IsGlobalInteractLandmark(CfgId))
                    return WorldMapLandmarkKind.MajorInteract;
                return base.WorldMapLandmark;
            }
        }

        public override string WorldMapLandmarkLabel
        {
            get
            {
                if (cacheCfg == null) return CfgId;
                if (!string.IsNullOrEmpty(cacheCfg.WorldMapMarkerLabel)) return cacheCfg.WorldMapMarkerLabel;
                if (!string.IsNullOrEmpty(cacheCfg.ShowName)) return cacheCfg.ShowName;
                return CfgId;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            LoadCfg();

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
        /// ??????
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

                foreach (var needFlag in rule.NeedSelfFlag)
                {
                    if (!this.CheckLocalSwitch(needFlag))
                    {
                        poassed = false;
                        break;
                    }
                }

                if (poassed)
                {
                    ChangeSelfStatus(rule.ToStatus, rule.ChangeView);
                    break;
                }
            }
        }

        /// <summary>
        /// ??????
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

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            

            // ????????
            LowFreqCheckStatusChange();

            InteractComp?.TickInteract(dt);
        }

        private float _lowFreqCheckStatusTimer = 0;
        /// <summary>
        /// ????
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
            realRecord.DynamicVariables.Clear();
            realRecord.DynamicVariables.AddRange(DynamicVariables);
        }
    }



}


