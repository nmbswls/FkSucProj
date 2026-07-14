using System.Collections.Generic;
using My.Map.Entity;

namespace My.Map.Logic
{
    public sealed class SummonedEntityRegistry
    {
        const float OwnerCombatEndGraceSeconds = 0.5f;

        readonly GameLogicAreaManager _area;
        readonly HashSet<long> _tracked = new();
        readonly Dictionary<(long OwnerId, string Group), List<long>> _groups = new();
        readonly Dictionary<long, float> _combatLostSince = new();
        readonly List<long> _scratchIds = new();

        public SummonedEntityRegistry(GameLogicAreaManager area)
        {
            _area = area;
        }

        public void Register(LogicEntityRecord record, int maxAlive)
        {
            if (record == null || record.OwnerEntityId == 0)
            {
                return;
            }

            Track(record);
            if (string.IsNullOrWhiteSpace(record.SummonGroup))
            {
                return;
            }

            var key = (record.OwnerEntityId, record.SummonGroup);
            if (!_groups.TryGetValue(key, out var entityIds))
            {
                entityIds = new List<long>();
                _groups.Add(key, entityIds);
            }

            if (!entityIds.Contains(record.Id))
            {
                entityIds.Add(record.Id);
            }

            while (maxAlive > 0 && entityIds.Count > maxAlive)
            {
                long oldestId = entityIds[0];
                entityIds.RemoveAt(0);
                Destroy(oldestId, "summon_limit");
            }
        }

        public void RebuildFromRecords()
        {
            _tracked.Clear();
            _groups.Clear();
            _combatLostSince.Clear();

            foreach (var record in _area.Repo.Records.Values)
            {
                Track(record);
            }

            foreach (var entityIds in _groups.Values)
            {
                entityIds.Sort();
            }
        }

        public void ClearForCombatReset(long ownerId)
        {
            CollectOwned(ownerId, ESummonLifetimeRule.WhileOwnerInCombat);
            DestroyScratch("summon_combat_reset");
        }

        public void ClearForSourceDestroyed(long ownerId)
        {
            _scratchIds.Clear();
            foreach (long entityId in _tracked)
            {
                if (TryGetRecord(entityId, out var record)
                    && record.OwnerEntityId == ownerId
                    && record.SummonLifetimeRule != ESummonLifetimeRule.Independent)
                {
                    _scratchIds.Add(entityId);
                }
            }
            DestroyScratch("summon_owner_destroyed");
        }

        public void ClearAll()
        {
            _tracked.Clear();
            _groups.Clear();
            _combatLostSince.Clear();
            _scratchIds.Clear();
        }

        public void Unregister(long entityId)
        {
            RemoveTracking(entityId);
        }

        public void Tick()
        {
            _scratchIds.Clear();
            _scratchIds.AddRange(_tracked);

            for (int i = 0; i < _scratchIds.Count; i++)
            {
                long entityId = _scratchIds[i];
                if (!TryGetRecord(entityId, out var record))
                {
                    RemoveTracking(entityId);
                    continue;
                }

                if (record.OwnerEntityId == 0
                    || record.SummonLifetimeRule == ESummonLifetimeRule.Independent)
                {
                    _combatLostSince.Remove(entityId);
                    continue;
                }

                if (!_area.Repo.Records.TryGetValue(record.OwnerEntityId, out var ownerRecord)
                    || ownerRecord.MarkDestroyed)
                {
                    Destroy(entityId, "summon_owner_missing");
                    continue;
                }

                var owner = _area.GetLogicEntiy(record.OwnerEntityId, false) as BaseUnitLogicEntity;
                if (owner == null)
                {
                    // An unloaded owner still exists. Do not treat AOI sleep as destruction.
                    continue;
                }

                if (owner.MarkDestroyed || owner.IsDead)
                {
                    Destroy(entityId, "summon_owner_dead");
                    continue;
                }

                if (record.SummonLifetimeRule != ESummonLifetimeRule.WhileOwnerInCombat)
                {
                    continue;
                }

                if (owner.IsInCombat)
                {
                    record.HasObservedOwnerCombat = true;
                    _combatLostSince.Remove(entityId);
                    continue;
                }

                if (!record.HasObservedOwnerCombat)
                {
                    continue;
                }

                if (!_combatLostSince.TryGetValue(entityId, out float lostSince))
                {
                    _combatLostSince[entityId] = LogicTime.time;
                    continue;
                }

                if (LogicTime.time - lostSince >= OwnerCombatEndGraceSeconds)
                {
                    Destroy(entityId, "summon_owner_combat_ended");
                }
            }
        }

        void Track(LogicEntityRecord record)
        {
            if (record == null || record.OwnerEntityId == 0)
            {
                return;
            }

            _tracked.Add(record.Id);
            if (string.IsNullOrWhiteSpace(record.SummonGroup))
            {
                return;
            }

            var key = (record.OwnerEntityId, record.SummonGroup);
            if (!_groups.TryGetValue(key, out var entityIds))
            {
                entityIds = new List<long>();
                _groups.Add(key, entityIds);
            }
            if (!entityIds.Contains(record.Id))
            {
                entityIds.Add(record.Id);
            }
        }

        void CollectOwned(long ownerId, ESummonLifetimeRule rule)
        {
            _scratchIds.Clear();
            foreach (long entityId in _tracked)
            {
                if (TryGetRecord(entityId, out var record)
                    && record.OwnerEntityId == ownerId
                    && record.SummonLifetimeRule == rule)
                {
                    _scratchIds.Add(entityId);
                }
            }
        }

        void DestroyScratch(string reason)
        {
            for (int i = 0; i < _scratchIds.Count; i++)
            {
                Destroy(_scratchIds[i], reason);
            }
            _scratchIds.Clear();
        }

        void Destroy(long entityId, string reason)
        {
            RemoveTracking(entityId);
            if (_area.Repo.HasRecord(entityId))
            {
                _area.ForceDestroyEntityNow(entityId, reason);
            }
        }

        void RemoveTracking(long entityId)
        {
            _tracked.Remove(entityId);
            _combatLostSince.Remove(entityId);

            foreach (var entityIds in _groups.Values)
            {
                entityIds.Remove(entityId);
            }
        }

        bool TryGetRecord(long entityId, out LogicEntityRecord record)
        {
            return _area.Repo.Records.TryGetValue(entityId, out record);
        }

    }
}
