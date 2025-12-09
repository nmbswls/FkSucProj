
using System;
using My.Map.Entity;
using My.Map;
using System.Collections.Generic;
using UnityEngine;

namespace My

{
    


    public partial class GameLogicManager
    {

        private Dictionary<Type, AbilityEffectExecutor> EffectExecutors = new(); // executor
        private AbilityEffectExecutor GetLogicFightEffectExecutor(MapFightEffectCfg effectType)
        {
            if (!EffectExecutors.TryGetValue(effectType.GetType(), out var executor))
            {
                switch (effectType)
                {
                    case MapAbilityEffectUnlockLootPoint:
                        {
                            executor = new AbilityEffectExecutor4UnlockLootPoint();
                        }
                        break;
                    case MapAbilityEffectUseLootPoint:
                        {
                            executor = new AbilityEffectExecutor4UseLootPoint();
                        }
                        break;
                    case MapAbilityEffectCostResourceCfg:
                        {
                            executor = new AbilityEffectExecutor4CostResource();
                        }
                        break;

                    case MapAbilityEffectApplyDamageCfg:
                        {
                            executor = new AbilityEffectExecutor4ApplyDamage();
                        }
                        break;

                    case MapAbilityEffectThrowStartCfg:
                        {
                            executor = new AbilityEffectExecutor4ThrowStart();
                        }
                        break;

                    case MapAbilityEffectAddResourceCfg:
                        {
                            executor = new AbilityEffectExecutor4AddResource();
                        }
                        break;
                    case MapAbilityEffectUseItemCfg:
                        {
                            executor = new AbilityEffectExecutor4UseItem();
                        }
                        break;
                    case MapAbilityEffectSpawnBulletCfg:
                        {
                            executor = new AbilityEffectExecutor4SpawnBullet();
                        }
                        break;
                    case MapAbilityEffectUseWeaponCfg:
                        {
                            executor = new AbilityEffectExecutor4UseWeapon();
                        }
                        break;

                    case MapAbilityEffectDefaultInteractCfg:
                        {
                            executor = new AbilityEffectExecutor4DefaultInteract();
                        }
                        break;

                    case MapAbilityEffectDashStartCfg:
                        {
                            executor = new AbilityEffectExecutor4DashStart();
                        }
                        break;
                    case MapAbilityEffectAddBuffCfg:
                        {
                            executor = new AbilityEffectExecutor4AddBuff();
                        }
                        break;
                    case MapAbilityEffectRemoveBuffCfg:
                        {
                            executor = new AbilityEffectExecutor4RemoveBuff();
                        }
                        break;
                    case MapAbilityEffectHitBoxCfg:
                        {
                            executor = new AbilityEffectExecutor4HitBox();
                        }
                        break;
                    case MapAbilityEffectIfBranchCfg:
                        {
                            executor = new AbilityEffectExecutor4IfBranch();
                        }
                        break;
                    case MapAbilityEffectOpenClickWindowCfg:
                        {
                            executor = new AbilityEffectExecutor4OpenClickWindow();
                        }
                        break;
                    case MapAbilityEffectDeepZhaquCfg:
                        {
                            executor = new AbilityEffectExecutor4DeepZhaqu();
                        }
                        break;

                    case MapAbilityEffectSpawnEntityCfg:
                        {
                            executor = new AbilityEffectExecutor4SpawnEntity();
                        }
                        break;
                    case MapAbilityEffectRangePreviewCfg:
                        {
                            executor = new AbilityEffectExecutor4RangePreview();
                        }
                        break;
                    case MapAbilityEffectNextPhaseCfg:
                        {
                            executor = new AbilityEffectExecutor4NextPhase();
                        }
                        break;
                    case MapAbilityEffectTeleportToCfg:
                        {
                            executor = new AbilityEffectExecutor4TeleportTo();
                        }
                        break;
                    case MapAbilityEffectCastSkillCfg:
                        {
                            executor = new AbilityEffectExecutor4CastSkill();
                        }
                        break;
                    case MapAbilityEffectControlledMoveCfg:
                        {
                            executor = new AbilityEffectExecutor4ControlledMove();
                        }
                        break;

                    case MapAbilityEffectConvertAttachCfg:
                        {
                            executor = new AbilityEffectExecutor4ConvertAttach();
                        }
                        break;
                }

                if (executor != null)
                {
                    EffectExecutors[effectType.GetType()] = executor;
                }
            }

            return executor;
        }

        public enum ESourceType
        {
            Unknown,
            Ability,
            Buff,
            BuffTrigger,
            BuffEffect,
            Item,
            Env,
            Aura,
            AreaEffect,
            Bullet,
            Mechanism,
            Throw,
        }

        /// <summary>
        /// 效果源信息
        /// </summary>
        [Serializable]
        public class EffectSourceInfo
        {
            public ESourceType SrcType; // 
            public long SrcEntityId;
            public long SrcInstId;
            public string SrcCfgId;

            public long SrcBuffId;
            public EFactionId SrcFactionId;
        }


        public class LogicFightEffectContext
        {
            public GameLogicManager Env { get; protected set; }
            public LogicFightEffectContext(GameLogicManager env, EffectSourceInfo sourceInfo)
            {
                this.Env = env;
                this.SourceInfo = sourceInfo;
            }

            public EffectSourceInfo SourceInfo; // 

            public long TargetId;              // 锁定对象 如果来自技能 则在释放时锁定 如果来自效果触发 则在逻辑中绑定
            public Vector2? TriggerPos;        // 发生地点 技能释放位置 子弹碰撞位置 buff触发位置等
            public Vector2? CastVec1;          // 施法参数1 技能施法参数
            public Vector2? CastVec2;          // 施法参数2

            // 变量集合
            public Dictionary<string, string> RunningVariables = new();
            public Dictionary<string, long> RunningStorage = new();

            public Dictionary<string, long> CacheAttrVal = new();

            public List<int> BindSceneFxIds = new();

            public string GetVariatyRawVal(OneVariaty oneVariaty)
            {
                if (oneVariaty.ValType == EOneVariatyType.Invalid)
                {
                    return string.Empty;
                }

                string strVal = oneVariaty.RawVal;
                if (!string.IsNullOrEmpty(oneVariaty.ReferName))
                {
                    do
                    {
                        if (RunningVariables != null && RunningVariables.TryGetValue(oneVariaty.ReferName, out var runningVal))
                        {
                            strVal = runningVal;
                            break;
                        }
                    }
                    while (false);
                }

                return strVal;
            }
        }

        public abstract class DelayedEffectWrapper
        {
            public float exeTIme;
        }
        public class DelayedFightEffectWrapper : DelayedEffectWrapper
        {
            public MapFightEffectCfg effectConf;
            public LogicFightEffectContext ctx;
        }
        public List<DelayedEffectWrapper> DelayedEffectQueue = new();

        private bool _delayQueueDirty = false;

        public void HandleLogicFightEffect(MapFightEffectCfg effectConf, LogicFightEffectContext effectCtx)
        {
            if (effectConf.PendingTime > 0)
            {
                DelayedEffectQueue.Add(new DelayedFightEffectWrapper()
                {
                    effectConf = effectConf,
                    ctx = effectCtx,
                    exeTIme = LogicTime.time + effectConf.PendingTime,
                });
                _delayQueueDirty = true;
                return;
            }

            var executor = GetLogicFightEffectExecutor(effectConf);
            executor?.Apply(effectConf, effectCtx);
        }

    }
}