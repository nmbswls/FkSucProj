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
                        BehaviorName = "BasicUnit",
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
                                new AIDecisionCheckHasMoveBehave()
                                {
                                }
                            },
                            TrueState = "MoveBehave",
                        });
                        //idleState.Transitions.Add(new AITransition()
                        //{
                        //    Decisions = new List<AIDecision>()
                        //    {
                        //        new AIDecisionCheckIsHunting()
                        //        {
                        //        }
                        //    },
                        //    TrueState = "Hunting",
                        //});

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
                                new AIDecisionCanLeaveAttact()
                                {
                                }
                            },
                            TrueState = "RecoverFromAttract"
                        });

                        config.States.Add(attractedState);
                    }

                    {
                        var returnState = new AIBehaviorConfig.StateInfo()
                        {
                            Name = "RecoverFromAttract",
                        };


                        returnState.ActionNames.Add("RecoveryFromAttract");

                        returnState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionReachMoveInterrupt()
                                {
                                }
                            },
                            TrueState = "Idle"
                        });

                        config.States.Add(returnState);
                    }


                    {
                        var returnState = new AIBehaviorConfig.StateInfo()
                        {
                            Name = "RecoverFromCombat",
                        };


                        returnState.ActionNames.Add("TryRecovery");

                        returnState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckCombatState()
                                {
                                    CheckState = EntityCombatStateComp.ECombatState.CombatRecover
                                }
                            },
                            FalseState = "Idle"
                        });

                        config.States.Add(returnState);
                    }


                    {
                        var moveState = new AIBehaviorConfig.StateInfo()
                        {
                            Name = "MoveBehave",
                        };

                        moveState.ActionNames.Add("NormalMoveDaemon");

                        moveState.ActionNames.Add("MoveDoPath");
                        moveState.ActionNames.Add("MoveHunting");
                        moveState.ActionNames.Add("MoveInPatrolGroup");
                        moveState.Transitions.Add(new AITransition()
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

                        config.States.Add(moveState);
                    }

                    {

                        var combatState = new AIBehaviorConfig.StateInfo()
                        {
                            Name = "Combat",
                        };

                        combatState.ActionNames.Add("CombatMain");
                        combatState.ActionNames.Add("TryUseSkill");
                        combatState.ActionNames.Add("DistanceControl");
                        combatState.ActionNames.Add("CombatQuickMove");


                        combatState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckCombatState()
                                {
                                    CheckState = EntityCombatStateComp.ECombatState.CombatRecover,
                                }
                            },
                            TrueState = "RecoverFromCombat",
                        });

                        combatState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckCombatState()
                                {
                                    CheckState = EntityCombatStateComp.ECombatState.NotCombat,
                                }
                            },
                            TrueState = "Idle",
                        });

                        config.States.Add(combatState);
                    }


                    {
                        config.CommonTransitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckCombatState()
                                {
                                    CheckState = EntityCombatStateComp.ECombatState.InCombat,
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

                    //{
                    //    var aAction = new AIActionReturnInterrupt()
                    //    {
                    //    };

                    //    config.Actions.Add(aAction);
                    //}


                    {
                        var aAction = new AIActionTryRecovery()
                        {
                        };

                        config.Actions.Add(aAction);
                    }
                    {
                        var aAction = new AIActionRecoveryFromAttract()
                        {
                        };

                        config.Actions.Add(aAction);
                    }
                    


                    {
                        var aAction = new AIActionMoveDoPath()
                        {

                        };
                        config.Actions.Add(aAction);
                    }

                    {
                        var action = new AIActionMoveHunting()
                        {

                        };
                        config.Actions.Add(action);
                    }

                    {
                        var action = new AIActionNormalMoveDaemon()
                        {

                        };
                        config.Actions.Add(action);
                    }

                    {
                        var action = new AIActionMoveInPatrolGroup()
                        {

                        };
                        config.Actions.Add(action);
                    }

                    {
                        var action = new AIActionCombatMain();
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

