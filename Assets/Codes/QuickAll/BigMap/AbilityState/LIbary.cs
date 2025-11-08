using Map.Entity;
using Map.Entity.Attr;
using System;
using System.Collections;
using System.Collections.Generic;
using Unit.Ability.Effect;
using UnityEngine;


public static class AbilityLibrary
{
    public static MapAbilitySpecConfig CreateDefaultUnlockLootPoint()
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

    public static MapAbilitySpecConfig CreateDefaultUseLootPoint()
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

    public static MapAbilitySpecConfig CreateDefaultUseItem()
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

    public static MapAbilitySpecConfig CreateDefaultDash()
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
    


    public static MapAbilitySpecConfig CreateDefaultShootAbility()
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

    public static MapAbilitySpecConfig CreateDefaultUseWeaponAbility()
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


    public static MapAbilitySpecConfig CreateOrRefreshZhaQu()
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
            WithProgress = true,
            DurationValue = new()
            {
                ValType = EOneVariatyType.Float,
                ReferName = "ExecutingTime"
            },
        };

        var newEffectGive = new MapAbilityEffectAddBuffCfg()
        {
            BuffId = "beizha",
            Layer = 1,
            Duration = -1,
            TargetType = 0,
        };
        mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffectGive, Kind = PhaseEventKind.OnEnter });


        //var newEffectSelf = new MapAbilityEffectAddBuffCfg()
        //{
        //    targetType = MapAbilityEffectSpawnBulletCfg.ETargetType.Dir,
        //    motionType = EMotionType.Linear,
        //    lifeTime = 0.6f,
        //    speed = 9f,
        //};
        //mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });


        var effectRemoveZha = new MapAbilityEffectRemoveBuffCfg()
        {
            BuffId = "beizha",
            Layer = 1,
            TargetType = 0,
        };
        mainPhase.Events.Add(new PhaseEffectEvent() { Effect = effectRemoveZha, Kind = PhaseEventKind.OnExit });

        spec.Phases.Add(mainPhase);
        return spec;
    }


    public static MapAbilitySpecConfig CreateDefaultMonsterAttack()
    {
        var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

        spec.Id = "attack";
        spec.TypeTag = AbilityTypeTag.Combat;

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
            Width = 3,
            Length = 1,
        };
        mainPhase.Events.Add(new PhaseEffectEvent() { Effect = newEffect, Kind = PhaseEventKind.OnEnter });

        spec.Phases.Add(mainPhase);
        return spec;
    }


    public static MapAbilitySpecConfig CreateDefaultEnemyQinfan()
    {
        var spec = ScriptableObject.CreateInstance<MapAbilitySpecConfig>();

        spec.Id = "default_enemy_qinfan";
        spec.TypeTag = AbilityTypeTag.Combat;

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
        hitCfg.Width = 0.5f;
        hitCfg.Length = 0.5f;

        {
            var ifBranch = new MapAbilityEffectIfBranchCfg();
            ifBranch.CheckType = MapAbilityEffectIfBranchCfg.ECheckType.AttrGreater;
            ifBranch.Param1 = AttrIdConsts.NoSelect;
            ifBranch.Param3 = 0;

            ifBranch.TrueBranchEffects = new();
            var trueEffect = new MapAbilityEffectCostResourceCfg();
            trueEffect.ResourceId = AttrIdConsts.HP;
            trueEffect.CostValue = 5;

            ifBranch.TrueBranchEffects.Add(trueEffect);

            ifBranch.FalseBranchEffects = new();
            var falseEffect = new MapAbilityEffectThrowStartCfg();

            falseEffect.ThrowMainBuffId = "qinfaning";
            falseEffect.Priority = 1;
            falseEffect.Duration = 2.0f;

            ifBranch.FalseBranchEffects.Add(falseEffect);


            hitCfg.OnHitEffects = new() { ifBranch };
        }

        mainPhase.Events.Add(new PhaseEffectEvent() { Effect = hitCfg, Kind = PhaseEventKind.OnEnter });

        spec.Phases.Add(mainPhase);
        return spec;
    }
}
