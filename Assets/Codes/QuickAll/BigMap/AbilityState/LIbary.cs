using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{
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

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
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

            spec.Id = "player_weapon";
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
                LockMovement = true,
                LockRotation = true,
                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.2"
                },
            };

            var newEffect = new MapAbilityEffectUseWeaponCfg()
            {
                WeaponName = "Weapon01",
                Duration = 0.3f,
                OnHitEffects = new()
            {
                new MapAbilityEffectCostResourceCfg()
                {
                    ResourceId = AttrIdConsts.HP,
                    CostValue = 25,
                    Flags = 1,
                }
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
            spec.DesiredUseDistance = 0.5f;

            spec.CoolDown = 2.0f;

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
            spec.CoolDown = 6.0f;
            spec.DesiredUseDistance = 0.8f;
            spec.Priority = 100;

            var mainPhase = new MapAbilityPhase()
            {
                PhaseName = "Executing",
                EnterDebugString = "准备抓取",

                DurationValue = new()
                {
                    ValType = EOneVariatyType.Float,
                    RawVal = "0.5"
                },
            };

            var hitCfg = new MapAbilityEffectHitBoxCfg();
            hitCfg.EffectType = EAbilityEffectType.HitBox;
            hitCfg.Shape = MapAbilityEffectHitBoxCfg.EShape.Square;
            hitCfg.Width = 2f;
            hitCfg.Length = 2f;
            hitCfg.TargetEntityType = EEntityType.Player;

            {

                var failEffect = new MapAbilityEffectCostResourceCfg();
                failEffect.ResourceId = AttrIdConsts.HP;
                failEffect.CostValue = 5;
                failEffect.Flags = 1;

                var throwEffect = new MapAbilityEffectThrowStartCfg();

                throwEffect.ThrowMainBuffId = "be_fcked";
                throwEffect.Priority = 1;
                throwEffect.Duration = 2.0f;
                throwEffect.ThrowFailEffect = failEffect;


                hitCfg.OnHitEffects = new() { throwEffect };
            }

            mainPhase.Events.Add(new PhaseEffectEvent() { Effect = hitCfg, Kind = PhaseEventKind.OnExit });

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
    }

}
