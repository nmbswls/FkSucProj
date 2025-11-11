using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Map.Entity.AI.Action;
using UnityEngine;

namespace My.Map.Entity.AI
{

    public enum EAIActionType
    {
        DoNothing,
        DoOneThing,
    }

    [Serializable]
    public abstract class AIAction
    {
        public enum InitializationModes { EveryTime, OnlyOnce, }
        /// whether initialization should happen only once, or every time the brain is reset
        public InitializationModes InitializationMode;
        protected bool _initialized { get; set; }

        public string Label;
        protected MapUnitAIBrain _brain;

        public virtual bool IsExclusive { get { return false; } }

        public AIActionStatus Status { get; set; }

        protected virtual bool ShouldInitialize
        {
            get
            {
                switch (InitializationMode)
                {
                    case InitializationModes.EveryTime:
                        return true;
                    case InitializationModes.OnlyOnce:
                        return _initialized == false;
                }
                return true;
            }
        }

        /// <summary>
		/// Initializes the action. Meant to be overridden
		/// </summary>
		public virtual void Initialization(MapUnitAIBrain aIBrain)
        {
            this._brain = aIBrain;
            _initialized = true;
        }

        public virtual float RateScore(MapUnitAIBrain aIBrain)
        {
            return 0;
        }

        public virtual void Start(MapUnitAIBrain aIBrain)
        {
            Status = AIActionStatus.Running;
        }

        public virtual void Tick(MapUnitAIBrain aIBrain)
        {
            if (Status != AIActionStatus.Running) return;
        }

        public virtual void Stop(MapUnitAIBrain aIBrain, AIActionStatus endStatus)
        {
            if (Status == AIActionStatus.Idle) return;
            Status = endStatus;
        }

        /// <summary>
        /// Describes what happens when the brain enters the state this action is in. Meant to be overridden.
        /// </summary>
        public virtual void OnEnterState()
        {
            Status = AIActionStatus.Idle;
        }

        /// <summary>
        /// Describes what happens when the brain exits the state this action is in. Meant to be overridden.
        /// </summary>
        public virtual void OnExitState()
        {
        }

        public virtual bool CanInterrupt(MapUnitAIBrain aiBrain, string reason, bool hard)
        {
            return true;
        }
    }

    [Serializable]
    public class AIActionDoNothing : AIAction
    {
        public string DoNothing;
        /// <summary>
        /// On PerformAction we do nothing
        /// </summary>
        public override void Tick(MapUnitAIBrain aIBrain)
        {

        }
    }

    [Serializable]
    public class AIActionDoOneThing : AIAction
    {
        public string DoOneThing;
        /// <summary>
        /// On PerformAction we do nothing
        /// </summary>
        public override void Tick(MapUnitAIBrain aIBrain)
        {

        }
    }
}

