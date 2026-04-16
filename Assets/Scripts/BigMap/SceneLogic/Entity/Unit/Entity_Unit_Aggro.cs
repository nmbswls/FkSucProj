using System.Collections.Generic;
using My.Map.Unit;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.PackageManager;
using UnityEngine;

namespace My.Map.Unit
{

    /// <summary>
    /// 仇恨模块
    /// </summary>
    public class UnitAggroSystem
    {
        private  BaseUnitLogicEntity _unit { get; set; }

        // --- 配置参数 ---
        private const float OutOfCombatTime = 8.0f;     // 脱战时间
        private const float LeashRadius = 15.0f;        // 硬脱战距离
        private const float BaseSightThreat = 20.0f;    // 目击基础仇恨

        // --- 连锁仇恨配置 ---
        private const float AllySenseInterval = 1.0f;   // 感知频率 (1秒一次足够了)
        private const float AllySenseRadius = 5.0f;    // 能感知到队友的范围
        private float _nextAllySenseTime = 0f;
        private float _clearCoolTimer = 0;

        // --- 核心数据 ---
        private class HostileInfo
        {
            public float TotalDamage = 0f;
            public float LastInteractionTime;
            public bool IsVisible = false;
        }

        private readonly Dictionary<long, HostileInfo> _threatTable = new Dictionary<long, HostileInfo>();

        public long CurrentTargetId { get; private set; } = 0;
        public Vector2? LastKnownTargetPos { get; protected set; } = null;
        public bool HasHostile => CurrentTargetId != 0;

        public UnitAggroSystem(BaseUnitLogicEntity unit)
        {
            _unit = unit;
            // 初始随机化，防止所有怪同一帧做检测 (性能尖峰)
            _nextAllySenseTime = LogicTime.time + Random.Range(0f, 1.0f);
        }

        public void Tick(float dt)
        {
            // 主动感知队友 (实现连锁仇恨的核心)
            //TickAllySense();

            OnVisionUpdate();

            // 2. 清理
            CleanupInvalidTargets();

            // 3. 评估
            ReevaluateTarget();

            // 4. 状态同步
            if (_threatTable.Count == 0 && CurrentTargetId != 0)
            {
                CurrentTargetId = 0;
                _unit.UnregisterGazeBySourceTag("Aggro");
            }

            if(CurrentTargetId != 0)
            {
                var targetEntity = _unit.LogicManager.GetLogicEntity(CurrentTargetId, false);
                if (targetEntity != null)
                {
                    LastKnownTargetPos = targetEntity.Pos;
                }
            }
        }

        /// <summary>
        /// 清理目标 并给予一段时间冷静
        /// </summary>
        public void ClearTarget(float coolTime = 3.0f)
        {
            _threatTable.Clear();

            _clearCoolTimer = LogicTime.time + coolTime;
        }


        /// <summary>
        /// 低频扩散
        /// 后续优化 将战意扩散到区域节点里
        /// </summary>
        private void TickAllySense()
        {
            if (LogicTime.time < _nextAllySenseTime) return;
            _nextAllySenseTime = LogicTime.time + AllySenseInterval;


            var allies = _unit.LogicManager.visionSenser.OverlapCircleAllEntity(_unit.Pos, AllySenseRadius, new EntityFilterParam()
            {
                CampFilterType = ECampFilterType.OnlySelf,
                SelfCampId = _unit.FactionId,
            });

            //foreach (var ally in allies)
            //{
            //    if (ally == null || ally.Id == _unit.Id) continue;

            //    // 关键判断：队友是否在战斗中？
            //    // 需要转型获取队友的 Aggro 组件信息，这里假设可以直接访问
            //    if (ally is NpcUnitLogicEntity npcAlly && npcAlly.AggroSystem.IsInCombat)
            //    {
            //        // 核心连锁逻辑：A -> B -> C
            //        // 我看到了队友 A 的目标 T
            //        long allyTargetId = npcAlly.AggroSystem.CurrentTargetId;
            //        if (allyTargetId == 0) continue;

            //        // 简单的验证：队友的目标距离我远不远？
            //        // 如果太远就不凑热闹了，防止全图怪暴动
            //        var targetEnt = _unit.LogicManager.GetLogicEntity(allyTargetId, false);
            //        if (targetEnt == null) continue;

            //        if (Vector3.Distance(_unit.Pos, targetEnt.Pos) < LeashRadius)
            //        {
            //            // 成功被连锁！
            //            // 将目标加入我的仇恨列表，给予一个基础仇恨值
            //            var info = GetOrAddHostile(allyTargetId);

            //            // 刷新时间，确保不会立刻脱战
            //            info.LastInteractionTime = LogicTime.time;

            //            // 这里的技巧：可以给一点点 Damage 模拟"我也生气了"，
            //            // 或者什么都不加，仅靠 ReevaluateTarget 里的 (BaseSightThreat) 逻辑选中它
            //            // 建议：稍微加一点点，确保持续性
            //            if (info.TotalDamage < 1f) info.TotalDamage = 1f;
            //        }
            //    }
            //}
        }

        // ============================================================
        // 下面保持精简逻辑
        // ============================================================

        public void OnTakeDamage(long attackerId, float amount)
        {
            var info = GetOrAddHostile(attackerId);
            info.TotalDamage += amount;
            info.LastInteractionTime = LogicTime.time;
        }

        public void OnVisionUpdate()
        {
            foreach (var kv in _threatTable) kv.Value.IsVisible = false;

            foreach(var pairInfo in _unit.VisionSystem.VisibleMap)
            {
                if(!pairInfo.Value.IsInView)
                {
                    continue;
                }

                var seeOneEntity = _unit.LogicManager.GetLogicEntity(pairInfo.Value.TargetId, false);
                if (seeOneEntity == null || seeOneEntity is not BaseUnitLogicEntity otherUnit) continue;

                if(!_unit.EnmitySystem.IsEnmityWith(otherUnit))
                {
                    continue;
                }

                var info = GetOrAddHostile(pairInfo.Value.TargetId);
                info.IsVisible = true;
                info.LastInteractionTime = LogicTime.time;
            }
        }

        private HostileInfo GetOrAddHostile(long id)
        {
            if (!_threatTable.TryGetValue(id, out var info))
            {
                info = new HostileInfo { LastInteractionTime = LogicTime.time };
                _threatTable[id] = info;
            }
            return info;
        }

        private void CleanupInvalidTargets()
        {
            List<long> toRemove = null;
            foreach (var kv in _threatTable)
            {
                // 规则：超时 或者 目标死亡/失效
                if (LogicTime.time - kv.Value.LastInteractionTime > OutOfCombatTime)
                {
                    if (toRemove == null) toRemove = new List<long>();
                    toRemove.Add(kv.Key);
                }
            }
            if (toRemove != null) foreach (var id in toRemove) _threatTable.Remove(id);
        }

        private void ReevaluateTarget()
        {
            if(_clearCoolTimer != 0 && LogicTime.time < _clearCoolTimer)
            {
                return;
            }

            if (_threatTable.Count == 0) 
            {
                return;
            }

            long bestTarget = 0;
            float maxScore = float.MinValue;

            foreach (var kv in _threatTable)
            {
                float score = kv.Value.TotalDamage;
                if (kv.Value.IsVisible) score += BaseSightThreat; // 视觉权重

                // 距离权重计算...

                if (score > maxScore) { maxScore = score; bestTarget = kv.Key; }
            }

            if (CurrentTargetId != bestTarget)
            {
                CurrentTargetId = bestTarget;
                if (CurrentTargetId != 0)
                    _unit.RegisterGaze("Aggro", CurrentTargetId, Vector2.zero, BaseUnitLogicEntity.EGazePriority.Combat, 0f);
            }
        }
    }


    
}

namespace My.Map
{
    public abstract partial class BaseUnitLogicEntity
    {
        public UnitAggroSystem AggroSystem { get; set; }

        public virtual long CurrentTargetId 
        { 
            get 
            { 
                return AggroSystem?.CurrentTargetId ?? 0; 
            } 
        }
        

        public virtual void InitAggroSystem()
        {
            AggroSystem = new(this);
        }
    }
}
