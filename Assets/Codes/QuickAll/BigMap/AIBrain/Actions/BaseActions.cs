using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Map.Entity.AI.Action
{
    public interface IAIAction
    {
        float Evaluate(MapUnitAIBrain aiBrain);               // 返回效用值（<=0 表示不考虑）
        void Start(MapUnitAIBrain aiBrain);
        void Tick(MapUnitAIBrain aiBrain);
        void Stop(MapUnitAIBrain aiBrain, int endReason);
        bool CanInterrupt(MapUnitAIBrain aiBrain, string reason, bool hard);
    }


    public class AIActionChangeFace : IAIAction
    {

        public MapUnitAIBrain AIBrain;

        protected float _lastFaceTickTime;

        public bool CanInterrupt(MapUnitAIBrain aiBrain, string reason, bool hard)
        {
            return true;
        }

        public float Evaluate(MapUnitAIBrain aiBrain)
        {
            if(AIBrain.Time > _lastFaceTickTime + 3f)
            {
                return 1;
            }
            return 0;
        }

        public void Start(MapUnitAIBrain aiBrain)
        {
            AIBrain.UnitEntity.FaceDir = Random.insideUnitCircle.normalized;
        }

        public void Stop(MapUnitAIBrain aiBrain, int endReason)
        {
        }

        public void Tick(MapUnitAIBrain aiBrain)
        {

        }
    }



    public class AIActionFollower : IAIAction
    {
        public MapUnitAIBrain AIBrain;


        private float _lastNavTimer;
        private Vector2? _lastNavPos;

        public bool CanInterrupt(MapUnitAIBrain aiBrain, string reason, bool hard)
        {
            return true;
        }

        public float Evaluate(MapUnitAIBrain aiBrain)
        {
            return 1;
        }

        public void Start(MapUnitAIBrain aiBrain)
        {
            AIBrain.UnitEntity.FaceDir = Random.insideUnitCircle.normalized;
        }

        public void Stop(MapUnitAIBrain aiBrain, int endReason)
        {
        }
        public void Tick(MapUnitAIBrain aiBrain)
        {
            _lastNavTimer -= Time.time;
            if (_lastNavTimer > 0)
            {
                return;
            }

            _lastNavTimer = 0.5f;

            var patrolGroup = AIBrain.UnitEntity.LogicManager.AreaManager.GetLogicEntiy(AIBrain.UnitEntity.FollowPatrolId);
            var followPos = patrolGroup.Pos;

            if (_lastNavPos == null || (_lastNavPos.Value - followPos).magnitude > 0.1f)
            {
                AIBrain.UnitEntity.StartTargettedMove(followPos, 0.3f);
            }
        }
    }


    public class AIActionDistanceControl : IAIAction
    {
        public MapUnitAIBrain AIBrain;

        public float desireDist = 1.0f;
        public AIActionDistanceControl(float desireDist)
        {
            this.desireDist = desireDist;
        }

        public bool CanInterrupt(MapUnitAIBrain aiBrain, string reason, bool hard)
        {
            return true;
        }

        public float Evaluate(MapUnitAIBrain aiBrain)
        {
            if (aiBrain.Distance < desireDist * 1.1f && aiBrain.Distance < desireDist * 0.9f)
            {
                return 0;
            }
            return 1;
        }

        public void Start(MapUnitAIBrain aiBrain)
        {
            AIBrain.UnitEntity.FaceDir = Random.insideUnitCircle.normalized;
        }

        public void Stop(MapUnitAIBrain aiBrain, int endReason)
        {
        }
        public void Tick(MapUnitAIBrain aiBrain)
        {
            
        }
    }
}


