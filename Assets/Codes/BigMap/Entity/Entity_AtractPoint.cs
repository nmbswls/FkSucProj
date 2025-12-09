using Config.Unit;
using Config;
using UnityEngine;
using Config.Map;
using System.Collections.Generic;
using System;
using Map.Logic;
using My.Map.Logic;
using My.Map.Entity;


namespace My.Map
{

    public interface IAttractSource
    { 
        long Id { get; }

        Vector2 Pos { get; }
    }

    public class AttractPointLogicEntity : LogicEntityBase, IAttractSource
    {

        public float _lastAttrctTime;
        public float _lifeTime;

        public float AttractInterval = 1f;

        public AttractPointLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {

        }


        public override EEntityType Type => EEntityType.AttractPoint;


        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            ApplyAttract();
        }

        /// <summary>
        /// Ö´ÐÐÎüÒýÁ¦
        /// </summary>
        protected void ApplyAttract()
        {
            if(_lastAttrctTime == 0 || (LogicTime.time - _lastAttrctTime) > AttractInterval)
            {
                _lastAttrctTime = LogicTime.time;


                var filterParam = new EntityFilterParam()
                {
                    FilterParamLists = new() { EEntityType.Npc },
                };

                var surrounds = LogicManager.visionSenser.OverlapCircleAllEntity(Pos, 5.0f, filterParam);

                foreach (var surround in surrounds)
                {
                    var unit = surround as NpcUnitLogicEntity;
                    if (unit != null)
                    {
                        unit.ApplyAttracted(Pos, 3.0f, this);
                    }
                }

            }
        }
    }
}

