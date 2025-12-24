using Map.Entity.AI.Action;
using Map.Logic;
using My.Map.Entity.AI.Action;
using Newtonsoft.Json;
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

        //public static Dictionary<string, string> _aiConfigJson = null;
        public static Dictionary<string, AIBehaviorConfig> _configs = null;

        public static AIBehaviorConfig Load(string name)
        {
            if (_configs == null)
            {
                _configs = new();

                {
                    var config = ScriptableObject.CreateInstance<AIBehaviorConfig>();
                    config.BehaviorName = "BasicUnit";


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

                        idleState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                                {
                                    new AIDecisionCheckCombatState()
                                    {
                                        CheckState = NpcCombatStateComp.ECombatState.InCombat,
                                    }
                                },
                            TrueState = "Combat",
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

                        attractedState.ActionNames.Add("AttractedMain");


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

                        attractedState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                                {
                                    new AIDecisionCheckCombatState()
                                    {
                                        CheckState = NpcCombatStateComp.ECombatState.InCombat,
                                    }
                                },
                            TrueState = "Combat",
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

                        returnState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                                {
                                    new AIDecisionCheckCombatState()
                                    {
                                        CheckState = NpcCombatStateComp.ECombatState.InCombat,
                                    }
                                },
                            TrueState = "Combat",
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
                                    CheckState = NpcCombatStateComp.ECombatState.CombatRecover
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

                        moveState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                                {
                                    new AIDecisionCheckCombatState()
                                    {
                                        CheckState = NpcCombatStateComp.ECombatState.InCombat,
                                    }
                                },
                            TrueState = "Combat",
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
                        combatState.ActionNames.Add("QuickCloser");


                        combatState.Transitions.Add(new AITransition()
                        {
                            Decisions = new List<AIDecision>()
                            {
                                new AIDecisionCheckCombatState()
                                {
                                    CheckState = NpcCombatStateComp.ECombatState.CombatRecover,
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
                                    CheckState = NpcCombatStateComp.ECombatState.NotCombat,
                                }
                            },
                            TrueState = "Idle",
                        });

                        config.States.Add(combatState);
                    }


                    //{
                    //    config.CommonTransitions.Add(new AITransition()
                    //    {
                    //        Decisions = new List<AIDecision>()
                    //        {
                    //            new AIDecisionCheckCombatState()
                    //            {
                    //                CheckState = NpcCombatStateComp.ECombatState.InCombat,
                    //            }
                    //        },
                    //        TrueState = "Combat",
                    //    });
                    //}

                    {
                        var aAction = new AIActionCfgDoNothing()
                        {
                        };

                        config.Actions.Add(aAction);
                    }

                    {
                        var actionCfg = new AIActionCfgAttractedMove()
                        {
                        };

                        config.Actions.Add(actionCfg);
                    }

                    //{
                    //    var aAction = new AIActionReturnInterrupt()
                    //    {
                    //    };

                    //    config.Actions.Add(aAction);
                    //}


                    {
                        var actionCfg = new AIActionCfgTryRecovery()
                        {
                        };

                        config.Actions.Add(actionCfg);
                    }
                    {
                        var actionCfg = new AIActionCfgRecoveryFromAttract()
                        {
                        };

                        config.Actions.Add(actionCfg);
                    }
                    


                    {
                        var actionCfg = new AIActionCfgMoveDoPath()
                        {

                        };
                        config.Actions.Add(actionCfg);
                    }

                    {
                        var actionCfg = new AIActionCfgMoveHunting()
                        {

                        };
                        config.Actions.Add(actionCfg);
                    }

                    {
                        var actionCfg = new AIActionCfgNormalMoveDaemon()
                        {

                        };
                        config.Actions.Add(actionCfg);
                    }

                    {
                        var actionCfg = new AIActionCfgMoveInPatrolGroup()
                        {

                        };
                        config.Actions.Add(actionCfg);
                    }

                    {
                        var actionCfg = new AIActionCfgCombatMain();
                        config.Actions.Add(actionCfg);
                    }


                    {
                        var actionCfg = new AIActionCfgTryUseSkill();
                        config.Actions.Add(actionCfg);
                    }

                    {
                        var actionCfg = new AIActionCfgDistanceControl()
                        {
                            GoodDistance = 1.5f
                        };
                        config.Actions.Add(actionCfg);
                    }
                    {
                        var actionCfg = new AIActionCfgCombatQuickCloser();
                        config.Actions.Add(actionCfg);
                    }

                    _configs[config.BehaviorName] = config;
                }


            }

            _configs.TryGetValue(name, out var result);
            return result;
        }
    }
}

