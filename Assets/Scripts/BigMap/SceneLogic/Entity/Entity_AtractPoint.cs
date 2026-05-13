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

        int AttractLevel { get; }
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

        public int AttractLevel => 1;

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            ApplyAttract();
        }

        /// <summary>
        /// 执行吸引力
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
                        unit.OnReceiveStimulus(new StimulusEvent(this.Pos, 20, 99, EStimulusType.Audio_Normal, this.Id));
                        //unit.ApplyAttracted(ENpcAttractSrcType.SrcEntity, 10, Pos, 3);
                    }
                }

            }
        }
    }
}

