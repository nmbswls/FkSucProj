
using System;

namespace My.Map.Entity.AI.Action
{

    [Serializable]
    public class AIActionCfgChangeFace : AIActionCfg
    {
        public override bool IsDecorate => false;
        public float ChangeFaceInterval = 5.0f;
    }
    public class AIActionChangeFace : AIAction
    {

        public AIActionCfgChangeFace realCfg { get { return (AIActionCfgChangeFace)cfg; } }

        [NonSerialized]
        protected float _lastFaceTickTime;

        public AIActionChangeFace(MapUnitAIBrain aIBrain, AIActionCfg cfg) : base(aIBrain, cfg)
        {
        }

        public override string Name => "ChangeFace";

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
            if(LogicTime.time - _lastFaceTickTime < realCfg.ChangeFaceInterval)
            {
                return;
            }
            _brain.NpcEntity.UpdateLookIntent(null, UnityEngine.Random.insideUnitCircle.normalized);
        }

        public override void Stop(AIActionStatus endStatus)
        {
            if (Status == AIActionStatus.Idle) return;
            Status = endStatus;
        }
    }
}

