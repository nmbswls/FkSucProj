using System;
using System.Collections.Generic;
using cfg.demo;
using My.Saving;

namespace My.Player
{
    public enum HumanTechNodeVisualState
    {
        Locked,
        Unlockable,
        InsufficientCost,
        Unlocked,
    }

    public sealed class HumanCivilizationSystem
    {
        readonly Dictionary<int, int> _techLevels = new();
        GameLogicManager _logic;

        public event Action<int> OnCivilizationLevelChanged;
        public event Action<int> OnTechNodeChanged;

        public void Initialize(GameLogicManager logic, SaveData savingData)
        {
            _logic = logic;
            _techLevels.Clear();
            var entries = savingData?.HumanCivilization?.TechNodes;

            if (entries == null)
            {
                return;
            }

            foreach (var entry in entries)
            {
                if (entry != null && entry.NodeId > 0 && entry.Level > 0)
                {
                    _techLevels[entry.NodeId] = entry.Level;
                }
            }
        }

        public void SaveTo(SaveData savingData)
        {
            if (savingData == null)
            {
                return;
            }

            savingData.HumanCivilization ??= new HumanCivilizationPersist();
            savingData.HumanCivilization.TechNodes ??= new List<HumanTechNodeLevelPersist>();
            savingData.HumanCivilization.TechNodes.Clear();
            foreach (var pair in _techLevels)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                savingData.HumanCivilization.TechNodes.Add(new HumanTechNodeLevelPersist
                {
                    NodeId = pair.Key,
                    Level = pair.Value,
                });
            }
        }

        public int GetUnlockedTechCount()
        {
            int count = 0;
            foreach (var pair in _techLevels)
            {
                if (pair.Value > 0)
                {
                    count++;
                }
            }

            return count;
        }

        public int GetTechNodeLevel(int nodeId)
        {
            return _techLevels.TryGetValue(nodeId, out var level) ? level : 0;
        }

        public int GetCivilizationLevel()
        {
            int result = 0;
            var table = My.Config.CfgMgr.Cfgs?.TbHumanCivilizationLevel;
            if (table == null)
            {
                return result;
            }

            foreach (var row in table.DataList)
            {
                if (row == null || row.Level <= result)
                {
                    continue;
                }

                if (CheckLevelConditions(row))
                {
                    result = row.Level;
                }
            }

            return result;
        }

        public HumanTechNodeVisualState GetTechNodeVisualState(int nodeId)
        {
            var node = My.Config.CfgMgr.Cfgs?.TbHumanTechNode?.GetOrDefault(nodeId);
            if (node == null)
            {
                return HumanTechNodeVisualState.Locked;
            }

            int current = GetTechNodeLevel(nodeId);
            if (current >= node.MaxLevel)
            {
                return HumanTechNodeVisualState.Unlocked;
            }

            if (node.RequiredCivilizationLevel > GetCivilizationLevel())
            {
                return HumanTechNodeVisualState.Locked;
            }

            var level = My.Config.CfgMgr.Cfgs?.TbHumanTechNodeLevel?.Get(nodeId, current + 1);
            if (level == null || !CheckPrerequisites(level) || !CheckCommonConditions(level.UnlockConds))
            {
                return HumanTechNodeVisualState.Locked;
            }

            return CanPayUnlockCosts(level) ? HumanTechNodeVisualState.Unlockable : HumanTechNodeVisualState.InsufficientCost;
        }

        public bool TryUnlockTechNode(int nodeId, out string failReason)
        {
            failReason = null;
            var node = My.Config.CfgMgr.Cfgs?.TbHumanTechNode?.GetOrDefault(nodeId);
            if (node == null)
            {
                failReason = "unknown tech node";
                return false;
            }

            int next = GetTechNodeLevel(nodeId) + 1;
            if (next > node.MaxLevel)
            {
                failReason = "tech node is already max level";
                return false;
            }

            if (node.RequiredCivilizationLevel > GetCivilizationLevel())
            {
                failReason = "civilization level is too low";
                return false;
            }

            var level = My.Config.CfgMgr.Cfgs?.TbHumanTechNodeLevel?.Get(nodeId, next);
            if (level == null || !CheckPrerequisites(level))
            {
                failReason = "prerequisite tech is missing";
                return false;
            }

            if (!CheckCommonConditions(level.UnlockConds))
            {
                failReason = "unlock conditions are not met";
                return false;
            }

            if (!TryPayUnlockCosts(level, out failReason))
            {
                return false;
            }

            _techLevels[nodeId] = next;
            OnTechNodeChanged?.Invoke(nodeId);
            OnCivilizationLevelChanged?.Invoke(GetCivilizationLevel());
            return true;
        }

        bool CheckLevelConditions(HumanCivilizationLevel row)
        {
            if (_logic == null || !CheckCommonConditions(row.UnlockConds))
            {
                return false;
            }

            if (row.CustomUnlockConds == null)
            {
                return true;
            }

            foreach (var cond in row.CustomUnlockConds)
            {
                if (!CheckCustomCondition(cond))
                {
                    return false;
                }
            }

            return true;
        }

        bool CheckCommonConditions(IReadOnlyList<CommonCheckCond> conditions)
        {
            return _logic != null && _logic.CheckCommonCondsAll(conditions);
        }

        bool CheckCustomCondition(HumanCivilizationCustomCond cond)
        {
            if (cond == null || string.IsNullOrEmpty(cond.Type))
            {
                return true;
            }

            if (cond.Type == "homestead_building_level")
            {
                var parts = cond.Param1?.Split('|');
                if (parts == null || parts.Length != 2 || _logic?.worldPersistState == null)
                {
                    return false;
                }

                return _logic.worldPersistState.GetFacilityDevelopmentLevel(parts[0], parts[1]) >= cond.Param2;
            }

            return false;
        }

        bool CheckPrerequisites(HumanTechNodeLevel level)
        {
            if (level.PrereqNodeIds == null)
            {
                return true;
            }

            foreach (var prerequisite in level.PrereqNodeIds)
            {
                if (GetTechNodeLevel(prerequisite) <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        bool HasUnlockCosts(HumanTechNodeLevel level)
        {
            return level?.UnlockCosts != null && level.UnlockCosts.Count > 0;
        }

        bool CanPayUnlockCosts(HumanTechNodeLevel level)
        {
            var player = _logic?.playerDataManager;
            if (player == null)
            {
                return false;
            }

            if (!HasUnlockCosts(level))
            {
                return true;
            }

            foreach (var cost in level.UnlockCosts)
            {
                if (cost != null && !string.IsNullOrEmpty(cost.ItemId) && cost.Count > 0
                    && !player.CheckHaveItem(cost.ItemId, cost.Count))
                {
                    return false;
                }
            }

            return true;
        }

        public long GetTechEffectValue(EHumanCivilizationAttribute effectKey)
        {
            if (effectKey == EHumanCivilizationAttribute.None)
            {
                return 0;
            }

            long value = 0;
            foreach (var pair in _techLevels)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                for (int level = 1; level <= pair.Value; level++)
                {
                    var row = My.Config.CfgMgr.Cfgs?.TbHumanTechNodeLevel?.Get(pair.Key, level);
                    if (row != null && row.EffectKey == effectKey)
                    {
                        value += row.EffectValue;
                    }
                }
            }

            return value;
        }

        public long GetTradeCost(long baseCost)
        {
            if (baseCost <= 0) return 0;
            var tier = GetTechEffectValue(EHumanCivilizationAttribute.ScavengeExchangeAccess);
            return Math.Max(1, (long)Math.Ceiling(baseCost * 100d / (100d + Math.Max(0, tier) * 10d)));
        }

        public long ModifyExplorationLoot(long baseAmount)
        {
            if (baseAmount <= 0) return 0;
            var tier = GetTechEffectValue(EHumanCivilizationAttribute.ExplorationLootValueBonus);
            return Math.Max(0, (long)Math.Floor(baseAmount * (1d + Math.Max(0, tier) * 0.1d)));
        }

        public bool HasTechEffect(EHumanCivilizationAttribute effectKey, long minValue = 1)
        {
            return GetTechEffectValue(effectKey) >= minValue;
        }
        bool TryPayUnlockCosts(HumanTechNodeLevel level, out string failReason)
        {
            failReason = null;
            var player = _logic?.playerDataManager;
            if (player == null)
            {
                failReason = "player data manager unavailable";
                return false;
            }

            if (!HasUnlockCosts(level))
            {
                return true;
            }

            foreach (var cost in level.UnlockCosts)
            {
                if (cost == null || string.IsNullOrEmpty(cost.ItemId) || cost.Count <= 0)
                {
                    continue;
                }

                if (!player.CheckHaveItem(cost.ItemId, cost.Count))
                {
                    failReason = "missing cost item: " + cost.ItemId;
                    return false;
                }
            }

            foreach (var cost in level.UnlockCosts)
            {
                if (cost != null && !string.IsNullOrEmpty(cost.ItemId) && cost.Count > 0)
                {
                    player.CostItem(cost.ItemId, cost.Count);
                }
            }

            return true;
        }
    }
}
