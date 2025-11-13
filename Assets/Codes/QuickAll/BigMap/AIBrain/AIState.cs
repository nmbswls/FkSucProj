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

        public List<AIAction> Actions = new();
        public List<AITransition> Transitions = new();

        private readonly List<AIAction> _running = new();
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

        }

        /// <summary>
        /// Meant to be overridden, called when the Brain enters a State this Decision is in
        /// </summary>
        public virtual void OnEnterState()
        {
            foreach (var action in Actions)
            {
                action.OnEnterState();
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
            foreach (var action in Actions)
            {
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
            foreach (var s in Actions)
            {
                float u = s.RateScore();
                if (u <= 0) continue;
                float score = u;
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        public void TryInterruptAllActions(bool hard, string reason)
        {
            for (int i = _running.Count - 1; i >= 0; --i)
            {
                var a = _running[i];
                if (a.CanInterrupt(reason, hard))
                {
                    a.Stop(AIActionStatus.Interrupted);
                    _running.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Performs this state's actions
        /// </summary>
        public virtual void PerformActions()
        {
            for (int i = _running.Count - 1; i >= 0; --i)
            {
                _running[i].Tick();
            }

            for (int i = _running.Count - 1; i >= 0; --i)
            {
                if (_running[i].Status != AIActionStatus.Running)
                    _running.RemoveAt(i);
            }

            bool hasExclusive = _running.Exists(s => s.IsExclusive);
            if (!hasExclusive)
            {
                var chosen = SelectBestAction();
                if (chosen != null && !_running.Contains(chosen))
                {
                    if (chosen.IsExclusive)
                    {
                        TryInterruptAllActions(hard: true, reason: "Exclusive");
                        _running.Clear();
                    }
                    chosen.Start();
                    _running.Add(chosen);
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
                    if(!decision.Decide())
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

