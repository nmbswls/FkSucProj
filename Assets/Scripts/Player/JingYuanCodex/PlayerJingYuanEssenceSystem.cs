using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Saving;

namespace My.Player
{
    /// <summary>
    /// Owns temporary PremiumEssenceInstance entities. This deliberately does not use the item bag.
    /// </summary>
    public sealed class PlayerJingYuanEssenceSystem : IPlayerSystem, IProgressionSource, IProgressionSkillSource
    {
        public const int DefaultWarehouseCapacity = 50;
        public const int DefaultTemporaryCapacity = 10;
        public const int DefaultEquippedCapacity = 1;

        readonly List<PremiumEssenceInstance> _warehouse = new();
        readonly List<PremiumEssenceInstance> _temporary = new();
        readonly List<PremiumEssenceInstance> _equipped = new();
        readonly PlayerSystemManager _owner;
        GameLogicManager _logic;

        public IReadOnlyList<PremiumEssenceInstance> Warehouse => _warehouse;
        public IReadOnlyList<PremiumEssenceInstance> Temporary => _temporary;
        public IReadOnlyList<PremiumEssenceInstance> Equipped => _equipped;
        public int WarehouseCapacity { get; private set; } = DefaultWarehouseCapacity;
        public int TemporaryCapacity { get; private set; } = DefaultTemporaryCapacity;
        public int EquippedCapacity { get; private set; } = DefaultEquippedCapacity;

        public event Action<int> EventOnExpired;
        public event Action EventOnChanged;
        public event Action<IProgressionSource> OnStatsChanged;
        public EProgressionModule ModuleName => EProgressionModule.JingYuanCodex;

        public PlayerJingYuanEssenceSystem(PlayerSystemManager owner)
        {
            _owner = owner;
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            _logic = ctx;
            _logic.EventOnNextDayPeriod -= HandleNextDay;
            _logic.EventOnNextDayPeriod += HandleNextDay;
            _warehouse.Clear();
            _temporary.Clear();
            _equipped.Clear();

            LoadList(savingData?.PlayerData?.PremiumEssenceWarehouse, _warehouse, PremiumEssenceStorageState.Warehouse);
            LoadList(savingData?.PlayerData?.PremiumEssenceTemporary, _temporary, PremiumEssenceStorageState.Temporary);
            LoadList(savingData?.PlayerData?.PremiumEssenceEquipped, _equipped, PremiumEssenceStorageState.Equipped);
            RemoveOverflow(_warehouse, WarehouseCapacity);
            RemoveOverflow(_temporary, TemporaryCapacity);
            RemoveOverflow(_equipped, EquippedCapacity);
        }

        public void PostInit(PlayerSystemManager owner) { }
        public void Tick(float dt) { }

        public bool TryAdd(PremiumEssenceInstance essence, PremiumEssenceStorageState target = PremiumEssenceStorageState.Temporary)
        {
            if (essence == null || essence.RemainingShelfLifeDays <= 0 || Contains(essence.InstanceId)) return false;
            var list = GetList(target);
            if (list == null || list.Count >= GetCapacity(target)) return false;
            essence.StorageState = target;
            if (essence.InstanceId <= 0) essence.InstanceId = ++_owner.ItemInstanceIdCounter;
            list.Add(essence);
            NotifyChanged();
            return true;
        }

        public PremiumEssenceInstance CreateAndAdd(EJingYuanType typeId, int npcLevel, string sourceType,
            PremiumEssenceStorageState target = PremiumEssenceStorageState.Temporary)
        {
            var essence = JingYuanEssenceCatalog.CreateInstance(typeId, npcLevel, sourceType);
            return TryAdd(essence, target) ? essence : null;
        }

        public bool TryMove(long instanceId, PremiumEssenceStorageState target)
        {
            if (!TryFind(instanceId, out var essence, out var source)) return false;
            if (source == target) return true;
            var targetList = GetList(target);
            if (targetList == null || targetList.Count >= GetCapacity(target)) return false;
            GetList(source).Remove(essence);
            essence.StorageState = target;
            targetList.Add(essence);
            NotifyChanged();
            return true;
        }

        public bool TryEquip(long instanceId)
        {
            if (!TryFind(instanceId, out var essence, out var source)) return false;
            if (source == PremiumEssenceStorageState.Equipped) return true;
            if (_equipped.Count >= EquippedCapacity)
            {
                if (_equipped.Count == 0) return false;
                var replaced = _equipped[0];
                var fallback = _temporary.Count < TemporaryCapacity
                    ? PremiumEssenceStorageState.Temporary
                    : (_warehouse.Count < WarehouseCapacity ? PremiumEssenceStorageState.Warehouse : PremiumEssenceStorageState.Temporary);
                if (!TryMove(replaced.InstanceId, fallback)) return false;
            }

            return TryMove(essence.InstanceId, PremiumEssenceStorageState.Equipped);
        }

        public bool TryUnequip(long instanceId)
        {
            if (!TryFind(instanceId, out _, out var source) || source != PremiumEssenceStorageState.Equipped) return false;
            var target = _temporary.Count < TemporaryCapacity
                ? PremiumEssenceStorageState.Temporary
                : PremiumEssenceStorageState.Warehouse;
            return TryMove(instanceId, target);
        }

        public bool TryRemove(long instanceId)
        {
            if (!TryFind(instanceId, out var essence, out var source)) return false;
            var removed = GetList(source).Remove(essence);
            if (removed) NotifyChanged();
            return removed;
        }

        public void EvaluateStats(StatMap targetMap)
        {
            if (targetMap == null) return;
            foreach (var essence in _equipped)
            {
                if (essence == null || essence.RemainingShelfLifeDays <= 0) continue;
                var effect = JingYuanEssenceCatalog.ResolveEffect(essence.TypeId, essence.DropLevel, essence.Concentration);
                if (effect != null && effect.AttrId != 0 && effect.AttrValue != 0)
                {
                    targetMap.Add(effect.AttrId, effect.AttrValue);
                }
            }
        }

        public void CollectContributedSkills(HashSet<string> applied, List<(string skillId, int level)> output)
        {
            if (output == null) return;
            foreach (var essence in _equipped)
            {
                if (essence?.ExtraAffixIds == null) continue;
                foreach (var skillId in essence.ExtraAffixIds)
                {
                    if (string.IsNullOrEmpty(skillId) || (applied != null && applied.Contains(skillId))) continue;
                    applied?.Add(skillId);
                    output.Add((skillId, 1));
                }
            }
        }

        public void WriteToSave(PlayerData data)
        {
            if (data == null) return;
            data.PremiumEssenceWarehouse ??= new List<PremiumEssencePersist>();
            data.PremiumEssenceTemporary ??= new List<PremiumEssencePersist>();
            data.PremiumEssenceEquipped ??= new List<PremiumEssencePersist>();
            WriteList(data.PremiumEssenceWarehouse, _warehouse);
            WriteList(data.PremiumEssenceTemporary, _temporary);
            WriteList(data.PremiumEssenceEquipped, _equipped);
        }

        void HandleNextDay()
        {
            var expired = 0;
            expired += AdvanceList(_warehouse);
            expired += AdvanceList(_temporary);
            expired += AdvanceList(_equipped);
            if (expired > 0)
            {
                NotifyChanged();
                EventOnExpired?.Invoke(expired);
            }
        }

        static int AdvanceList(List<PremiumEssenceInstance> list)
        {
            var expired = 0;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var essence = list[i];
                if (essence == null || --essence.RemainingShelfLifeDays <= 0)
                {
                    list.RemoveAt(i);
                    expired++;
                }
            }
            return expired;
        }

        void NotifyChanged()
        {
            OnStatsChanged?.Invoke(this);
            EventOnChanged?.Invoke();
        }

        void LoadList(List<PremiumEssencePersist> source, List<PremiumEssenceInstance> target, PremiumEssenceStorageState state)
        {
            if (source == null) return;
            foreach (var row in source)
            {
                if (row == null || row.InstanceId <= 0 || row.RemainingShelfLifeDays <= 0 || Contains(row.InstanceId)) continue;
                target.Add(new PremiumEssenceInstance
                {
                    InstanceId = row.InstanceId,
                    TypeId = row.TypeId,
                    Concentration = row.Concentration < 0 ? 0 : (row.Concentration > 100 ? 100 : row.Concentration),
                    DropLevel = Math.Max(1, row.DropLevel),
                    ExtraAffixIds = row.ExtraAffixIds ?? new List<string>(),
                    RemainingShelfLifeDays = row.RemainingShelfLifeDays,
                    SourceType = row.SourceType,
                    StorageState = state,
                });
            }
        }

        static void WriteList(List<PremiumEssencePersist> target, List<PremiumEssenceInstance> source)
        {
            target ??= new List<PremiumEssencePersist>();
            target.Clear();
            foreach (var essence in source)
            {
                if (essence == null || essence.RemainingShelfLifeDays <= 0) continue;
                target.Add(new PremiumEssencePersist
                {
                    InstanceId = essence.InstanceId,
                    TypeId = essence.TypeId,
                    Concentration = essence.Concentration,
                    DropLevel = essence.DropLevel,
                    ExtraAffixIds = essence.ExtraAffixIds == null ? new List<string>() : new List<string>(essence.ExtraAffixIds),
                    RemainingShelfLifeDays = essence.RemainingShelfLifeDays,
                    SourceType = essence.SourceType,
                });
            }
        }

        List<PremiumEssenceInstance> GetList(PremiumEssenceStorageState state) => state switch
        {
            PremiumEssenceStorageState.Warehouse => _warehouse,
            PremiumEssenceStorageState.Equipped => _equipped,
            PremiumEssenceStorageState.Temporary => _temporary,
            _ => null,
        };

        int GetCapacity(PremiumEssenceStorageState state) => state switch
        {
            PremiumEssenceStorageState.Warehouse => WarehouseCapacity,
            PremiumEssenceStorageState.Equipped => EquippedCapacity,
            PremiumEssenceStorageState.Temporary => TemporaryCapacity,
            _ => 0,
        };

        bool Contains(long id) => Find(id, _warehouse) != null || Find(id, _temporary) != null || Find(id, _equipped) != null;

        bool TryFind(long id, out PremiumEssenceInstance essence, out PremiumEssenceStorageState state)
        {
            essence = Find(id, _warehouse); state = PremiumEssenceStorageState.Warehouse;
            if (essence != null) return true;
            essence = Find(id, _temporary); state = PremiumEssenceStorageState.Temporary;
            if (essence != null) return true;
            essence = Find(id, _equipped); state = PremiumEssenceStorageState.Equipped;
            return essence != null;
        }

        static PremiumEssenceInstance Find(long id, List<PremiumEssenceInstance> list)
        {
            foreach (var item in list) if (item != null && item.InstanceId == id) return item;
            return null;
        }

        static void RemoveOverflow(List<PremiumEssenceInstance> list, int capacity)
        {
            while (list.Count > capacity) list.RemoveAt(list.Count - 1);
        }
    }
}
