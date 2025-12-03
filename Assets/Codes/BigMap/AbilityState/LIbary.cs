using Config.Unit;
using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;


namespace My.Map.Entity
{

    public static class SkillLibrary
    {
        public static Dictionary<string, EntitySkillCfg> _skillDict = null;

        public static EntitySkillCfg GetSkillConfig(string skillName)
        {
            if (_skillDict == null)
            {
                _skillDict = new();

                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "queen_attack";
                    cfg.MainAbilityId = "queen_attack_01";
                    cfg.CoolDown = 0.2f;
                    cfg.DesiredUseDistance = 0.8f;

                    _skillDict[cfg.SkillId] = cfg;
                }


            }

            _skillDict.TryGetValue(skillName, out var skillCfg);
            return skillCfg;
        }
    }


    public static class AbilityLibrary
    {

        public static Dictionary<string, MapAbilitySpecConfig> _abilityDict = null;

        public static MapAbilitySpecConfig GetAbilityConfig(string abilityName)
        {
            if(_abilityDict == null)
            {
                _abilityDict = new();

                {
                    var ab = CreateDefaultUnlockLootPoint();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateDefaultUseLootPoint();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateDefaultUseItem();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateDefaultDash();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateDefaultShootAbility();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateDefaultUseWeaponAbility();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateOrRefreshZhaQu();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateDeepZhaQu();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateDefaultMonsterAttack();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateDefaultEnemyQinfan();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateFixClothesAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateSpawnAttractAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateDefaultDashSlash();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateBasicAoeSlash();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateQueenAttackAbility1();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateQueenAttackAbility2();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateQueenAttackAbility3();
                    _abilityDict[ab.Id] = ab;
                }
            }

            _abilityDict.TryGetValue(abilityName, out var abConfig);
            return abConfig;
        }


        private static MapAbilitySpecConfig CreateDefaultUnlockLootPoint()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "unlock_loot_point";
            spec.TypeTag = AbilityTypeTag.Interaction;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                WithProgress = true,
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
                },
            };

            var newEffect = new MapAbilityEffectUnlockLootPoint();
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnExit });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultUseLootPoint()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "use_loot_point";
            spec.TypeTag = AbilityTypeTag.Interaction;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                WithProgress = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    ReferName = "PhaseExecutingTime"
                },
            };

            var newEffect = new MapAbilityEffectUseLootPoint();
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnExit });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultUseItem()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();
            spec.Id = "use_item";
            spec.TypeTag = AbilityTypeTag.Interaction;


            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                WithProgress = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    ReferName = "PhaseExecutingTime"
                },
            };

            var newEffect = new MapAbilityEffectUseItemCfg()
            {
                UseItemId = new()
                {
                    ValType = EOneVariatyType.String,
                    ReferName = "ItemId"
                }
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnExit });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultDash()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();
            spec.Id = "default_dash";
            spec.TypeTag = AbilityTypeTag.Combat;
            //spec.CoolDown = 1.0f;

            spec.CauseAttract = true;
            spec.AttractPower = 10.0f;
            spec.AttractRange = 2.0f;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                PhaseBuff = new() { "phase_move"},
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3"
                },
            };

            var newEffect = new MapAbilityEffectDashStartCfg()
            {
                IsTimeMode = true,
                DashDuration = 0.3f,
                DashSpeed = 8f,
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultShootAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_shoot";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                WithProgress = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "1"
                },
            });

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockMovement = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.2"
                },
            };

            var newEffect = new MapAbilityEffectSpawnBulletCfg()
            {
                targetType = MapAbilityEffectSpawnBulletCfg.ETargetType.Dir,
                motionType = EMotionType.Linear,
                lifeTime = 0.6f,
                speed = 9f,
                TriggerOnCollide = true,
                TriggerOnLifeEnd = true,

                HitEffects = new()
                {
                    new MapAbilityEffectHitBoxCfg()
                    {
                        Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                        Radius = 1.0f,
                        CampFilterType = ECampFilterType.NotSelf,

                        OnHitEffects = new()
                        {
                            new MapAbilityEffectAddResourceCfg()
                            {
                                ResourceId  = AttrIdConsts.UnitEnterHVal,
                                AddValue = 50000,
                            }
                        }
                    }
                },
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });

            //spec.Phases.Add(new MapAbilityPhase()
            //{
            //    PhaseName = "Post",
            //    DurationValue = new()
            //    {
            //        ValType = EOneVariatyType.Float,
            //        RawVal = "0.1"
            //    },
            //});

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultUseWeaponAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "default_weapon";
            spec.TypeTag = AbilityTypeTag.Combat;
            //spec.CoolDown = 0.2f;
            //spec.DesiredUseDistance = 0.5f;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.1"
                },
            });

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockMovement = true,
                LockRotation = true,
                ImmuneKnock = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.15"
                },
            };

            var newEffect = new MapAbilityEffectUseWeaponCfg()
            {
                WeaponName = "Weapon01",
                Duration = 0.12f,
                OnHitEffects = new()
            {

                new MapAbilityEffectApplyDamageCfg()
                {
                    BaseDamage = 25000,
                    KnockBackForce = 1.6f,
                },
                //new MapAbilityEffectCostResourceCfg()
                //{
                //    ResourceId = AttrIdConsts.HP,
                //    CostValue = 25,
                //    Flags = 1,
                //},
            }
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });


            //spec.Phases.Add(new MapAbilityPhase()
            //{
            //    PhaseName = "Post",
            //    DurationValue = new()
            //    {
            //        ValType = EOneVariatyType.Float,
            //        RawVal = "0.1"
            //    },
            //});

            spec.Phases.Add(mainPhase);
            return spec;
        }


        private static MapAbilitySpecConfig CreateOrRefreshZhaQu()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "zhaqu";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.1"
                },
            });

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockRotation = true,
                LockMovement = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3"
                },
            };

            //var newEffectGive = new MapAbilityEffectAddBuffCfg()
            //{
            //    BuffId = "beizha",
            //    Layer = 1,
            //    Duration = -1,
            //    TargetType = 0,
            //};
            //mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffectGive, Kind = PhaseEventKind.OnEnter });


            {
                var throwCfg = new MapAbilityEffectThrowStartCfg()
                {
                    Priority = 999,
                    Duration = 2.0f,
                    ThrowMainBuffId = "beizha",
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = throwCfg, Kind = PhaseEventKind.OnEnter });
            }


            //var newEffectSelf = new MapAbilityEffectAddBuffCfg()
            //{
            //    targetType = MapAbilityEffectSpawnBulletCfg.ETargetType.Dir,
            //    motionType = EMotionType.Linear,
            //    lifeTime = 0.6f,
            //    speed = 9f,
            //};
            //mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });


            //var effectRemoveZha = new MapAbilityEffectRemoveBuffCfg()
            //{
            //    BuffId = "beizha",
            //    Layer = 1,
            //    TargetType = 0,
            //};
            //mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effectRemoveZha, Kind = PhaseEventKind.OnExit });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDeepZhaQu()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "deep_zhaqu";
            spec.TypeTag = AbilityTypeTag.Interaction;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Preparing",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
                },
            };

            {
                var deepZhaquCfg = new MapAbilityEffectDeepZhaquCfg()
                {

                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = deepZhaquCfg, Kind = PhaseEventKind.OnExit });
            }

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultMonsterAttack()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "attack";
            spec.TypeTag = AbilityTypeTag.Combat;
            //spec.DesiredUseDistance = 0.5f;

            //spec.CoolDown = 2.0f;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.4"
                },
            });

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3"
                },
            };

            var newEffect = new MapAbilityEffectHitBoxCfg()
            {
                Shape = MapAbilityEffectHitBoxCfg.EShape.Square,
                TargetEntityType = EEntityType.Player,
                Width = 1.2f,
                Length = 1f,

                OnHitEffects = new()
                {
                    new MapAbilityEffectCostResourceCfg()
                    {
                        ResourceId  = AttrIdConsts.HP,
                        CostValue = 5,
                        Flags = 1,
                    }
                }
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultEnemyQinfan()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "default_enemy_qinfan";
            spec.TypeTag = AbilityTypeTag.HMode;
            //spec.CoolDown = 6.0f;
            //spec.DesiredUseDistance = 0.8f;
            //spec.Priority = 100;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                EnterDebugString = "准备抓取",
                PhaseBuff = new() { "jian_su_self" },

                ShowRangePreview = true,
                PreviewIntent = new()
                {
                    FaceOffset = new Vector2(0.3f, 0),
                    IsCircle = false,
                    RangeWidth = 0.8f,
                    RangeLen = 1.2f,
                },

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
                },
            };

            var hitCfg = new MapAbilityEffectHitBoxCfg();
            hitCfg.EffectType = EAbilityEffectType.HitBox;
            hitCfg.Shape = MapAbilityEffectHitBoxCfg.EShape.Square;
            hitCfg.Width = 0.9f;
            hitCfg.Length = 1.2f;
            hitCfg.TargetEntityType = EEntityType.Player;

            {

                var failEffect = new MapAbilityEffectCostResourceCfg();
                failEffect.ResourceId = AttrIdConsts.HP;
                failEffect.CostValue = 5;
                failEffect.Flags = 1;

                var throwEffect = new MapAbilityEffectThrowStartCfg();

                throwEffect.ThrowMainBuffId = "be_fcked";
                throwEffect.Priority = 1;
                throwEffect.Duration = 20f;
                throwEffect.ThrowFailEffect = failEffect;


                hitCfg.OnHitEffects = new() { throwEffect };
            }

            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = hitCfg, Kind = PhaseEventKind.OnExit });


            spec.Phases.Add(mainPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.9"
                },
            };

            spec.Phases.Add(postPhase);

            return spec;
        }

        private static MapAbilitySpecConfig CreateFixClothesAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "fix_clothes";
            spec.TypeTag = AbilityTypeTag.Utility;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Prepare",
                LockMovement = true,
                LockRotation = true,
                WithProgress = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "2"
                },
            };


            var effect = new MapAbilityEffectAddResourceCfg()
            {
                ResourceId = AttrIdConsts.PlayerClothes,
                AddValue = 80000,
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effect, Kind = PhaseEventKind.OnExit });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateSpawnAttractAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "spawn_attract";
            spec.TypeTag = AbilityTypeTag.Utility;
            //spec.CoolDown = 10.0f;
            //spec.StackCount = 5;
            spec.TargetType = MapAbilitySpecConfig.ETargetType.Point;
            spec.Range1 = 5.0f;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Main",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3"
                },
            };


            var effect = new MapAbilityEffectSpawnEntityCfg()
            {
                EntityType = EEntityType.AttractPoint,
                CfgId = "attract_01",
                LifeTime = 10.0f,
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effect, Kind = PhaseEventKind.OnExit });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultDashSlash()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "default_dash_slash";
            spec.TypeTag = AbilityTypeTag.Combat;
            //spec.CoolDown = 8.0f;
            //spec.DesiredUseDistance = 2.0f;
            //spec.Priority = 100;
            spec.TargetType = MapAbilitySpecConfig.ETargetType.Point;
            spec.Range1 = 2.5f;

            var preparePhase = new MapAbilityPhase()
            {
                PhaseName = "Prepare",
                LockMovement = true,

                ShowRangePreview = true,
                PreviewIntent = new()
                {
                    FaceOffset = new Vector2(0.3f, 0),
                    IsCircle = false,
                    RangeWidth = 1.2f,
                    RangeLen = 2.5f,
                },

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
                },
            };

            spec.Phases.Add(preparePhase);


            var dashingPhase = new MapAbilityPhase()
            {
                PhaseName = "Dashing",
                LockMovement = true,
                LockRotation = true,

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
                },
            };

            {
                var dashEffect = new MapAbilityEffectDashStartCfg()
                {
                    IsFixPointMode = true,
                    DashSpeed = 8f,
                    DashOverrideHitRadius = 0.8f,

                    IsTargetDir = true,
                    OnHitEffects = new()
                    {
                        // 提前进入下一phase
                        new MapAbilityEffectNextPhaseCfg()
                        {
                            MatchPhase = "Dashing",
                            MatchSkill = "default_dash_slash"
                        },
                    },
                };

                dashingPhase.Events.Add(new PhaseEffectEvent() { Effect = dashEffect, Kind = PhaseEventKind.OnEnter});
            }


            spec.Phases.Add(dashingPhase);
            

            var slashPhase = new MapAbilityPhase()
            {
                PhaseName = "Slash",
                LockMovement = true,
                LockRotation = true,

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.12"
                },
            };

            {
                var hitEffect = new MapAbilityEffectUseWeaponCfg()
                {
                    WeaponName = "Weapon01",
                    Duration = 0.12f,
                    OnHitEffects = new()
                    {

                        new MapAbilityEffectApplyDamageCfg()
                        {
                            BaseDamage = 25000,
                            KnockBackForce = 1.0f,
                        },
                    }
                };

                slashPhase.Events.Add(new PhaseEffectEvent() { Effect = hitEffect, Kind = PhaseEventKind.OnEnter });
            }

            spec.Phases.Add(slashPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                LockRotation = true,
                InterruptMask = EAbilityInterruptMask.InputCancel,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3"
                },
            };

            spec.Phases.Add(postPhase);

            return spec;
        }

        private static MapAbilitySpecConfig CreateBasicAoeSlash()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "basic_aoe_slash";
            spec.TypeTag = AbilityTypeTag.Combat;
            //spec.CoolDown = 3.0f;
            //spec.DesiredUseDistance = 1.0f;
            //spec.Priority = 20;

            var preparePhase = new MapAbilityPhase()
            {
                PhaseName = "Prepare",
                LockMovement = true,
                LockRotation = true,

                ShowRangePreview = true,
                PreviewIntent = new()
                {
                    FaceOffset = new Vector2(0, 0),
                    IsCircle = true,
                    RangeRadius = 1.2f,
                },

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.8"
                },
            };

            spec.Phases.Add(preparePhase);


            var slashPhase = new MapAbilityPhase()
            {
                PhaseName = "Slash",
                LockMovement = true,
                LockRotation = true,

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.12"
                },
            };


            {
                var hitCfg = new MapAbilityEffectHitBoxCfg();
                hitCfg.EffectType = EAbilityEffectType.HitBox;
                hitCfg.Shape = MapAbilityEffectHitBoxCfg.EShape.Circle;
                hitCfg.Radius = 1.2f;
                hitCfg.TargetEntityType = EEntityType.Player;

                {
                    var dmgEffect = new MapAbilityEffectApplyDamageCfg()
                    {
                        BaseDamage = 25000,
                        KnockBackForce = 1.0f,
                    };

                    hitCfg.OnHitEffects = new() { dmgEffect };
                }

                slashPhase.Events.Add(new PhaseEffectEvent() { Effect = hitCfg, Kind = PhaseEventKind.OnEnter });
                
            }

            spec.Phases.Add(slashPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                LockRotation = true,
                InterruptMask = EAbilityInterruptMask.InputCancel,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3"
                },
            };

            spec.Phases.Add(postPhase);

            return spec;
        }

        private static MapAbilitySpecConfig CreateQueenAttackAbility1()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "queen_attack_01";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.15"
                },
            });

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockMovement = true,
                LockRotation = true,
                ImmuneKnock = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.12"
                },
            };

            var newEffect = new MapAbilityEffectUseWeaponCfg()
            {
                WeaponName = "Weapon01",
                Duration = 0.15f,
                OnHitEffects = new()
                {

                    new MapAbilityEffectApplyDamageCfg()
                    {
                        BaseDamage = 25000,
                        KnockBackForce = 1.6f,
                    },
                }
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(mainPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                CanInputInterrupt = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.1"
                },
            };
            spec.Phases.Add(postPhase);
            return spec;
        }
        private static MapAbilitySpecConfig CreateQueenAttackAbility2()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "queen_attack_02";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.2"
                },
            });

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockMovement = true,
                LockRotation = true,
                ImmuneKnock = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.25"
                },
            };

            var newEffect = new MapAbilityEffectUseWeaponCfg()
            {
                WeaponName = "Weapon02",
                Duration = 0.12f,
                OnHitEffects = new()
                {

                    new MapAbilityEffectApplyDamageCfg()
                    {
                        BaseDamage = 25000,
                        KnockBackForce = 1.6f,
                    },
                }
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(mainPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                CanInputInterrupt = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.1"
                },
            };
            spec.Phases.Add(postPhase);
            return spec;
        }
        private static MapAbilitySpecConfig CreateQueenAttackAbility3()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "queen_attack_03";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3"
                },
            });

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockMovement = true,
                LockRotation = true,
                ImmuneKnock = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.35"
                },
            };

            var newEffect = new MapAbilityEffectUseWeaponCfg()
            {
                WeaponName = "Weapon03",
                Duration = 0.35f,
                OnHitEffects = new()
                {

                    new MapAbilityEffectApplyDamageCfg()
                    {
                        BaseDamage = 25000,
                        KnockBackForce = 1.6f,
                    },
                }
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(mainPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                CanInputInterrupt = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.1"
                },
            };
            spec.Phases.Add(postPhase);
            return spec;
        }
    }

}
