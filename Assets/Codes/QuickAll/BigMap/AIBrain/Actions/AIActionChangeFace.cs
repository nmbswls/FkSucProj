
using System;

namespace My.Map.Entity.AI.Action
{

    public class AIActionChangeFace : AIAction
    {

        public float ChangeFaceInterval = 5.0f;

        [NonSerialized]
        protected float _lastFaceTickTime;

        public override float RateScore(MapUnitAIBrain aIBrain)
        {
            return 1;
        }

        public override void Start(MapUnitAIBrain aIBrain)
        {
            base.Start(aIBrain);
        }

        public override void Tick(MapUnitAIBrain aIBrain)
        {
            if(aIBrain.blackboard.Time - _lastFaceTickTime < ChangeFaceInterval)
            {
                return;
            }

            aIBrain.UnitEntity.FaceDir = UnityEngine.Random.insideUnitCircle.normalized;
        }

        public override void Stop(MapUnitAIBrain aIBrain, AIActionStatus endStatus)
        {
            if (Status == AIActionStatus.Idle) return;
            Status = endStatus;
        }
    }
}

