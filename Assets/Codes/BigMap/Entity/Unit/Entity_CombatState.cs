

using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.PackageManager;
using UnityEngine;
using static My.Map.EntityCombatStateComp;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map
{

    public interface IUnitWithBattle
    {
        ECombatState CombatState { get; }
    }

    public class EntityCombatStateComp : IUnitWithBattle
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
        private float sightDecayPerSec = 8f;       // SightThreat 衰减速率（快于伤害）
        private float sightCooldown = 1f;         // 单位级目击冷却
        private int perMinuteQuotaCount = 3;       // 每分钟最多目击注入次数
        private float aggregatedWeight = 1.0f;     // 聚合时 SightThreat 权重（可<1，

        // 脱战相关
        private float outCombatDelay = 8f;         // 无伤害静默到此延迟后可脱战
        private float minCombatDuration = 3f;      // 进战后至少维持时间
        private float resetDelayAtHome = 1.0f;     // 回到home后静置多久才脱战

        // 牵引边界
        private float leashRadius = 26f;           // 软牵引半径
        private float hardLeashRadius = 40f;       // 硬牵引边界（超出加速脱战）

        // DamageThreat 衰减（线性）
        private float baseDecayPerSec = 6f;        // 每秒线性衰减
        private float invisibleExtraDecay = 4f;    // 不可见额外衰减


        // 时序
        private float enterCombatTime = -999f;
        private float lastDamageTakenTime = -999f;
        private float lastDamageGivenTime = -999f;
        private float lastReturnHomeTime = -999f;

        // 目击限流
        private float nextSightAllowedTime = 0f;
        private int sightQuotaUsed = 0;
        private float sightQuotaWindowStart = 0f;

        private float minPerSightInjection = 8f;    // 单次下限
        private float maxPerSightInjection = 24f;   // 单次上限
        private float perTargetSightCap = 40f;      // 同目标Sight累积上限
        private int sightPerMinuteQuota = 3;        // 每分钟目击注入次数上限

        private float fixedSightThreat = 20f;       // 固定值注入

        private bool seedDamageOnSight = false;
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

        private float _lastRecoverTimer;

        private float allySenseInterval = 0.5f;
        private float nextAllySenseTime = 0f;
        private float allySenseRadius = 20f;
        public long PrimaryTargetId { get; private set; } = 0;

        public EntityCombatStateComp(BaseUnitLogicEntity entity)
        {
            this.UnitEntity = entity;
        }


        public void Tick(float dt)
        {
            DecayDamageThreat(dt);
            DecaySightThreat(dt);
            ReevaluatePrimaryTarget();
            TryExitCombat();

            AllySenseThreatTick();

            EnemySightThreatTick();
        }

        /// <summary>
        /// 更新威胁 给予伤害
        /// </summary>
        /// <param name="targetId"></param>
        /// <param name="amount"></param>
        public void OnGiveDamage(long targetId, float amount)
        {
            if (CombatState != ECombatState.CombatRecover)
            {
                return;
            }

            lastDamageGivenTime = LogicTime.time;
            AddDamageThreat(targetId);
            EnterCombat(targetId);
        }

        /// <summary>
        /// 更新威胁 受伤
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="amount"></param>
        public void OnTakeDamage(long srcId, float amount)
        {
            if (CombatState != ECombatState.CombatRecover)
            {
                return;
            }

            lastDamageTakenTime = LogicTime.time;
            AddDamageThreat(srcId);
            EnterCombat(srcId);
        }

        

        private void DecayDamageThreat(float dt)
        {
            if (damageThreat.Count == 0) return;

            tempVisibleTargets.Clear();
            foreach (var kv in damageThreat)
            {
                var targetEntity = UnitEntity.LogicManager.GetLogicEntity(kv.Key, false);
                if(targetEntity != null && UnitEntity.LogicManager.visionSenser.CanSee(UnitEntity.Pos, UnitEntity.FaceDir, targetEntity.Pos, 5.0f, 60f))
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
            }
        }

        private void TryExitCombat()
        {
            if (CombatState != ECombatState.InCombat) return;
            if (LogicTime.time - enterCombatTime < minCombatDuration) return;

            bool giverSilent = (LogicTime.time - lastDamageGivenTime) >= outCombatDelay;
            bool takerSilent = (LogicTime.time - lastDamageTakenTime) >=outCombatDelay;
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

        private void AddDamageThreat(long targetId)
        {
            if (targetId == 0) return;
            if (CombatState == ECombatState.CombatRecover)
            {
                return;
            }

            if (damageThreat.TryGetValue(targetId, out float v))
                damageThreat[targetId] = v + 10.0f;
            else
                damageThreat[targetId] = 10.0f;


            if (CombatState != ECombatState.InCombat) EnterCombat(targetId);
        }

        private bool TryAddSightThreat(long targetId)
        {
            if (targetId == 0) return false;
            if(CombatState == ECombatState.CombatRecover)
            {
                return false;
            }

            // 冷却与配额
            if (LogicTime.time < nextSightAllowedTime) return false;
            //if (LogicTime.time - sightQuotaWindowStart > 60f)
            //{
            //    sightQuotaWindowStart = LogicTime.time;
            //    sightQuotaUsed = 0;
            //}
            //if (sightQuotaUsed >= sightPerMinuteQuota) return false;

            float current = 0f;
            // 上限检查
            //sightThreat.TryGetValue(targetId, out current);
            //float room = perTargetSightCap - current;
            //if (room <= 0f) return false;

            float applied = fixedSightThreat;
            //if (applied <= 0f) return false;

            sightThreat[targetId] = current + applied;

            nextSightAllowedTime = LogicTime.time + sightCooldown;
            sightQuotaUsed++;

            if (CombatState != ECombatState.InCombat) EnterCombat(targetId);

            // 如需更稳定，可同步少量伤害威胁
            if (seedDamageOnSight)
            {
                AddDamageThreat(targetId);
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

            Debug.Log($"EnterCombat unit:{UnitEntity.Id} enemy {primaryTargetId}");
        }

        public void ExitCombat(string reason = "Silent")
        {
            CombatState = ECombatState.CombatRecover;
            _lastRecoverTimer = LogicTime.time;
            PrimaryTargetId = 0;
            damageThreat.Clear();
            sightThreat.Clear();

            //Events.RaiseExitCombat(reason);
            // TODO: 停止AI，回位等
        }


        private void AllySenseThreatTick()
        {
            if (LogicTime.time < nextAllySenseTime) return;
            nextAllySenseTime = LogicTime.time + allySenseInterval;

            if (CombatState == ECombatState.InCombat) return; // 未进战时才尝试自拉入


            var list = UnitEntity.LogicManager.visionSenser.OverlapCircleAllEntity(UnitEntity.Pos, allySenseRadius, new EntityFilterParam()
            {
                CampFilterType = ECampFilterType.OnlySelf,
                SelfCampId = UnitEntity.FactionId,
            });
            BaseUnitLogicEntity bestWitness = null;
            float bestScore = 0f;

            foreach (var e in list)
            {
                if(e == null || e is not BaseUnitLogicEntity witness)
                {
                    continue;
                }
                if (witness.CombatState != ECombatState.InCombat) 
                    continue;

                float confidence = ComputeWitnessConfidence(witness);
                if (confidence < minConfidence) continue;

                long witnessPrimary = witness.combatStateComp.PrimaryTargetId;
                if (witnessPrimary == 0) continue;
                var witnessPrimaryEntity = UnitEntity.LogicManager.GetLogicEntity(witnessPrimary, false);
                if (witnessPrimaryEntity == null) continue;
                float dist = Vector3.Distance(UnitEntity.Pos, witnessPrimaryEntity.Pos);
                if(dist > leashRadius)
                {
                    continue;
                }

                if (confidence > bestScore)
                {
                    bestScore = confidence;
                    bestWitness = witness;
                }
            }

            if (bestWitness != null)
            {
                // 采用 SightThreat 注入，内部含冷却与配额控制
                TryAddSightThreat(bestWitness.combatStateComp.PrimaryTargetId);
            }
        }


        private float ComputeWitnessConfidence(BaseUnitLogicEntity witness)
        {
            float dist = Vector2.Distance(UnitEntity.Pos, witness.Pos);
            float distWeight = Mathf.Clamp01(1f - (dist / allySenseRadius)); // 0~1

            Vector2 dir = (witness.Pos - UnitEntity.Pos).normalized;
            float angle = Vector3.Angle(UnitEntity.FaceDir, dir);
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

            if(_enmitySightTimer + 1.0f > LogicTime.time)
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
                if(!UnitEntity.CheckIsEmnityFaction(seeOneEntity.FactionId))
                {
                    continue;
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