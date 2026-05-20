
using System;
using My.Map.Entity;
using My.Map;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static My.UI.UIManager;

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

                    case MapFightEffectApplyDamageCfg:
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
                            executor = new AbilityFightExecutor4UseWeapon();
                        }
                        break;

                    case MapAbilityEffectDefaultInteractCfg:
                        {
                            executor = new AbilityEffectExecutor4DefaultInteract();
                        }
                        break;
                    case MapFightEffectQueueModeCfg:
                        {
                            executor = new AbilityFightExecutor4QueueMode();
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
                    case MapFightEffectShowCloseupWindowCfg:
                        {
                            executor = new AbilityEffectExecutor4ShowCloseupWindow();
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
                    case MapFightEffectTriggerAlert:
                        {
                            executor = new AbilityEffectExecutor4TriggerAlert();
                        }
                        break;
                    case MapFightEffectEasyEffect:
                        {
                            executor = new AbilityEffectExecutor4EasyEffect();
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

                    case MapAbilityEffectHitAttachCfg:
                        {
                            executor = new AbilityEffectExecutor4HitAttach();
                        }
                        break;
                    case MapFightEffectSpecialMoveToCfg:
                        {
                            executor = new AbilityFightExecutor4SpecialMoveTo();
                        }
                        break;

                    case MapFightEffectHModeBlurtCfg:
                        {
                            executor = new AbilityEffectExecutor4HModeBlurt();
                        }
                        break;
                    case MapFightEffectInterruptCaster:
                        {
                            executor = new AbilityEffectExecutor4InterruptCaster();
                        }
                        break;

                    case MapFightEffectCauseNoise:
                        {
                            executor = new AbilityEffectExecutor4CauseNoise();
                        }
                        break;
                    case MapFightEffectCreateAreaEffectCfg:
                        {
                            executor = new AbilityEffectExecutor4CreateAreaEffect();
                        }
                        break;
                    case MapFightEffectAddLiquidCfg:
                        {
                            executor = new AbilityEffectExecutor4AddLiquid();
                        }
                        break;

                    case MapFightEffectApplyHImpulseCfg:
                        {
                            executor = new AbilityEffectExecutor4ApplyHImpulseCfg();
                        }
                        break;
                        
                    case MapFightEffectWantedIncidentBroadcastCfg:
                        {
                            executor = new AbilityEffectExecutor4WantedIncidentBroadcast();
                        }
                        break;
                    case MapAbilityEffectGiveItemCfg:
                        {
                            executor = new AbilityEffectExecutor4GiveItem();
                        }
                        break;

                    case MapFightEffectKnockBackCfg:
                        {
                            executor = new AbilityFightExecutor4KnockBack();
                        }
                        break;
                    case MapFightEffectBroadcastAttractCfg:
                        {
                            executor = new AbilityEffectExecutor4BroadcastAttract();
                        }
                        break;
                    case MapFightEffectXuLiStageCfg:
                        {
                            executor = new AbilityFightExecutor4XuLiStage();
                        }
                        break;
                    case MapFightEffectShowEffect:
                        {
                            executor = new AbilityFightExecutor4ShowEffect();
                        }
                        break;
                    case MapAbilityEffectSneakBackstabResolveCfg:
                        {
                            executor = new AbilityEffectExecutor4SneakBackstabResolve();
                        }
                        break;
                    case MapAbilityEffectThrowTimedInputCfg:
                        {
                            executor = new AbilityEffectExecutor4ThrowTimedInput();
                        }
                        break;
                    case MapAbilityEffectThrowTimedInputBranchCfg:
                        {
                            executor = new AbilityEffectExecutor4ThrowTimedInputBranch();
                        }
                        break;
                    case MapAbilityEffectThrowBreakFreeCfg:
                        {
                            executor = new AbilityEffectExecutor4ThrowBreakFree();
                        }
                        break;
                    case MapFightEffectEnqueueDetachedSkill:
                        {
                            executor = new AbilityEffectExecutor4EnqueueDetachedSkill();
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
            None,
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
            HitBox,
        }

        /// <summary>
        /// Ч��Դ��Ϣ
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

            public string SrcAbilityId;
            public int SrcAbilityPhaseId;
        }

        public enum EFightCtxType
        {
            None,
            Ability,
            Bullet,
            HitBox,
            HitWeapon,
            Buff,
            Trap,
        }

        public class LogicFightEffectContext
        {

            public GameLogicManager Env { get; protected set; }
            public EFightCtxType CtxType { get; set; }
            public LogicFightEffectContext(GameLogicManager env, EFightCtxType ctxType, EffectSourceInfo sourceInfo)
            {
                this.Env = env;
                this.CtxType = ctxType;
                this.SourceInfo = sourceInfo;
            }

            public EffectSourceInfo SourceInfo; // 

            public long TargetId;              // �������� ������Լ��� �����ͷ�ʱ���� �������Ч������ �����߼��а�
            public Vector2? TriggerPos;        // �����ص� �����ͷ�λ�� �ӵ���ײλ�� buff����λ�õ�
            
            public Vector2? CastVec1;          // 1.���� Ϊʩ������ 
                                               // 2.hitbox ������
                                               // 3.���� ��λ��diff

            public Vector2? CastVec2;          // ʩ������2

            public Vector2? InputVec;          // ���� ֻ������ʩ���Żḳֵ

            // ��������
            public Dictionary<string, string> RunningVariables = new();
            public Dictionary<string, long> RunningStorage = new();

            public Dictionary<string, long> CacheAttrVal = new();

            public List<int> BindSceneFxIds = new();

            public List<long> OutHitWindowIds = new();

            public int ThrowTimelineEventIndex = -1;

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

        
    }
}