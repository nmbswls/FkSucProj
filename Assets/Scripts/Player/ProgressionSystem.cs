
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

        public bool TryUnlockTalentNode(int nodeId, out string failReason)
        {
            return TalentManager.TryUnlockNode(nodeId, out failReason);
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
            // ∏¯ Ù–‘
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
        readonly HashSet<int> _unlocked = new();
        readonly List<TalentNodeProgressionProvider> _activeProviders = new();

        public ProgressionAggregator TalentAggregator { get; private set; }

        public Dictionary<int, PlayerTalentNode> TalentNodeDict { get; } = new();

        public void Initialize(GameLogicManager logic, SaveData savingData)
        {
            _logic = logic;
            TalentAggregator = new ProgressionAggregator("TalentTotal");
            _unlocked.Clear();
            _activeProviders.Clear();
            TalentNodeDict.Clear();

            if (savingData?.PlayerData?.UnlockedTalentNodeIds != null)
            {
                foreach (var id in savingData.PlayerData.UnlockedTalentNodeIds)
                {
                    _unlocked.Add(id);
                }
            }

            RebuildProvidersFromUnlockState();
        }

        public void SaveTo(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            pd.UnlockedTalentNodeIds ??= new List<int>();
            pd.UnlockedTalentNodeIds.Clear();
            foreach (var id in _unlocked)
            {
                pd.UnlockedTalentNodeIds.Add(id);
            }
        }

        public bool IsUnlocked(int nodeId) => _unlocked.Contains(nodeId);

        public TalentNodeVisualState GetNodeVisualState(int nodeId)
        {
            if (_unlocked.Contains(nodeId))
            {
                return TalentNodeVisualState.Unlocked;
            }

            if (ValidateUnlockRequirements(nodeId, out _))
            {
                return TalentNodeVisualState.Unlockable;
            }

            return TalentNodeVisualState.Locked;
        }

        public bool TryUnlockNode(int nodeId, out string failReason)
        {
            failReason = null;
            if (!ValidateUnlockRequirements(nodeId, out failReason))
            {
                return false;
            }

            var cfg = CfgMgr.Cfgs?.TbTalentNode?.GetOrDefault(nodeId);
            if (cfg == null)
            {
                failReason = "no_cfg";
                return false;
            }

            if (cfg.UnlockCosts != null)
            {
                foreach (var c in cfg.UnlockCosts)
                {
                    if (c == null || string.IsNullOrEmpty(c.ItemId) || c.Count <= 0)
                    {
                        continue;
                    }

                    _logic.playerDataManager.CostItem(c.ItemId, c.Count);
                }
            }

            _unlocked.Add(nodeId);
            AddProviderForCfg(cfg);

            if (!string.IsNullOrEmpty(cfg.PassiveSkillId))
            {
                _logic.playerDataManager.TryAddLearnedSkill(cfg.PassiveSkillId);
                _logic.playerDataManager.SyncLearnedSkillsToPlayerEntity();
            }

            return true;
        }

        bool ValidateUnlockRequirements(int nodeId, out string failReason)
        {
            failReason = null;
            if (_logic?.playerDataManager == null)
            {
                failReason = "no_player_mgr";
                return false;
            }

            if (_unlocked.Contains(nodeId))
            {
                failReason = "already_unlocked";
                return false;
            }

            var cfg = CfgMgr.Cfgs?.TbTalentNode?.GetOrDefault(nodeId);
            if (cfg == null)
            {
                failReason = "no_cfg";
                return false;
            }

            if (cfg.PrereqNodeIds != null)
            {
                foreach (var pre in cfg.PrereqNodeIds)
                {
                    if (!_unlocked.Contains(pre))
                    {
                        failReason = "prereq";
                        return false;
                    }
                }
            }

            if (cfg.UnlockConds != null && cfg.UnlockConds.Count > 0)
            {
                if (!_logic.CheckCommonCondsAll(cfg.UnlockConds))
                {
                    failReason = "conds";
                    return false;
                }
            }

            if (cfg.UnlockCosts != null)
            {
                foreach (var c in cfg.UnlockCosts)
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

        void RebuildProvidersFromUnlockState()
        {
            foreach (var p in _activeProviders)
            {
                TalentAggregator.RemoveChild(p);
            }

            _activeProviders.Clear();
            TalentNodeDict.Clear();

            var table = CfgMgr.Cfgs?.TbTalentNode;
            if (table == null)
            {
                return;
            }

            foreach (var id in _unlocked)
            {
                var cfg = table.GetOrDefault(id);
                if (cfg == null)
                {
                    continue;
                }

                AddProviderForCfg(cfg);
            }
        }

        void AddProviderForCfg(TalentNode cfg)
        {
            var pairs = new List<StatPair>();
            if (cfg.StatBonuses != null)
            {
                foreach (var b in cfg.StatBonuses)
                {
                    if (b == null)
                    {
                        continue;
                    }

                    pairs.Add(new StatPair(b.AttrId, b.Val));
                }
            }

            var provider = new TalentNodeProgressionProvider(pairs);
            TalentAggregator.AddChild(provider);
            _activeProviders.Add(provider);
            TalentNodeDict[cfg.NodeId] = new PlayerTalentNode
            {
                NodeId = cfg.NodeId,
                Provider = provider,
            };
        }
    }

    public class PlayerTalentNode
    {
        public int NodeId;
        public TalentNodeProgressionProvider Provider;
    }
}
