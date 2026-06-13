using cfg.demo;
using Map.Entity;
using My.Config;
using My.Map.Entity;
using My.Map.Fight;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using static My.Config.LogicInteractOutput;
using static My.Map.Entity.MapAbilityEffectDashStartCfg;
using static My.Map.Entity.MapAbilitySpecConfig;
using static My.Map.Fight.FightStruct;


namespace My.Map.Entity
{

    public static class SkillLibrary
    {
        static bool _passiveValidated;

        // Luban：demo_tbentityskilldata.json ← Config/Datas/skill.xlsx
        public static EntitySkillData GetSkillConfig(string skillName)
        {
            if (string.IsNullOrEmpty(skillName))
            {
                return null;
            }

            var tables = CfgMgr.Cfgs;
            if (tables == null)
            {
                Debug.LogError("[SkillLibrary] CfgMgr.Cfgs is null. Call CfgMgr.LoadGameConfigs before using skills.");
                return null;
            }

            if (!_passiveValidated)
            {
                ValidatePassiveSkillRows(tables.TbEntitySkillData);
                _passiveValidated = true;
            }

            return tables.TbEntitySkillData.GetOrDefault(skillName);
        }

        public static Dictionary<string, string> CloneAbilityExtraMap(EntitySkillData row)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            if (row?.AbilityExtra == null)
            {
                return d;
            }

            foreach (var p in row.AbilityExtra)
            {
                if (p == null || string.IsNullOrEmpty(p.Key))
                {
                    continue;
                }

                d[p.Key] = p.Val ?? "";
            }

            return d;
        }

        private static void ValidatePassiveSkillRows(TbEntitySkillData table)
        {
            if (table?.DataList == null)
            {
                return;
            }

            foreach (var cfg in table.DataList)
            {
                if (cfg == null || !cfg.IsPassive)
                {
                    continue;
                }

                var buffIds = SkillPassiveBuffUtil.GetPassiveBuffIds(cfg);
                if (buffIds.Count == 0)
                {
                    Debug.LogWarning($"[SkillLibrary] Passive skill '{cfg.SkillId}' has no passive_buff_ids.");
                    continue;
                }

                foreach (var buffId in buffIds)
                {
                    if (BuffLibrary.GetBuffDefinition(buffId) == null)
                    {
                        Debug.LogWarning(
                            $"[SkillLibrary] Passive skill '{cfg.SkillId}' buff '{buffId}' missing in BuffLibrary.");
                    }
                }
            }
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

                // 玩家通用能力
                {
                    var ab = CreatePlayerCommonInteract();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerSneakBackstab();
                    _abilityDict[ab.Id] = ab;
                }

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
                    var ab = CreateShootKnifeAbility();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateGrappleHookAbility();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateUseHumanWeapon();
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
                    var ab = CreateDefaultOrcAttack();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateDefaultHAttack();
                    _abilityDict[ab.Id] = ab;
                }
                


                {
                    var ab = CreateDefaultMonsterAttack();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateDefaultRangeAttack();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateCannonMortarShotAbility();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateDefaultEnemyHarass();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateDefaultEnemyKnockdownPush();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateSupplyItemAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateEnchantItemAbility();
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
                    var ab = CreateFightEffectPlaceStunTrapAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerSummonAllyTurretAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateDebugApplyFearBuffAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateDebugApplyLuredBuffAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerTraceBullet1Ability();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerMortarAcquireAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateItemThrowSmokeGrenadeAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateOrbSkillCastAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateOrbSkillSummonAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerEnterExposeAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerReturnDisguiseAbility();
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
                    var ab = CreateQueenDashAttack();
                    _abilityDict[ab.Id] = ab;
                }


                {
                    var ab = CreateQueenAttackHeavy();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateForceDashPushDown();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateNpcGrapplePushPlayer();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreateDefaultPushAbility();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerFQNormalZiwei();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerFQCrazyZiwei();
                    _abilityDict[ab.Id] = ab;
                }
                
                {
                    var ab = CreatePlayerFQHitPop();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerFQHitBreast();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerFQDashAssult();
                    _abilityDict[ab.Id] = ab;
                }

                {
                    var ab = CreatePlayerSmallStarggering();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreatePlayerPushSurround();
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
                    //var ab = CreatePlayerPutDown();
                    //_abilityDict[ab.Id] = ab;
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
                    var ab = CreatePlayerHModeControl();
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

                {
                    var ab = CreateNpcStaticDoing();
                    _abilityDict[ab.Id] = ab;
                }
                {
                    var ab = CreateChongZhuangAbility();
                    _abilityDict[ab.Id] = ab;
                }
                
            }

            _abilityDict.TryGetValue(abilityName, out var abConfig);
            return abConfig;
        }

        private static MapAbilitySpecConfig CreatePlayerCommonInteract()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_common_interact";
            spec.TypeTag = AbilityTypeTag.Interaction;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Interacting",
                LockMovement = true,
                LockRotation = true,
                EnableVariablePhaseBuff = true, 
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    ReferName = "InteractTime"
                },
            };

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreatePlayerSneakBackstab()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_sneak_backstab";
            spec.TypeTag = AbilityTypeTag.Interaction;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Interacting",
                LockMovement = true,
                LockRotation = true,
                EnableVariablePhaseBuff = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    ReferName = "InteractTime",
                },
            };

            spec.Phases.Add(mainPhase);
            spec.OnCompleteEffects.Add(new MapAbilityEffectSneakBackstabResolveCfg());
            return spec;
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
            spec.Id = "queen_dash";
            spec.TypeTag = AbilityTypeTag.Combat;
            //spec.CoolDown = 1.0f;

            spec.CauseAttract = true;
            spec.AttractPower = 10;
            spec.AttractRange = 2.0f;

            spec.CastType = ECastType.NoTarget;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                PhaseBuff = new() { "phase_move", "phase_perfect_dodge"},
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
                DashMode = EDashMode.FixTime,
                DirMode = EDirMode.InputDir,
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        
        private static MapAbilitySpecConfig CreateShootKnifeAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "shoot_knife";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.CastType = ECastType.NoTarget; // 向面前射出子弹
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;
            spec.DesiredUseDistance = 5.0f;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                WithProgress = true,
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
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.2"
                },
            };

            var newEffect = new MapAbilityEffectSpawnBulletCfg()
            {
                BulletId = "small_knife",
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

                BulletHitResult = new()
                {
                    OnHitEffects = new()
                    {
                        new MapAbilityEffectHitBoxCfg()
                        {
                            Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                            Radius = 1.0f,
                            CampFilterType = ECampFilterType.NotSelf,
                            MaxCatchCount = 1,
                            HitResult = new()
                            {
                                OnHitEffects = new()
                                {
                                    new MapAbilityEffectAddResourceCfg()
                                    {
                                        ResourceId  = AttrIdConsts.NPCHVal,
                                        AddValue = 50000,
                                    }
                                }
                            },

                        }
                    },
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

        private static MapAbilitySpecConfig CreateGrappleHookAbility()
        {
            const float hookMaxRange = GrappleHookSpecs.MaxLength;
            const float hookFlySpeed = GrappleHookSpecs.FlySpeed;
            const float hookFlyTime = hookMaxRange / hookFlySpeed;

            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "grapple_hook";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.CastType = ECastType.NoTarget;
            spec.AdjustFaceDir = true;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;
            spec.DesiredUseDistance = hookMaxRange;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Prepare",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.1",
                },
            });

            var firePhase = new MapAbilityPhase()
            {
                PhaseName = "Fire",
                LockMovement = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5",
                },
            };

            var spawnHook = new MapAbilityEffectSpawnBulletCfg()
            {
                BulletId = GrappleHookSpecs.BulletId,
                MotionData = new LinearMotionData()
                {
                    speed = hookFlySpeed,
                    useCCD = true,
                    radius = 0.12f,
                },
                lockViewAngle = false,
                SpawnPos = MapAbilityEffectSpawnBulletCfg.ESpawnPos.TriggerPos,
                SpawnDir = MapAbilityEffectSpawnBulletCfg.ESpawnDir.ToCastPos,
                lifeTime = hookFlyTime,
                bulletMaxPenetration = 1,
                TriggerOnCollide = true,
                TriggerOnLifeEnd = true,
                BulletHitResult = new()
                {
                    OnHitEffects = new()
                    {
                        // 命中后立即推进技能相位；玩家贴近由 PlayerGrappleController 驱动
                        new MapAbilityEffectNextPhaseCfg()
                        {
                            MatchSkill = "grapple_hook",
                            MatchPhase = "Fire",
                        },
                    },
                },
                ExplodeEffects = new()
                {
                    new MapAbilityEffectNextPhaseCfg()
                    {
                        MatchSkill = "grapple_hook",
                        MatchPhase = "Fire",
                    },
                },
            };
            firePhase.Events.Add(new PhaseEffectEvent() { Effect = spawnHook, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(firePhase);

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Recovery",
                InterruptMask = EAbilityInterruptMask.Move | EAbilityInterruptMask.Cast,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.15",
                },
            });

            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultShootAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "queen_shoot";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.CastType = ECastType.NoTarget; // 向面前射出子弹
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;
            spec.DesiredUseDistance = 5.0f;

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

                BulletHitResult = new()
                {
                    OnHitEffects = new()
                    {
                        new MapAbilityEffectHitBoxCfg()
                        {
                            Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                            Radius = 1.0f,
                            CampFilterType = ECampFilterType.NotSelf,

                            HitResult = new()
                            {
                                OnHitEffects = new()
                                {
                                    new MapAbilityEffectAddResourceCfg()
                                    {
                                        ResourceId  = AttrIdConsts.NPCHVal,
                                        AddValue = 50000,
                                    }
                                }
                            },

                        }
                    },
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

            spec.CastType = ECastType.NoTarget;

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

                    BulletHitResult = new()
                    {
                        OnHitEffects = new()
                        {
                            new MapAbilityEffectHitBoxCfg()
                            {
                                Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                                Radius = 1.0f,
                                CampFilterType = ECampFilterType.NotSelf,

                                HitResult = new()
                                {
                                     OnHitEffects = new()
                                     {
                                        new MapAbilityEffectAddResourceCfg()
                                        {
                                            ResourceId  = AttrIdConsts.NPCHVal,
                                            AddValue = 50_000,
                                        }
                                     }
                                }
                            }
                        },
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

                    BulletHitResult = new()
                    {
                        OnHitEffects = new()
                        {
                            new MapAbilityEffectHitBoxCfg()
                            {
                                Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                                Radius = 1.0f,
                                CampFilterType = ECampFilterType.NotSelf,

                                HitResult = new()
                                {
                                    OnHitEffects = new()
                                    {
                                        new MapAbilityEffectAddResourceCfg()
                                        {
                                            ResourceId  = AttrIdConsts.NPCHVal,
                                            AddValue = 50000,
                                        }
                                    }
                                },

                            }
                        },
                    }

                    
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

                    BulletHitResult = new()
                    {
                        OnHitEffects = new()
                        {
                            new MapAbilityEffectHitBoxCfg()
                            {
                                Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                                Radius = 1.0f,
                                CampFilterType = ECampFilterType.NotSelf,

                                HitResult = new()
                                {
                                    OnHitEffects = new()
                                    {
                                        new MapAbilityEffectAddResourceCfg()
                                        {
                                            ResourceId  = AttrIdConsts.NPCHVal,
                                            AddValue = 50000,
                                        }
                                    }
                                },

                            }
                        },
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

                    ExplodeEffects = new()
                    {
                        new MapAbilityEffectHitBoxCfg()
                        {
                            Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                            Radius = 1.0f,
                            CampFilterType = ECampFilterType.NotSelf,

                            HitResult = new()
                            {
                                OnHitEffects = new()
                                {
                                    new MapAbilityEffectAddResourceCfg()
                                    {
                                        ResourceId  = AttrIdConsts.NPCHVal,
                                        AddValue = 50000,
                                    }
                                }
                            },
                            
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

                    BulletHitResult = new()
                    {
                        OnHitEffects = new()
                        {
                            new MapAbilityEffectHitBoxCfg()
                            {
                                Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                                Radius = 1.0f,
                                CampFilterType = ECampFilterType.NotSelf,

                                HitResult = new()
                                {
                                    OnHitEffects = new()
                                    {
                                        new MapAbilityEffectAddResourceCfg()
                                        {
                                            ResourceId  = AttrIdConsts.NPCHVal,
                                            AddValue = 50000,
                                        }
                                    }
                                },

                            }
                        },
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

                new MapFightEffectApplyDamageCfg()
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

            spec.CastType = ECastType.LockTarget;

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
                    Duration = 3.0f,
                    ThrowMainBuffId = "force_zha_target_buff",

                    OnThrowCompleteEffects = new()
                    {
                        new MapAbilityEffectAddResourceCfg()
                        {
                            ResourceId = AttrIdConsts.NPCHVal,
                            AddValue = 30_000,
                        },
                        new MapFightEffectKnockBackCfg()
                        {
                            DirType = MapFightEffectKnockBackCfg.EKnockBackType.CastDir,
                        }
                    }
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

            spec.CastType = ECastType.LockTarget;

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

        private static MapAbilitySpecConfig CreateDefaultOrcAttack()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "default_normal_attack"; // 表示一次最简单的挥打
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

            
            // 抬手动画
            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,

                AnimTag = "normal_attack",

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3",
                    ReferName = "DefaultAttackPre",
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
                    RawVal = "0.3",
                    ReferName = "DefaultAttackTime"
                },
            };

            var newEffect = new MapAbilityEffectHitBoxCfg()
            {
                Shape = MapAbilityEffectHitBoxCfg.EShape.Direction,
                TargetEntityType = EEntityType.Player,
                CampFilterType = ECampFilterType.NotSelf,
                Width = 1f,
                Length = 1.2f,

                HitResult = new()
                {
                    OnHitEffects = new()
                    {
                        new MapFightEffectApplyDamageCfg()
                        {
                            ExtraDamageRate = new()
                            {
                                new AttrKvPair(){AttrId = AttrIdConsts.Attack, Val = 10000}
                            },
                            KnockBackForce = 0.4f,
                        },
                    }
                },

            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultHAttack()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "default_h_attack"; // 表示一次最简单的挥打
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;


            // 抬手动画
            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,

                AnimTag = "normal_attack",

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3",
                    ReferName = "DefaultAttackPre",
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
                    RawVal = "0.3",
                    ReferName = "DefaultAttackTime"
                },
            };

            var newEffect = new MapAbilityEffectHitBoxCfg()
            {
                Shape = MapAbilityEffectHitBoxCfg.EShape.Direction,
                TargetEntityType = EEntityType.Player,
                CampFilterType = ECampFilterType.NotSelf,
                Width = 1f,
                Length = 1.2f,

                HitResult = new()
                {
                    OnHitEffects = new()
                    {
                        new MapFightEffectApplyDamageCfg()
                        {
                            DamageCategory = EDmgCategory.H,
                            ExtraDamageRate = new()
                            {
                                new AttrKvPair(){AttrId = AttrIdConsts.Attack, Val = 10000}
                            },
                            KnockBackForce = 0.4f,
                        },
                    }
                },

            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultMonsterAttack()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "attack";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

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
                Length = 1.2f,

                HitResult = new()
                {
                    KnockForce = 0.4f,
                    OnHitEffects = new()
                    {
                        new MapAbilityEffectCostResourceCfg()
                        {
                            ResourceId  = AttrIdConsts.HP,
                            CostValue = 500,
                            IsEnmity = true,
                        },
                        //new MapFightEffectKnockBackCfg()
                        //{
                        //    KnockBackForce = 0.4f,
                        //    DirType = MapFightEffectKnockBackCfg.EKnockBackType.CastDir,
                        //}
                    }
                },
                
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });

            spec.Phases.Add(mainPhase);
            return spec;
        }


        private static MapAbilitySpecConfig CreateDefaultRangeAttack()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "default_range_attack";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.CastType = ECastType.Directional;
            spec.Range1 = 6.0f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.4",
                    ReferName = "PreTime"
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
                    RawVal = "0.3",
                    ReferName = "ShootTime"
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

                BulletHitResult = new()
                {
                    OnHitEffects = new()
                    {
                        new MapAbilityEffectHitBoxCfg()
                        {
                            Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                            Radius = 1.0f,
                            CampFilterType = ECampFilterType.NotSelf,

                            HitResult = new()
                            {
                                OnHitEffects = new()
                                {
                                    new MapAbilityEffectAddResourceCfg()
                                    {
                                        ResourceId  = AttrIdConsts.NPCHVal,
                                        AddValue = 50000,
                                    }
                                }
                            },
                        }
                    },
                },
                
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(mainPhase);

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Post",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.25",
                    ReferName = "PostTime"
                },
            });

            return spec;
        }

        private static MapAbilitySpecConfig CreateCannonMortarShotAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "cannon_mortar_shot_01";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.CastType = ECastType.Point;
            spec.Range1 = 14f;
            spec.DesiredUseAngle = 60f;
            spec.DesiredUseDistance = 12f;
            spec.TargetSelectPolicy = ETargetSelectPolicy.PrimaryTarget;

            var prePhase = new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                WithProgress = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.65"
                },
            };

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Main",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.2"
                },
            };

            var spawnBullet = new MapAbilityEffectSpawnBulletCfg()
            {
                BulletId = "cannon_shell_01",
                MotionData = new ParabolaMotionData()
                {
                    horizontalSpeed = 11f,
                    arcHeight = 4f,
                    gravity = 26f,
                    hitRadius = 0.5f,
                },
                SpawnPos = MapAbilityEffectSpawnBulletCfg.ESpawnPos.TriggerPos,
                SpawnDir = MapAbilityEffectSpawnBulletCfg.ESpawnDir.ToCastPos,
                isHoming = true,
                homingSelectPolicy = ETargetSelectPolicy.CastPoint,
                lifeTime = 10f,
                showRangeWarn = true,
                BulletShape = new Shape()
                {
                    Type = EShapeType.Circle,
                    Radius = 1.2f,
                },
                TriggerOnCollide = true,
                TriggerOnLifeEnd = true,
                BulletHitResult = new HitResult()
                {
                    OnHitEffects = new List<MapFightEffectCfg>
                    {
                        new MapAbilityEffectHitBoxCfg()
                        {
                            Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                            Radius = 1.2f,
                            CampFilterType = ECampFilterType.NotSelf,
                            HitResult = new HitResult()
                            {
                                OnHitEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectAddResourceCfg()
                                    {
                                        ResourceId = AttrIdConsts.HP,
                                        AddValue = 25_000,
                                    }
                                }
                            }
                        }
                    }
                },
            };

            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = spawnBullet, Kind = PhaseEventKind.OnExit });
            spec.Phases.Add(prePhase);
            spec.Phases.Add(mainPhase);
            return spec;
        }


        // 骚扰技能：向前冲刺接触主角后触发投技，3轮H冲击，每轮给玩家左键挣脱机会
        private static MapAbilitySpecConfig CreateDefaultEnemyHarass()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "default_enemy_harass";
            spec.TypeTag = AbilityTypeTag.HMode;
            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 0.8f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

            var dashingPhase = new MapAbilityPhase()
            {
                PhaseName = "Dashing",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.8"
                },
            };

            var throwCfg = new MapAbilityEffectThrowStartCfg()
            {
                Priority = 1,
                Duration = 5.0f,
                OnTargetBreakFreeEffects = new List<MapFightEffectCfg>
                {
                    new MapFightEffectEasyEffect() { EffectText = "Harass interrupted by player." },
                },
                ThrowTimelineEvents = new List<ThrowTimelineEventSpec>
                {
                    // ---- 第1轮 ----
                    new ThrowTimelineEventSpec
                    {
                        TimeFromStart = 0f,
                        Effects = new List<MapFightEffectCfg>
                        {
                            new MapAbilityEffectThrowTimedInputCfg
                            {
                                PromptText = "点击左键挣脱",
                                ResultVarKey = "harass_qi_1",
                                TimeoutSeconds = 1.4f,
                                InputMode = MapAbilityEffectThrowTimedInputCfg.EInputMode.MouseLeftClick,
                            },
                        },
                    },
                    new ThrowTimelineEventSpec
                    {
                        TimeFromStart = 0f,
                        Effects = new List<MapFightEffectCfg>
                        {
                            new MapAbilityEffectThrowTimedInputBranchCfg
                            {
                                ResultVarKey = "harass_qi_1",
                                // 玩家成功挣脱
                                SuccessBranchEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectThrowBreakFreeCfg(),
                                },
                                // 玩家未挣脱，施加H冲击
                                FailBranchEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectAddResourceCfg()
                                    {
                                        ResourceId = AttrIdConsts.PlayerPleasure,
                                        AddValue = 15_000,
                                        IsEnmity = true,
                                    },
                                },
                            },
                        },
                    },

                    // ---- 第2轮 ----
                    new ThrowTimelineEventSpec
                    {
                        TimeFromStart = 1.6f,
                        Effects = new List<MapFightEffectCfg>
                        {
                            new MapAbilityEffectThrowTimedInputCfg
                            {
                                PromptText = "点击左键挣脱",
                                ResultVarKey = "harass_qi_2",
                                TimeoutSeconds = 1.4f,
                                InputMode = MapAbilityEffectThrowTimedInputCfg.EInputMode.MouseLeftClick,
                            },
                        },
                    },
                    new ThrowTimelineEventSpec
                    {
                        TimeFromStart = 1.6f,
                        Effects = new List<MapFightEffectCfg>
                        {
                            new MapAbilityEffectThrowTimedInputBranchCfg
                            {
                                ResultVarKey = "harass_qi_2",
                                SuccessBranchEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectThrowBreakFreeCfg(),
                                },
                                FailBranchEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectAddResourceCfg()
                                    {
                                        ResourceId = AttrIdConsts.PlayerPleasure,
                                        AddValue = 15_000,
                                        IsEnmity = true,
                                    },
                                },
                            },
                        },
                    },

                    // ---- 第3轮 ----
                    new ThrowTimelineEventSpec
                    {
                        TimeFromStart = 3.2f,
                        Effects = new List<MapFightEffectCfg>
                        {
                            new MapAbilityEffectThrowTimedInputCfg
                            {
                                PromptText = "点击左键挣脱",
                                ResultVarKey = "harass_qi_3",
                                TimeoutSeconds = 1.4f,
                                InputMode = MapAbilityEffectThrowTimedInputCfg.EInputMode.MouseLeftClick,
                            },
                        },
                    },
                    new ThrowTimelineEventSpec
                    {
                        TimeFromStart = 3.2f,
                        Effects = new List<MapFightEffectCfg>
                        {
                            new MapAbilityEffectThrowTimedInputBranchCfg
                            {
                                ResultVarKey = "harass_qi_3",
                                SuccessBranchEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectThrowBreakFreeCfg(),
                                },
                                FailBranchEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectAddResourceCfg()
                                    {
                                        ResourceId = AttrIdConsts.PlayerPleasure,
                                        AddValue = 15_000,
                                        IsEnmity = true,
                                    },
                                },
                            },
                        },
                    },
                },
            };

            var dashEffect = new MapAbilityEffectDashStartCfg()
            {
                DashMode = EDashMode.FixDistance,
                DirMode = EDirMode.LookDir,
                DashSpeed = 8f,
                MaxDistance = 5f,
                DashOverrideHitRadius = 0.6f,
                EndOnHitUnit = true,
                StopOnWall = true,
                EndAbilityPhaseWhenEnds = true,
                OnHitEffects = new() { throwCfg },
            };

            dashingPhase.Events.Add(new PhaseEffectEvent() { Effect = dashEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(dashingPhase);

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Post",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.8"
                },
            });

            return spec;
        }

        // 推倒技能：蓄力后向前范围释放，累积玩家推倒进度，满后进入被推倒状态
        private static MapAbilitySpecConfig CreateDefaultEnemyKnockdownPush()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "default_enemy_knockdown_push";
            spec.TypeTag = AbilityTypeTag.HMode;
            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1.5f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

            // 蓄力阶段：固定时长，显示进度，受击可被打断
            var chargePhase = new MapAbilityPhase()
            {
                PhaseName = "Charge",
                WithProgress = true,
                LockMovement = true,
                LockRotation = true,
                InterruptMask = EAbilityInterruptMask.Hit,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "1.5"
                },
            };
            spec.Phases.Add(chargePhase);

            // 释放阶段：向前矩形打击盒
            var releasePhase = new MapAbilityPhase()
            {
                PhaseName = "Release",
                LockMovement = true,
                LockRotation = true,
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
            hitCfg.Width = 1.5f;
            hitCfg.Length = 2.5f;
            hitCfg.TargetEntityType = EEntityType.Player;
            hitCfg.HitResult = new()
            {
                OnHitEffects = new()
                {
                    // 累积推倒进度；满条判定在 Entity_Player.OnResourceAttriChanged 中统一处理
                    new MapAbilityEffectAddResourceCfg()
                    {
                        ResourceId = AttrIdConsts.PlayerKnockDown,
                        AddValue = 35_000,
                        IsEnmity = true,
                    },
                },
            };

            releasePhase.Events.Add(new PhaseEffectEvent() { Effect = hitCfg, Kind = PhaseEventKind.OnExit });
            spec.Phases.Add(releasePhase);

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Post",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.8"
                },
            });

            return spec;
        }


        private static MapAbilitySpecConfig CreateSupplyItemAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_supply_item";
            spec.TypeTag = AbilityTypeTag.Utility;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Prepare",
                LockMovement = true,
                LockRotation = true,
                WithProgress = true,

                InterruptMask = EAbilityInterruptMask.Move | EAbilityInterruptMask.Hit,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "2"
                },
            };

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateEnchantItemAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_enchant_item";
            spec.TypeTag = AbilityTypeTag.Utility;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Prepare",
                LockMovement = true,
                LockRotation = true,
                WithProgress = true,

                InterruptMask = EAbilityInterruptMask.Move | EAbilityInterruptMask.Hit,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "2"
                },
            };

            spec.Phases.Add(mainPhase);
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

                InterruptMask = EAbilityInterruptMask.Move | EAbilityInterruptMask.Hit,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "5"
                },
            };


            var effect = new MapFightEffectFixExposeCfg()
            {
                RestoreValue = 80000,
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

            spec.CastType = ECastType.Point;
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

        private static MapAbilitySpecConfig CreateDebugApplyFearBuffAbility()
        {
            return CreateDebugApplySteerBuffAbility("debug_apply_fear", "fear", 3f);
        }

        private static MapAbilitySpecConfig CreateDebugApplyLuredBuffAbility()
        {
            return CreateDebugApplySteerBuffAbility("debug_apply_lured", "lured", 3f);
        }

        private static MapAbilitySpecConfig CreateDebugApplySteerBuffAbility(string abilityId, string buffId, float buffDuration)
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = abilityId;
            spec.TypeTag = AbilityTypeTag.Utility;
            spec.CastType = ECastType.LockTarget;
            spec.Range1 = 8f;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Main",
                LockMovement = false,
                LockRotation = false,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.05"
                },
            };

            var addBuff = new MapAbilityEffectAddBuffCfg()
            {
                BuffId = buffId,
                Duration = buffDuration,
                Layer = 1,
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = addBuff, Kind = PhaseEventKind.OnEnter });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateFightEffectPlaceStunTrapAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = FightEffectConstants.PlaceStunTrapAbilityId;
            spec.TypeTag = AbilityTypeTag.Utility;

            spec.CastType = ECastType.Point;
            spec.Range1 = 6.0f;

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
                EntityType = EEntityType.Trap,
                CfgId = FightEffectConstants.StunTrapCfgId,
                LifeTime = 90.0f,
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effect, Kind = PhaseEventKind.OnExit });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateOrbSkillCastAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "orb_skill_cast";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.CastType = ECastType.NoTarget;

            spec.CastCosts.Add(new SkillCostEntry
            {
                CostType = ESkillCostType.Resource,
                ResourceId = AttrIdConsts.SkillProxyOrbAmmo,
                Amount = 1,
                Target = ESkillCostTarget.HostEntity,
            });

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Main",
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.05"
                },
            };

            var spawnBullet = new MapAbilityEffectSpawnBulletCfg()
            {
                BulletId = "player_trace_bullet_01",
                MotionData = new LinearMotionData()
                {
                    speed = 10f,
                },
                SpawnPos = MapAbilityEffectSpawnBulletCfg.ESpawnPos.TriggerPos,
                SpawnDir = MapAbilityEffectSpawnBulletCfg.ESpawnDir.AlignHoming,
                isHoming = true,
                homingSelectPolicy = ETargetSelectPolicy.NearestEnemyInRadius,
                nearestEnemyAcquireRadius = 8f,
                bulletMaxPenetration = 1,
                lifeTime = 4f,
                TriggerOnCollide = true,
                BulletHitResult = new HitResult()
                {
                    OnHitEffects = new List<MapFightEffectCfg>
                    {
                        new MapAbilityEffectHitBoxCfg()
                        {
                            Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                            Radius = 0.6f,
                            CampFilterType = ECampFilterType.NotSelf,
                            MaxCatchCount = 1,
                            HitResult = new HitResult()
                            {
                                OnHitEffects = new List<MapFightEffectCfg>
                                {
                                    new MapFightEffectApplyDamageCfg()
                                    {
                                        BaseDamage = 8000,
                                    }
                                }
                            }
                        }
                    }
                },
            };

            mainPhase.Events.Add(new PhaseEffectEvent()
            {
                Effect = spawnBullet,
                Kind = PhaseEventKind.Timed,
                TimeOffset = 0f,
            });
            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateOrbSkillSummonAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "orb_skill_summon";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.CastType = ECastType.NoTarget;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Main",
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.1"
                },
            };

            mainPhase.Events.Add(new PhaseEffectEvent()
            {
                Effect = new MapAbilityEffectSpawnSkillProxyCfg()
                {
                    CfgId = "orb_skill_v1",
                    LifeTime = 15f,
                },
                Kind = PhaseEventKind.Timed,
                TimeOffset = 0f,
            });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreatePlayerEnterExposeAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_enter_expose";
            spec.TypeTag = AbilityTypeTag.Utility;
            spec.CastType = ECastType.NoTarget;

            var chargePhase = new MapAbilityPhase()
            {
                PhaseName = "Charge",
                HoldingPhase = true,
                CancelableBySceneCancel = true,
                LockMovement = true,
                LockRotation = true,
                ImmuneKnock = true,
                ProgressSceneEffect = "Skill/enter_expose_xuli",
                ProgressEffectNormalizeDuration = 3f,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "99"
                },
            };

            var chargeEndEffect = new MapFightEffectXuLiStageCfg()
            {
                CheckPhaseName = "Charge",
                StageInfos =
                {
                    new MapFightEffectXuLiStageCfg.EStageInfo()
                    {
                        NeedTime = 3f,
                        StageEffects = new()
                        {
                            new MapFightEffectEnterExposeCfg(),
                        }
                    },
                    new MapFightEffectXuLiStageCfg.EStageInfo()
                    {
                        NeedTime = 0f,
                        StageEffects = new()
                        {
                            new MapFightEffectInterruptCaster(),
                        }
                    },
                }
            };
            chargePhase.Events.Add(new PhaseEffectEvent() { Effect = chargeEndEffect, Kind = PhaseEventKind.OnExit });
            spec.Phases.Add(chargePhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreatePlayerReturnDisguiseAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_return_disguise";
            spec.TypeTag = AbilityTypeTag.Utility;
            spec.CastType = ECastType.NoTarget;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Prepare",
                LockMovement = true,
                LockRotation = true,
                WithProgress = true,
                InterruptMask = EAbilityInterruptMask.Hit,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "5"
                },
            };

            var effect = new MapFightEffectFixExposeCfg()
            {
                RestoreValue = 80000,
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effect, Kind = PhaseEventKind.OnExit });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreatePlayerSummonAllyTurretAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_summon_ally_turret";
            spec.TypeTag = AbilityTypeTag.Utility;

            spec.CastType = ECastType.Point;
            spec.Range1 = 6.0f;

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
                EntityType = EEntityType.Npc,
                CfgId = "summon_ally_turret_01",
                LifeTime = 60.0f,
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effect, Kind = PhaseEventKind.OnExit });

            spec.Phases.Add(mainPhase);
            return spec;
        }


        private static MapAbilitySpecConfig CreatePlayerTraceBullet1Ability()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_trace_bullet_01";
            spec.TypeTag = AbilityTypeTag.Utility;

            spec.CastType = ECastType.NoTarget;
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

            {
                var newEffect = new MapAbilityEffectSpawnBulletCfg()
                {
                    BulletId = "player_trace_bullet_01",
                    MotionData = new ParabolaMotionData()
                    {
                        horizontalSpeed = 9f,
                        arcHeight = 3.0f,
                    },

                    SpawnPos = MapAbilityEffectSpawnBulletCfg.ESpawnPos.TriggerPos,

                    isHoming = true,
                    homingSelectPolicy = ETargetSelectPolicy.PrimaryTarget,
                    SpawnDir = MapAbilityEffectSpawnBulletCfg.ESpawnDir.Random,

                    showRangeWarn = true,


                    lifeTime = 999f,
                    //BulletShape = new FightStruct.Shape()
                    //{
                    //    Type = FightStruct.EShapeType.Circle,
                    //    Radius = 0.8f,
                    //},

                    //BulletHitResult = new()
                    //{
                    //    OnHitEffects = new()
                    //    {
                    //        new MapAbilityEffectHitBoxCfg()
                    //        {
                    //            Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                    //            Radius = 1.0f,
                    //            CampFilterType = ECampFilterType.NotSelf,

                    //            HitResult = new()
                    //            {
                    //                 OnHitEffects = new()
                    //                 {
                    //                    new MapAbilityEffectAddResourceCfg()
                    //                    {
                    //                        ResourceId  = AttrIdConsts.UnitHVal,
                    //                        AddValue = 50_000,
                    //                    }
                    //                 }
                    //            }
                    //        }
                    //    },
                    //},


                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnExit });
            }

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreatePlayerMortarAcquireAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_mortar_acquire_01";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.CastType = ECastType.NoTarget;
            spec.Range1 = 10f;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Main",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.35"
                },
            };

            var spawnBullet = new MapAbilityEffectSpawnBulletCfg()
            {
                BulletId = "player_trace_bullet_01",
                MotionData = new ParabolaMotionData()
                {
                    horizontalSpeed = 11f,
                    arcHeight = 4.2f,
                    gravity = 26f,
                    hitRadius = 0.7f,
                },
                SpawnPos = MapAbilityEffectSpawnBulletCfg.ESpawnPos.TriggerPos,
                SpawnDir = MapAbilityEffectSpawnBulletCfg.ESpawnDir.AlignHoming,
                isHoming = true,
                homingSelectPolicy = ETargetSelectPolicy.NearestEnemyInRadius,
                nearestEnemyAcquireRadius = 10f,
                bulletMaxPenetration = 1,
                lifeTime = 12f,
                showRangeWarn = false,
                BulletHitResult = new HitResult()
                {
                    OnHitEffects = new List<MapFightEffectCfg>
                    {
                        new MapAbilityEffectHitBoxCfg()
                        {
                            Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                            Radius = 0.85f,
                            CampFilterType = ECampFilterType.NotSelf,
                            HitResult = new HitResult()
                            {
                                OnHitEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectAddResourceCfg()
                                    {
                                        ResourceId = AttrIdConsts.NPCHVal,
                                        AddValue = 42000,
                                    }
                                }
                            }
                        }
                    }
                },
            };

            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = spawnBullet, Kind = PhaseEventKind.OnExit });
            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateItemThrowSmokeGrenadeAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "item_throw_smoke_grenade";
            spec.TypeTag = AbilityTypeTag.Utility;
            spec.CastType = ECastType.Point;
            spec.Range1 = 8f;
            spec.AdjustFaceDir = true;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Main",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.25"
                },
            };

            var spawnBullet = new MapAbilityEffectSpawnBulletCfg()
            {
                BulletId = "smoke_grenade",
                MotionData = new ParabolaMotionData()
                {
                    horizontalSpeed = 10f,
                    arcHeight = 3.5f,
                    gravity = 24f,
                    hitRadius = 0f,
                },
                SpawnPos = MapAbilityEffectSpawnBulletCfg.ESpawnPos.TriggerPos,
                SpawnDir = MapAbilityEffectSpawnBulletCfg.ESpawnDir.ToCastPos,
                isHoming = true,
                homingSelectPolicy = ETargetSelectPolicy.CastPoint,
                lifeTime = 12f,
                bulletMaxPenetration = 0,
                TriggerOnCollide = false,
                ExplodeEffects = new List<MapFightEffectCfg>
                {
                    new MapFightEffectCreateAreaEffectCfg()
                    {
                        CfgId = "smoke_grenade_area",
                        LifeTime = 8f,
                    },
                },
            };

            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = spawnBullet, Kind = PhaseEventKind.OnExit });
            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateDefaultDashSlash()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "default_dash_slash";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.CastType = ECastType.Point;
            spec.DesiredUseDistance = 2.8f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

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
                    RawVal = "1"
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
                    RawVal = "0.6"
                },
            };

            {
                var dashEffect = new MapAbilityEffectDashStartCfg()
                {
                    //IsFixPointMode = true,
                    DashMode = EDashMode.ToTarget,
                    DashSpeed = 5f,
                    MaxDistance = 3.0f,
                    DashOverrideHitRadius = 0.8f,
                    DirMode = EDirMode.LookDir,

                    DashWeaponName = "Charge",
                    EndOnHitUnit = true,

                    OnHitEffects = new()
                    {
                        // 提前进入下一phase
                        // 这个应该放入配置
                        //new MapAbilityEffectNextPhaseCfg()
                        //{
                        //    MatchPhase = "Dashing",
                        //    MatchSkill = "default_dash_slash"
                        //},
                        new MapFightEffectApplyDamageCfg()
                        {
                            BaseDamage = 25000,
                            KnockBackForce = 0.8f,
                        },
                    },
                };

                //var hitEffect = new MapAbilityEffectUseWeaponCfg()
                //{
                //    WeaponName = "Charge",
                //    Duration = 0.45f,
                //    OnHitEffects = new()
                //    {

                //        new MapAbilityEffectApplyDamageCfg()
                //        {
                //            BaseDamage = 25000,
                //            KnockBackForce = 0.3f,
                //        },
                //    }
                //};

                dashingPhase.Events.Add(new PhaseEffectEvent() { Effect = dashEffect, Kind = PhaseEventKind.OnEnter});
                //dashingPhase.Events.Add(new PhaseEffectEvent() { Effect = hitEffect, Kind = PhaseEventKind.OnEnter });
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

            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1.0f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

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
                    var dmgEffect = new MapFightEffectApplyDamageCfg()
                    {
                        BaseDamage = 25000,
                        KnockBackForce = 0.8f,
                    };

                    hitCfg.HitResult = new()
                    {
                       OnHitEffects = new() { dmgEffect }
                    };
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

        private static MapAbilitySpecConfig CreateQueenDashAttack()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            // 做一个冲刺挑飞 等冲完再打
            spec.Id = "queen_dash_attack_01";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.DefaultStepDistance = 0.3f;
            spec.AdjustFaceDir = true;

            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1.0f;


            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.15"
                },

                AnimTag = "dash_attack_01",
            });

            var dashEffect = new MapAbilityEffectDashStartCfg()
            {
                DashDuration = 0.5f,
                DashSpeed = 24f,
                DashMode = EDashMode.FixTime,
                DirMode = EDirMode.LookDir,
            };


            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockRotation = true,
                ImmuneKnock = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
                },
            };

            
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = dashEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(mainPhase);

            //var postPhase = new MapAbilityPhase()
            //{
            //    PhaseName = "Post",
            //    InterruptMask = EAbilityInterruptMask.Move | EAbilityInterruptMask.Cast,
            //    DurationValue = new()
            //    {
            //        ValType = EOneVariatyType.Float,
            //        RawVal = "0.25"
            //    },
            //};
            //spec.Phases.Add(postPhase);
            return spec;
        }


        private static MapAbilitySpecConfig CreateQueenAttackAbility1()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "queen_attack_01";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.DefaultStepDistance = 0.3f;
            spec.AdjustFaceDir = true;

            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1.0f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

            spec.AddTargetCorrection = true;
            spec.GoodCorrectionnDist = 1.0f;
            spec.MaxCorrectionValue = 0.8f;

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

                AnimTag = "attack_01",
                StepSnapSource = EPhaseStepSnapSource.InheritFromAbility,
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
                    RawVal = "0.2"
                },
            };

            var newEffect = new MapAbilityEffectUseWeaponCfg()
            {
                WeaponName = "DevailClawAttack",
                AnimName = "player_weapon01_01",
                Duration = 0.2f,
                OnHitEffects = new()
                {

                    new MapFightEffectApplyDamageCfg()
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
            spec.DefaultStepDistance = 0.3f;
            spec.AdjustFaceDir = true;

            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1.0f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

            spec.AddTargetCorrection = true;
            spec.GoodCorrectionnDist = 1.0f;
            spec.MaxCorrectionValue = 0.8f;


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
                AnimTag = "attack_02",
                StepSnapSource = EPhaseStepSnapSource.InheritFromAbility,
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
                    RawVal = "0.2"
                },
            };

            var newEffect = new MapAbilityEffectUseWeaponCfg()
            {
                WeaponName = "DevailClawAttack",
                AnimName = "player_weapon01_02",
                Duration = 0.2f,
                OnHitEffects = new()
                {

                    new MapFightEffectApplyDamageCfg()
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
                    RawVal = "0.25"
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
            spec.DefaultStepDistance = 0.0f;
            spec.AdjustFaceDir = true;

            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1.0f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

            spec.AddTargetCorrection = true;
            spec.GoodCorrectionnDist = 1.0f;
            spec.MaxCorrectionValue = 0.8f;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
                },

                AnimTag = "queen_attack_03",
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

                AnimTag = "queen_attack_03_down",
                StepSnapSource = EPhaseStepSnapSource.InheritFromAbility,
            };

            //var newEffect = new MapAbilityEffectUseWeaponCfg()
            //{
            //    WeaponName = "Weapon01",
            //    AnimName = "player_weapon01_03",
            //    Duration = 0.4f,
            //    OnHitEffects = new()
            //    {

            //        new MapFightEffectApplyDamageCfg()
            //        {
            //            BaseDamage = 25000,
            //            KnockBackForce = 1f,
            //        },
            //    }
            //};

            var dashEffect = new MapAbilityEffectDashStartCfg()
            {
                DashDuration = 0.15f,
                DashSpeed = 8f,
                DashMode = EDashMode.FixTime,
                DirMode = EDirMode.LookDir,
            };


            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = dashEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(mainPhase);



            var hitEffect = new MapAbilityEffectHitBoxCfg()
            {
                Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,

                Radius = 2.5f,
                //CenterOffset = 1.3f,
                CampFilterType = ECampFilterType.NotSelf,
                CenterPosType = 2,


                HitResult = new()
                {
                    OnHitEffects = new()
                    {
                        new MapAbilityEffectAddBuffCfg()
                        {
                            BuffId = "force_stun",
                            Duration = 1.0f,
                        },
                        new MapFightEffectApplyDamageCfg()
                        {
                            BaseDamage = 25000,
                            KnockBackForce = 1f,
                        },
                    }
                }

            };


            //var newEffect = new MapAbilityEffectUseWeaponCfg()
            //{
            //    WeaponName = "Weapon01",
            //    AnimName = "player_weapon01_03",
            //    Duration = 0.4f,
            //    OnHitEffects = new()
            //    {

            //        new MapFightEffectApplyDamageCfg()
            //        {
            //            BaseDamage = 25000,
            //            KnockBackForce = 1f,
            //        },
            //    }
            //};
            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                InterruptMask = EAbilityInterruptMask.Cast | EAbilityInterruptMask.Move,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.4"
                },
            };
            postPhase.Events.Add(new PhaseEffectEvent() { Effect = hitEffect, Kind = PhaseEventKind.OnEnter });

            
            spec.Phases.Add(postPhase);
            return spec;
        }
        private static MapAbilitySpecConfig CreateQueenAttackHeavy()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "queen_attack_heavy";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.DefaultStepDistance = 0.3f;
            spec.AdjustFaceDir = true;

            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1.0f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

            //spec.AddTargetCorrection = true;
            //spec.GoodCorrectionnDist = 1.0f;
            //spec.MaxCorrectionValue = 0.8f;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Xuli",
                HoldingPhase = true,

                PhaseBuff = new() { "jian_su_self" },

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "5"
                },

                AnimTag = "queen_attack_heavy_xuli",
                ProgressSceneEffect = "Skill/player_shield",
                ProgressEffectNormalizeDuration = 5f,
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
                    RawVal = "0.5"
                },

                AnimTag = "queen_attack_heavy",
            };


            var xuliEffect = new MapFightEffectXuLiStageCfg()
            {
                CheckPhaseName = "Xuli",

                StageInfos =
                {
                    new MapFightEffectXuLiStageCfg.EStageInfo()
                    {
                        NeedTime = 2.0f,

                        StageEffects = new()
                        {
                            //new MapFightEffectShowEffect()
                            //{
                            //    ShowMode = MapFightEffectShowEffect.EShowMode.EntityAligned,

                            //    ShowPos = new Vector2(0.5f, 0.2f),
                            //},

                            new MapAbilityEffectUseWeaponCfg()
                            {
                                WeaponName = "QueenHeavy",
                                AnimName = "player_weapon01_heavy3",
                                Duration = 0.5f,
                                OnHitEffects = new()
                                {

                                    new MapFightEffectApplyDamageCfg()
                                    {
                                        BaseDamage = 15000,
                                        KnockBackForce = 0.6f,
                                    },
                                }
                            }
                        }
                    },

                    new MapFightEffectXuLiStageCfg.EStageInfo()
                    {
                        NeedTime = 1.0f,

                        StageEffects = new()
                        {
                            new MapAbilityEffectUseWeaponCfg()
                            {
                                WeaponName = "QueenHeavy",
                                AnimName = "player_weapon01_heavy2",
                                Duration = 0.5f,
                                OnHitEffects = new()
                                {

                                    new MapFightEffectApplyDamageCfg()
                                    {
                                        BaseDamage = 25000,
                                        KnockBackForce = 0.6f,
                                    },
                                }
                            }
                        }
                    },

                    new MapFightEffectXuLiStageCfg.EStageInfo()
                    {
                        NeedTime = 0.0f,

                        StageEffects = new()
                        {
                            new MapAbilityEffectUseWeaponCfg()
                            {
                                WeaponName = "QueenHeavy",
                                AnimName = "player_weapon01_heavy1",
                                Duration = 0.5f,
                                OnHitEffects = new()
                                {

                                    new MapFightEffectApplyDamageCfg()
                                    {
                                        BaseDamage = 40000,
                                        KnockBackForce = 0.6f,
                                    },
                                }
                            }
                        }
                    },
                }
            };


            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = xuliEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(mainPhase);


            var postPhase = new MapAbilityPhase()
            {
                PhaseName = "Post",
                InterruptMask = EAbilityInterruptMask.Cast | EAbilityInterruptMask.Move,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.4"
                },
            };
            spec.Phases.Add(postPhase);
            return spec;
        }

        /// <summary>
        /// 扑到
        /// </summary>
        /// <returns></returns>
        private static MapAbilitySpecConfig CreateForceDashPushDown()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "force_dash_push_down";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.DefaultStepDistance = 0.3f;
            spec.AdjustFaceDir = true;

            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1.0f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;


            var xuliPhase = new MapAbilityPhase()
            {
                PhaseName = "XuLi",
                HoldingPhase = true,
                PhaseBuff = new() { "jian_su_self" },
                ImmuneKnock = true,
                AnimTag = "",
                ProgressSceneEffect = "Skill/force_dash_xuli",
                ProgressEffectNormalizeDuration = 3f,

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "99.0"
                },
            };

            var xuliEndEffect = new MapFightEffectXuLiStageCfg()
            {
                CheckPhaseName = "XuLi",

                StageInfos =
                {
                    new MapFightEffectXuLiStageCfg.EStageInfo()
                    {
                        NeedTime = 1.0f,

                        StageEffects = new()
                        {

                        }
                    },


                    new MapFightEffectXuLiStageCfg.EStageInfo()
                    {
                        NeedTime = 0.0f,

                        StageEffects = new()
                        {
                            new MapFightEffectInterruptCaster()
                            {

                            }
                        }
                    },
                }
            };
            xuliPhase.Events.Add(new PhaseEffectEvent() { Effect = xuliEndEffect, Kind = PhaseEventKind.OnExit });
            spec.Phases.Add(xuliPhase);

            //var intervalPhase = new MapAbilityPhase()
            //{
            //    PhaseName = "Inteval",
            //    LockMovement = true,
            //    LockRotation = true,
            //    ImmuneKnock = true,
            //    DurationValue = new()
            //    {
            //        ValType = EOneVariatyType.Float,
            //        RawVal = "0.2"
            //    },
            //};

            //intervalPhase.Events.Add(new PhaseEffectEvent() { Effect = xuliEffect, Kind = PhaseEventKind.Timed, TimeOffset = 0.01f});
            //spec.Phases.Add(intervalPhase);

            
            var dashEffect = new MapAbilityEffectDashStartCfg()
            {
                DashMode = EDashMode.FixDistance,
                MaxDistance = 1.5f,
                DashSpeed = 9f,
                DashWeaponName = "Catch",
                DirMode = EDirMode.LookDir,

                EndOnHitUnit = true,
                StopOnWall = true,
            };

            var dashPhase = new MapAbilityPhase()
            {
                PhaseName = "Dash",
                LockMovement = true,
                LockRotation = true,
                ImmuneKnock = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.65"
                },
            };


            dashPhase.Events.Add(new PhaseEffectEvent() { Effect = dashEffect, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(dashPhase);


            // 检查必须由目标
            {
                var checkPhase1 = new MapAbilityPhase()
                {
                    PhaseName = "Check1",
                    LockMovement = true,
                    LockRotation = true,
                    ImmuneKnock = true,
                    DurationValue = new()
                    {
                        ValType = EOneVariatyType.Float,
                        RawVal = "0"
                    },
                    UsePhaseHitAsTarget = "Dash",
                };

                var checkHitCfg = new MapAbilityEffectIfBranchCfg()
                {
                    CheckType = MapAbilityEffectIfBranchCfg.ECheckType.HasTarget,
                    FalseBranchEffects = new()
                    {
                        new MapFightEffectInterruptCaster()
                            {

                            }
                    }
                };
                checkPhase1.Events.Add(new PhaseEffectEvent() { Effect = checkHitCfg, Kind = PhaseEventKind.OnEnter });
                spec.Phases.Add(checkPhase1);
            }

            // 检查必须成功触发对抗判定
            {
                var checkPhase2 = new MapAbilityPhase()
                {
                    PhaseName = "Check2",
                    LockMovement = true,
                    LockRotation = true,
                    ImmuneKnock = true,
                    DurationValue = new()
                    {
                        ValType = EOneVariatyType.Float,
                        RawVal = "0"
                    },
                };

                var checkWinCfg = new MapAbilityEffectIfBranchCfg()
                {
                    CheckType = MapAbilityEffectIfBranchCfg.ECheckType.BodyVsWin,
                    Param5 = 10000,
                    Param6 = 15000, // 防守方弱势
                    FalseBranchEffects = new()
                    {
                        new MapAbilityEffectCostResourceCfg()
                        {
                            IsSelf = true,
                            ResourceId = AttrIdConsts.PlayerClothes,
                            CostValue = -10_000,
                        },
                        new MapFightEffectEasyEffect()
                        {
                            EffectText = "没倒",
                        },
                        new MapFightEffectInterruptCaster()
                        {

                        },

                        new MapFightEffectKnockBackCfg()
                        {
                            ApplyTarget = false,
                            ApplySelf = true,
                            DirType = MapFightEffectKnockBackCfg.EKnockBackType.AwayFromTarget,
                            KnockBackForce = 0.85f,
                        },
                    }
                };
                checkPhase2.Events.Add(new PhaseEffectEvent() { Effect = checkWinCfg, Kind = PhaseEventKind.OnEnter });
                spec.Phases.Add(checkPhase2);
            }


            var throwCfg = new MapAbilityEffectThrowStartCfg()
            {
                Priority = 999,
                Duration = 2.5f,
                LauncherHoldAnimTag = "player_force_grapple",
                AlignLauncherToTargetOnStart = true,
                AlignLauncherDuration = 0.12f,
                FreezeThrowTimelineDuringLauncherAlign = true,
                //ThrowMainBuffId = "force_zha_target_buff",
                OnTargetBreakFreeEffects = new List<MapFightEffectCfg>
                {
                    new MapFightEffectEasyEffect() { EffectText = "失败了。" },

                    new MapFightEffectEnqueueDetachedSkill(){ SkillId = "npc_grapple_push_player" }
                },
                OnThrowCompleteEffects = new List<MapFightEffectCfg>
                {
                    new MapAbilityEffectAddBuffCfg() 
                    {
                        BuffId = "force_stun",
                        Duration = 2.5f,
                    },
                    new MapFightEffectWantedIncidentBroadcastCfg()
                    {
                        Behave = EWantedBehaveType.ForceExtractAssault,
                        Radius = 10f,
                        TempEnmityAmount = 30f,
                        EvilAlertDuration = 8f,
                        OnlyPeaceNpc = true,
                    },
                },

                ThrowTimelineEvents = new List<ThrowTimelineEventSpec>
                {
                    new ThrowTimelineEventSpec
                    {
                        TimeFromStart = 0f,
                        Effects = new List<MapFightEffectCfg>
                        {
                            new MapAbilityEffectThrowTimedInputCfg
                            {
                                PromptText = "点击Space",
                                ResultVarKey = "ThrowTimedInput",
                                TimeoutSeconds = 1.2f,
                            },
                        },
                    },
                    new ThrowTimelineEventSpec
                    {
                        TimeFromStart = 0f,
                        Effects = new List<MapFightEffectCfg>
                        {
                            new MapAbilityEffectThrowTimedInputBranchCfg
                            {
                                ResultVarKey = "ThrowTimedInput",
                                SuccessBranchEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectAddBuffCfg()
                                    {
                                        BuffId = "force_fck_bonus",
                                        Layer = 1,
                                        Duration = 0.5f,
                                    },
                                    new MapAbilityEffectAddResourceCfg()
                                    {
                                        ResourceId = AttrIdConsts.NPCHVal,
                                        AddValue = 20_000,
                                        IsEnmity = true,
                                        //ExtraAttrInfos = new List<AttrKvPair>(){new(){ AttrId  = AttrIdConsts.DamageXiXue, Val = 2000} }
                                    },
                                    new MapFightEffectCauseNoise()
                                    {

                                    }
                                },
                                FailBranchEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectThrowBreakFreeCfg(),
                                },
                            },
                        },
                    },
                    new ThrowTimelineEventSpec
                    {
                        TimeFromStart = 1f,
                        Effects = new List<MapFightEffectCfg>
                        {
                            new MapAbilityEffectThrowTimedInputCfg
                            {
                                PromptText = "点击Space",
                                ResultVarKey = "ThrowTimedInput",
                                TimeoutSeconds = 1.2f,
                            },
                        },
                    },
                    new ThrowTimelineEventSpec
                    {
                        TimeFromStart = 1,
                        Effects = new List<MapFightEffectCfg>
                        {
                            new MapAbilityEffectThrowTimedInputBranchCfg
                            {
                                ResultVarKey = "ThrowTimedInput",
                                SuccessBranchEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectAddBuffCfg()
                                    {
                                        BuffId = "force_fck_bonus",
                                        Layer = 1,
                                        Duration = 0.5f,
                                    },

                                    new MapAbilityEffectAddResourceCfg()
                                    {
                                        ResourceId = AttrIdConsts.NPCHVal,
                                        AddValue = 20_000,
                                        IsEnmity = true,
                                        //ExtraAttrInfos = new List<AttrKvPair>(){new(){ AttrId  = AttrIdConsts.DamageXiXue, Val = 2000} }
                                    },
                                    new MapFightEffectCauseNoise()
                                    {

                                    }
                                },
                                FailBranchEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectThrowBreakFreeCfg(),
                                },
                            },
                        },
                    },
                    new ThrowTimelineEventSpec
                    {
                        TimeFromStart = 2f,
                        Effects = new List<MapFightEffectCfg>
                        {
                            new MapAbilityEffectThrowTimedInputCfg
                            {
                                PromptText = "点击Space",
                                ResultVarKey = "ThrowTimedInput",
                                TimeoutSeconds = 1.2f,
                            },
                        },
                    },
                    new ThrowTimelineEventSpec
                    {
                        TimeFromStart = 2f,
                        Effects = new List<MapFightEffectCfg>
                        {
                            new MapAbilityEffectThrowTimedInputBranchCfg
                            {
                                ResultVarKey = "ThrowTimedInput",
                                SuccessBranchEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectAddBuffCfg()
                                    {
                                        BuffId = "force_fck_bonus",
                                        Layer = 1,
                                        Duration = 0.5f,
                                    },
                                    new MapAbilityEffectAddResourceCfg()
                                    {
                                        ResourceId = AttrIdConsts.NPCHVal,
                                        AddValue = 20_000,
                                        IsEnmity = true,
                                        //ExtraAttrInfos = new List<AttrKvPair>(){new(){ AttrId  = AttrIdConsts.DamageXiXue, Val = 2000} }
                                    },
                                    new MapFightEffectCauseNoise()
                                    {

                                    }
                                },
                                FailBranchEffects = new List<MapFightEffectCfg>
                                {
                                    new MapAbilityEffectThrowBreakFreeCfg(),
                                },
                            },
                        },
                    },
                },
            };

            var throwEffectCfg = new MapFightEffectEasyEffect()
            {
                EffectText = "抓",
            };

            var executePhase = new MapAbilityPhase()
            {
                PhaseName = "Execute",
                LockMovement = true,
                LockRotation = true,
                ImmuneKnock = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3"
                },
                UsePhaseHitAsTarget = "Dash",
            };

            executePhase.Events.Add(new PhaseEffectEvent() { Effect = throwCfg, Kind = PhaseEventKind.OnEnter });
            executePhase.Events.Add(new PhaseEffectEvent() { Effect = throwEffectCfg, Kind = PhaseEventKind.OnEnter });
            spec.Phases.Add(executePhase);
            return spec;
        }

        // 挣脱投技后由 NPC 对玩家施放的推开技（脱手入队，主目标为玩家）
        private static MapAbilitySpecConfig CreateNpcGrapplePushPlayer()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "npc_grapple_push_player";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.DefaultStepDistance = 0.15f;

            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

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
                    RawVal = "0.18"
                },
            };

            // 直接对主目标击退（不经武器判定）
            mainPhase.Events.Add(new PhaseEffectEvent()
            {
                Effect = new MapFightEffectEasyEffect()
                {
                    EffectText = "走开",
                },
                Kind = PhaseEventKind.OnEnter,
            });

            // 直接对主目标击退（不经武器判定）
            mainPhase.Events.Add(new PhaseEffectEvent()
            {
                Effect = new MapFightEffectKnockBackCfg()
                {
                    ApplyTarget = true,
                    DirType = MapFightEffectKnockBackCfg.EKnockBackType.AwayFromSrc,
                    KnockBackForce = 0.72f,
                },
                Kind = PhaseEventKind.OnEnter,
            });
            spec.Phases.Add(mainPhase);

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Post",
                InterruptMask = EAbilityInterruptMask.Cast | EAbilityInterruptMask.Move,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.15"
                },
            });
            return spec;
        }

        
        private static MapAbilitySpecConfig CreateDefaultPushAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "default_push";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.DefaultStepDistance = 0f;
            //spec.CoolDown = 0.2f;
            //spec.DesiredUseDistance = 0.5f;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                AnimTag = "attack_01",
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
                    RawVal = "0.24"
                },
            };

            var newEffect = new MapAbilityEffectUseWeaponCfg()
            {
                WeaponName = "Push",
                AnimName = "player_push",
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
                        ResourceId = AttrIdConsts.NPCHVal,
                        AddValue = 40_000,
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

        private static MapAbilitySpecConfig CreatePlayerFQNormalZiwei()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_fq_normal_ziwei";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.DefaultStepDistance = 0f;

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
                    RawVal = "0.24"
                },
            };

            var newEffect = new MapAbilityEffectAddResourceCfg()
            {
                ResourceId = AttrIdConsts.NPCSJProgress,
                AddValue  = 10_000,
            };

            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreatePlayerFQCrazyZiwei()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_fq_crazy_ziwei";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.DefaultStepDistance = 0f;

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
                    RawVal = "1.2"
                },
            };

            var newEffect = new MapAbilityEffectAddResourceCfg()
            {
                ResourceId = AttrIdConsts.NPCSJProgress,
                AddValue = 6_000,
            };

            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.Timed, TimeOffset = 0.0f });
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.Timed, TimeOffset = 0.2f });
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.Timed, TimeOffset = 0.4f });
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.Timed, TimeOffset = 0.6f });
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.Timed, TimeOffset = 0.8f });
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.Timed, TimeOffset = 1.0f });

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreatePlayerFQHitPop()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_fq_hit_hop";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.DefaultStepDistance = 0f;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
                },
            });

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                PhaseBuff = new List<string> { "b_player_fq_hit_pop" },
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "5"
                },
            });

            return spec;
        }
        
        private static MapAbilitySpecConfig CreatePlayerFQHitBreast()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_fq_hit_breast";
            spec.TypeTag = AbilityTypeTag.HMode;
            spec.DefaultStepDistance = 0f;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
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

            var hEffect = new MapAbilityEffectAddResourceCfg()
            {
                ResourceId = AttrIdConsts.NPCSJProgress,
                AddValue = 4_000,
            };

            var hVfxEffect = new MapFightEffectShowEffect()
            {
                ShowMode = MapFightEffectShowEffect.EShowMode.TriggerPos,
                EffectName = "h_voice_vfx",
            };

            var liquidEffect1 = new MapFightEffectAddLiquidCfg()
            {
                ElementType = EGroundLiquidType.Milk,
                Range = 1.5f,
                Duration = 10,

                OffsetRange = 1.0f,
            };
            

            var liquidEffect2 = new MapFightEffectAddLiquidCfg()
            {
                ElementType = EGroundLiquidType.Milk,
                Range = 1.5f,
                Duration = 10,

                OffsetRange = 1.5f,
            };

            var liquidEffect3 = new MapFightEffectAddLiquidCfg()
            {
                ElementType = EGroundLiquidType.Milk,
                Range = 1.5f,
                Duration = 10,

                OffsetRange = 2.0f,
            };

            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = liquidEffect1, Kind = PhaseEventKind.Timed, TimeOffset = 0.0f });
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = hEffect, Kind = PhaseEventKind.Timed, TimeOffset = 0.0f });
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = hVfxEffect, Kind = PhaseEventKind.Timed, TimeOffset = 0.0f });

            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = liquidEffect2, Kind = PhaseEventKind.Timed, TimeOffset = 0.4f });
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = hEffect, Kind = PhaseEventKind.Timed, TimeOffset = 0.4f });
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = hVfxEffect, Kind = PhaseEventKind.Timed, TimeOffset = 0.4f });

            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = liquidEffect3, Kind = PhaseEventKind.Timed, TimeOffset = 0.8f });
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = hEffect, Kind = PhaseEventKind.Timed, TimeOffset = 0.8f });
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = hVfxEffect, Kind = PhaseEventKind.Timed, TimeOffset = 0.8f });

            spec.Phases.Add(mainPhase);
            return spec;
        }
        


        private static MapAbilitySpecConfig CreatePlayerFQDashAssult()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_fq_dash_assult";
            spec.TypeTag = AbilityTypeTag.HMode;
            spec.DefaultStepDistance = 0f;


            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockMovement = true,
                LockRotation = true,
                ImmuneKnock = true,

                PhaseBuff = new List<string>() { "supor_armor", "immune_fear", "b_player_fq_assult_speedup" },
                
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "1.6"
                },
            };

            // 随机选择范围内单位作为target
            var randomSelectCfg = new MapFightEffectOverrideTargetCfg()
            {
                IsRandomPick = true
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = randomSelectCfg, Kind = PhaseEventKind.OnEnter});

            // 自己被目标吸引
            var addBuffCfg = new MapAbilityEffectAddBuffCfg()
            {
                RevertAdd = true,
                BuffId = "lured",
                Duration = 1.6f,
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = addBuffCfg, Kind = PhaseEventKind.OnEnter });

            spec.Phases.Add(mainPhase);
            return spec;
        }
        
        private static MapAbilitySpecConfig CreatePlayerSmallStarggering()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_small_staggering";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.DefaultStepDistance = 0f;
            spec.CastType = ECastType.ToFace;
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
                    RawVal = "0.12"
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
                    RawVal = "0.24"
                },
            };

            var newEffect = new MapAbilityEffectHitBoxCfg()
            {
                Shape = MapAbilityEffectHitBoxCfg.EShape.Direction,
                Width = 0.8f,
                Length = 0.6f,
                CampFilterType = ECampFilterType.NotSelf,

                HitResult = new()
                {
                    OnHitEffects = new()
                    {
                        new MapAbilityEffectAddBuffCfg()
                        {
                            BuffId = "force_stun",
                            Duration = 3.0f,
                        },
                    }
                }
                
            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });

            spec.Phases.Add(mainPhase);
            return spec;
            
        }

        private static MapAbilitySpecConfig CreatePlayerPushSurround()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "player_push_surround";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.DefaultStepDistance = 0f;
            spec.CastType = ECastType.NoTarget;
            //spec.CoolDown = 0.2f;
            //spec.DesiredUseDistance = 0.5f;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                AnimTag = "准备",
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
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
                    RawVal = "0.1"
                },
            };

            var newEffect = new MapAbilityEffectHitBoxCfg()
            {
                Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                Radius = 1.5f,
                CampFilterType = ECampFilterType.NotSelf,

                HitResult = new()
                {
                    OnHitEffects = new()
                    {
                        new MapAbilityEffectAddBuffCfg()
                        {
                            BuffId = "player_push_surround_debuff",
                            Duration = 5.0f,
                        },
                        new MapFightEffectKnockBackCfg()
                        {
                            KnockBackForce = 1.5f,
                            DirType = MapFightEffectKnockBackCfg.EKnockBackType.CastDir,
                        },
                    }
                }

            };
            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });

            spec.Phases.Add(mainPhase);
            return spec;

        }

        private static MapAbilitySpecConfig CreateUseHumanWeapon()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "use_human_weapon";
            spec.TypeTag = AbilityTypeTag.Combat;
            spec.DefaultStepDistance = 0.4f;
            spec.CastType = ECastType.NoTarget;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                LockRotation = true,
                AffectByAtkSpeed = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.05"
                },
            });

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockMovement = true,
                LockRotation = true,
                ImmuneKnock = true,
                AnimTag = "attack_01",
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3"
                },
            };

            var newEffect = new MapAbilityEffectUseWeaponCfg()
            {
                WeaponName = My.Player.HumanWeaponCatalog.ViewKey,
                AnimName = "player_use_human_weapon_melee",
                Duration = 0.3f,
                OnHitEffects = new()
                {
                    new MapFightEffectApplyDamageCfg()
                    {
                        BaseDamage = 0,
                        ExtraDamageRate = new()
                        {
                            new AttrKvPair { AttrId = AttrIdConsts.CastWeaponLevel, Val = 10000 },
                        },
                        KnockBackForce = 0.1f,
                    },
                    new MapAbilityEffectAddResourceCfg()
                    {
                        ResourceId = AttrIdConsts.UnitKnockDown,
                        AddValueFromAttrId = AttrIdConsts.CastStunValue,
                    },
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
            spec.DefaultStepDistance = 0.4f;

            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1.0f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;


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
                Duration = 0.24f,
                OnHitEffects = new()
                {

                    new MapFightEffectApplyDamageCfg()
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
            spec.DefaultStepDistance = 0.4f;

            spec.CastType = ECastType.NoTarget;
            spec.DesiredUseDistance = 1.0f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

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

                    new MapFightEffectApplyDamageCfg()
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

            //spec.SelectPolicy = FightStruct.ESelectPolicy.PrimaryTarget;

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
                SelfAdd = true,
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


        private static MapAbilitySpecConfig CreatePlayerPutDown()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();
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

            spec.CastType = ECastType.LockTarget;

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

            spec.CastType = ECastType.Circle;
            spec.Range1 = 2.5f;
            spec.Range2 = 2.0f;

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

                    HitResult = new()
                    {
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
                    }
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

            spec.CastType = ECastType.Point;
            spec.Range1 = 2.9f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

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
                    DashMode = EDashMode.ToTarget,
                    DashSpeed = 6f,
                    MaxDistance = 3.0f,
                    DashWeaponName = "Catch",
                    DirMode = EDirMode.LookDir,
                    OnHitEffects = new()
                    {
                        // 提前进入下一phase
                        new MapAbilityEffectNextPhaseCfg()
                        {
                            MatchPhase = "Executing",
                            MatchSkill = "evil_child_attach"
                        },
                        new MapAbilityEffectConvertAttachCfg()
                        {
                            AttachId = "evil_child_attach",
                        },
                    },
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = dashEffect, Kind = PhaseEventKind.OnEnter });
            }

            //{
            //    var hitEffect = new MapAbilityEffectUseWeaponCfg()
            //    {
            //        WeaponName = "Catch",
            //        Duration = 0.4f,
            //        MaxHit = 1,
            //        OnHitEffects = new()
            //        {
            //            new MapAbilityEffectConvertAttachCfg()
            //            {
            //                AttachId = "evil_child_attach",
            //            },
            //        }
            //    };
            //    mainPhase.Events.Add(new PhaseEffectEvent() { Effect = hitEffect, Kind = PhaseEventKind.OnEnter });
            //}
            
            spec.Phases.Add(mainPhase);
            return spec;
        }
    
        
        private static MapAbilitySpecConfig CreateEvilChildInsertionAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "evil_child_insertion";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.CastType = ECastType.LockTarget;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

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
            spec.CastType = ECastType.NoTarget;


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
                    SelfAdd = true,
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
            spec.CastType = ECastType.LockTarget;

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
                    ResourceId = AttrIdConsts.NPCHVal,
                    AddValue = 100_000,
                    IsEnmity = false,
                };

                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = addHEffect, Kind = PhaseEventKind.OnExit });
            }

            spec.Phases.Add(mainPhase);

            return spec;
        }

        private static MapAbilitySpecConfig CreatePlayerHModeControl()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "h_mode_control";
            spec.TypeTag = AbilityTypeTag.Utility;
            spec.CastType = ECastType.LockTarget;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Controlling",
                LockRotation = true,
                LockMovement = true,
                ImmuneKnock = true,
                PhaseBuff = new() { "super_armor", "phase_move" },
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.6"
                },
            };

            {
                var addStunEffect = new MapAbilityEffectAddBuffCfg()
                {
                    BuffId = "force_stun",
                    Duration = 0.6f,
                    Layer = 1,
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = addStunEffect, Kind = PhaseEventKind.OnEnter });
            }

            {
                var closeToEffect = new MapFightEffectSpecialMoveToCfg()
                {
                    Duration = 0.25f,
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = closeToEffect, Kind = PhaseEventKind.OnEnter });
            }

            {
                var enterControlEffect = new MapFightEffectNpcDirectControlCfg()
                {
                    InEnter = true,
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = enterControlEffect, Kind = PhaseEventKind.OnExit });
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

                EnterDebugString = "满",

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "1"
                },
            };
            spec.Phases.Add(mainPhase);

            {
                var blurEffect = new MapFightEffectHModeBlurtCfg();

                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = blurEffect, Kind = PhaseEventKind.OnExit });
            }
            return spec;
        }


        private static MapAbilitySpecConfig CreateNpcStaticDoing()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "npc_static_doing";
            spec.TypeTag = AbilityTypeTag.Utility;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                LockRotation = true,
                ImmuneKnock = true,

                InterruptMask = EAbilityInterruptMask.Move | EAbilityInterruptMask.Hit | EAbilityInterruptMask.Cast,

                AnimTag = "special_doing",

                HoldingPhase = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "99.0"
                },
            };

            spec.Phases.Add(mainPhase);

            return spec;

        }

        private static MapAbilitySpecConfig CreateNpcCloseKaiyou()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "close_kaiyou";
            spec.TypeTag = AbilityTypeTag.Utility;

            spec.CastType = ECastType.LockTarget;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Execute",
                LockRotation = true,
                LockMovement = true,
                ImmuneKnock = true,

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.2"
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
                buffEffect.SelfAdd = true;
                buffEffect.BuffId = "dark_dance";
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = buffEffect, Kind = PhaseEventKind.OnExit });
            }

            spec.Phases.Add(mainPhase);
            return spec;
        }

        private static MapAbilitySpecConfig CreateChongZhuangAbility()
        {
            var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

            spec.Id = "chongzhuang";
            spec.TypeTag = AbilityTypeTag.Combat;

            spec.CastType = ECastType.Directional;
            spec.Range1 = 2f;
            spec.TargetSelectPolicy = FightStruct.ETargetSelectPolicy.PrimaryTarget;

            spec.Phases.Add(new MapAbilityPhase()
            {
                PhaseName = "Pre",
                LockMovement = true,
                EnterDebugString = "冲",

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
                AnimTag = "catch",
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.3"
                },
            };

            {
                var dashEffect = new MapAbilityEffectDashStartCfg()
                {
                    DashMode = EDashMode.FixDistance,
                    MaxDistance = 1.5f,
                    DashSpeed = 9f,
                    DashWeaponName = "Catch",
                    DirMode = EDirMode.LookDir,
                    OnHitEffects = new()
                    {
                        //// 提前进入下一phase
                        //new MapAbilityEffectNextPhaseCfg()
                        //{
                        //    MatchPhase = "Dashing",
                        //    MatchSkill = "default_dash_slash"
                        //},
                        new MapFightEffectApplyDamageCfg()
                        {
                            BaseDamage = 7000,
                            KnockBackForce = 0.5f,
                        },

                        new MapAbilityEffectCostResourceCfg()
                        {
                            ResourceId = AttrIdConsts.PlayerClothes,
                            CostValue = 5000,
                        },
                    },
                };
                mainPhase.Events.Add(new PhaseEffectEvent() { Effect = dashEffect, Kind = PhaseEventKind.OnEnter });
            }

            //{
            //    var hitEffect = new MapAbilityEffectUseWeaponCfg()
            //    {
            //        WeaponName = "Catch",
            //        Duration = 0.6f,
            //        MaxHit = 1,
            //        OnHitEffects = new()
            //        {
            //            new MapAbilityEffectApplyDamageCfg()
            //            {
            //                BaseDamage = 7000,
            //                KnockBackForce = 0.5f,
            //            },

            //            new MapAbilityEffectCostResourceCfg()
            //            {
            //                ResourceId = AttrIdConsts.PlayerClothes,
            //                CostValue = 5000,
            //            },
            //        }
            //    };
            //    mainPhase.Events.Add(new PhaseEffectEvent() { Effect = hitEffect, Kind = PhaseEventKind.OnEnter });
            //}

            spec.Phases.Add(mainPhase);
            return spec;
        }
    }


}
