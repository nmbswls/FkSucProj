using Map.Entity.AI.Action;
using Map.Logic;
using My.Map.Entity.AI.Action;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using static AIBehaviorConfig;
using static UnityEditor.VersionControl.Asset;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.InputSystem.DefaultInputActions;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Entity.AI
{


    public static class AITemplateConfigLoader
    {


        public static Dictionary<string, AIBehaviorConfig> _configs = null;

        public static AIBehaviorConfig Get(string name)
        {
            if(_configs == null)
            {
                _configs = new();

                {
                    var config = new AIBehaviorConfig()
                    {
                        BehaviorName = "StaticNpc",
                    };
                    _configs[config.BehaviorName] = config;

                    {
                        var idleState = new AIBehaviorConfig.StateInfo()
                        {
                            Name = "Idle",
                        };
                        
                        idleState.ActionNames.Add("DoNothing");

                        idleState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckAttracted()
                                {
                                    IsHas = true,
                                }
                            },
                            TrueState = "Attracted",
                        });
                        idleState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckIsInPatrolGroup()
                                {
                                }
                            },
                            TrueState = "MoveInPatrolGroup",
                        });
                        idleState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckIsHunting()
                                {
                                }
                            },
                            TrueState = "Hunting",
                        });

                        config.States.Add(idleState);
                    }

                    {
                        var attractedState = new AIBehaviorConfig.StateInfo()
                        {
                            Name = "Attracted",
                        };


                        attractedState.ActionNames.Add("AttractedMove");


                        attractedState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckAttracted()
                                {
                                    IsHas = false,
                                }
                            },
                            TrueState = "Return"
                        });

                        config.States.Add(attractedState);
                    }


                    {
                        var returnState = new AIBehaviorConfig.StateInfo()
                        {
                            Name = "Return",
                        };


                        returnState.ActionNames.Add("ReturnInterrupt");

                        returnState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckReturn()
                                {
                                }
                            },
                            FalseState = "Idle"
                        });

                        config.States.Add(returnState);
                    }

                    {
                        var returnState = new AIBehaviorConfig.StateInfo()
                        {
                            Name = "MoveInPatrolGroup",
                        };


                        returnState.ActionNames.Add("FollowPatrolGroup");

                        returnState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckAttracted()
                                {
                                    IsHas = true,
                                }
                            },
                            TrueState = "Attracted",
                        });

                        config.States.Add(returnState);
                    }

                    {
                        var huntingState = new AIBehaviorConfig.StateInfo()
                        {
                            Name = "Hunting",
                        };


                        huntingState.ActionNames.Add("HuntingPlayer");

                        huntingState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckAttracted()
                                {
                                    IsHas = true,
                                }
                            },
                            TrueState = "Attracted",
                        });

                        config.States.Add(huntingState);
                    }

                    {

                        var combatState = new AIBehaviorConfig.StateInfo()
                        {
                            Name = "Combat",
                        };

                        combatState.ActionNames.Add("TryUseSkill");
                        combatState.ActionNames.Add("DistanceControl");
                        combatState.ActionNames.Add("CombatQuickMove");


                        combatState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckInBattle()
                                {
                                }
                            },
                            FalseState = "Return",
                        });

                        config.States.Add(combatState);
                    }


                    {
                        config.CommonTransitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckInBattle()
                                {
                                }
                            },
                            TrueState = "Combat",
                        });
                    }

                    {
                        var aAction = new AIActionDoNothing()
                        {
                        };

                        config.Actions.Add(aAction);
                    }

                    {
                        var aAction = new AIActionAttractedMove()
                        {
                        };

                        config.Actions.Add(aAction);
                    }

                    {
                        var aAction = new AIActionReturnInterrupt()
                        {
                        };

                        config.Actions.Add(aAction);
                    }

                    {
                        var aAction = new AIActionFollowPatrolGroup()
                        {

                        };
                        config.Actions.Add(aAction);
                    }

                    

                    {
                        var action = new AIActionHuntingPlayer()
                        {

                        };
                        config.Actions.Add(action);
                    }

                    {
                        var action = new AIActionTryUseSkill();
                        config.Actions.Add(action);
                    }

                    {
                        var action = new AIActionDistanceControl();
                        config.Actions.Add(action);
                    }
                    {
                        var action = new AIActionCombatQuickMove();
                        config.Actions.Add(action);
                    }
                }
            }


            _configs.TryGetValue(name, out var result);
            return result;
        }
    }
}

