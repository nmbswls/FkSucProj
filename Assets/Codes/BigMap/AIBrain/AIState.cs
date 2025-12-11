using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Map.Entity.AI.Action;
using UnityEngine;

namespace My.Map.Entity.AI
{
    public class AIBrainState
    {
        public string StateName;
        protected MapUnitAIBrain _brain;

        public List<AIAction> DecorateActions = new();
        public List<AIAction> NormalActions = new();
        public List<AITransition> Transitions = new();

        private readonly List<AIAction> _runningDecorate = new();
        private AIAction _runningNormal = null;
        /// <summary>
        /// On Awake we grab our Brain
        /// </summary>
        public AIBrainState(MapUnitAIBrain brain)
        {
            _brain = brain;
        }

        /// <summary>
        /// Meant to be overridden, called when the game starts
        /// </summary>
        public virtual void Initialization()
        {
            _runningDecorate.Clear();
            foreach (var action in DecorateActions)
            {
                _runningDecorate.Add(action);
            }
        }

        /// <summary>
        /// Meant to be overridden, called when the Brain enters a State this Decision is in
        /// </summary>
        public virtual void OnEnterState()
        {
            foreach (var action in NormalActions)
            {
                action.OnEnterState();
            }

            foreach (var action in DecorateActions)
            {
                action.OnEnterState();
                action.Start();
            }

            foreach (AITransition transition in Transitions)
            {
                foreach(var oneDecision in transition.Decisions)
                {
                    oneDecision.OnEnterState();
                }
                
            }
        }

        /// <summary>
		/// On exit state we pass that info to our actions and decisions
		/// </summary>
		public virtual void OnExitState()
        {
            if(_runningNormal != null)
            {
                _runningNormal.Stop(AIActionStatus.Interrupted);
                _runningNormal = null;
            }

            foreach (var action in NormalActions)
            {
                action.OnExitState();
            }

            foreach (var action in DecorateActions)
            {
                action.Stop(AIActionStatus.Interrupted);
                action.OnExitState();
            }

            foreach (AITransition transition in Transitions)
            {
                foreach (var oneDecision in transition.Decisions)
                {
                    oneDecision.OnExitState();
                }
            }
        }

        public AIAction SelectBestAction()
        {
            AIAction best = null;
            float bestScore = float.NegativeInfinity;
            foreach (var s in NormalActions)
            {
                if(s.Status == AIActionStatus.Running)
                {
                    continue;
                }
                float u = s.RateScore();
                if (u <= 0) continue;
                float score = u;
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        public void TryInterruptAllActions(bool hard, string reason)
        {
            //for (int i = _running.Count - 1; i >= 0; --i)
            //{
            //    var a = _running[i];
            //    if (a.CanInterrupt(reason, hard))
            //    {
            //        a.Stop(AIActionStatus.Interrupted);
            //        _running.RemoveAt(i);
            //    }
            //}
        }

        /// <summary>
        /// Performs this state's actions
        /// </summary>
        public virtual void PerformActions()
        {
            for (int i = _runningDecorate.Count - 1; i >= 0; --i)
            {
                _runningDecorate[i].Tick();
            }

            if(_runningNormal != null)
            {
                _runningNormal.Tick();
                if (_runningNormal.Status != AIActionStatus.Running)
                    _runningNormal = null;
            }
            
            if(_runningNormal == null || _runningNormal.CanInterrupt(null, false))
            {
                var chosen = SelectBestAction();
                if (chosen != null && _runningNormal != chosen)
                {
                    _runningNormal.Stop(AIActionStatus.Interrupted);
                    chosen.Start();
                    _runningNormal = chosen;
                }
            }
        }

        /// <summary>
        /// Tests this state's transitions
        /// </summary>
        public virtual void EvaluateTransitions()
        {
            if (Transitions.Count == 0) { return; }
            for (int i = 0; i < Transitions.Count; i++)
            {
                bool pass = true;
                foreach(var decision in Transitions[i].Decisions)
                {
                    if(!decision.Decide(_brain))
                    {
                        pass = false; break;
                    }
                }

                if (pass)
                {
                    if (!string.IsNullOrEmpty(Transitions[i].TrueState))
                    {
                        _brain.TransitionToState(Transitions[i].TrueState);
                        break;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(Transitions[i].FalseState))
                    {
                        _brain.TransitionToState(Transitions[i].FalseState);
                        break;
                    }
                }
            }
        }
    }
}

