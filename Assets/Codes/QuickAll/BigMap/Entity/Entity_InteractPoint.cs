using Config;
using Config.Map;
using Map.Logic.Chunk;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Map.Entity
{

    
    public class InteractPointLogic : LogicEntityBase
    {

        public MapInteractPointConfig cacheCfg;

        public InteractPointLogic(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
        }

        public override EEntityType Type => EEntityType.InteractPoint;

        public override void Initialize()
        {
            base.Initialize();

            cacheCfg = MapInteractPointLoader.Get(CfgId);
        }

        // 状态
        public int CurrStatusId = 0;

        /// <summary>
        /// 检查出现条件
        /// </summary>
        public void CheckAppearCond()
        {

        }



        public override void Tick(float now, float dt)
        {

        }
    }

}


