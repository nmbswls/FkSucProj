


using System;
using System.Collections.Generic;
using Config;
using Config.Map;
using My.Map.Logic;
using UnityEngine;

namespace My.Map
{
    public class LogicEntityFixFacility : LogicEntityBase
    {

        public MapFixFacilityConfig Cfg;

        public LogicEntityFixFacility(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
        }

        public override EEntityType Type => EEntityType.FixFacility;



        public bool IsRepaired = false;
        /// <summary>
        /// 建造材料
        /// </summary>
        public Dictionary<string, long> PutBuildMaterial = new();

        public event Action EventOnRepaired;

        protected override void LoadCfg()
        {
            Cfg = MapFixFacilityCfgLoader.Get(CfgId);
        }

        protected virtual void OnTick(float dt)
        {
            base.OnTick(dt);

            CheckAutoRepair();
        }


        private void CheckAutoRepair()
        {
            if(IsRepaired)
            {
                return;
            }

            if (!Cfg.AutoRepair)
            {
                return;
            }

            bool failed = false;
            foreach (var cond in Cfg.AutoRepairCond)
            {
                if (!LogicManager.CheckCommonCond(cond))
                {
                    failed = true;
                    break;
                }
            }

            if(failed)
            {
                return;
            }

            OnRepairFinish();
        }


        public void OnRepairFinish()
        {
            string outputPlacement = Cfg.PlacementId;

            // 标记
            IsRepaired = true;

            // 播放
            EventOnRepaired?.Invoke();

            // 
            LogicManager.homeDataManager.DoRepairFacility(CfgId, this.Pos);
        }
    }
}