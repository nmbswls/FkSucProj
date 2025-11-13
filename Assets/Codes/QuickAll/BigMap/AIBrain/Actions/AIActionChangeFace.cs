
using System;

namespace My.Map.Entity.AI.Action
{

    public class AIActionChangeFace : AIAction
    {

        public float ChangeFaceInterval = 5.0f;

        [NonSerialized]
        protected float _lastFaceTickTime;

        public override float RateScore()
        {
            return 1;
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Tick()
        {
            if(LogicTime.time - _lastFaceTickTime < ChangeFaceInterval)
            {
                return;
            }

            _brain.UnitEntity.FaceDir = UnityEngine.Random.insideUnitCircle.normalized;
        }

        public override void Stop(AIActionStatus endStatus)
        {
            if (Status == AIActionStatus.Idle) return;
            Status = endStatus;
        }
    }
}

