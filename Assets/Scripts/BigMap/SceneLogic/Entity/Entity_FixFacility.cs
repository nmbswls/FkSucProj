


using System;
using System.Collections.Generic;
using Config;
using Config.Map;
using My.Map.Logic;
using My.Saving;
using UnityEngine;

namespace My.Map
{
    public class LogicEntityRepairPoint : LogicEntityBase
    {

        public MapFacilityRuinConfig Cfg;

        /// <summary>
        /// 状态
        /// </summary>
        public bool IsRepaired = false;
        public Dictionary<string, long> PutBuildMaterial = new();
        public int RepairProgress = 0;

        private RepairPointRuntimeSave _boundRuinPersist;

        public LogicEntityRepairPoint(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            RepairPointRuntimeSave? persistInfo = null;

            if (!string.IsNullOrEmpty(SrcUniqName))
            {
                persistInfo = logicManager.worldPersistState.GetOrCreateRuineRepairState(SrcUniqName);
            }

            if (persistInfo != null)
            {
                _boundRuinPersist = persistInfo;
                IsRepaired = persistInfo.IsRepaired;
                PutBuildMaterial.Clear();
                if (persistInfo.PutMaterial != null)
                {
                    foreach (var kv in persistInfo.PutMaterial)
                    {
                        PutBuildMaterial[kv.Key] = kv.Value;
                    }
                }

                RepairProgress = persistInfo.RepairProgress;
            }
        }

        private void SyncRuinStateToPersist()
        {
            if (_boundRuinPersist == null)
            {
                return;
            }

            _boundRuinPersist.IsRepaired = IsRepaired;
            _boundRuinPersist.RepairProgress = RepairProgress;
            _boundRuinPersist.PutMaterial ??= new Dictionary<string, long>();
            _boundRuinPersist.PutMaterial.Clear();
            foreach (var kv in PutBuildMaterial)
            {
                _boundRuinPersist.PutMaterial[kv.Key] = kv.Value;
            }
        }

        public override EEntityType Type => EEntityType.FacilityRuin;

        
        public event Action EventOnRepaired;

        protected override void LoadCfg()
        {
            Cfg = MapFixFacilityCfgLoader.Get(CfgId);
        }

        protected override void OnTick(float dt)
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

            if(!CheckIsRepairOpen())
            {
                return;
            }

            OnRepairFinish();
        }

        public bool CheckIsRepairOpen()
        {
            bool passed = true;
            foreach (var cond in Cfg.OpenRepairCond)
            {
                if (!LogicManager.CheckCommonCond(cond))
                {
                    passed = false;
                    break;
                }
            }
            return passed;
        }

        public bool CheckEnoughRepairMaterial()
        {
            if(Cfg == null || Cfg.AutoRepair)
            {
                return true;
            }
            for (int i = 0; i < Cfg.RepairMaterials.keys.Count; i++)
            {
                string itemId = Cfg.RepairMaterials.keys[i];
                long needAmount  = Cfg.RepairMaterials.values[i];

                PutBuildMaterial.TryGetValue(itemId, out var currAmount);
                if(currAmount <  needAmount)
                {
                    return false;
                }
            }
            return true;
        }


        public void TryPutInMaterial()
        {
            if (Cfg == null || Cfg.AutoRepair)
            {
                return;
            }
            for (int i = 0; i < Cfg.RepairMaterials.keys.Count; i++)
            {
                string itemId = Cfg.RepairMaterials.keys[i];
                long needAmount = Cfg.RepairMaterials.values[i];

                PutBuildMaterial[itemId] = needAmount;
            }
        }

        public void TryManualRepair()
        {
            // 在这里抛一个阻塞上去
            OnRepairFinish();

            LogicManager.PendingCostDayPeriod();
        }

        public void OnRepairFinish()
        {
            // 标记
            if (LogicManager.homeDataManager?.DoRepairFacility(CfgId, this.Pos) != true)
            {
                return;
            }

            IsRepaired = true;

            SyncRuinStateToPersist();

            // 播放
            EventOnRepaired?.Invoke();

            // 
        }
    }
}
