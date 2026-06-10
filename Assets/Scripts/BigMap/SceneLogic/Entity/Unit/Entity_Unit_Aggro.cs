using System.Collections.Generic;
using My;
using My.Map;
using My.Map.Entity;
using My.Map.Unit;
using UnityEngine;

namespace My.Map.Unit
{
    public enum EAggroMode
    {
        Npc,
        Player,
    }

    // 仇恨模块：Npc 为 AI 仇恨；Player 为战斗交互日志（仅记录造成/受到敌意伤害）
    public class UnitAggroSystem
    {
        private BaseUnitLogicEntity _unit { get; set; }
        private readonly EAggroMode _mode;

        private const float OutOfCombatTime = 8.0f;
        private const float BaseSightThreat = 20.0f;
        private const float PartySyncInterval = 0.5f;
        private const float PartySyncScanRadius = 30f;
        private const float SharedThreatRatio = 0.5f;
        private float _nextPartySyncTime = 0f;
        private float _clearCoolTimer = 0;

        private class HostileInfo
        {
            public float TotalDamage = 0f;
            public float LastInteractionTime;
            public bool IsVisible = false;
            public bool IsDealt;
        }

        private readonly Dictionary<long, HostileInfo> _threatTable = new Dictionary<long, HostileInfo>();

        public long CurrentTargetId { get; private set; } = 0;
        public Vector2? LastKnownTargetPos { get; protected set; } = null;
        public bool HasHostile => CurrentTargetId != 0 && _threatTable.Count > 0;
        public bool CombatEngaged { get; private set; }

        float SharedThreatFloor => BaseSightThreat * SharedThreatRatio;

        public UnitAggroSystem(BaseUnitLogicEntity unit, EAggroMode mode = EAggroMode.Npc)
        {
            _unit = unit;
            _mode = mode;
            _nextPartySyncTime = LogicTime.time + Random.Range(0f, PartySyncInterval);
        }

        public void Tick(float dt)
        {
            if (_mode == EAggroMode.Player)
            {
                TickPlayerCombatLog();
                return;
            }

            TickPartyLeaderSync();
            if (_unit.IsNoAggro()) return;

            OnVisionUpdate();
            CleanupInvalidTargets();
            ReevaluateTarget();

            if (_threatTable.Count == 0 && CurrentTargetId != 0)
            {
                CurrentTargetId = 0;
                _unit.UnregisterGazeBySourceTag("Aggro");
            }

            if (CurrentTargetId != 0)
            {
                var targetEntity = _unit.LogicManager.GetLogicEntity(CurrentTargetId, false);
                if (targetEntity != null)
                {
                    LastKnownTargetPos = targetEntity.Pos;
                }
            }

            if (_threatTable.Count == 0)
            {
                CombatEngaged = false;
            }
        }

        public void ClearTarget(float coolTime = 3.0f)
        {
            _threatTable.Clear();
            CombatEngaged = false;
            CurrentTargetId = 0;
            _clearCoolTimer = LogicTime.time + coolTime;
        }

        public bool HasThreatEntry(long id)
        {
            return _threatTable.ContainsKey(id);
        }

        public bool HasSelfDamageThreat()
        {
            foreach (var kv in _threatTable)
            {
                if (kv.Value.TotalDamage > SharedThreatFloor)
                {
                    return true;
                }
            }

            return false;
        }

        public void AddSharedThreat(long entityId, float amount)
        {
            if (_unit.IsNoAggro() || entityId == 0)
            {
                return;
            }

            var info = GetOrAddHostile(entityId);
            if (info.TotalDamage < amount)
            {
                info.TotalDamage = amount;
            }

            info.LastInteractionTime = LogicTime.time;
        }

        // 玩家造成敌意伤害时写入交互表
        public void OnDealHostileDamage(long targetId, float amount)
        {
            if (_mode != EAggroMode.Player || targetId == 0)
            {
                return;
            }

            RecordPlayerInteraction(targetId, amount, isDealt: true);
            CurrentTargetId = targetId;
            CombatEngaged = true;
            MarkPlayerInCombat();
        }

        void TickPartyLeaderSync()
        {
            if (_unit.IsNoAggro())
            {
                return;
            }

            if (!IsPartyAlly(_unit))
            {
                return;
            }

            if (LogicTime.time < _nextPartySyncTime)
            {
                return;
            }

            _nextPartySyncTime = LogicTime.time + PartySyncInterval;

            var player = _unit.LogicManager.playerLogicEntity;
            if (player == null)
            {
                CombatEngaged = false;
                return;
            }

            bool engaged = HasSelfDamageThreat()
                || player.IsInCombat
                || (player.AggroSystem?.CombatEngaged ?? false)
                || ExistsEnemyTargetingPlayer(player.Id);

            if (!engaged)
            {
                CombatEngaged = false;
                return;
            }

            CombatEngaged = true;

            long playerMainTarget = player.CurrentTargetId;
            if (playerMainTarget != 0)
            {
                AddSharedThreat(playerMainTarget, SharedThreatFloor);
            }

            foreach (var one in _unit.LogicManager.FindEntityInRange(player.Pos, PartySyncScanRadius))
            {
                if (one is not NpcUnitLogicEntity npc || npc.MarkDestroyed || npc.IsDead)
                {
                    continue;
                }

                if (npc.CurrentTargetId == player.Id)
                {
                    AddSharedThreat(npc.Id, SharedThreatFloor);
                }
            }
        }

        bool ExistsEnemyTargetingPlayer(long playerId)
        {
            var player = _unit.LogicManager.playerLogicEntity;
            if (player == null)
            {
                return false;
            }

            foreach (var one in _unit.LogicManager.FindEntityInRange(player.Pos, PartySyncScanRadius))
            {
                if (one is not NpcUnitLogicEntity npc || npc.MarkDestroyed || npc.IsDead)
                {
                    continue;
                }

                if (npc.CurrentTargetId == playerId)
                {
                    return true;
                }
            }

            return false;
        }

        public void OnTakeDamage(long attackerId, float amount)
        {
            if (_unit.IsNoAggro()) return;

            if (_mode == EAggroMode.Player)
            {
                if (attackerId == 0)
                {
                    return;
                }

                RecordPlayerInteraction(attackerId, amount, isDealt: false);
                CombatEngaged = true;
                if (ShouldUpdatePlayerFocus(attackerId))
                {
                    CurrentTargetId = attackerId;
                }

                MarkPlayerInCombat();
                return;
            }

            if (IsPartyAlly(_unit))
            {
                CombatEngaged = true;
            }

            var info = GetOrAddHostile(attackerId);
            info.TotalDamage += amount;
            info.LastInteractionTime = LogicTime.time;
        }

        public void OnVisionUpdate()
        {
            if (IsPartyAlly(_unit)
                && _unit.FactionId == EFactionId.Ally
                && !CombatEngaged)
            {
                return;
            }

            foreach (var kv in _threatTable) kv.Value.IsVisible = false;

            foreach (var pairInfo in _unit.VisionSystem.VisibleMap)
            {
                if (!pairInfo.Value.IsInView)
                {
                    continue;
                }

                if (!pairInfo.Value.IsWitnessed)
                {
                    continue;
                }

                var seeOneEntity = _unit.LogicManager.GetLogicEntity(pairInfo.Value.TargetId, false);
                if (seeOneEntity == null || seeOneEntity is not BaseUnitLogicEntity otherUnit) continue;

                if (!_unit.IsEnmityWith(otherUnit))
                {
                    continue;
                }

                var info = GetOrAddHostile(pairInfo.Value.TargetId);
                info.IsVisible = true;
                info.LastInteractionTime = LogicTime.time;
            }
        }

        void TickPlayerCombatLog()
        {
            ExpirePlayerEntries();

            if (_threatTable.Count == 0)
            {
                CombatEngaged = false;
                CurrentTargetId = 0;
                return;
            }

            if (CurrentTargetId != 0 && !_threatTable.ContainsKey(CurrentTargetId))
            {
                CurrentTargetId = 0;
            }
        }

        void RecordPlayerInteraction(long entityId, float amount, bool isDealt)
        {
            var info = GetOrAddHostile(entityId);
            info.TotalDamage += amount;
            info.LastInteractionTime = LogicTime.time;
            if (isDealt)
            {
                info.IsDealt = true;
            }
        }

        bool ShouldUpdatePlayerFocus(long attackerId)
        {
            if (CurrentTargetId == 0)
            {
                return true;
            }

            if (!_threatTable.TryGetValue(CurrentTargetId, out var focusInfo))
            {
                return true;
            }

            if (LogicTime.time - focusInfo.LastInteractionTime > OutOfCombatTime)
            {
                return true;
            }

            // 已有出手焦点时不被受击抢走
            return !focusInfo.IsDealt;
        }

        void ExpirePlayerEntries()
        {
            List<long> toRemove = null;
            foreach (var kv in _threatTable)
            {
                if (LogicTime.time - kv.Value.LastInteractionTime > OutOfCombatTime)
                {
                    toRemove ??= new List<long>();
                    toRemove.Add(kv.Key);
                }
            }

            if (toRemove == null)
            {
                return;
            }

            foreach (var id in toRemove)
            {
                _threatTable.Remove(id);
            }
        }

        void MarkPlayerInCombat()
        {
            if (_unit.LogicManager?.GameSession != null)
            {
                _unit.LogicManager.GameSession.IsPeaceful = false;
            }
        }

        static bool IsPartyAlly(BaseUnitLogicEntity unit)
        {
            return unit != null && unit.FactionId == EFactionId.Ally;
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
                var ent = _unit.LogicManager.GetLogicEntity(kv.Key, false) as BaseUnitLogicEntity;

                if (ent != null
                    && kv.Value.TotalDamage <= SharedThreatFloor
                    && !_unit.IsEnmityWith(ent))
                {
                    toRemove ??= new List<long>();
                    toRemove.Add(kv.Key);
                    continue;
                }

                if (LogicTime.time - kv.Value.LastInteractionTime > OutOfCombatTime)
                {
                    toRemove ??= new List<long>();
                    toRemove.Add(kv.Key);
                }
            }

            if (toRemove != null)
            {
                foreach (var id in toRemove)
                {
                    _threatTable.Remove(id);
                }
            }
        }

        private void ReevaluateTarget()
        {
            if (_clearCoolTimer != 0 && LogicTime.time < _clearCoolTimer)
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
                if (kv.Value.IsVisible) score += BaseSightThreat;

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
            AggroSystem = new UnitAggroSystem(this);
        }

        public virtual bool IsNoAggro()
        {
            return false;
        }
    }
}
