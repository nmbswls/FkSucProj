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


    public class LogicEntityTeleporter : LogicEntityBase
    {

        public string TargetMapId;
        public string TargetNamedPoint;

        public LogicEntityTeleporter(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var realRecotd = (LogicEntityRecord4Teleporter)bindingRecord;

            this.TargetMapId = realRecotd.TargetMap;
            this.TargetNamedPoint = realRecotd.TargetNamedPoint;
        }


        public override EEntityType Type => EEntityType.Teleporter;

        public override void Initialize()
        {
            base.Initialize();

        }
    }



}


