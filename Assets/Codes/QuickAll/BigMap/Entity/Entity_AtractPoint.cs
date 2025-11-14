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

        float AttractPower { get; }

        Vector2 Pos { get; }
        
        float AttractRange { get; }
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

        public float AttractPower
        {
            get { return 5.0f; }
        }

        public float AttractRange
        {
            get { return 5.0f; }
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            ApplyAttract();
        }

        protected void ApplyAttract()
        {
            if(_lastAttrctTime == 0 || (LogicTime.time - _lastAttrctTime) > AttractInterval)
            {
                _lastAttrctTime = LogicTime.time;


                var filterParam = new EntityFilterParam()
                {
                    FilterParamLists = new() { EEntityType.Monster, EEntityType.Npc },
                };

                var surrounds = LogicManager.visionSenser.OverlapCircleAllEntity(Pos, AttractRange, filterParam);

                foreach (var surround in surrounds)
                {
                    var unit = surround as BaseUnitLogicEntity;
                    if (unit != null)
                    {
                        unit.GetAttracted(this);
                    }
                }
            }
        }
    }
}

