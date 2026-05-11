
using System.Collections.Generic;
using static My.Map.Entity.MapFightEffectShowEffect;

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


                _library["lock_move"] = new BuffDefinition()
                {
                    BuffId = "lock_move",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Unmovable, ModifierValue = 1 } },
                    DefaultDuration = -1,
                    IsHidden = true,
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
                    ModifierAttrs = new() { new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.NoKiller, ModifierValue = 1,},
                    new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_ExtraDmg, ModifierValue = -9000,}},
                    DefaultDuration = -1,
                    IsHidden = true,
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
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,

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

                _library["ground_gc_liquid"] = new BuffDefinition()
                {
                    BuffId = "ground_gc_liquid",
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

                // 移动粉雾区效（TbMapAreaEffect player_pink_mist_trail）；数值可再调
                _library["player_pink_mist"] = new BuffDefinition()
                {
                    BuffId = "player_pink_mist",
                    LayerOverrideType = EBuffLayerOverrideType.Duplicate,
                    DefaultDuration = -1,
                    ModifierAttrs = new()
                    {
                        new BuffDefinition.OneModPair() { ModifierAttrId = AttrIdConsts.Basic_MoveSpeed, ModifierValue = -1500 },
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
            }


            _library.TryGetValue(buffId, out BuffDefinition def);
            return def;
        }
    }



}