using Config.Unit;
using Map.Entity;
using My.Map.Entity;
using My.Map.Fight;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using static Config.Unit.EntitySkillCfg;
using static System.Net.WebRequestMethods;


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
                    cfg.SelectPolicy = FightStruct.ESelectPolicy.PrimaryTarget;


                    _skillDict[cfg.SkillId] = cfg;
                }

                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "default_dash";
                    cfg.MainAbilityId = "default_dash";
                    cfg.CoolDown = 1.0f;
                    cfg.DesiredUseDistance = 999f;
                    cfg.SelectPolicy = FightStruct.ESelectPolicy.PrimaryTarget;
                    cfg.BufferCacheTime = 0.2f;

                    _skillDict[cfg.SkillId] = cfg;
                }

                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "queen_shoot";
                    cfg.MainAbilityId = "queen_shoot";
                    cfg.CoolDown = 5.0f;
                    cfg.DesiredUseDistance = 5.0f;
                    cfg.SelectPolicy = FightStruct.ESelectPolicy.PrimaryTarget;

                    _skillDict[cfg.SkillId] = cfg;
                }
                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "default_push";
                    cfg.MainAbilityId = "default_push";
                    cfg.CoolDown = 0.6f;
                    cfg.DesiredUseDistance = 1.0f;
                    cfg.SelectPolicy = FightStruct.ESelectPolicy.PrimaryTarget;

                    _skillDict[cfg.SkillId] = cfg;
                }

                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "default_dash_slash";
                    cfg.MainAbilityId = "default_dash_slash";
                    cfg.CoolDown = 8.0f;
                    cfg.DesiredUseDistance = 2.0f;
                    cfg.SelectPolicy = FightStruct.ESelectPolicy.PrimaryTarget;

                    _skillDict[cfg.SkillId] = cfg;
                }
                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "basic_aoe_slash";
                    cfg.MainAbilityId = "basic_aoe_slash";
                    cfg.CoolDown = 6.0f;
                    cfg.DesiredUseDistance = 1.0f;
                    cfg.SelectPolicy = FightStruct.ESelectPolicy.PrimaryTarget;

                    _skillDict[cfg.SkillId] = cfg;
                }
                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "default_enemy_qinfan";
                    cfg.MainAbilityId = "default_enemy_qinfan";
                    cfg.CoolDown = 6.0f;
                    cfg.DesiredUseDistance = 0.8f;
                    cfg.SelectPolicy = FightStruct.ESelectPolicy.PrimaryTarget;
                    cfg.NeedHMode = true;
                    _skillDict[cfg.SkillId] = cfg;
                }
                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "guard_attack";
                    cfg.MainAbilityId = "guard_attack";
                    cfg.CoolDown = 1.0f;
                    cfg.DesiredUseDistance = 0.8f;
                    cfg.SelectPolicy = FightStruct.ESelectPolicy.PrimaryTarget;

                    _skillDict[cfg.SkillId] = cfg;
                }
                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "crazy_fire";
                    cfg.MainAbilityId = "crazy_fire";
                    cfg.CoolDown = 21.0f;
                    cfg.DesiredUseDistance = 5f;
                    cfg.Priority = 2000;
                    cfg.SelectPolicy = FightStruct.ESelectPolicy.PrimaryTarget;

                    _skillDict[cfg.SkillId] = cfg;
                }

                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "queen_counter";
                    cfg.MainAbilityId = "queen_counter";
                    cfg.CoolDown = 10.0f;
                    cfg.DesiredUseDistance = 0f;
                    cfg.Priority = 1;
                    //cfg.tar
                    cfg.SelectPolicy = FightStruct.ESelectPolicy.PrimaryTarget;
                    cfg.TargetType = EntitySkillCfg.ETargetType.NoTarget;

                    _skillDict[cfg.SkillId] = cfg;
                }

                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "spawn_attract";
                    cfg.MainAbilityId = "spawn_attract";
                    cfg.CoolDown = 10.0f;
                    cfg.DesiredUseDistance = 5f;
                    cfg.Priority = 1;

                    cfg.StackCount = 5;
                    cfg.TargetType = EntitySkillCfg.ETargetType.Point;
                    cfg.Range1 = 5.0f;

                    _skillDict[cfg.SkillId] = cfg;
                }

                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "queen_pull_all";
                    cfg.MainAbilityId = "queen_pull_all";
                    cfg.CoolDown = 3.0f;
                    cfg.DesiredUseDistance = 5f;
                    cfg.Priority = 1;

                    cfg.TargetType = ETargetType.Circle;
                    cfg.Range1 = 2.5f;
                    cfg.Range2 = 2.0f;

                    _skillDict[cfg.SkillId] = cfg;
                }

                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "evil_child_attach";
                    cfg.MainAbilityId = "evil_child_attach";
                    cfg.CoolDown = 6.0f;
                    cfg.DesiredUseDistance = 1.0f;
                    cfg.Priority = 1;

                    _skillDict[cfg.SkillId] = cfg;
                }

                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "evil_child_insertion";
                    cfg.MainAbilityId = "evil_child_insertion";
                    cfg.CoolDown = 6.0f;
                    cfg.DesiredUseDistance = 0.3f;
                    cfg.Priority = 1;

                    cfg.TargetType = ETargetType.LockTarget;

                    _skillDict[cfg.SkillId] = cfg;
                }

                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "player_normal_defend";
                    cfg.MainAbilityId = "player_normal_defend";
                    cfg.CoolDown = 1.0f;
                    cfg.DesiredUseDistance = 1.0f;
                    cfg.Priority = 1;

                    _skillDict[cfg.SkillId] = cfg;
                }

                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "player_enter_queen";
                    cfg.MainAbilityId = "player_enter_queen";
                    cfg.CoolDown = 1.0f;
                    cfg.DesiredUseDistance = 1.0f;
                    cfg.Priority = 1;

                    cfg.TargetType = ETargetType.Self;
                    cfg.CastConditions.Add(new CastCondition() { Type = ECastConditionType.NoQueenMode});
                    _skillDict[cfg.SkillId] = cfg;
                }
                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "player_quit_queen";
                    cfg.MainAbilityId = "player_quit_queen";

                    cfg.TargetType = ETargetType.Self;
                    cfg.CastConditions.Add(new CastCondition() { Type = ECastConditionType.QueenMode });
                    _skillDict[cfg.SkillId] = cfg;
                }

                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "player_ziwei";
                    cfg.MainAbilityId = "player_ziwei";

                    cfg.TargetType = ETargetType.Self;
                    cfg.CastConditions.Add(new CastCondition() { Type = ECastConditionType.NoQueenMode });
                    _skillDict[cfg.SkillId] = cfg;
                }

                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "h_mode_execute";
                    cfg.MainAbilityId = "h_mode_execute";
                    cfg.CoolDown = 10.0f;

                    cfg.TargetType = ETargetType.LockTarget;
                    _skillDict[cfg.SkillId] = cfg;
                }


                {
                    var cfg = new EntitySkillCfg();
                    cfg.SkillId = "player_dark_dance";
                    cfg.MainAbilityId = "player_dark_dance";
                    cfg.CoolDown = 20.0f;

                    cfg.TargetType = ETargetType.Self;
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
                    var ab = CreateDefaultInteractProgress();
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
                    var ab = CreateCrazyFireAbility();
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
                {
                    var ab = CreateDefaultPushAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateDefaultGuardAttack1();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateDefaultGuardAttack2();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateQueenCounter();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateQueenCounterPayback();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreatePullAllEnemy();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateLittleNpcAttachAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateHitAttachAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerNormalDefend();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerEnterQueenMode();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerZiWei();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerHModeExecute();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateNpcHModeSJ();
                    _abilityDict[ab.Id] = ab;
                }

                // 插入
                {
                    var ab = CreateEvilChildInsertionAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateNpcCloseKaiyou();
                    _abilityDict[ab.Id] = ab;
                }


                {
                    var ab = CreatePlayerDarkDance();
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

        private static MapAbilitySpecConfig CreateDefaultInteractProgress()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();
            spec.Id = "default_interact";
            spec.TypeTag = AbilityTypeTag.Interaction;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                WithProgress = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    ReferName = "InteractTime"
                },
            };

            var newEffect = new MapAbilityEffectDefaultInteractCfg()
            {
                InteractEntityId = new()
                {
                    ValType = EOneVariatyType.Int,
                    ReferName = "EntityId"
                },
                TriggerId = new()
                {
                    ValType = EOneVariatyType.Int,
                    ReferName = "SelectId"
                },
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

            spec.Id = "queen_shoot";
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
                BulletId = "ProjectileOne",
                MotionData = new LinearMotionData()
                {
                    speed = 9f,
                },

                lockViewAngle = false,

                SpawnPos = MapAbilityEffectSpawnBulletCfg.ESpawnPos.TriggerPos,
                SpawnDir = MapAbilityEffectSpawnBulletCfg.ESpawnDir.ToCastPos,

                lifeTime = 0.6f,


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
                                ResourceId  = AttrIdConsts.UnitHVal,
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

        private static MapAbilitySpecConfig CreateCrazyFireAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "crazy_fire";
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

            {
                var newEffect = new MapAbilityEffectSpawnBulletCfg()
                {
                    BulletId = "ProjectileGroundFire",
                    PendingTime = 3.0f,
                    MotionData = new InstanceMotionData()
                    {
                        prepareTime = 5f,

                        homingSterRate = 1.0f,

                        homingConstantSpeed = true,
                        speed = 2,
                        homingOverrideSpeed = 5,
                    },

                    SpawnPos = MapAbilityEffectSpawnBulletCfg.ESpawnPos.TriggerPos,
                    SpawnDir = MapAbilityEffectSpawnBulletCfg.ESpawnDir.Random,

                    showRangeWarn = true,

                    isHoming = true,

                    lifeTime = 999f,
                    BulletShape = new FightStruct.Shape()
                    {
                        Type = FightStruct.EShapeType.Circle,
                        Radius = 0.8f,
                    },

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
                                ResourceId  = AttrIdConsts.UnitHVal,
                                AddValue = 50_000,
                            }
                        }
                    }
                },
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            }
            {
                var newEffect = new MapAbilityEffectSpawnBulletCfg()
                {
                    BulletId = "ProjectileGroundFire",
                    PendingTime = 5.0f,
                    MotionData = new InstanceMotionData()
                    {
                        prepareTime = 5f,

                        homingSterRate = 1.0f,
                        homingConstantSpeed = true,
                        speed = 2,

                        homingOverrideSpeed = 5,
                    },
                    showRangeWarn = true,
                    SpawnPos = MapAbilityEffectSpawnBulletCfg.ESpawnPos.TriggerPos,
                    SpawnDir = MapAbilityEffectSpawnBulletCfg.ESpawnDir.Random,
                    isHoming = true,


                    lifeTime = 999f,
                    BulletShape = new FightStruct.Shape()
                    {
                        Type = FightStruct.EShapeType.Circle,
                        Radius = 0.8f,
                    },

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
                                ResourceId  = AttrIdConsts.UnitHVal,
                                AddValue = 50000,
                            }
                        }
                    }
                },
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            }
            {
                var newEffect = new MapAbilityEffectSpawnBulletCfg()
                {
                    BulletId = "ProjectileGroundFire",
                    PendingTime = 7.0f,
                    MotionData = new InstanceMotionData()
                    {
                        prepareTime = 5f,

                        homingSterRate = 1.0f,
                        homingConstantSpeed = true,
                        speed = 2,

                        homingOverrideSpeed = 5,
                    },
                    showRangeWarn = true,
                    isHoming = true,

                    SpawnPos = MapAbilityEffectSpawnBulletCfg.ESpawnPos.TriggerPos,
                    SpawnDir = MapAbilityEffectSpawnBulletCfg.ESpawnDir.Random,
                    lifeTime = 999f,
                    BulletShape = new FightStruct.Shape()
                    {
                        Type = FightStruct.EShapeType.Circle,
                        Radius = 0.8f,
                    },

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
                                ResourceId  = AttrIdConsts.UnitHVal,
                                AddValue = 50000,
                            }
                        }
                    }
                },
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            }
            {
                var newEffect = new MapAbilityEffectSpawnBulletCfg()
                {
                    BulletId = "ProjectileGroundFire",
                    PendingTime = 9.0f,
                    MotionData = new InstanceMotionData()
                    {
                        prepareTime = 5f,

                        homingSterRate = 1.0f,
                        homingConstantSpeed = true,
                        speed = 2,
                        homingOverrideSpeed = 5,
                    },
                    showRangeWarn = true,
                    isHoming = true,


                    SpawnPos = MapAbilityEffectSpawnBulletCfg.ESpawnPos.TriggerPos,
                    SpawnDir = MapAbilityEffectSpawnBulletCfg.ESpawnDir.Random,
                    lifeTime = 999f,
                    BulletShape = new FightStruct.Shape()
                    { 
                        Type = FightStruct.EShapeType.Circle,
                        Radius = 0.8f,
                    },

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
                                    ResourceId  = AttrIdConsts.UnitHVal,
                                    AddValue = 50000,
                                }
                            }
                        }
                    },
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            }
            {
                var newEffect = new MapAbilityEffectSpawnBulletCfg()
                {
                    BulletId = "ProjectileGroundFire",
                    PendingTime = 11.0f,
                    MotionData = new InstanceMotionData()
                    {
                        prepareTime = 5f,

                        homingSterRate = 1.0f,
                        homingConstantSpeed = true,
                        speed = 2,
                        homingOverrideSpeed = 5,
                    },

                    showRangeWarn = true,
                    isHoming = true,

                    SpawnPos = MapAbilityEffectSpawnBulletCfg.ESpawnPos.TriggerPos,
                    SpawnDir = MapAbilityEffectSpawnBulletCfg.ESpawnDir.Random,
                    lifeTime = 999f,
                    BulletShape = new FightStruct.Shape()
                    {
                        Type = FightStruct.EShapeType.Circle,
                        Radius = 0.8f,
                    },

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
                                ResourceId  = AttrIdConsts.UnitHVal,
                                AddValue = 50000,
                            }
                        }
                    }
                },
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            }
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
                    KnockBackForce = 0.3f,
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
            //    lifeTime = 0.6f,0.6f
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
                        IsEnmity = true,
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
                    ShapeInfo = new FightStruct.Shape()
                    {
                        Type = FightStruct.EShapeType.Square,
                        Width = 0.8f,
                        Length = 1.2f,
                    },
                },

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
                },
            };

            var hitCfg = new MapAbilityEffectHitBoxCfg();
            hitCfg.CenterPosType = 0;
            hitCfg.EffectType = EAbilityEffectType.HitBox;
            hitCfg.Shape = MapAbilityEffectHitBoxCfg.EShape.Square;
            hitCfg.Width = 0.9f;
            hitCfg.Length = 1.2f;
            hitCfg.TargetEntityType = EEntityType.Player;

            {

                var failEffect = new MapAbilityEffectCostResourceCfg();
                failEffect.ResourceId = AttrIdConsts.HP;
                failEffect.CostValue = 5;
                failEffect.IsEnmity = true;

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
            //spec.TargetType = MapAbilitySpecConfig.ETargetType.Point;
            //spec.Range1 = 5.0f;

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
            //spec.TargetType = MapAbilitySpecConfig.ETargetType.LockTarget;
            //spec.Range1 = 2.5f;

            var preparePhase = new MapAbilityPhase()
            {
                PhaseName = "Prepare",
                LockMovement = true,

                ShowRangePreview = true,
                PreviewIntent = new()
                {
                    FaceOffset = new Vector2(0.3f, 0),
                    ShapeInfo = new FightStruct.Shape()
                    {
                        Type = FightStruct.EShapeType.Square,
                        Width = 0.9f,
                        Length = 2.5f,
                    },
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

                    IsLockTarget = true,
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

                var hitEffect = new MapAbilityEffectUseWeaponCfg()
                {
                    WeaponName = "Charge",
                    Duration = 0.45f,
                    OnHitEffects = new()
                    {

                        new MapAbilityEffectApplyDamageCfg()
                        {
                            BaseDamage = 25000,
                            KnockBackForce = 0.3f,
                        },
                    }
                };

                dashingPhase.Events.Add(new PhaseEffectEvent() { Effect = dashEffect, Kind = PhaseEventKind.OnEnter});
                dashingPhase.Events.Add(new PhaseEffectEvent() { Effect = hitEffect, Kind = PhaseEventKind.OnEnter });
            }


            spec.Phases.Add(dashingPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                LockRotation = true,
                InterruptMask = EAbilityInterruptMask.Cast,
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
                    ShapeInfo = new FightStruct.Shape()
                    {
                        Type = FightStruct.EShapeType.Circle,
                        Radius = 1.2f,
                        Length = 2.5f,
                    },
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
                        KnockBackForce = 0.8f,
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
                InterruptMask = EAbilityInterruptMask.Move | EAbilityInterruptMask.Cast,

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
            spec.MaxStepDistance = 0.5f;
            spec.AdjustFaceDir = true;

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
                    RawVal = "0.25"
                },
            };

            var newEffect = new MapAbilityEffectUseWeaponCfg()
            {
                WeaponName = "Weapon01",
                Duration = 0.32f,
                OnHitEffects = new()
                {

                    new MapAbilityEffectApplyDamageCfg()
                    {
                        BaseDamage = 25000,
                        KnockBackForce = 0.6f,
                    },
                }
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(mainPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                InterruptMask = EAbilityInterruptMask.Move | EAbilityInterruptMask.Cast,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.25"
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
            spec.MaxStepDistance = 0.5f;
            spec.AdjustFaceDir = true;

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
                Duration = 0.32f,
                OnHitEffects = new()
                {

                    new MapAbilityEffectApplyDamageCfg()
                    {
                        BaseDamage = 25000,
                        KnockBackForce = 0.6f,
                    },
                }
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(mainPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                InterruptMask = EAbilityInterruptMask.Cast | EAbilityInterruptMask.Move,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.2"
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
            spec.MaxStepDistance = 0.5f;
            spec.AdjustFaceDir = true;

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
                Duration = 0.4f,
                OnHitEffects = new()
                {

                    new MapAbilityEffectApplyDamageCfg()
                    {
                        BaseDamage = 25000,
                        KnockBackForce = 1f,
                    },
                }
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(mainPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                InterruptMask = EAbilityInterruptMask.Cast | EAbilityInterruptMask.Move,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.25"
                },
            };
            spec.Phases.Add(postPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultPushAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "default_push";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.MaxStepDistance = 0f;
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
                    RawVal = "0.15"
                },
            });

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockMovement = true,
                LockRotation = true,
                ImmuneKnock = true,
                AnimTag = "Push",
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.2"
                },
            };

            var newEffect = new MapAbilityEffectUseWeaponCfg()
            {
                WeaponName = "Push",
                Duration = 0.24f,
                OnHitEffects = new()
                {
                    //new MapAbilityEffectApplyDamageCfg()
                    //{
                    //    BaseDamage = 1000,
                    //    KnockBackForce = 0.6f,
                    //},
                    new MapAbilityEffectAddResourceCfg()
                    {
                        ResourceId = AttrIdConsts.UnitHVal,
                        AddValue = 1000,
                        IsEnmity = false,
                    },
                    new MapAbilityEffectCostResourceCfg()
                    {
                        ResourceId = AttrIdConsts.HP,
                        CostValue = 1,
                        IsEnmity = false,
                    },
                    new MapFightEffectKnockBackCfg()
                    {
                        KnockBackForce = 0.6f,
                        DirType = MapFightEffectKnockBackCfg.EKnockBackType.CastDir,
                    }
                }
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });

            spec.Phases.Add(mainPhase);
            return spec;
        }


        private static MapAbilitySpecConfig CreateDefaultGuardAttack1()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "guard_attack_01";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.MaxStepDistance = 0.4f;

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
                WeaponName = "Hit_01",
                Duration = 0.12f,
                OnHitEffects = new()
                {

                    new MapAbilityEffectApplyDamageCfg()
                    {
                        BaseDamage = 25000,
                        KnockBackForce = 0.3f,
                    },
                }
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(mainPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                InterruptMask = EAbilityInterruptMask.Cast | EAbilityInterruptMask.Move,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.25"
                },
            };
            spec.Phases.Add(postPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultGuardAttack2()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "guard_attack_02";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.MaxStepDistance = 0.4f;

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
                WeaponName = "Hit_02",
                Duration = 0.12f,
                OnHitEffects = new()
                {

                    new MapAbilityEffectApplyDamageCfg()
                    {
                        BaseDamage = 25000,
                        KnockBackForce = 0.3f,
                    },
                }
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(mainPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                InterruptMask = EAbilityInterruptMask.Cast | EAbilityInterruptMask.Move,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.1"
                },
            };
            spec.Phases.Add(postPhase);
            return spec;
        }


        private static MapAbilitySpecConfig CreateQueenCounter()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "queen_counter";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Prepare",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.6"
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
                    RawVal = "2"
                },
            };

            var effect = new MapAbilityEffectAddBuffCfg()
            {
                BuffId = "queen_countering",
                TargetType = 1,
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(mainPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                InterruptMask = EAbilityInterruptMask.Cast | EAbilityInterruptMask.Move,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.2"
                },
            };
            spec.Phases.Add(postPhase);
            return spec;
        }

        /// <summary>
        /// 描述已经锁定施法目标后 对目标进行一次反击
        /// </summary>
        /// <returns></returns>
        private static MapAbilitySpecConfig CreateQueenCounterPayback()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "queen_counter_payback";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.MaxStepDistance = 0.4f;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Prepare",
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
                    RawVal = "1"
                },
            };

            {
                //var effect = new MapAbilityEffectAddBuffCfg()
                //{
                //    BuffId = "queen_countering",
                //    TargetType = 0,
                //};
                //mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effect, Kind = PhaseEventKind.OnEnter });


            }

            {
                var effect = new MapAbilityEffectTeleportToCfg()
                {
                    
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effect, Kind = PhaseEventKind.OnEnter });
            }

            spec.Phases.Add(mainPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",

                InterruptMask = EAbilityInterruptMask.Move | EAbilityInterruptMask.Cast,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
                },
            };
            spec.Phases.Add(postPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreatePullAllEnemy()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "queen_pull_all";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Prepare",
                LockMovement = true,
                LockRotation = true,
                ImmuneKnock = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.8"
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
                    RawVal = "1.2"
                },
            };


            {
                // 怎么弄hitbox
                var effect = new MapAbilityEffectHitBoxCfg()
                {
                    Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                    Radius = 2.0f,

                    CampFilterType = ECampFilterType.NotSelf,
                    IncludeEnmity = true,
                    CenterPosType = 1,

                    OnHitEffects = new()
                    {
                        new MapAbilityEffectControlledMoveCfg()
                        {
                            TargetType = 0,
                            UseCastVec = true,
                            FixedDuration = 0.45f,
                            IsEnmity = true,
                            ControlForce = 10.0f,
                        }
                    },
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effect, Kind = PhaseEventKind.OnEnter });
            }

            spec.Phases.Add(mainPhase);

            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",

                InterruptMask = EAbilityInterruptMask.Move | EAbilityInterruptMask.Cast,

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3"
                },
            };
            spec.Phases.Add(postPhase);
            return spec;
        }


        private static MapAbilitySpecConfig CreateLittleNpcAttachAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "evil_child_attach";
            spec.TypeTag = AbilityTypeTag.Combat;
            //spec.MaxStepDistance = 0.5f;
            //spec.CoolDown = 0.2f;
            //spec.DesiredUseDistance = 0.5f;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                EnterDebugString = "?",

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "1.0"
                },
            });

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockMovement = true,
                LockRotation = true,
                ImmuneKnock = true,
                AnimTag = "Push",
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.6"
                },
            };

            {
                var dashEffect = new MapAbilityEffectDashStartCfg()
                {
                    IsFixPointMode = true,
                    DashSpeed = 6f,
                    IsLockTarget = true,
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
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = dashEffect, Kind = PhaseEventKind.OnEnter });
            }

            {
                var hitEffect = new MapAbilityEffectUseWeaponCfg()
                {
                    WeaponName = "Catch",
                    Duration = 0.4f,
                    MaxHit = 1,
                    OnHitEffects = new()
                    {
                        new MapAbilityEffectConvertAttachCfg()
                        {
                            AttachId = "evil_child_attach",
                        },
                    }
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = hitEffect, Kind = PhaseEventKind.OnEnter });
            }
            
            spec.Phases.Add(mainPhase);
            return spec;
        }
    
        
        private static MapAbilitySpecConfig CreateEvilChildInsertionAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "evil_child_insertion";
            spec.TypeTag = AbilityTypeTag.Combat;
            //spec.MaxStepDistance = 0.5f;
            //spec.CoolDown = 0.2f;
            //spec.DesiredUseDistance = 0.5f;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                EnterDebugString = "I",

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.8"
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
                    RawVal = "0.6"
                },
            };


            {
                var hitEffect = new MapAbilityEffectUseWeaponCfg()
                {
                    WeaponName = "Give",
                    Duration = 0.4f,
                    MaxHit = 1,
                    OnHitEffects = new()
                    {
                        new MapAbilityEffectGiveItemCfg()
                        {
                            ItemId = "insertion_maoqiu",
                            Count = 1,
                            SpecificBagId = 1,
                        },
                    }
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = hitEffect, Kind = PhaseEventKind.OnEnter });
            }

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateHitAttachAbility()
        {

            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "hit_attach";
            spec.TypeTag = AbilityTypeTag.Utility;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockMovement = true,
                LockRotation = true,
                WithProgress = true,
                AnimTag = "挣扎",
                InterruptMask = EAbilityInterruptMask.Move | EAbilityInterruptMask.Cast,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "1.5"
                },
            };

            {
                var dashEffect = new MapAbilityEffectHitAttachCfg()
                {
                    HitHp = 1,
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = dashEffect, Kind = PhaseEventKind.OnExit });
            }

            spec.Phases.Add(mainPhase);
            return spec;
            
        }
    
        
        private static MapAbilitySpecConfig CreatePlayerNormalDefend()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_normal_defend";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
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
                LockRotation = true,

                PhaseBuff = new List<string>() { "player_normal_defend_on" },

                InterruptMask = EAbilityInterruptMask.Cast,
                HoldingPhase = true
            };

            {
                //var dashEffect = new MapAbilityEffectHitAttachCfg()
                //{
                //    HitHp = 1,
                //};
                //mainPhase.Events.Add(new PhaseEffectEvent() { Effect = dashEffect, Kind = PhaseEventKind.OnExit });
            }

            spec.Phases.Add(mainPhase);
            return spec;
            
        }


        private static MapAbilitySpecConfig CreatePlayerEnterQueenMode()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_enter_queen";
            spec.TypeTag = AbilityTypeTag.Combat;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Prepare",
                LockRotation = true,
                LockMovement = true,
                ImmuneKnock = true,
                EnterDebugString = "Queen!",

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
                },
            };


            {
                var effect = new MapFightEffectQueueModeCfg()
                {
                    InEnter = true,
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effect, Kind = PhaseEventKind.OnExit });
            }

            {
                var effect = new MapAbilityEffectAddBuffCfg()
                {
                    TargetType = 1,
                    BuffId = "queen_mode_on",
                    Layer = 1,
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effect, Kind = PhaseEventKind.OnExit });
            }

            spec.Phases.Add(mainPhase);

            return spec;

        }



        private static MapAbilitySpecConfig CreatePlayerZiWei()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_ziwei";
            spec.TypeTag = AbilityTypeTag.Utility;

            var prePhase = new MapAbilityPhase()
            {
                PhaseName = "Prepare",
                LockRotation = true,
                LockMovement = true,
                ImmuneKnock = true,
                AnimTag = "ziwei",
                EnterDebugString = "开抠",

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "1.0"
                },
            };

            spec.Phases.Add(prePhase);


            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockRotation = true,
                ImmuneKnock = true,

                InterruptMask = EAbilityInterruptMask.Move | EAbilityInterruptMask.Hit | EAbilityInterruptMask.Cast,

                AnimTag = "ziwei",


                PhaseBuff = new() { "player_ziwei" },

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "99.0"
                },
            };

            {
                //mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effect, Kind = PhaseEventKind.OnExit });
            }

            spec.Phases.Add(mainPhase);

            return spec;

        }


        private static MapAbilitySpecConfig CreatePlayerHModeExecute()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "h_mode_execute";
            spec.TypeTag = AbilityTypeTag.Utility;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockRotation = true,
                LockMovement = true,
                ImmuneKnock = true,

                PhaseBuff = new() { "super_armor", "phase_move" },

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "1.5"
                },
            };

            {

                var addStunEffect = new MapAbilityEffectAddBuffCfg()
                {
                    BuffId = "force_stun",
                    Duration = 1.5f,
                    Layer = 1,
                    TargetType = 0,
                };

                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = addStunEffect, Kind = PhaseEventKind.OnEnter });
            }
            {

                var closeToEffect = new MapFightEffectSpecialMoveToCfg()
                {
                    Duration = 0.3f,
                };

                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = closeToEffect, Kind = PhaseEventKind.OnEnter });
            }

            {

                var addHEffect = new MapAbilityEffectAddResourceCfg()
                {
                    ResourceId = AttrIdConsts.UnitHVal,
                    AddValue = 100_000,
                    IsEnmity = false,
                };

                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = addHEffect, Kind = PhaseEventKind.OnExit });
            }

            spec.Phases.Add(mainPhase);

            return spec;
        }
    
    
        private static MapAbilitySpecConfig CreateNpcHModeSJ()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "unit_h_mode_sj";
            spec.TypeTag = AbilityTypeTag.Utility;
            spec.IsDodge = true;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Execute",
                LockRotation = true,
                LockMovement = true,
                ImmuneKnock = true,
                ForbidDodge = true,

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.8"
                },
            };
            spec.Phases.Add(mainPhase);

            {
                var blurEffect = new MapFightEffectHModeBlurtCfg();

                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = blurEffect, Kind = PhaseEventKind.OnExit });
            }
            return spec;
        }


        private static MapAbilitySpecConfig CreateNpcCloseKaiyou()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "close_kaiyou";
            spec.TypeTag = AbilityTypeTag.Utility;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Execute",
                LockRotation = true,
                LockMovement = true,
                ImmuneKnock = true,

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.8"
                },
            };
            spec.Phases.Add(mainPhase);

            {
                var closeupCfg = new MapFightEffectShowCloseupWindowCfg();
                closeupCfg.WindowType = "kaiyou";
                closeupCfg.Duration = 2.0f;

                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = closeupCfg, Kind = PhaseEventKind.OnEnter });
            }

            {
                var addBuff = new MapAbilityEffectAddBuffCfg();
                addBuff.BuffId = "immune_kaiyou";
                addBuff.Duration = 10.0f;

                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = addBuff, Kind = PhaseEventKind.OnEnter });
            }

            {
                var addEffect1 = new MapAbilityEffectAddResourceCfg();
                addEffect1.ResourceId = AttrIdConsts.PlayerPleasure;
                addEffect1.AddValue = 3000;
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = addEffect1, Kind = PhaseEventKind.OnEnter });
            }
            return spec;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static MapAbilitySpecConfig CreatePlayerDarkDance()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_dark_dance";
            spec.TypeTag = AbilityTypeTag.Utility;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Execute",
                LockRotation = true,
                LockMovement = true,
                ImmuneKnock = true,

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3"
                },
            };

            {
                var buffEffect = new MapAbilityEffectAddBuffCfg();
                buffEffect.TargetType = 1;
                buffEffect.BuffId = "dark_dance";
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = buffEffect, Kind = PhaseEventKind.OnExit });
            }

            spec.Phases.Add(mainPhase);
            return spec;
        }
    }


}
