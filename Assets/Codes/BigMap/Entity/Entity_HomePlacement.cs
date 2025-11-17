using Config.Unit;
using Config;
using UnityEngine;
using Config.Map;
using System.Collections.Generic;
using System;
using static UnityEditor.Progress;
using My.Player.Bag;
using Map.Logic.Events;
using My.Map.Logic;


namespace My.Map
{
    public class HomePlacementLogicEntity : LogicEntityBase
    {

        public string DropId;
        public HomePlacementLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            
        }

        public override EEntityType Type => EEntityType.HomePlacement;

    }
     
}

