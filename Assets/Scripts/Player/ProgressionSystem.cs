
using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    public class PlayerProgressionSystem : IPlayerSystem
    {
        protected GameLogicManager LogicManager { get; private set; }

        public PlayerMain BaseStats { get; private set; }
        public PlayerGearManager GearManager { get; private set; }
        public PlayerTalentManager TalentManager { get; private set; }

        public ProgressionAggregator ProgressionRoot { get; private set; }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            LogicManager = ctx;

            TalentManager = new PlayerTalentManager();
            TalentManager.Initialize(ctx, savingData);

            GearManager = new PlayerGearManager();
            GearManager.InitializeAggregatorOnly();

            BaseStats = new PlayerMain();
            BaseStats.Initialize(savingData);

            ProgressionRoot = new ProgressionAggregator("Root");
            ProgressionRoot.AddChild(BaseStats.MainAggregator);
            ProgressionRoot.AddChild(GearManager.GearAggregator);
            ProgressionRoot.AddChild(TalentManager.TalentAggregator);

            ProgressionRoot.OnStatsChanged += (src) =>
            {
                RefreshPlayerBigMapAttr();
            };
        }

        public void Tick(float dt)
        {
        }

        private Dictionary<int, float> _lastKnownValues = new Dictionary<int, float>();

        public void RefreshPlayerBigMapAttr()
        {
            StatMap currentStats = ProgressionRoot.GetRawCache();

            foreach (var kvp in currentStats)
            {
                int statId = kvp.Key;
                float newValue = kvp.Value;

                if (_lastKnownValues.TryGetValue(statId, out float oldValue))
                {
                    if (Mathf.Approximately(oldValue, newValue))
                    {
                        continue;
                    }
                }

                _lastKnownValues[statId] = newValue;
            }
        }

        public long GetFinalAttribute(int id)
        {
            return ProgressionRoot.GetValue(id);
        }

        public void OnPlayerKillUnit()
        {
        }

        public bool TryUpgradeTalentNode(int nodeId, out string failReason)
        {
            return TalentManager.TryUpgradeNode(nodeId, out failReason);
        }

        public int GetTalentNodeLevel(int nodeId)
        {
            return TalentManager.GetNodeLevel(nodeId);
        }

        public PlayerTalentManager.TalentNodeVisualState GetTalentNodeVisualState(int nodeId)
        {
            return TalentManager.GetNodeVisualState(nodeId);
        }
    }

    public class PlayerMain
    {
        public ProgressionAggregator MainAggregator;

        private BasicProgressionProvider BasicProvider;
        private LevelProgressionProvider LevelProvider;

        public void Initialize(SaveData savingData = null)
        {
            MainAggregator = new ProgressionAggregator("Main");

            BasicProvider = new BasicProgressionProvider();
            LevelProvider = new LevelProgressionProvider();

            if (savingData != null)
            {
                LevelProvider.SetLevel(savingData.PlayerData.Level);
            }

            MainAggregator.AddChild(BasicProvider);
            MainAggregator.AddChild(LevelProvider);
        }

        public void OnLevelUpdate(int newLevel)
        {
            LevelProvider.SetLevel(newLevel);
        }

        public void OnFallenAmountUpdate(long fallednAmount)
        {
            //
            // ùùùùùù
        }
    }

    public class PlayerGearManager
    {
        public ProgressionAggregator GearAggregator { get; private set; }

        PlayerEquipmentManager _equipment;
        readonly List<GearEquipProgressionProvider> _equipProviders = new();

        public void InitializeAggregatorOnly()
        {
            GearAggregator = new ProgressionAggregator("GearTotal");
        }

        public void BindEquipment(PlayerEquipmentManager equipmentManager)
        {
            _equipment = equipmentManager;
        }

        public void RebuildStatProvidersFromEquipment()
        {
            if (GearAggregator == null)
            {
                return;
            }

            foreach (var p in _equipProviders)
            {
                GearAggregator.RemoveChild(p);
            }

            _equipProviders.Clear();
            if (_equipment == null)
            {
                GearAggregator.ForceDirty();
                return;
            }

            foreach (var t in _equipment.EnumerateEquipped())
            {
                var pairs = ResolveGearStatPairs(t.itemId);
                if (pairs == null || pairs.Count == 0)
                {
                    continue;
                }

                var prov = new GearEquipProgressionProvider(t.itemId, pairs);
                GearAggregator.AddChild(prov);
                _equipProviders.Add(prov);
            }

            GearAggregator.ForceDirty();
        }

        static List<StatPair> ResolveGearStatPairs(string itemId)
        {
            return null;
        }
    }

    public sealed class PlayerTalentManager
    {
        public enum TalentNodeVisualState
        {
            Locked,
            Unlockable,
            Unlocked,
        }

        GameLogicManager _logic;
        readonly Dictionary<int, int> _nodeLevels = new();
        readonly List<TalentNodeProgressionProvider> _activeProviders = new();

        public ProgressionAggregator TalentAggregator { get; private set; }

        public Dictionary<int, PlayerTalentNode> TalentNodeDict { get; } = new();

        public void Initialize(GameLogicManager logic, SaveData savingData)
        {
            _logic = logic;
            TalentAggregator = new ProgressionAggregator("TalentTotal");
            _nodeLevels.Clear();
            _activeProviders.Clear();
            TalentNodeDict.Clear();

            if (savingData?.PlayerData?.TalentNodeLevels != null)
            {
                foreach (var e in savingData.PlayerData.TalentNodeLevels)
                {
                    if (e == null || e.NodeId <= 0 || e.Level <= 0)
                    {
                        continue;
                    }

                    _nodeLevels[e.NodeId] = e.Level;
                }
            }

            RebuildProvidersFromLevels();
        }

        public void SaveTo(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            pd.TalentNodeLevels ??= new List<TalentNodeLevelPersist>();
            pd.TalentNodeLevels.Clear();
            foreach (var kv in _nodeLevels)
            {
                if (kv.Value <= 0)
                {
                    continue;
                }

                pd.TalentNodeLevels.Add(new TalentNodeLevelPersist
                {
                    NodeId = kv.Key,
                    Level = kv.Value,
                });
            }
        }

        public int GetNodeLevel(int nodeId)
        {
            return _nodeLevels.TryGetValue(nodeId, out var lv) ? lv : 0;
        }

        public bool IsUnlocked(int nodeId) => GetNodeLevel(nodeId) >= 1;

        public TalentNodeVisualState GetNodeVisualState(int nodeId)
        {
            var nodeCfg = CfgMgr.Cfgs?.TbTalentNode?.GetOrDefault(nodeId);
            if (nodeCfg == null)
            {
                return TalentNodeVisualState.Locked;
            }

            int current = GetNodeLevel(nodeId);
            if (current >= nodeCfg.MaxLevel)
            {
                return TalentNodeVisualState.Unlocked;
            }

            if (ValidateUpgradeRequirements(nodeId, out _))
            {
                return TalentNodeVisualState.Unlockable;
            }

            return TalentNodeVisualState.Locked;
        }

        public bool TryUpgradeNode(int nodeId, out string failReason)
        {
            failReason = null;
            if (!ValidateUpgradeRequirements(nodeId, out failReason))
            {
                return false;
            }

            int next = GetNodeLevel(nodeId) + 1;
            var levelRow = CfgMgr.Cfgs?.TbTalentNodeLevel?.Get(nodeId, next);
            if (levelRow == null)
            {
                failReason = "no_level_cfg";
                return false;
            }

            var nodeCfg = CfgMgr.Cfgs.TbTalentNode.GetOrDefault(nodeId);
            if (nodeCfg == null)
            {
                failReason = "no_cfg";
                return false;
            }

            if (levelRow.UnlockCosts != null)
            {
                foreach (var c in levelRow.UnlockCosts)
                {
                    if (c == null || string.IsNullOrEmpty(c.ItemId) || c.Count <= 0)
                    {
                        continue;
                    }

                    _logic.playerDataManager.CostItem(c.ItemId, c.Count);
                }
            }

            _nodeLevels[nodeId] = next;
            RebuildProvidersFromLevels();

            if (!string.IsNullOrEmpty(nodeCfg.PassiveSkillId))
            {
                _logic.playerDataManager.TryGrantPassiveSkill(nodeCfg.PassiveSkillId, next);
            }

            return true;
        }

        bool ValidateUpgradeRequirements(int nodeId, out string failReason)
        {
            failReason = null;
            if (_logic?.playerDataManager == null)
            {
                failReason = "no_player_mgr";
                return false;
            }

            var nodeCfg = CfgMgr.Cfgs?.TbTalentNode?.GetOrDefault(nodeId);
            if (nodeCfg == null)
            {
                failReason = "no_cfg";
                return false;
            }

            int current = GetNodeLevel(nodeId);
            int next = current + 1;
            if (next > nodeCfg.MaxLevel)
            {
                failReason = "max_level";
                return false;
            }

            var levelRow = CfgMgr.Cfgs?.TbTalentNodeLevel?.Get(nodeId, next);
            if (levelRow == null)
            {
                failReason = "no_level_cfg";
                return false;
            }

            if (levelRow.PrereqNodeIds != null)
            {
                foreach (var pre in levelRow.PrereqNodeIds)
                {
                    if (GetNodeLevel(pre) < 1)
                    {
                        failReason = "prereq";
                        return false;
                    }
                }
            }

            if (levelRow.UnlockConds != null && levelRow.UnlockConds.Count > 0)
            {
                if (!_logic.CheckCommonCondsAll(levelRow.UnlockConds))
                {
                    failReason = "conds";
                    return false;
                }
            }

            if (levelRow.UnlockCosts != null)
            {
                foreach (var c in levelRow.UnlockCosts)
                {
                    if (c == null || string.IsNullOrEmpty(c.ItemId) || c.Count <= 0)
                    {
                        continue;
                    }

                    if (!_logic.playerDataManager.CheckHaveItem(c.ItemId, c.Count))
                    {
                        failReason = "cost_" + c.ItemId;
                        return false;
                    }
                }
            }

            return true;
        }

        void RebuildProvidersFromLevels()
        {
            foreach (var p in _activeProviders)
            {
                TalentAggregator.RemoveChild(p);
            }

            _activeProviders.Clear();
            TalentNodeDict.Clear();

            var levelTable = CfgMgr.Cfgs?.TbTalentNodeLevel;
            if (levelTable == null)
            {
                return;
            }

            foreach (var kv in _nodeLevels)
            {
                int nodeId = kv.Key;
                int current = kv.Value;
                if (current <= 0)
                {
                    continue;
                }

                var pairs = new List<StatPair>();
                for (int lv = 1; lv <= current; lv++)
                {
                    var row = levelTable.Get(nodeId, lv);
                    if (row?.StatBonuses == null)
                    {
                        continue;
                    }

                    foreach (var b in row.StatBonuses)
                    {
                        if (b == null)
                        {
                            continue;
                        }

                        pairs.Add(new StatPair(b.AttrId, b.Val));
                    }
                }

                if (pairs.Count == 0)
                {
                    continue;
                }

                var provider = new TalentNodeProgressionProvider(pairs);
                TalentAggregator.AddChild(provider);
                _activeProviders.Add(provider);
                TalentNodeDict[nodeId] = new PlayerTalentNode
                {
                    NodeId = nodeId,
                    Provider = provider,
                };
            }

            TalentAggregator.ForceDirty();
        }
    }

    public class PlayerTalentNode
    {
        public int NodeId;
        public TalentNodeProgressionProvider Provider;
    }
}
