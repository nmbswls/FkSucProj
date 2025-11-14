using Config;
using Config.Map;
using My.Map.Logic;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Rendering.CameraUI;

namespace My.Map.Entity
{

    
    public class InteractPointLogic : LogicEntityBase
    {

        // 状态
        //public bool Appear = false;
        public int CurrStatusId = 0;

        public MapInteractPointConfig cacheCfg;

        public event Action OnStatusChange;

        public InteractPointLogic(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            cacheCfg = MapInteractPointLoader.Get(CfgId);
            CurrStatusId = 0;
        }

        public override EEntityType Type => EEntityType.InteractPoint;

        public override void Initialize()
        {
            base.Initialize();

        }


        /// <summary>
        /// 检查出现条件
        /// </summary>
        public void CheckInteractCondition()
        {
            if (CurrStatusId == 0)
            {
                var stateConf = cacheCfg.MainStatusInfo;
                if (stateConf.CheckCond != null)
                {
                }

            }
        }



        public override void Tick(float dt)
        {

        }

        public void DoTriggerInteract(int interactId)
        {
            if(CurrStatusId == 0)
            {
                var stateConf = cacheCfg.MainStatusInfo;
                if (stateConf.Outputs != null)
                {
                    foreach (var output in stateConf.Outputs)
                    {
                        switch (output.OutputType)
                        {
                            case MapInteractPointConfig.LogicInteractOutput.EOutputType.ChangeSelfStatus:
                                {
                                    ChangeSelfStatus(output.Param1);
                                }
                                break;
                        }

                    }
                }

            }
            else
            {
                Debug.Log("DoTriggerInteract no result");
            }
        }

        public void ChangeSelfStatus(int newStatus)
        {
            OnStatusChange?.Invoke();
        }
    }


}


