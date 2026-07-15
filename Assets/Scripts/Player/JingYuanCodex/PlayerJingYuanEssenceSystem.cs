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
        public const int DefaultTemporaryHardCapacity = 50;
        public const float DefaultTemporaryOverflowGraceSeconds = 60f;
        public const int DefaultEquippedCapacity = 1;

        readonly List<PremiumEssenceInstance> _warehouse = new();
        readonly List<PremiumEssenceInstance> _temporary = new();
        readonly List<PremiumEssenceInstance> _equipped = new();
        readonly PlayerSystemManager _owner;
        long _residue;
        float _temporaryOverflowRemainingSeconds;
        GameLogicManager _logic;

        public IReadOnlyList<PremiumEssenceInstance> Warehouse => _warehouse;
        public IReadOnlyList<PremiumEssenceInstance> Temporary => _temporary;
        public IReadOnlyList<PremiumEssenceInstance> Equipped => _equipped;
        public long JingYuanResidue => _residue;
        public int WarehouseCapacity { get; private set; } = DefaultWarehouseCapacity;
        public int TemporaryCapacity { get; private set; } = DefaultTemporaryCapacity;
        public int TemporaryHardCapacity { get; private set; } = DefaultTemporaryHardCapacity;
        public int EquippedCapacity { get; private set; } = DefaultEquippedCapacity;
        public float TemporaryOverflowRemainingSeconds => _temporaryOverflowRemainingSeconds;
        public bool IsTemporaryOverCapacity => _temporary.Count > TemporaryCapacity;

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
            _residue = Math.Max(0, savingData?.PlayerData?.JingYuanResidue ?? 0);
            _temporaryOverflowRemainingSeconds = Math.Max(0, savingData?.PlayerData?.PremiumEssenceOverflowRemainingSeconds ?? 0);

            LoadList(savingData?.PlayerData?.PremiumEssenceWarehouse, _warehouse, PremiumEssenceStorageState.Warehouse);
            LoadList(savingData?.PlayerData?.PremiumEssenceTemporary, _temporary, PremiumEssenceStorageState.Temporary);
            LoadList(savingData?.PlayerData?.PremiumEssenceEquipped, _equipped, PremiumEssenceStorageState.Equipped);
            RemoveOverflow(_warehouse, WarehouseCapacity);
            RemoveOverflow(_temporary, TemporaryHardCapacity);
            RemoveOverflow(_equipped, EquippedCapacity);
            if (_temporary.Count > TemporaryCapacity && _temporaryOverflowRemainingSeconds <= 0)
                _temporaryOverflowRemainingSeconds = DefaultTemporaryOverflowGraceSeconds;
        }

        public void PostInit(PlayerSystemManager owner) { }
        public void Tick(float dt)
        {
            if (!IsTemporaryOverCapacity)
            {
                _temporaryOverflowRemainingSeconds = 0;
                return;
            }

            if (_temporaryOverflowRemainingSeconds <= 0)
                _temporaryOverflowRemainingSeconds = DefaultTemporaryOverflowGraceSeconds;
            _temporaryOverflowRemainingSeconds = Math.Max(0, _temporaryOverflowRemainingSeconds - Math.Max(0, dt));
            if (_temporaryOverflowRemainingSeconds <= 0)
                DismantleTemporaryOverflow();
        }

        public bool TryAdd(PremiumEssenceInstance essence, PremiumEssenceStorageState target = PremiumEssenceStorageState.Temporary)
        {
            if (essence == null || essence.RemainingShelfLifeDays <= 0 || Contains(essence.InstanceId)) return false;
            var list = GetList(target);
            if (list == null || (target != PremiumEssenceStorageState.Temporary && list.Count >= GetCapacity(target))
                || (target == PremiumEssenceStorageState.Temporary && list.Count >= TemporaryHardCapacity)) return false;
            essence.StorageState = target;
            if (essence.InstanceId <= 0) essence.InstanceId = ++_owner.ItemInstanceIdCounter;
            list.Add(essence);
            NotifyChanged();
            return true;
        }

        public bool TryAddFromItemStack(ItemStack stack, PremiumEssenceStorageState target = PremiumEssenceStorageState.Temporary)
        {
            var drop = stack?.InstanceInfo?.Get<ItemInstance4PremiumEssence>();
            if (drop == null || stack.Count <= 0) return false;
            var essence = JingYuanEssenceCatalog.CreateInstanceAtLevel(drop.TypeId, drop.DropLevel, drop.QualityTier, "drop_pickup", stack.ItemID);
            essence.InstanceId = drop.InstanceId > 0 ? drop.InstanceId : essence.InstanceId;
            essence.Concentration = Math.Max(0, Math.Min(100, drop.Concentration));
            return TryAdd(essence, target);
        }

        public PremiumEssenceInstance CreateAndAdd(EJingYuanType typeId, int npcLevel, string sourceType,
            PremiumEssenceStorageState target = PremiumEssenceStorageState.Temporary)
        {
            var essence = JingYuanEssenceCatalog.CreateInstance(typeId, npcLevel, sourceType);
            return TryAdd(essence, target) ? essence : null;
        }

        public PremiumEssenceInstance CreateAndAddAbstractDrop(EJingYuanType typeId, int dropLevel, int qualityTier,
            string sourceItemId, PremiumEssenceStorageState target = PremiumEssenceStorageState.Temporary)
        {
            var essence = JingYuanEssenceCatalog.CreateInstanceAtLevel(typeId, dropLevel, qualityTier,
                "abstract_item", sourceItemId);
            return TryAdd(essence, target) ? essence : null;
        }

        public bool TryMove(long instanceId, PremiumEssenceStorageState target)
        {
            if (!TryFind(instanceId, out var essence, out var source)) return false;
            if (source == target) return true;
            var targetList = GetList(target);
            if (targetList == null || (target != PremiumEssenceStorageState.Temporary && targetList.Count >= GetCapacity(target))
                || (target == PremiumEssenceStorageState.Temporary && targetList.Count >= TemporaryHardCapacity)) return false;
            GetList(source).Remove(essence);
            essence.StorageState = target;
            targetList.Add(essence);
            NotifyChanged();
            return true;
        }

        public bool TrySwapTemporary(int firstIndex, int secondIndex)
        {
            if (firstIndex < 0 || secondIndex < 0 || firstIndex >= _temporary.Count || secondIndex >= _temporary.Count || firstIndex == secondIndex) return false;
            (_temporary[firstIndex], _temporary[secondIndex]) = (_temporary[secondIndex], _temporary[firstIndex]);
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

        public bool TryRenew(long instanceId)
        {
            if (!TryFind(instanceId, out var essence, out _)) return false;
            var rule = CfgMgr.Cfgs?.TbJingYuanRenewalRule?.GetOrDefault("default");
            if (rule == null || essence.RenewalCount >= rule.MaxRenewalCount) return false;
            var inventory = _owner?.InventorySystem;
            if (inventory == null || !inventory.CheckHaveItem(rule.DesireCrystalItemId, rule.DesireCrystalCost)) return false;
            if (!TrySpendResidue(rule.ResidueCost)) return false;
            if (inventory.CostItem(rule.DesireCrystalItemId, rule.DesireCrystalCost) > 0)
            {
                AddResidue(rule.ResidueCost);
                return false;
            }
            essence.RemainingShelfLifeDays += Math.Max(0, rule.AddedShelfLifeDays);
            essence.RenewalCount++;
            NotifyChanged();
            return true;
        }

        public bool TryTune(long targetId, long donorId) => TryTune(targetId, donorId, false);

        public bool TryTune(long targetId, long donorId, bool useResidueBoost)
        {
            if (!TryFind(targetId, out var target, out _) || !TryFind(donorId, out var donor, out _) || target == donor) return false;
            if (target.TypeId != donor.TypeId) return false;
            var rule = CfgMgr.Cfgs?.TbJingYuanTuneRule?.GetOrDefault("default");
            if (rule == null) return false;
            var successRate = GetTuneSuccessRate(target, donor, useResidueBoost, rule);
            if (useResidueBoost && !TrySpendResidue(rule.ResidueBoostCost)) return false;
            var success = UnityEngine.Random.Range(0, 100) < successRate;
            TryRemove(donorId);
            AddResidue(success ? rule.ResidueOnSuccess : rule.ResidueOnFailure);
            if (!success)
            {
                NotifyChanged();
                return false;
            }
            var gain = UnityEngine.Random.Range(rule.MinGain, rule.MaxGain + 1);
            target.Concentration = Math.Min(100, target.Concentration + gain);
            if (donor.ExtraAffixIds != null && donor.ExtraAffixIds.Count > 0
                && UnityEngine.Random.Range(0, 100) < rule.AffixInheritRate)
            {
                target.ExtraAffixIds ??= new List<string>();
                foreach (var affix in donor.ExtraAffixIds)
                    if (!target.ExtraAffixIds.Contains(affix)) { target.ExtraAffixIds.Add(affix); break; }
            }
            NotifyChanged();
            return true;
        }

        public int GetTuneSuccessRate(long targetId, long donorId, bool useResidueBoost)
        {
            if (!TryFind(targetId, out var target, out _) || !TryFind(donorId, out var donor, out _) || target == donor || target.TypeId != donor.TypeId) return -1;
            var rule = CfgMgr.Cfgs?.TbJingYuanTuneRule?.GetOrDefault("default");
            return rule == null ? -1 : GetTuneSuccessRate(target, donor, useResidueBoost, rule);
        }

        static int GetTuneSuccessRate(PremiumEssenceInstance target, PremiumEssenceInstance donor, bool useResidueBoost, cfg.demo.JingYuanTuneRule rule)
        {
            var successRate = rule.BaseSuccessRate
                + Math.Max(0, donor.Concentration - target.Concentration) * rule.ConcentrationDiffRate
                - target.Concentration * rule.TargetConcentrationPenalty / 100
                - Math.Max(0, target.DropLevel - donor.DropLevel) * rule.LowerLevelPenaltyPerLevel;
            if (useResidueBoost) successRate += rule.ResidueBoostSuccessRate;
            return Math.Max(rule.MinSuccessRate, Math.Min(rule.MaxSuccessRate, successRate));
        }

        public void AddResidue(long amount)
        {
            if (amount <= 0) return;
            _residue = Math.Max(0, _residue + amount);
            NotifyChanged();
        }

        bool TrySpendResidue(long amount)
        {
            if (amount <= 0) return true;
            if (_residue < amount) return false;
            _residue -= amount;
            return true;
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
            data.JingYuanResidue = _residue;
            data.PremiumEssenceOverflowRemainingSeconds = _temporaryOverflowRemainingSeconds;
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

        int AdvanceList(List<PremiumEssenceInstance> list)
        {
            var expired = 0;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var essence = list[i];
                if (essence == null || --essence.RemainingShelfLifeDays <= 0)
                {
                    if (essence != null) AddResidue(CalculateDismantleResidue(essence));
                    list.RemoveAt(i);
                    expired++;
                }
            }
            return expired;
        }

        public int GetTemporaryOverflowCount() => Math.Max(0, _temporary.Count - TemporaryCapacity);

        void DismantleTemporaryOverflow()
        {
            var count = 0;
            for (var i = _temporary.Count - 1; i >= TemporaryCapacity; i--)
            {
                var essence = _temporary[i];
                if (essence != null) AddResidue(CalculateDismantleResidue(essence));
                _temporary.RemoveAt(i);
                count++;
            }
            _temporaryOverflowRemainingSeconds = 0;
            if (count > 0) NotifyChanged();
        }

        static int CalculateDismantleResidue(PremiumEssenceInstance essence)
        {
            if (essence == null) return 0;
            return Math.Max(1, essence.DropLevel + essence.QualityTier + essence.Concentration / 25);
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
                    QualityTier = Math.Max(1, row.QualityTier),
                    SourceItemId = row.SourceItemId,
                    ExtraAffixIds = row.ExtraAffixIds ?? new List<string>(),
                    RemainingShelfLifeDays = row.RemainingShelfLifeDays,
                    RenewalCount = Math.Max(0, row.RenewalCount),
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
                    QualityTier = essence.QualityTier,
                    SourceItemId = essence.SourceItemId,
                    ExtraAffixIds = essence.ExtraAffixIds == null ? new List<string>() : new List<string>(essence.ExtraAffixIds),
                    RemainingShelfLifeDays = essence.RemainingShelfLifeDays,
                    RenewalCount = essence.RenewalCount,
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
