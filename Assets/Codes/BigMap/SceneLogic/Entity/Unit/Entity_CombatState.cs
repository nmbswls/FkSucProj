

using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.PackageManager;
using UnityEngine;
using static My.Map.NpcCombatStateComp;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map
{

    public interface IUnitWithBattle
    {
        ECombatState CombatState { get; }
    }

    public class NpcCombatStateComp : IUnitWithBattle
    {

        public BaseUnitLogicEntity UnitEntity { get; private set; }

        // 基础表：将其视为 DamageThreat 存储
        private readonly Dictionary<long, float> damageThreat = new Dictionary<long, float>();
        // SightThreat 子表：仅存“看见”产生的威胁
        private readonly Dictionary<long, float> sightThreat = new Dictionary<long, float>();


        private float baseSightThreat = 20f;       // 初次目击注入基础值
        private float minPerInjection = 8f;        // 单次注入下限
        private float maxPerInjection = 24f;       // 单次注入上限
        private float perTargetCap = 40f;          // 同一目标 SightThreat 累积上限

        private float sightDecayPerSec = 5f;       // SightThreat 衰减速率（快于伤害）
        private float sightCooldown = 1f;         // 单位级目击冷却
        private int perMinuteQuotaCount = 3;       // 每分钟最多目击注入次数
        private float aggregatedWeight = 1.0f;     // 聚合时 SightThreat 权重（可<1，

        // 脱战相关
        private float outCombatDelay = 8f;         // 无伤害静默到此延迟后可脱战
        private float minCombatDuration = 3f;      // 进战后至少维持时间
        private float resetDelayAtHome = 1.0f;     // 回到home后静置多久才脱战

        // 牵引边界
        private float leashRadius = 8f;           // 软牵引半径
        private float hardLeashRadius = 12f;       // 硬牵引边界（超出加速脱战）

        // DamageThreat 衰减（线性）
        private float baseDecayPerSec = 6f;        // 每秒线性衰减
        private float invisibleExtraDecay = 4f;    // 不可见额外衰减


        // 时序
        private float enterCombatTime = -999f;
        private float lastDamageTakenTime = -999f;
        private float lastDamageGivenTime = -999f;
        private float lastExitCombatTime = -999f;
        private float lastTryRecoverTime = -999;

        private float fixedSightThreat = 10f;       // 固定值注入

        private float minConfidence = 0.6f;       // 目击置信度阈值
        private float fovAngle = 140f;            // 可选前方扇形，扩大容错

        // 可见性缓存（示例）
        private readonly HashSet<long> tempVisibleTargets = new HashSet<long>();

        public enum ECombatState
        {
            NotCombat,
            InCombat,
            CombatRecover,
            Escape,
        }
        public ECombatState CombatState { get; set; }

        private float allySenseInterval = 1.0f;
        private float nextAllySenseTime = 0f;
        private float allySenseRadius = 5f;
        public long PrimaryTargetId { get; private set; } = 0;

        

        public NpcCombatStateComp(BaseUnitLogicEntity entity)
        {
            this.UnitEntity = entity;
        }


        public void Tick(float dt)
        {
            DecayDamageThreat(dt);
            DecaySightThreat(dt);

            AllySenseThreatTick();

            EnemySightThreatTick();
            ReevaluatePrimaryTarget();

            // 保底进行脱战
            if(LogicTime.time - lastTryRecoverTime > 60.0f && CombatState == ECombatState.CombatRecover)
            {
                TryRecover();
            }

            if(CombatState == ECombatState.InCombat)
            {
                if(PrimaryTargetId != 0)
                {
                    UnitEntity.RegisterGaze("Combat", PrimaryTargetId, Vector2.zero, BaseUnitLogicEntity.EGazePriority.Combat, 2f);
                }
            }
            else
            {
                UnitEntity.UnregisterGazeBySourceTag("Combat");
            }

            TryExitCombat();

        }

        public void TryRecover()
        {
            if(CombatState == ECombatState.CombatRecover)
            {
                Debug.Log($"entity:{UnitEntity.Id} recover from");
                CombatState = ECombatState.NotCombat;
            }
        }

        /// <summary>
        /// 更新威胁 给予伤害
        /// </summary>
        /// <param name="targetId"></param>
        /// <param name="amount"></param>
        public void OnGiveDamage(long targetId, float amount)
        {
            if (CombatState != ECombatState.NotCombat)
            {
                return;
            }

            lastDamageGivenTime = LogicTime.time;
            AddDamageThreat(targetId, 2);
            EnterCombat(targetId);
        }

        /// <summary>
        /// 更新威胁 受伤
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="amount"></param>
        public void OnTakeDamage(long srcId, float amount)
        {
            if (CombatState != ECombatState.NotCombat)
            {
                return;
            }

            lastDamageTakenTime = LogicTime.time;
            AddDamageThreat(srcId, 10);
            EnterCombat(srcId);
        }


        public void TryUnitFlee()
        {

        }
        
        /// <summary>
        /// 伤害衰减
        /// </summary>
        /// <param name="dt"></param>
        private void DecayDamageThreat(float dt)
        {
            if (damageThreat.Count == 0) return;

            tempVisibleTargets.Clear();
            foreach (var kv in damageThreat)
            {
                var targetEntity = UnitEntity.LogicManager.GetLogicEntity(kv.Key, false);
                if(targetEntity != null && UnitEntity.LogicManager.visionSenser.CanSee(UnitEntity.Pos, UnitEntity.CurrentLook, targetEntity.Pos, 6.0f, 140f))
                {
                    tempVisibleTargets.Add(kv.Key);
                }
            }

            var keys = new List<long>(damageThreat.Keys);
            foreach (var tid in keys)
            {
                float extra = tempVisibleTargets.Contains(tid) ? 0f : invisibleExtraDecay;
                float dec = (baseDecayPerSec + extra) * dt;
                float newVal = damageThreat[tid] - dec;
                if (newVal <= 0f) damageThreat.Remove(tid);
                else damageThreat[tid] = newVal;
            }
        }

        private float GetAggregatedThreat(long targetId)
        {
            float dmg = 0f; damageThreat.TryGetValue(targetId, out dmg);
            float sight = 0f; sightThreat.TryGetValue(targetId, out sight);
            // sight 可采用权重降低其影响
            return dmg + sight * aggregatedWeight;
        }

        private void DecaySightThreat(float dt)
        {
            if (sightThreat.Count == 0) return;
            var keys = new List<long>(sightThreat.Keys);
            foreach (var tid in keys)
            {
                float v = sightThreat[tid] - sightDecayPerSec * dt;
                if (v <= 0f) sightThreat.Remove(tid);
                else sightThreat[tid] = v;
            }
        }

        /// <summary>
        /// 评估主目标
        /// </summary>
        private void ReevaluatePrimaryTarget()
        {
            if (CombatState != ECombatState.InCombat) return;
            if (damageThreat.Count == 0 && sightThreat.Count == 0) return;

            long bestTid = 0;
            float bestScore = float.MinValue;

            // 合并key集合
            var keySet = new HashSet<long>(damageThreat.Keys);
            foreach (var k in sightThreat.Keys) keySet.Add(k);

            foreach (var tid in keySet)
            {
                // 距离加权（远距降低优先级）
                float distWeight = 1f;
                var targetEntity = UnitEntity.LogicManager.GetLogicEntity(tid, false);
                if (targetEntity != null)
                {
                    float dist = Vector2.Distance(UnitEntity.Pos, targetEntity.Pos);
                    if (dist > leashRadius) distWeight = 0.5f;
                }

                float score;
                if (UnitEntity.VisibilityComp.IsTargetVisible(tid))
                {
                    score = 0.1f;
                }
                else
                {
                    score = GetAggregatedThreat(tid) * distWeight;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTid = tid;
                }
            }

            if (bestTid != 0 && bestTid != PrimaryTargetId)
            {
                PrimaryTargetId = bestTid;
                // 可选：事件或日志记录主目标变化
                UnitEntity.UnregisterGazeBySourceTag("Combat");
                UnitEntity.RegisterGaze("Combat", PrimaryTargetId, Vector2.zero, BaseUnitLogicEntity.EGazePriority.Combat, 2f);
            }
        }

        private void TryExitCombat()
        {
            if (CombatState != ECombatState.InCombat) return;
            if (LogicTime.time - enterCombatTime < minCombatDuration) return;

            bool giverSilent = (LogicTime.time - lastDamageGivenTime) >= outCombatDelay;
            bool takerSilent = (LogicTime.time - lastDamageTakenTime) >= outCombatDelay;
            bool noThreat = damageThreat.Count == 0 && sightThreat.Count == 0;

            bool hardLeashExceeded = false;
            if (PrimaryTargetId != 0)
            {
                var primaryEntity = UnitEntity.LogicManager.GetLogicEntity(PrimaryTargetId);
                if (primaryEntity != null)
                {
                    float dist = Vector2.Distance(UnitEntity.Pos, primaryEntity.Pos);
                    hardLeashExceeded = dist > hardLeashRadius;
                }
            }

            
            if ((giverSilent && takerSilent && noThreat) || hardLeashExceeded)
            {
                string reason = "Silent";
                //if (Vector3.Distance(selfTf.position, HomePos) < 1.5f &&
                //    Time.time - lastReturnHomeTime >= config.resetDelayAtHome)
                //{
                //    reason = "Reset";
                //}
                ExitCombat(reason);
            }
        }


        private void AddDamageThreat(long targetId, float addVal)
        {
            if (targetId == 0) return;
            if (CombatState == ECombatState.CombatRecover)
            {
                return;
            }

            if (damageThreat.TryGetValue(targetId, out float v))
                damageThreat[targetId] = v + addVal;
            else
                damageThreat[targetId] = addVal;


            if (CombatState != ECombatState.InCombat) EnterCombat(targetId);
        }

        private bool TryAddSightThreat(long targetId)
        {
            if (targetId == 0) return false;
            //if(CombatState != ECombatState.NotCombat)
            //{
            //    return false;
            //}

            float applied = fixedSightThreat;
            if(sightThreat.ContainsKey(targetId))
            {
                sightThreat[targetId] = Mathf.Max(sightThreat[targetId], applied);
            }
            else
            {
                sightThreat[targetId] = fixedSightThreat;
            }

            if (CombatState == ECombatState.NotCombat)
            {
                EnterCombat(targetId);
            }

            return true;
        }

        public void EnterCombat(long primaryTargetId)
        {
            CombatState = ECombatState.InCombat;
            PrimaryTargetId = primaryTargetId;
            enterCombatTime = LogicTime.time;
            //Events.RaiseEnterCombat(primaryTargetId);
            // TODO: 启动AI/导航追击 PrimaryTargetId

            Debug.Log($"d unit:{UnitEntity.Id} enemy {primaryTargetId}");

            UnitEntity.LogicManager.OnUnitCombatStateUpdate(this.UnitEntity);
        }

        public void ExitCombat(string reason = "Silent")
        {
            CombatState = ECombatState.CombatRecover;
            PrimaryTargetId = 0;

            UnitEntity.UnregisterGazeBySourceTag("Combat");

            damageThreat.Clear();
            sightThreat.Clear();

            //Events.RaiseExitCombat(reason);
            lastTryRecoverTime = LogicTime.time + 1.0f;
            lastExitCombatTime = LogicTime.time;
        }

        /// <summary>
        /// 低频拉取周围同阵营单位的威胁列表
        /// </summary>
        private void AllySenseThreatTick()
        {
            if (LogicTime.time < nextAllySenseTime) return;
            nextAllySenseTime = LogicTime.time + allySenseInterval;

            if (CombatState != ECombatState.NotCombat) return; // 未进战时才尝试自拉入


            var list = UnitEntity.LogicManager.visionSenser.OverlapCircleAllEntity(UnitEntity.Pos, allySenseRadius, new EntityFilterParam()
            {
                CampFilterType = ECampFilterType.OnlySelf,
                SelfCampId = UnitEntity.FactionId,

                FilterType = EEntityType.Npc,
            });

            float bestScore = 0f;

            foreach (var e in list)
            {
                if(e == null || e is not NpcUnitLogicEntity witness)
                {
                    continue;
                }
                if (witness.CombatState != ECombatState.InCombat) 
                    continue;
                if (witness.IsInHMode()) continue;

                //float confidence = ComputeWitnessConfidence(witness);
                //if (confidence < minConfidence) continue;

                long witnessPrimary = witness.combatStateComp.PrimaryTargetId;
                if (witnessPrimary == 0) continue;
                var witnessPrimaryEntity = UnitEntity.LogicManager.GetLogicEntity(witnessPrimary);
                if (witnessPrimaryEntity == null) continue;
                float dist = Vector3.Distance(UnitEntity.Pos, witnessPrimaryEntity.Pos);
                if (dist > leashRadius)
                {
                    continue;
                }

                TryAddSightThreat(witness.combatStateComp.PrimaryTargetId);
            }
        }


        private float ComputeWitnessConfidence(BaseUnitLogicEntity witness)
        {
            float dist = Vector2.Distance(UnitEntity.Pos, witness.Pos);
            float distWeight = Mathf.Clamp01(1f - (dist / allySenseRadius)); // 0~1

            Vector2 dir = (witness.Pos - UnitEntity.Pos).normalized;
            float angle = Vector3.Angle(UnitEntity.CurrentLook, dir);
            float fovWeight = angle <= fovAngle * 0.5f ? 1f : 0.7f;

           // bool hasLoS = true;
            //float losWeight = hasLoS ? 1f : 0.5f;

            // 简化：乘法组合
            float confidence = distWeight * fovWeight * 1.0f;
            return confidence;
        }


        private float _enmitySightTimer = 0;
        /// <summary>
        /// 
        /// </summary>
        private void EnemySightThreatTick()
        {

            if(_enmitySightTimer + 0.3f > LogicTime.time)
            {
                return;
            }

            _enmitySightTimer = LogicTime.time;

            long bestEnemyId = 0;

            // 获取感知组件中维护的可见单位列表
            foreach (var seeOne in UnitEntity.VisibilityComp.VisibleMap.Values)
            {
                var seeOneEntity = UnitEntity.LogicManager.GetLogicEntity(seeOne.TargetId, false);
                if (seeOneEntity == null) continue;

                if (UnitEntity is NpcUnitLogicEntity npcUnit && npcUnit.IsInHMode())
                {
                    if(seeOneEntity.Type != EEntityType.Player)
                    {
                        continue;
                    }
                }
                else 
                {
                    bool baseEnmity = false;
                    if (UnitEntity.CheckIsEmnityFaction(seeOneEntity.FactionId))
                    {
                        baseEnmity = true;
                    }

                    if(seeOneEntity is PlayerLogicEntity playerEntity && playerEntity.IsQueenMode)
                    {
                        baseEnmity = true;
                    }

                    if(!baseEnmity)
                    {
                        continue;
                    }
                }
                

                // todo 获取最佳目标
                if (seeOne.IsInView && seeOne.LastSeenTime + 0.5f > LogicTime.time)
                {
                    bestEnemyId = seeOne.TargetId;
                    break;
                }
            }

            if (bestEnemyId == 0) return;

            TryAddSightThreat(bestEnemyId);
        }
    }
}