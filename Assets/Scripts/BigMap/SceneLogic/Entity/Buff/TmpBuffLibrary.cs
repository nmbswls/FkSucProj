using My.Map;
using System.Collections.Generic;

namespace My.Map.Entity
{

    public static class BuffLibrary
    {
        public static Dictionary<string, BuffDefinition> _library;
        public static BuffDefinition GetBuffDefinition(string buffId)
        {
            if (_library == null)
            {
                _library = new();

                _library["talent_expose_extra_charm_1"] = new BuffDefinition()
                {
                    BuffId = "talent_expose_extra_charm_1",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    MaxStackLayer = 1,
                    ModifierAttrs = new()
                    {
                    },
                    DurationEffect = new BuffDurationEffet()
                    {
                        DurationType = EBuffDurationType.RelativeExposeLevel,
                        ParamStr1 = AttrIdConsts.PlayerCharm_Static,
                        ParamStr2 = "0:0,1:0,2:5000,3:5000,4:5000",
                        CommonFlag1 = true,
                    },
                    DefaultDuration = -1,
                };
                _library["talent_expose_extra_charm_2"] = new BuffDefinition()
                {
                    BuffId = "talent_expose_extra_charm_2",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    MaxStackLayer = 1,
                    ModifierAttrs = new()
                    {
                    },
                    DurationEffect = new BuffDurationEffet()
                    {
                        DurationType = EBuffDurationType.RelativeExposeLevel,
                        ParamStr1 = AttrIdConsts.PlayerCharm_Static,
                        ParamStr2 = "0:0,1:0,2:10000,3:10000,4:10000",
                        CommonFlag1 = true,
                    },
                    DefaultDuration = -1,
                };
                _library["talent_expose_extra_charm_3"] = new BuffDefinition()
                {
                    BuffId = "talent_expose_extra_charm_3",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    MaxStackLayer = 1,
                    ModifierAttrs = new()
                    {
                    },
                    DurationEffect = new BuffDurationEffet()
                    {
                        DurationType = EBuffDurationType.RelativeExposeLevel,
                        ParamStr1 = AttrIdConsts.PlayerCharm_Static,
                        ParamStr2 = "0:0,1:0,2:10000,3:10000,4:10000",
                        CommonFlag1 = true,
                    },
                    DefaultDuration = -1,
                };

                _library["b_lamp_extra_vision"] = new BuffDefinition()
                {
                    BuffId = "b_lamp_extra_vision",

                    Desc = "灯的视野",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.UnitVisionRangeMul, ModifierValue = 2000 } },
                    DefaultDuration = -1,

                    Icon = "b_lamp_extra_vision",
                };
                

                _library["player_expose_charm"] = new BuffDefinition()
                {
                    BuffId = "player_expose_charm",

                    Desc = "魅力暴露",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.PlayerCharm, ModifierValue = 5000 } },
                    DefaultDuration = -1,

                    Icon = "player_expose_charm",
                };

                _library["desire_level_charm"] = new BuffDefinition()
                {
                    BuffId = "desire_level_charm",

                    Desc = "",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.PlayerCharm, ModifierValue = 1 } },
                    DefaultDuration = -1,
                    IsHidden = true,
                };
                _library["desire_level_damage_resist"] = new BuffDefinition()
                {
                    BuffId = "desire_level_damage_resist",

                    Desc = "",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.NonH_JianShang_Rate, ModifierValue = 1 } },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                //

                _library["player_naishou_to_jianshang"] = new BuffDefinition()
                {
                    BuffId = "player_naishou_to_jianshang",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    DefaultDuration = -1,
                    IsHidden = true,

                    DurationEffect = new BuffDurationEffet()
                    {
                        ParamStr1 = AttrIdConsts.PhysicalResist,
                        ParamStr2 = AttrIdConsts.Final_Fix_DR_All,
                        ParamFloat1 = 0.2f,
                    }
                };

                _library["player_burst_h_voice"] = new BuffDefinition()
                {
                    BuffId = "player_burst_h_voice",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = -1,
                    IsHidden = true,

                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.PlayerHVoice,
                            OutputFightEffects = new()
                            {
                                new MapAbilityEffectHitBoxCfg()
                                {
                                    Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                                    Width = 2.5f,
                                    CampFilterType = ECampFilterType.NotSelf,

                                    HitResult = new()
                                    {
                                        OnHitEffects = new()
                                        {
                                            new MapAbilityEffectAddBuffCfg()
                                            {
                                                BuffId = "force_stun",
                                                Duration = 1.0f,
                                            },
                                            new MapAbilityEffectAddResourceCfg()
                                            {
                                                ResourceId = AttrIdConsts.NPCHVal,
                                                AddValue = 15_000,
                                            },
                                        }
                                    }
                                },

                            }
                        }
                    },
                };

                _library["player_burst_milk"] = new BuffDefinition()
                {
                    BuffId = "player_burst_milk",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = -1,
                    IsHidden = true,

                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            ShowSpecific = true,

                            TriggerType = ETriggerType.FinalDmgReduced,
                            NeedCount = 6000,

                            OutputFightEffects = new()
                            {
                                new MapFightEffectEasyEffect()
                                {
                                    EffectText = "爆乳",
                                },
                                new MapFightEffectShowEffect()
                                {
                                    ShowMode = MapFightEffectShowEffect.EShowMode.TriggerPos,
                                    EffectName = "h_voice_vfx",
                                },
                                
                                 new MapFightEffectAddLiquidCfg()
                                {
                                    ElementType = EGroundLiquidType.Milk,
                                    Range = 1.5f,
                                    Duration = 10,
                                },
                                new MapAbilityEffectHitBoxCfg()
                                {
                                    Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                                    Width = 2.5f,
                                    CampFilterType = ECampFilterType.NotSelf,

                                    HitResult = new()
                                    {
                                        OnHitEffects = new()
                                        {
                                            new MapAbilityEffectAddBuffCfg()
                                            {
                                                BuffId = "force_stun",
                                                Duration = 1.0f,
                                            },
                                            new MapAbilityEffectAddResourceCfg()
                                            {
                                                ResourceId = AttrIdConsts.NPCHVal,
                                                AddValue = 15_000,
                                            },

                                           
                                        }
                                    }
                                },

                            }
                        }
                    },
                };

                _library["b_player_fq_assult_speedup"] = new BuffDefinition()
                {
                    BuffId = "b_player_fq_assult_speedup",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = -1,

                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = 3000 } },
                };

                _library["b_player_fq_hit_hop"] = new BuffDefinition()
                {
                    BuffId = "b_player_fq_hit_hop",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = -1,
                    IsHidden = true,

                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            ShowSpecific = true,

                            TriggerType = ETriggerType.Tick,
                            TriggerParam1 = 500,

                            OutputFightEffects = new()
                            {
                                new MapFightEffectEasyEffect()
                                {
                                    EffectText = "震",
                                },
                                 
                                new MapAbilityEffectHitBoxCfg()
                                {
                                    Shape = MapAbilityEffectHitBoxCfg.EShape.Circle,
                                    Width = 2.5f,
                                    CampFilterType = ECampFilterType.NotSelf,

                                    HitResult = new()
                                    {
                                        OnHitEffects = new()
                                        {
                                            new MapAbilityEffectAddBuffCfg()
                                            {
                                                BuffId = "simple_one_jiansu",
                                                Layer = 30,
                                                Duration = 2.0f,
                                            },
                                            new MapFightEffectApplyHImpulseCfg()
                                            {
                                                BaseVal = 15_000,
                                            },
                                        }
                                    }
                                },

                            }
                        }
                    },
                };

                _library["charm_fck_bonus"] = new BuffDefinition()
                {
                    BuffId = "charm_fck_bonus",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.PlayerJingYuRate, ModifierValue = -1500}},

                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["charm_fck_deeper"] = new BuffDefinition()
                {
                    BuffId = "charm_fck_deeper",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.DesireDensityAmplify, ModifierValue = 100 } },

                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["force_fck_bonus"] = new BuffDefinition()
                {
                    BuffId = "force_fck_bonus",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.PlayerJingYuRate, ModifierValue = 4000 } },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["lock_move"] = new BuffDefinition()
                {
                    BuffId = "lock_move",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 } },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["chain_bind"] = new BuffDefinition()
                {
                    BuffId = "chain_bind",
                    Desc = "锁链捆缚",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = 3f,
                    EffectId = "Buff/chain_bind",
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 },
                    },
                    OnAttachEffects = new()
                    {
                        new MapFightEffectShowEffect()
                        {
                            ShowMode = MapFightEffectShowEffect.EShowMode.TargetAligned,
                            EffectName = "Buff/chain_bind_spawn",
                        },
                    },
                };
                _library["as_presentation"] = new BuffDefinition()
                {
                    BuffId = "as_presentation",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = -1,
                    IsHidden = true,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ImmuneKnock, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_JianShang, ModifierValue = 10000 },
                    },
                };
                _library["lock_face"] = new BuffDefinition()
                {
                    BuffId = "lock_face",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.LockFace, ModifierValue = 1 } },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["fast_turn"] = new BuffDefinition()
                {
                    BuffId = "fast_turn",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.FastTurn, ModifierValue = 1 } },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["simple_knock_down"] = new BuffDefinition()
                {
                    BuffId = "simple_knock_down",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    MaxStackLayer = 1,
                    Icon = "simple_knock_down",
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Stun, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.LockFace, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 },
                    },
                    
                    DefaultDuration = 2f,
                    HeadHintPriority = 100,
                };

                _library["fear"] = new BuffDefinition()
                {
                    BuffId = "fear",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    MaxStackLayer = 1,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Fear, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 },
                    },
                    DurationEffect = new BuffDurationEffet()
                    {
                        DurationType = EBuffDurationType.SteerInput,
                        ParamStr1 = nameof(EBuffMoveSteerMode.AwayFromCaster),
                        ParamFloat1 = 1f,
                        ParamFloat2 = 0.2f,
                    },
                    DefaultDuration = 3f,
                    Icon = "fallback",
                    HeadHintPriority = 80,
                };

                _library["immune_fear"] = new BuffDefinition()
                {
                    BuffId = "immune_fear",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ImmuneFear, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ImmuneSteerInput, ModifierValue = 1 },
                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["b_player_fq_lured"] = new BuffDefinition()
                {
                    BuffId = "b_player_fq_lured",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    MaxStackLayer = 1,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Lured, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 },
                    },
                    DurationEffects = new()
                    {
                        new BuffDurationEffet()
                        {
                            DurationType = EBuffDurationType.SteerInput,
                            ParamStr1 = nameof(EBuffMoveSteerMode.TowardCaster),
                            ParamFloat1 = 1f,
                            ParamFloat2 = 0.2f,
                        },
                        new BuffDurationEffet()
                        {
                            DurationType = EBuffDurationType.NearCasterWatch,
                            ParamFloat1 = 0.35f,
                        },
                    },
                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.NearCaster,
                            RemoveOnTrigger = true,
                            OutputFightEffects = new()
                            {
                                new MapFightEffectShowCloseupWindowCfg()
                                {
                                    WindowType = "htangle",
                                    Duration = 5f,
                                },
                            },
                        },
                    },
                    DefaultDuration = 3f,
                    Icon = "fallback",
                };

                _library["lured"] = new BuffDefinition()
                {
                    BuffId = "lured",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    MaxStackLayer = 1,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Lured, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 },
                    },
                    DurationEffect = new BuffDurationEffet()
                    {
                        DurationType = EBuffDurationType.SteerInput,
                        ParamStr1 = nameof(EBuffMoveSteerMode.TowardCaster),
                        ParamFloat1 = 1f,
                        ParamFloat2 = 0.2f,
                    },
                    DefaultDuration = 3f,
                    Icon = "fallback",
                };

                _library["immune_lured"] = new BuffDefinition()
                {
                    BuffId = "immune_lured",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ImmuneLured, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ImmuneSteerInput, ModifierValue = 1 },
                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["immune_knock"] = new BuffDefinition()
                {
                    BuffId = "immune_knock",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ImmuneKnock, ModifierValue = 1 } },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["super_armor"] = new BuffDefinition()
                {
                    BuffId = "super_armor",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.SuperArmor, ModifierValue = 1 } },
                    DefaultDuration = -1,
                };

                _library["immune_kaiyou"] = new BuffDefinition()
                {
                    BuffId = "immune_kaiyou",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ImmumeKaiYou, ModifierValue = 1 } },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["player_faqing"] = new BuffDefinition()
                {
                    BuffId = "player_faqing",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    //ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ImmumeKaiYou, ModifierValue = 1 } },
                    DefaultDuration = -1,
                };

                _library["player_hungry"] = new BuffDefinition()
                {
                    BuffId = "player_hungry",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = 2500 } },
                    DefaultDuration = -1,

                    Icon = "player_hungry",
                };

                _library["player_zhazhi"] = new BuffDefinition()
                {
                    BuffId = "player_zhazhi",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.PlayerZhaZhiMode, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.NoKiller, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_ExtraDmg, ModifierValue = -9000 },
                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["status_burn"] = new BuffDefinition()
                {
                    BuffId = "status_burn",
                    Desc = "烧伤",
                    Icon = "status_burn",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = 5f,
                    PotencyCopyAttrs = new()
                    {
                        new BuffDefinition.OneModPair()
                        {
                            ModifierAttrId = AttrIdConsts.PlayerSpellPower,
                        },
                    },
                    PotencyBase = 150,
                    PotencyCalcRates = new()
                    {
                        new BuffDefinition.OneModPair()
                        {
                            ModifierAttrId = AttrIdConsts.PlayerSpellPower,
                            ModifierValue = 10000,
                        },
                    },
                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.Tick,
                            TriggerParam1 = 800,
                            OutputFightEffects = new()
                            {
                                new MapFightEffectApplyDamageCfg()
                                {
                                    BaseDamage = 150,
                                    ExtraDamageRate = new()
                                    {
                                        new AttrKvPair { AttrId = AttrIdConsts.PlayerSpellPower, Val = 10000 },
                                    },
                                    DamageCategory = EDmgCategory.Magic,
                                },
                            },
                        },
                    },
                };

                _library["status_yuhuo"] = new BuffDefinition()
                {
                    BuffId = "status_yuhuo",
                    Desc = "浴火",
                    Icon = "status_yuhuo",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = 5f,
                    PotencyCopyAttrs = new()
                    {
                        new BuffDefinition.OneModPair()
                        {
                            ModifierAttrId = AttrIdConsts.PlayerSpellPower,
                        },
                    },
                    PotencyBase = 80,
                    PotencyCalcRates = new()
                    {
                        new BuffDefinition.OneModPair()
                        {
                            ModifierAttrId = AttrIdConsts.PlayerSpellPower,
                            ModifierValue = 8000,
                        },
                    },
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.HValYiShang, ModifierValue = 1000 },
                    },
                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.Tick,
                            TriggerParam1 = 800,
                            OutputFightEffects = new()
                            {
                                new MapFightEffectApplyDamageCfg()
                                {
                                    ResourceId = AttrIdConsts.NPCHVal,
                                    BaseDamage = 80,
                                    ExtraDamageRate = new()
                                    {
                                        new AttrKvPair { AttrId = AttrIdConsts.PlayerSpellPower, Val = 8000 },
                                    },
                                    IsEnmity = true,
                                },
                            },
                        },
                    },
                };

                _library["status_freeze"] = new BuffDefinition()
                {
                    BuffId = "status_freeze",
                    Desc = "严寒",
                    Icon = "status_freeze",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = 6f,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = -3500 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_JianShang, ModifierValue = 4000 },
                    },
                };

                _library["status_poison"] = new BuffDefinition()
                {
                    BuffId = "status_poison",
                    Desc = "中毒",
                    Icon = "status_poison",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = 8f,
                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.Tick,
                            TriggerParam1 = 1000,
                            OutputFightEffects = new()
                            {
                                new MapFightEffectResourcePercentDamageCfg()
                                {
                                    ResourceId = AttrIdConsts.HP,
                                    RateBp = 50,
                                    IsEnmity = true,
                                },
                            },
                        },
                    },
                };

                _library["status_bleed"] = new BuffDefinition()
                {
                    BuffId = "status_bleed",
                    Desc = "流血",
                    Icon = "status_bleed",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = 5f,
                    PotencyCopyAttrs = new()
                    {
                        new BuffDefinition.OneModPair()
                        {
                            ModifierAttrId = AttrIdConsts.PlayerSpellPower,
                        },
                    },
                    PotencyBase = 150,
                    PotencyCalcRates = new()
                    {
                        new BuffDefinition.OneModPair()
                        {
                            ModifierAttrId = AttrIdConsts.PlayerSpellPower,
                            ModifierValue = 10000,
                        },
                    },
                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.Tick,
                            TriggerParam1 = 800,
                            OutputFightEffects = new()
                            {
                                new MapFightEffectApplyDamageCfg()
                                {
                                    BaseDamage = 150,
                                    ExtraDamageRate = new()
                                    {
                                        new AttrKvPair { AttrId = AttrIdConsts.PlayerSpellPower, Val = 10000 },
                                    },
                                    DamageCategory = EDmgCategory.Physics,
                                    IsEnmity = true,
                                },
                            },
                        },
                    },
                };

                _library["status_yijin"] = new BuffDefinition()
                {
                    BuffId = "status_yijin",
                    Desc = "遗精",
                    Icon = "status_yijin",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    MaxStackLayer = 10,
                    DefaultDuration = 8f,
                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.Tick,
                            TriggerParam1 = 8000,
                            OutputFightEffects = new()
                            {
                                new MapFightEffectMiniBlurtCfg()
                                {
                                    LayerScaleUsage = EBuffLayerScaleUsage.Custom,
                                    BaseSjAmount = 0.25f,
                                    FixedSjDamage = 0.4f,
                                },
                                new MapFightEffectAddLiquidCfg()
                                {
                                    ElementType = EGroundLiquidType.GcLiquid,
                                    Range = 0.4f,
                                    Duration = 8f,
                                },
                            },
                        },
                    },
                };

                _library["status_stiff"] = new BuffDefinition()
                {
                    BuffId = "status_stiff",
                    Desc = "僵直",
                    Icon = "status_stiff",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = 6f,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = -3500 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_JianShang, ModifierValue = 4000 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.NPCSJProgress_GainRate, ModifierValue = -5000 },
                    },
                };

                _library["status_yindu"] = new BuffDefinition()
                {
                    BuffId = "status_yindu",
                    Desc = "淫毒",
                    Icon = "status_yindu",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = 8f,
                };

                _library["player_unlock_yuhuo"] = new BuffDefinition()
                {
                    BuffId = "player_unlock_yuhuo",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.PlayerUnlockYuhuo, ModifierValue = 1 },
                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["player_unlock_yindu"] = new BuffDefinition()
                {
                    BuffId = "player_unlock_yindu",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.PlayerUnlockYindu, ModifierValue = 1 },
                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["player_unlock_jiang"] = new BuffDefinition()
                {
                    BuffId = "player_unlock_jiang",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.PlayerUnlockJiang, ModifierValue = 1 },
                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["player_unlock_yijin"] = new BuffDefinition()
                {
                    BuffId = "player_unlock_yijin",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.PlayerUnlockYijin, ModifierValue = 1 },
                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["simple_one_jiansu"] = new BuffDefinition()
                {
                    BuffId = "simple_one_jiansu",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = -1,
                    IsHidden = true,

                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = -1000 },
                    },
                };

                // ????buff?????? ?????????
                // ??????? ????Χ???h?
                _library["player_clothes_expose"] = new BuffDefinition()
                {
                    BuffId = "player_clothes_expose",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    //ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ImmumeKaiYou, ModifierValue = 1 } },

                    DefaultDuration = -1,
                    IsHidden = true,
                };


                _library["player_push_surround_debuff"] = new BuffDefinition()
                {
                    BuffId = "player_push_surround_debuff",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = -3000 } },
                    DefaultDuration = -1,
                    IsHidden = true,
                };


                _library["social_charmed"] = new BuffDefinition()
                {
                    BuffId = "social_charmed",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Charmed, ModifierValue = 1 }
                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };


                _library["force_stun"] = new BuffDefinition()
                {
                    BuffId = "force_stun",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Stun, ModifierValue = 1 } ,
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.LockFace, ModifierValue = 1 },
                    },
                    DefaultDuration = -1,

                    Icon = "force_stun",
                    HeadHintPriority = 100,
                };

                _library["unit_stagger"] = new BuffDefinition()
                {
                    BuffId = "unit_stagger",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    MaxStackLayer = 1,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.UnitStagger, ModifierValue = 1 },
                    },
                    DefaultDuration = 1.2f,
                    Icon = "fallback",
                    HeadHintPriority = 88,
                    IsHidden = true,
                };

                _library["unit_knockfly"] = new BuffDefinition()
                {
                    BuffId = "unit_knockfly",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    MaxStackLayer = 1,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.UnitKnockfly, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.LockFace, ModifierValue = 1 },
                    },
                    DefaultDuration = 1.2f,
                    Icon = "fallback",
                    HeadHintPriority = 90,
                };

                _library["not_fight_target"] = new BuffDefinition()
                {
                    BuffId = "not_fight_target",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.NoSelect, ModifierValue = 1 } ,
                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["system_no_logic"] = new BuffDefinition()
                {
                    BuffId = "system_no_logic",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.NoSelect, ModifierValue = 1 } ,
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Sleep, ModifierValue = 1 } ,
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.LockFace, ModifierValue = 1 } ,
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 } ,
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 } ,
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.NoInteract, ModifierValue = 1 } ,
                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };


                _library["immune_evil_shock"] = new BuffDefinition()
                {
                    BuffId = "immune_evil_shock",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ImmuneEvilShock, ModifierValue = 1 } },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["evil_shock"] = new BuffDefinition()
                {
                    BuffId = "evil_shock",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Stun, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.LockFace, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 }
                    },
                    OnAttachEffects = new()
                    {
                        new MapFightEffectEasyEffect()
                        {
                            EffectText = "????"
                        },
                    },
                    HeadHintPriority = 1,
                    OnDetachEffects = new()
                    {
                        new MapFightEffectTriggerAlert()
                        {
                            AlertDuration = 15f,
                        }
                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["force_zha_target_buff"] = new BuffDefinition()
                {
                    BuffId = "force_zha_target_buff",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Stun, ModifierValue = 1 } ,
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.NpcFcked, ModifierValue = 1 }
                    },
                    
                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.Tick,
                            TriggerParam1 = 500, // ?0.2?????
                            //OutputEffects = new()
                            //{
                            //    new BuffEffectCfg()
                            //    {
                            //        EffectType = EBuffEffectType.ShowFx,
                            //    },
                            //},
                            OutputFightEffects = new()
                            {
                                new MapAbilityEffectAddResourceCfg()
                                {
                                    ResourceId = AttrIdConsts.NPCHVal,
                                    AddValue = 10_000,
                                    IsEnmity = true,
                                    //ExtraAttrInfos = new List<AttrKvPair>(){new(){ AttrId  = AttrIdConsts.DamageXiXue, Val = 2000} }
                                },
                                new MapFightEffectCauseNoise()
                                {
                                    
                                }
                            }
                        }
                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["give_hide_aura"] = new BuffDefinition()
                {
                    BuffId = "give_hide_aura",
                    DefaultDuration = -1,
                    AuraRange = 1.0f,
                    IsAura = true,
                    AuraBuffId = "give_hide",
                    IsHidden = true,
                };

                _library["give_hide"] = new BuffDefinition()
                {
                    BuffId = "give_hide",
                    DefaultDuration = -1,
                    LayerStackMode = EBuffLayerStackMode.IndependentStack,
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    MaxStackLayer = 1,

                    ModifierAttrs = new() {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.HideView, ModifierValue = 1 } ,
                    },
                    IsHidden = true,
                };

                _library["hide_marked"] = new BuffDefinition()
                {
                    BuffId = "hide_marked",
                    DefaultDuration = -1,
                    //ModifierAttrs = new() {
                    //    new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.HidingMask, ModifierValue = 1 } ,
                    //},
                    IsHidden = true,
                };

                _library["unsensored"] = new BuffDefinition()
                {
                    BuffId = "unsensored",
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["fcked_marked"] = new BuffDefinition()
                {
                    BuffId = "fcked_marked",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.NpcFcked, ModifierValue = 1 },

                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };


                _library["be_fcked"] = new BuffDefinition()
                {
                    BuffId = "be_fcked",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    ModifierAttrs = new() {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Stun, ModifierValue = 1 } ,
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.LockFace, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.NpcFcked, ModifierValue = 1 },

                    },
                    DurationEffect = new BuffDurationEffet()
                    {
                        DurationType = EBuffDurationType.AnimOverride,
                        ParamStr1 = "test",
                    },
                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.Tick,
                            TriggerParam1 = 200, // ?0.2?????
                            OutputFightEffects = new()
                            {
                                new MapAbilityEffectAddResourceCfg()
                                {
                                    ResourceId = AttrIdConsts.PlayerKnockDown,
                                    AddValue = 2,
                                    IsEnmity = true,
                                }
                            }
                        }
                    },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                // 玩家被推倒状态：进入被推倒动画，完全无法行动，持续固定时长后自动结束
                _library["player_knocked_down"] = new BuffDefinition()
                {
                    BuffId = "player_knocked_down",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Stun, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.LockFace, ModifierValue = 1 },
                    },
                    // AnimOverride: ParamStr1=被替换的基础 locomotion 名，ParamStr2=目标 clip 名
                    DurationEffects = new List<BuffDurationEffet>()
                    {
                        new BuffDurationEffet()
                        {
                            DurationType = EBuffDurationType.AnimOverride,
                            ParamStr1 = "idle",
                            ParamStr2 = "knocked_down",
                        },
                        new BuffDurationEffet()
                        {
                            DurationType = EBuffDurationType.AnimOverride,
                            ParamStr1 = "move",
                            ParamStr2 = "knocked_down",
                        },
                        new BuffDurationEffet()
                        {
                            DurationType = EBuffDurationType.AnimOverride,
                            ParamStr1 = "walk",
                            ParamStr2 = "knocked_down",
                        },
                    },
                    DefaultDuration = 3f,
                    IsHidden = false,
                };

                _library["immune_knockdown_closeup"] = new BuffDefinition()
                {
                    BuffId = "immune_knockdown_closeup",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = 10f,
                    IsHidden = true,
                };


                _library["jingyu"] = new BuffDefinition()
                {
                    BuffId = "jingyu",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    DefaultDuration = -1,
                };


                _library["jian_su_self"] = new BuffDefinition()
                {
                    BuffId = "jian_su_self",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = -8000 } },
                    DefaultDuration = -1,
                };

                // ??????????????????
                _library["player_crouch_stance"] = new BuffDefinition()
                {
                    BuffId = "player_crouch_stance",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = -4500 } },

                    DefaultDuration = -1,
                    IsHidden = true,
                };

                // ??????壺???????
                _library["player_carry_slow"] = new BuffDefinition()
                {
                    BuffId = "player_carry_slow",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = -7000 } },
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                // ????????????idle/move/walk -> carry_hold????? AnimHolder ??????? clip???????????????????????
                _library["player_carry_ov_idle"] = new BuffDefinition()
                {
                    BuffId = "player_carry_ov_idle",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = -1,
                    IsHidden = true,
                    DurationEffect = new BuffDurationEffet()
                    {
                        DurationType = EBuffDurationType.AnimOverride,
                        ParamStr1 = "idle",
                        ParamStr2 = "carry_hold",
                    },
                };

                _library["player_carry_ov_move"] = new BuffDefinition()
                {
                    BuffId = "player_carry_ov_move",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = -1,
                    IsHidden = true,
                    DurationEffect = new BuffDurationEffet()
                    {
                        DurationType = EBuffDurationType.AnimOverride,
                        ParamStr1 = "move",
                        ParamStr2 = "carry_hold",
                    },
                };

                _library["player_carry_ov_walk"] = new BuffDefinition()
                {
                    BuffId = "player_carry_ov_walk",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = -1,
                    IsHidden = true,
                    DurationEffect = new BuffDurationEffet()
                    {
                        DurationType = EBuffDurationType.AnimOverride,
                        ParamStr1 = "walk",
                        ParamStr2 = "carry_hold",
                    },
                };

                _library["throwing"] = new BuffDefinition()
                {
                    BuffId = "throwing",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.LockFace, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.SuperArmor, ModifierValue = 1 },
                    },
                    DefaultDuration = -1,
                };

                _library["phase_move"] = new BuffDefinition()
                {
                    BuffId = "phase_move",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.NoSelect, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Ghost, ModifierValue = 1 }
                    },
                    DefaultDuration = -1,
                };

                _library["phase_perfect_dodge"] = new BuffDefinition()
                {
                    BuffId = "phase_perfect_dodge",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = 0.15f,
                    IsHidden = true,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.PerfectDodgeWindow, ModifierValue = 1 },
                    },
                };


                // ?????黤??
                _library["gc_self_yishang"] = new BuffDefinition()
                {
                    BuffId = "gc_self_yishang",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    DefaultDuration = -1,

                    DurationEffect = new BuffDurationEffet()
                    {
                        DurationType = EBuffDurationType.HitEffect,
                        ParamStr1 = "Hit / player_shield",
                        ParamFloat1 = 0.3f,
                    },

                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Special_YiShang, ModifierValue = 1000 },
                    },
                };


                // ???gc????
                _library["gc_self_yishang"] = new BuffDefinition()
                {
                    BuffId = "gc_self_yishang",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    DefaultDuration = -1,

                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Special_YiShang, ModifierValue = 1000 },
                    },
                };




                _library["gc_self_debuff"] = new BuffDefinition()
                {
                    BuffId = "gc_self_debuff",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = 5f,

                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = -5000 },
                    },
                };

                _library["player_mini_gc_debuff"] = new BuffDefinition()
                {
                    BuffId = "player_mini_gc_debuff",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = PlayerGamePlayRule.MiniGcSlowDuration,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair()
                        {
                            ModifierAttrId = AttrIdConsts.Basic_MoveSpeed,
                            ModifierValue = PlayerGamePlayRule.MiniGcMoveSpeedPenalty,
                        },
                    },
                };

                _library["b_ground_gc_liquid"] = new BuffDefinition()
                {
                    BuffId = "b_ground_gc_liquid",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = -1,

                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.HValYiShang, ModifierValue = 1000 },
                    },

                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.Tick,
                            TriggerParam1 = 500, // ?0.5?????
                            OutputFightEffects = new()
                            {
                                new MapAbilityEffectAddResourceCfg()
                                {
                                    ResourceId = AttrIdConsts.NPCHVal,
                                    AddValue = 500,
                                    IsEnmity = true,
                                }
                            }
                        }
                    },
                };

                _library["b_ground_milk_liquid"] = new BuffDefinition()
                {
                    BuffId = "b_ground_milk_liquid",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = -1,

                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.HValYiShang, ModifierValue = 1000 },
                    },

                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.Tick,
                            TriggerParam1 = 500, // ?0.5?????
                            OutputFightEffects = new()
                            {
                                new MapAbilityEffectAddResourceCfg()
                                {
                                    ResourceId = AttrIdConsts.NPCHVal,
                                    AddValue = 500,
                                    IsEnmity = true,
                                }
                            }
                        }
                    },
                };

                _library["ground_fire_1"] = new BuffDefinition()
                {
                    BuffId = "ground_fire_1",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = -1,
                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.Tick,
                            TriggerParam1 = 800,
                            OutputFightEffects = new()
                            {
                                new MapAbilityEffectAddResourceCfg()
                                {
                                    ResourceId = AttrIdConsts.HP,
                                    AddValue = -200,
                                    IsEnmity = true,
                                }
                            }
                        }
                    },
                };

                // 烟雾弹区效 smoke_grenade_area（RefreshDuration 每 tick 刷新时长）
                _library["smoke_vision_debuff"] = new BuffDefinition()
                {
                    BuffId = "smoke_vision_debuff",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    LayerStackMode = EBuffLayerStackMode.Classic,
                    TurnOverrideType = EBuffTurnOverrideType.Replace,
                    MaxStackLayer = 1,
                    DefaultDuration = 3f,
                    FlushWitnessOnApply = true,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.UnitVisionRangeMul, ModifierValue = -5000 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.UnitVisionFovMul, ModifierValue = -3000 },
                    },
                };

                // 粉雾地面格由 GroundMistManager 维护；player_pink_mist buff 由 TickGroundMistOverlay 施加
                _library["player_pink_mist"] = new BuffDefinition()
                {
                    BuffId = "player_pink_mist",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    LayerStackMode = EBuffLayerStackMode.IndependentStack,
                    MaxStackLayer = 1,
                    DefaultDuration = -1,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = -4500 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.NPCHVal_Basic_Up, ModifierValue = 1000 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.DesireMistAffected, ModifierValue = 1 },
                    },
                };

                _library["player_stealth"] = new BuffDefinition()
                {
                    BuffId = "player_stealth",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = -1,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Ghost, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ForbidSkillOp, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Invisible, ModifierValue = 1 },
                    },
                };

                _library["player_normal_defend_on"] = new BuffDefinition()
                {
                    BuffId = "player_normal_defend_on",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = -1,

                    EffectId = "Skill/player_shield",

                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_JianShang, ModifierValue = 5000 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = -3000 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ImmuneKnock, ModifierValue = 1 },
                    },

                    DurationEffect = new BuffDurationEffet()
                    {
                        DurationType = EBuffDurationType.AnimOverride,
                        ParamStr1 = "test",
                    },
                };

                _library["queen_countering"] = new BuffDefinition()
                {
                    BuffId = "queen_countering",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = 2.0f,
                    ZOffsetOverride = 0.2f,

                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_JianShang, ModifierValue = 9000 },
                    },

                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.OnHit,
                            OutputFightEffects = new()
                            {
                                new MapAbilityEffectCastSkillCfg()
                                {
                                    UseTargetAsTarget = true,
                                    UseTargetAsCastVec = true,
                                    SkillId = "queen_counter_payback",
                                }
                            },
                            RemoveOnTrigger = true,
                        }
                    },
                };

                _library["as_attaching"] = new BuffDefinition()
                {
                    BuffId = "as_attaching",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = -1,
                    ZOffsetOverride = 0.08f,

                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.HideView, ModifierValue = 1 },
                    },
                };

                _library["attach_small"] = new BuffDefinition()
                {
                    BuffId = "attach_small",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    DefaultDuration = -1,

                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_PleasureAdd, ModifierValue = 20 },
                    },
                };


                _library["queen_mode_on"] = new BuffDefinition()
                {
                    BuffId = "queen_mode_on",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    DefaultDuration = 15.0f,

                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_KnockResistent, ModifierValue = 8000 },
                    },

                    OnDetachEffects = new()
                    {
                        new MapFightEffectQueueModeCfg()
                        {
                            InEnter = false
                        }
                    },
                };

                _library["player_ziwei"] = new BuffDefinition()
                {
                    BuffId = "player_ziwei",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_PleasureAdd, ModifierValue = 100 },
                    },
                    DefaultDuration = -1,

                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.Tick,
                            TriggerParam1 = 1000, // ?0.2?????
                            OutputFightEffects = new()
                            {
                                new MapFightEffectBroadcastAttractCfg()
                                {
                                    Power = 0.8f,
                                    Range = 5f,
                                }
                            }
                        }
                    },
                };

                /// С???????????????
                _library["insertion_debuff_small"] = new BuffDefinition()
                {
                    BuffId = "insertion_debuff_small",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Stun, ModifierValue = 1 },
                    },
                    DefaultDuration = 0.5f,
                    Icon = "fallback",
                    HeadHintPriority = 100,

                    OnAttachEffects = new()
                    {
                        new MapAbilityEffectAddResourceCfg()
                        {
                            ResourceId = AttrIdConsts.PlayerPleasure,
                            AddValue = 200,
                        }
                    },
                };

                _library["dark_dance"] = new BuffDefinition()
                {
                    BuffId = "dark_dance",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Invisible, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = 5000 },
                    },
                    EffectId = "dark_dance_aura",
                    DefaultDuration = 5f,
                };

                // ???????? queen_desire_charm_01 ????? Buff????????????
                _library["queen_desire_charm_01"] = new BuffDefinition()
                {
                    BuffId = "queen_desire_charm_01",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    MaxStackLayer = 10,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.PlayerCharm, ModifierValue = 3000 },
                    },
                    DefaultDuration = -1,
                    Icon = "queen_desire_charm_01",
                };

                _library["h_spirit_immune"] = new BuffDefinition()
                {
                    BuffId = "h_spirit_immune",

                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = -1,

                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.OnHit,
                            OutputFightEffects = new()
                            {
                                new MapAbilityEffectAddResourceCfg()
                                {
                                    ResourceId = AttrIdConsts.NPCSJProgress,
                                    AddValue = 20_000,
                                }
                            },
                            RemoveOnTrigger = true,
                        }
                    },

                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.ImmuneKnock, ModifierValue = 1 },
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = 1 },
                        
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Final_Fix_DR_All, ModifierValue = 99999999 },
                    },

                    IsHidden = true,
                };

                // 威仪本体：层数资源 + 每层威仪减伤；受击抵扣时消层。恢复类效果由独立 efx buff 通过 AddBuff 叠加层数。
                _library["player_weiyi"] = new BuffDefinition()
                {
                    BuffId = "player_weiyi",
                    Desc = "威仪",
                    LayerOverrideType = EBuffLayerOverrideType.AddLayer,
                    MaxStackLayer = 5,
                    DefaultDuration = -1,
                    SupportsEffectToggle = true,
                    Icon = "player_weiyi",

                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Weiyi_JianShang, ModifierValue = 99999999 },
                    },

                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.OnDamageTaken,
                            NeedCount = 1,
                            SubtractLayerOnTrigger = 1,
                        },
                    },
                };

                // 脱战恢复威仪（符文等挂载的载体 buff，向 player_weiyi 加层）
                _library["efx_weiyi_regen_ooc"] = new BuffDefinition()
                {
                    BuffId = "efx_weiyi_regen_ooc",
                    Desc = "脱战恢复威仪",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    MaxStackLayer = 1,
                    DefaultDuration = -1,
                    IsHidden = true,

                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.Tick,
                            OutputFightEffects = new()
                            {
                                new MapAbilityEffectAddBuffCfg()
                                {
                                    BuffId = "player_weiyi",
                                    Layer = 1,
                                },
                            },
                        },
                    },

                    DurationEffects = new()
                    {
                        new BuffDurationEffet()
                        {
                            DurationType = EBuffDurationType.OutOfCombatWatch,
                            ParamFloat1 = 3f,
                        },
                    },
                };

                _library["orb_skill_regen"] = new BuffDefinition()
                {
                    BuffId = "orb_skill_regen",
                    Desc = "能量球弹药恢复",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    MaxStackLayer = 1,
                    DefaultDuration = -1,
                    IsHidden = true,
                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.Tick,
                            TriggerInterval = 4f,
                            OutputFightEffects = new()
                            {
                                new MapAbilityEffectAddResourceCfg()
                                {
                                    ResourceId = AttrIdConsts.Ammo,
                                    AddValue = 1,
                                    IsSelf = true,
                                },
                            },
                        },
                    },
                };

                _library["orb_skill_owner_link"] = new BuffDefinition()
                {
                    BuffId = "orb_skill_owner_link",
                    DefaultDuration = -1,
                    IsHidden = true,
                };

                _library["passive_perfect_dodge_weiyi"] = new BuffDefinition()
                {
                    BuffId = "passive_perfect_dodge_weiyi",
                    Desc = "完美闪避获得威仪",
                    LayerOverrideType = EBuffLayerOverrideType.Replace,
                    MaxStackLayer = 1,
                    DefaultDuration = -1,
                    IsHidden = true,
                    TriggerList = new()
                    {
                        new BuffTriggerRuleConfig()
                        {
                            TriggerType = ETriggerType.OnPerfectDodge,
                            OutputFightEffects = new()
                            {
                                new MapAbilityEffectAddBuffCfg()
                                {
                                    BuffId = "player_weiyi",
                                    Layer = 1,
                                },
                            },
                        },
                    },
                };
            }


            _library.TryGetValue(buffId, out BuffDefinition def);
            return def;
        }
    }



}