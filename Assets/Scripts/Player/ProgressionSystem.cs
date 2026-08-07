
using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Quest;
using My.Saving;
using My.UI.Talent;
using UnityEngine;

namespace My.Player
{
    public class PlayerProgressionSystem : IPlayerSystem, ITalentProgressionContext
    {
        protected GameLogicManager LogicManager { get; private set; }
        bool _bossEncounterEventsBound;

        public PlayerMain BaseStats { get; private set; }
        public PlayerGearManager GearManager { get; private set; }
        public PlayerTalentManager TalentManager { get; private set; }
        public HumanCivilizationSystem HumanCivilization { get; private set; }
        public DemonCultSystem DemonCult { get; private set; }

        public ProgressionAggregator ProgressionRoot { get; private set; }
        public ProgressionAggregator BodyPartAggregator { get; private set; }
        public ProgressionAggregator EventGrantAggregator { get; private set; }
        public ProgressionAggregator RuneAggregator { get; private set; }
        public ProgressionAggregator JingYuanEssenceAggregator { get; private set; }
        public bool IsBodyPartBound { get; private set; }
        public bool IsEventGrantBound { get; private set; }
        public bool IsRuneBound { get; private set; }

        BodyPartProgressionProvider _boundBodyPartProvider;
        EventGrantProgressionProvider _boundEventGrantProvider;
        RuneProgressionProvider _boundRuneProvider;
        PlayerJingYuanEssenceSystem _boundJingYuanEssenceProvider;

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            LogicManager = ctx;
            IsBodyPartBound = false;
            IsEventGrantBound = false;
            IsRuneBound = false;
            _boundBodyPartProvider = null;
            _boundEventGrantProvider = null;
            _boundRuneProvider = null;
            _boundJingYuanEssenceProvider = null;

            HumanCivilization = new HumanCivilizationSystem();
            HumanCivilization.Initialize(ctx, savingData);

            TalentManager = new PlayerTalentManager();
            TalentManager.Initialize(ctx, savingData);

            DemonCult = new DemonCultSystem();
            DemonCult.Initialize(ctx, savingData);

            if (!_bossEncounterEventsBound)
            {
                PlayerEventBus.Subscribe<PlayerBossEncounterDefeatedEvent>(OnBossEncounterDefeated);
                _bossEncounterEventsBound = true;
            }

            GearManager = new PlayerGearManager();
            GearManager.InitializeAggregatorOnly();

            BaseStats = new PlayerMain();
            BaseStats.Initialize(savingData);

            BodyPartAggregator = new ProgressionAggregator("BodyPart");
            EventGrantAggregator = new ProgressionAggregator("EventGrant");
            RuneAggregator = new ProgressionAggregator("Rune");
            JingYuanEssenceAggregator = new ProgressionAggregator("JingYuanEssence");

            ProgressionRoot = new ProgressionAggregator("Root");
            ProgressionRoot.AddChild(BaseStats.MainAggregator);
            ProgressionRoot.AddChild(GearManager.GearAggregator);
            ProgressionRoot.AddChild(TalentManager.TalentAggregator);
            ProgressionRoot.AddChild(BodyPartAggregator);
            ProgressionRoot.AddChild(EventGrantAggregator);
            ProgressionRoot.AddChild(RuneAggregator);
            ProgressionRoot.AddChild(JingYuanEssenceAggregator);

            ProgressionRoot.OnStatsChanged += (src) =>
            {
                RefreshPlayerBigMapAttr();
                var player = LogicManager?.playerLogicEntity;
                player?.RefreshProgressionYCAttrs();
            };
        }

        public void PostInit(PlayerSystemManager owner)
        {
            BindBodyPartSystem(owner?.BodyPartSystem);
            BindEventGrantSystem(owner?.EventGrantSystem);
            BindRuneSystem(owner?.RuneSystem);
            BindJingYuanEssenceSystem(owner?.JingYuanEssenceSystem);
        }

        void OnBossEncounterDefeated(PlayerBossEncounterDefeatedEvent evt)
        {
            DemonCult?.TryApplyAncientWorldProgress(
                evt.AncientWorldProgressKey,
                evt.AncientWorldProgressStageDelta);
        }

        // 养成侧技能贡献；优先级与历史被动装配一致：符文 > EventGrant
        public void CollectContributedSkills(HashSet<string> applied, List<(string skillId, int level)> output)
        {
            if (output == null)
            {
                return;
            }

            RuneAggregator?.CollectContributedSkills(applied, output);
            EventGrantAggregator?.CollectContributedSkills(applied, output);
            _boundJingYuanEssenceProvider?.CollectContributedSkills(applied, output);
        }

        public void CollectSkillModifiers(List<SkillModifierSpec> output)
        {
            if (output == null)
            {
                return;
            }

            ProgressionRoot?.CollectSkillModifiers(output);
        }

        public float ResolveSkillPhaseDuration(string skillId, string abilityId, string phaseName, float baseDuration)
        {
            var modifiers = new List<SkillModifierSpec>();
            CollectSkillModifiers(modifiers);
            return baseDuration * SkillModifierUtil.ResolvePhaseDurationMultiplier(
                modifiers,
                skillId,
                abilityId,
                phaseName);
        }

        void BindJingYuanEssenceSystem(PlayerJingYuanEssenceSystem essenceSystem)
        {
            if (JingYuanEssenceAggregator == null || essenceSystem == null)
            {
                return;
            }

            if (_boundJingYuanEssenceProvider != null)
            {
                JingYuanEssenceAggregator.RemoveChild(_boundJingYuanEssenceProvider);
            }

            _boundJingYuanEssenceProvider = essenceSystem;
            JingYuanEssenceAggregator.AddChild(_boundJingYuanEssenceProvider);
            ProgressionRoot.ForceDirty();
        }

        void BindRuneSystem(PlayerRuneSystem runeSystem)
        {
            if (RuneAggregator == null || runeSystem == null)
            {
                return;
            }

            if (_boundRuneProvider != null)
            {
                RuneAggregator.RemoveChild(_boundRuneProvider);
            }

            _boundRuneProvider = runeSystem.ProgressionProvider;
            RuneAggregator.AddChild(_boundRuneProvider);
            IsRuneBound = true;
        }


        void BindEventGrantSystem(PlayerEventGrantSystem eventGrantSystem)
        {
            if (EventGrantAggregator == null || eventGrantSystem == null)
            {
                return;
            }

            if (_boundEventGrantProvider != null)
            {
                EventGrantAggregator.RemoveChild(_boundEventGrantProvider);
            }

            _boundEventGrantProvider = eventGrantSystem.ProgressionProvider;
            EventGrantAggregator.AddChild(_boundEventGrantProvider);
            IsEventGrantBound = true;
        }

        void BindBodyPartSystem(PlayerBodyPartSystem bodyPartSystem)
        {
            if (BodyPartAggregator == null || bodyPartSystem == null)
            {
                return;
            }

            if (_boundBodyPartProvider != null)
            {
                BodyPartAggregator.RemoveChild(_boundBodyPartProvider);
            }

            _boundBodyPartProvider = bodyPartSystem.ProgressionProvider;
            BodyPartAggregator.AddChild(_boundBodyPartProvider);
            IsBodyPartBound = true;
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
            // ??????
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
            var gear = PartGearCatalog.GetOrDefault(itemId);
            if (gear == null)
            {
                return null;
            }

            var pairs = new List<StatPair>(2);
            if (gear.BaseArm != 0)
            {
                pairs.Add(new StatPair((int)EYCAttribute.InnerArm, gear.BaseArm));
            }

            if (gear.BaseHpMax != 0)
            {
                pairs.Add(new StatPair((int)EYCAttribute.HPMax, gear.BaseHpMax));
            }

            return pairs;
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
            if (nodeCfg == null || nodeCfg.MaxLevel <= 0)
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

            Debug.Log($"TryUpgradeNode {nodeId} to {next} ");

            _nodeLevels[nodeId] = next;
            RebuildProvidersFromLevels();

            if (!string.IsNullOrEmpty(levelRow.PassiveSkillId))
            {
                _logic.playerDataManager.TryGrantPassiveSkill(levelRow.PassiveSkillId, next);
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

            if (nodeCfg.MaxLevel <= 0)
            {
                failReason = "placeholder";
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
                    var prerequisiteCfg = CfgMgr.Cfgs?.TbTalentNode?.GetOrDefault(pre);
                    if (prerequisiteCfg == null || prerequisiteCfg.TalentTreeId != nodeCfg.TalentTreeId)
                    {
                        failReason = "cross_tree_prereq";
                        return false;
                    }

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
            var humanCivilizationBonuses = new Dictionary<EHumanCivilizationAttribute, long>();

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
                var skillModifiers = new List<SkillModifierSpec>();
                for (int lv = 1; lv <= current; lv++)
                {
                    var row = levelTable.Get(nodeId, lv);
                    if (row == null)
                    {
                        continue;
                    }

                    if (row.StatBonuses != null)
                    {
                        foreach (var b in row.StatBonuses)
                        {
                            if (b == null)
                            {
                                continue;
                            }

                            if (b.HumanAttrId != EHumanCivilizationAttribute.None)
                            {
                                humanCivilizationBonuses.TryGetValue(b.HumanAttrId, out var currentBonus);
                                humanCivilizationBonuses[b.HumanAttrId] = currentBonus + b.Val;
                            }
                            else if (b.AttrId != 0)
                            {
                                pairs.Add(new StatPair(b.AttrId, b.Val));
                            }
                        }
                    }

                    if (row.SkillModifiers != null)
                    {
                        foreach (var modifier in row.SkillModifiers)
                        {
                            if (modifier == null || string.IsNullOrEmpty(modifier.SkillId) || modifier.Value <= 0f)
                            {
                                continue;
                            }

                            if (!Enum.TryParse<ESkillModifierType>(modifier.ModifierType, true, out var modifierType))
                            {
                                continue;
                            }

                            skillModifiers.Add(new SkillModifierSpec
                            {
                                SkillId = modifier.SkillId,
                                AbilityId = modifier.AbilityId,
                                PhaseName = modifier.PhaseName,
                                ModifierType = modifierType,
                                Value = modifier.Value,
                                SourceId = $"talent:{nodeId}:{lv}",
                            });
                        }
                    }
                }

                if (pairs.Count == 0 && skillModifiers.Count == 0)
                {
                    continue;
                }

                var provider = new TalentNodeProgressionProvider(pairs, skillModifiers);
                TalentAggregator.AddChild(provider);
                _activeProviders.Add(provider);
                TalentNodeDict[nodeId] = new PlayerTalentNode
                {
                    NodeId = nodeId,
                    Provider = provider,
                };
            }

            TalentAggregator.ForceDirty();
            _logic?.playerDataManager?.ProgressionSystem?.HumanCivilization?.SetTalentBonuses(humanCivilizationBonuses);
        }
    }

    public class PlayerTalentNode
    {
        public int NodeId;
        public TalentNodeProgressionProvider Provider;
    }
}
