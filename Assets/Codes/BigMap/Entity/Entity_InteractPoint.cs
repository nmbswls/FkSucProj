using Config;
using Config.Map;
using Map.Logic.Events;
using My.Config;
using My.Map.Logic;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Config.Map.MapInteractPointConfig;
using static UnityEditor.Rendering.CameraUI;

namespace My.Map.Entity
{

    
    public class InteractPointLogic : LogicEntityBase, IWithInteract
    {

        // ×´Ì¬
        //public bool Appear = false;
        public int CurrStatusId = 0;

        public MapInteractPointConfig cacheCfg;

        public EntityInteractComp InteractComp;

        public event Action OnStatusChange;

        public InteractPointLogic(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            cacheCfg = MapInteractPointLoader.Get(CfgId);

            var realRecord = (LogicEntityRecord4InteractPoint)bindingRecord;
            CurrStatusId = realRecord.Status;

            InteractComp = new(this);

            var curState = GetCurrentStatusInfo();
            if(curState != null)
            {
                InteractComp.RegisterInteractInfo(curState.InteractInfos);
            }

            //InitStatusChangeListner();
        }

        public override EEntityType Type => EEntityType.InteractPoint;

        public override void Initialize()
        {
            base.Initialize();

            CheckStatusCondition();
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
                foreach(var cond in rule.Conds)
                {
                    if (!LogicManager.CheckCommonCond(cond))
                    {
                        poassed = false;
                        break;
                    }
                }
                
                if(poassed)
                {
                    ChangeSelfStatus(rule.ToStatus);
                    break;
                }
            }
        }

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

        }


        public void ChangeSelfStatus(int newStatus)
        {
            int oldStat = CurrStatusId;
            CurrStatusId = newStatus;
            var curState = GetCurrentStatusInfo();
            if (curState != null)
            {
                InteractComp.RegisterInteractInfo(curState.InteractInfos);
            }
            else
            {
                InteractComp.RegisterInteractInfo(new());
            }

            OnStatusChange?.Invoke();
        }
    }


}


