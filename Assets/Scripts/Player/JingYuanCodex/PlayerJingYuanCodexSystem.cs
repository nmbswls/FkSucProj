using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map.Entity;
using My.Quest;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    public enum EJingYuanProgressSource
    {
        Blurt,
        ItemGrant,
    }

    public sealed class JingYuanCodexRuntimeState
    {
        public string CodexId;
        public int ExtractCount;
        public long TotalAmount;
        public int Level;
    }

    public sealed class PlayerJingYuanCodexSystem : IPlayerSystem
    {
        const string LogTag = "[PlayerJingYuanCodexSystem]";
        const int BaseTuneSlotCount = 1;

        // 吸收射精时转化为调精浓度的比例（其余逻辑仍走精元/精浴）
        const float BlurtToConcentrationRate = 0.2f;

        readonly PlayerSystemManager _owner;
        readonly Dictionary<string, JingYuanCodexRuntimeState> _progress = new(StringComparer.Ordinal);
        readonly Dictionary<int, string> _equippedTunes = new();
        readonly JingYuanCodexProgressionProvider _progressionProvider;

        GameLogicManager _logic;
        long _concentration;
        bool _suppressEntityConcentrationSync;
        bool _eventsBound;
        float _concentrationDrainAcc;

        public JingYuanCodexProgressionProvider ProgressionProvider => _progressionProvider;

        public PlayerJingYuanCodexSystem(PlayerSystemManager owner)
        {
            _owner = owner;
            _progressionProvider = new JingYuanCodexProgressionProvider(this);
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            _logic = ctx;
            _progress.Clear();
            _equippedTunes.Clear();
            _concentration = savingData?.PlayerData?.TiaoJingConcentration ?? 0;

            JingYuanCodexCatalog.RebuildTagIndex();
            BindEvents();

            if (savingData?.PlayerData?.JingYuanCodexProgress != null)
            {
                foreach (var entry in savingData.PlayerData.JingYuanCodexProgress)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.CodexId))
                    {
                        continue;
                    }

                    var state = GetOrCreateState(entry.CodexId);
                    state.ExtractCount = Math.Max(0, entry.ExtractCount);
                    state.TotalAmount = Math.Max(0, entry.TotalAmount);
                    state.Level = JingYuanCodexCatalog.ResolveLevel(entry.CodexId, state.ExtractCount, state.TotalAmount);
                }
            }

            if (savingData?.PlayerData?.EquippedJingYuanTunes != null)
            {
                foreach (var entry in savingData.PlayerData.EquippedJingYuanTunes)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.CodexId))
                    {
                        continue;
                    }

                    _equippedTunes[entry.Slot] = entry.CodexId;
                }
            }
        }

        public void PostInit(PlayerSystemManager owner)
        {
        }

        public void Tick(float dt)
        {
            if (dt <= 0f)
            {
                return;
            }

            _concentrationDrainAcc += dt;
            if (_concentrationDrainAcc < 1f)
            {
                return;
            }

            var interval = _concentrationDrainAcc;
            _concentrationDrainAcc = 0f;
            TickConcentrationDrain(interval);
        }

        void BindEvents()
        {
            if (_eventsBound)
            {
                return;
            }

            PlayerEventBus.Subscribe<PlayerJingYuanBlurtAbsorbedEvent>(OnBlurtAbsorbedEvent);
            PlayerEventBus.Subscribe<PlayerResourceChangedEvent>(OnPlayerResourceChanged);
            PlayerEventBus.Subscribe<PlayerEntityReadyEvent>(OnPlayerEntityReadyEvent);
            _eventsBound = true;
        }

        void OnPlayerEntityReadyEvent(PlayerEntityReadyEvent _)
        {
            SyncConcentrationToPlayerEntity(force: true);
        }

        void OnBlurtAbsorbedEvent(PlayerJingYuanBlurtAbsorbedEvent ev)
        {
            HandleBlurtAbsorbed(ev.JingyuanTag, ev.SjAmount);
        }

        void OnPlayerResourceChanged(PlayerResourceChangedEvent ev)
        {
            if (ev.AttrId == AttrIdConsts.PlayerTiaoJingConcentration)
            {
                OnEntityConcentrationChanged(ev.After);
            }
        }

        public void WriteToSave(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            pd.TiaoJingConcentration = _concentration;

            pd.JingYuanCodexProgress ??= new List<JingYuanCodexProgressPersist>();
            pd.JingYuanCodexProgress.Clear();
            foreach (var kv in _progress)
            {
                var state = kv.Value;
                if (state == null || string.IsNullOrEmpty(state.CodexId))
                {
                    continue;
                }

                if (state.ExtractCount <= 0 && state.TotalAmount <= 0)
                {
                    continue;
                }

                pd.JingYuanCodexProgress.Add(new JingYuanCodexProgressPersist
                {
                    CodexId = state.CodexId,
                    ExtractCount = state.ExtractCount,
                    TotalAmount = state.TotalAmount,
                });
            }

            pd.EquippedJingYuanTunes ??= new List<JingYuanTuneEquipPersist>();
            pd.EquippedJingYuanTunes.Clear();
            foreach (var kv in _equippedTunes)
            {
                pd.EquippedJingYuanTunes.Add(new JingYuanTuneEquipPersist
                {
                    Slot = kv.Key,
                    CodexId = kv.Value,
                });
            }
        }

        public long Concentration => _concentration;

        public int GetSlotCap()
        {
            var extra = _owner?.ProgressionSystem?.GetFinalAttribute((int)EYCAttribute.TiaoJingSlotCap) ?? 0;
            return BaseTuneSlotCount + (int)extra;
        }

        public bool TryGetProgress(string codexId, out JingYuanCodexRuntimeState state)
        {
            state = null;
            if (string.IsNullOrEmpty(codexId))
            {
                return false;
            }

            return _progress.TryGetValue(codexId, out state);
        }

        public void HandleBlurtAbsorbed(string jingyuanTag, float sjAmount)
        {
            if (string.IsNullOrEmpty(jingyuanTag) || sjAmount <= 0f)
            {
                return;
            }

            var defs = JingYuanCodexCatalog.GetDefsByTag(jingyuanTag);
            if (defs.Count == 0)
            {
                return;
            }

            var amountDelta = (long)Mathf.Max(0f, sjAmount * 1000f);
            foreach (var def in defs)
            {
                if (def == null)
                {
                    continue;
                }

                AddProgress(def.CodexId, 1, amountDelta, EJingYuanProgressSource.Blurt);
            }

            AddConcentrationFromBlurtAbsorb(sjAmount);
        }

        public void AddConcentrationFromBlurtAbsorb(float sjAmount)
        {
            if (sjAmount <= 0f)
            {
                return;
            }

            var delta = (long)(sjAmount * BlurtToConcentrationRate * 1000f);
            if (delta <= 0)
            {
                return;
            }

            ApplyConcentrationDelta(delta);
        }

        public bool AddProgress(string codexId, int extractDelta, long amountDelta, EJingYuanProgressSource source)
        {
            if (JingYuanCodexCatalog.GetDef(codexId) == null)
            {
                Debug.LogWarning($"{LogTag} AddProgress failed: invalid codex '{codexId}'.");
                return false;
            }

            if (extractDelta <= 0 && amountDelta <= 0)
            {
                return false;
            }

            var state = GetOrCreateState(codexId);
            var oldLevel = state.Level;
            if (extractDelta > 0)
            {
                state.ExtractCount += extractDelta;
            }

            if (amountDelta > 0)
            {
                state.TotalAmount += amountDelta;
            }

            state.Level = JingYuanCodexCatalog.ResolveLevel(codexId, state.ExtractCount, state.TotalAmount);

            PlayerEventBus.Publish(new PlayerJingYuanCodexProgressEvent
            {
                CodexId = codexId,
                ExtractCount = state.ExtractCount,
                TotalAmount = state.TotalAmount,
                Level = state.Level,
                Source = source,
            });

            if (state.Level > oldLevel)
            {
                PlayerEventBus.Publish(new PlayerJingYuanCodexLevelUpEvent
                {
                    CodexId = codexId,
                    OldLevel = oldLevel,
                    NewLevel = state.Level,
                });
                _progressionProvider.NotifyChanged();
                _owner?.ProgressionSystem?.ProgressionRoot?.ForceDirty();
            }

            return true;
        }

        public bool CanEquipTune(int slot, string codexId, out string failReason)
        {
            failReason = null;
            if (slot < 0 || slot >= GetSlotCap())
            {
                failReason = "invalid_slot";
                return false;
            }

            if (string.IsNullOrEmpty(codexId))
            {
                failReason = "invalid_codex";
                return false;
            }

            if (JingYuanCodexCatalog.GetDef(codexId) == null)
            {
                failReason = "invalid_codex";
                return false;
            }

            if (!_progress.TryGetValue(codexId, out var state) || state.Level <= 0)
            {
                failReason = "codex_not_unlocked";
                return false;
            }

            foreach (var kv in _equippedTunes)
            {
                if (kv.Key != slot && kv.Value == codexId)
                {
                    failReason = "already_equipped";
                    return false;
                }
            }

            return true;
        }

        public bool TryEquipTune(int slot, string codexId)
        {
            if (!CanEquipTune(slot, codexId, out var failReason))
            {
                Debug.LogWarning($"{LogTag} TryEquipTune failed: {failReason}, slot={slot}, codex={codexId}");
                return false;
            }

            _equippedTunes[slot] = codexId;
            PlayerEventBus.Publish(new PlayerJingYuanTuneEquipChangedEvent { Slot = slot, CodexId = codexId });
            ResyncTunePassives();
            return true;
        }

        public bool TryUnequipTune(int slot)
        {
            if (!_equippedTunes.Remove(slot))
            {
                return false;
            }

            PlayerEventBus.Publish(new PlayerJingYuanTuneEquipChangedEvent { Slot = slot, CodexId = null });
            ResyncTunePassives();
            return true;
        }

        public void OnEntityConcentrationChanged(long after)
        {
            if (_suppressEntityConcentrationSync)
            {
                return;
            }

            var before = _concentration;
            _concentration = Math.Max(0, after);
            if (_concentration != before)
            {
                ResyncTunePassives();
            }
        }

        public void TickConcentrationDrain(float intervalSec)
        {
            if (_equippedTunes.Count <= 0 || intervalSec <= 0f)
            {
                return;
            }

            if (_concentration <= 0)
            {
                return;
            }

            int level = JingYuanCodexCatalog.GetConcentrationLevel(_concentration);
            var costPerSec = JingYuanCodexCatalog.GetConcentrationCostPerSec(level);
            if (costPerSec <= 0f)
            {
                return;
            }

            var delta = -(long)(costPerSec * intervalSec * 1000f);
            ApplyConcentrationDelta(delta, syncEntity: true);
        }

        public void CollectEquippedTunePassiveSkills(HashSet<string> applied, List<(string skillId, int level)> output)
        {
            if (output == null || _equippedTunes.Count == 0)
            {
                return;
            }

            foreach (var codexId in _equippedTunes.Values)
            {
                if (string.IsNullOrEmpty(codexId))
                {
                    continue;
                }

                if (!_progress.TryGetValue(codexId, out var state))
                {
                    continue;
                }

                var tier = JingYuanCodexCatalog.ResolveTuneTier(codexId, _concentration, state.Level);
                if (tier == null || string.IsNullOrEmpty(tier.PassiveSkillId) || tier.PassiveLevel <= 0)
                {
                    continue;
                }

                if (applied != null && applied.Contains(tier.PassiveSkillId))
                {
                    continue;
                }

                output.Add((tier.PassiveSkillId, tier.PassiveLevel));
            }
        }

        public void AccumulateProgressionStats(StatMap targetMap)
        {
            if (targetMap == null)
            {
                return;
            }

            foreach (var kv in _progress)
            {
                var state = kv.Value;
                if (state == null || state.Level <= 0)
                {
                    continue;
                }

                JingYuanCodexCatalog.SumStatBonusesUpToLevel(state.CodexId, state.Level, targetMap);
            }
        }

        public void ResyncTunePassives()
        {
            _owner?.SyncLearnedSkillsToPlayerEntity();
        }

        JingYuanCodexRuntimeState GetOrCreateState(string codexId)
        {
            if (!_progress.TryGetValue(codexId, out var state))
            {
                state = new JingYuanCodexRuntimeState { CodexId = codexId };
                _progress[codexId] = state;
            }

            return state;
        }

        void ApplyConcentrationDelta(long delta, bool syncEntity = true)
        {
            if (delta == 0)
            {
                return;
            }

            var before = _concentration;
            _concentration = Math.Max(0, _concentration + delta);
            if (syncEntity)
            {
                SyncConcentrationToPlayerEntity(force: true);
            }

            if (_concentration != before)
            {
                OnConcentrationChanged(before, _concentration);
            }
        }

        void OnConcentrationChanged(long before, long after)
        {
            ResyncTunePassives();
        }

        public void SyncConcentrationToPlayerEntity(bool force)
        {
            var player = _logic?.playerLogicEntity;
            if (player == null)
            {
                return;
            }

            var current = player.GetAttr(AttrIdConsts.PlayerTiaoJingConcentration);
            if (!force && current == _concentration)
            {
                return;
            }

            _suppressEntityConcentrationSync = true;
            player.ForceSetResource(AttrIdConsts.PlayerTiaoJingConcentration, _concentration);
            _suppressEntityConcentrationSync = false;
        }
    }
}
