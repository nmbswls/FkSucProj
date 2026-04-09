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


    public class LogicEntitySimpleBlock : LogicEntityBase
    {

        public LogicEntitySimpleBlock(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var realRecord = (LogicEntityRecord4SimpleBlock)bindingRecord;
        }


        public override EEntityType Type => EEntityType.SimpleBlock;

        public override void Initialize()
        {
            base.Initialize();

        }
    }



}


