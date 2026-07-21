using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map.Logic;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    public sealed class PlayerBodyPartSystem : IPlayerSystem
    {
        readonly PlayerSystemManager _owner;
        readonly Dictionary<EBodyPart, BodyPartRuntimeState> _parts = new();
        readonly BodyPartProgressionProvider _progressionProvider;

        public BodyPartProgressionProvider ProgressionProvider => _progressionProvider;

        public PlayerBodyPartSystem(PlayerSystemManager owner)
        {
            _owner = owner;
            _progressionProvider = new BodyPartProgressionProvider(this);
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            _parts.Clear();
            foreach (var def in BodyPartCatalog.GetAllPartsSorted())
            {
                if (def.PartId == EBodyPart.None)
                {
                    continue;
                }

                _parts[def.PartId] = new BodyPartRuntimeState
                {
                    PartId = def.PartId,
                    Level = 1,
                    Exp = 0,
                };
            }

            if (savingData?.PlayerData?.BodyParts != null)
            {
                foreach (var entry in savingData.PlayerData.BodyParts)
                {
                    if (entry == null || entry.PartId == (int)EBodyPart.None)
                    {
                        continue;
                    }

                    if (!Enum.IsDefined(typeof(EBodyPart), entry.PartId))
                    {
                        continue;
                    }

                    var partId = (EBodyPart)entry.PartId;
                    if (!_parts.TryGetValue(partId, out var state))
                    {
                        continue;
                    }

                    state.Exp = Math.Max(0, entry.Exp);
                    state.Level = Math.Max(1, entry.Level);
                    state.Level = BodyPartCatalog.ResolveLevelByExp(partId, state.Exp);
                    if (state.Level <= 0)
                    {
                        state.Level = 1;
                    }
                }
            }

            RebuildAllLocalStats();
        }

        public void PostInit(PlayerSystemManager owner)
        {
        }

        public void Tick(float dt)
        {
        }

        public void WriteToSave(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            pd.BodyParts ??= new List<BodyPartPersist>();
            pd.BodyParts.Clear();
            foreach (var kv in _parts)
            {
                var state = kv.Value;
                if (state == null || state.PartId == EBodyPart.None)
                {
                    continue;
                }

                pd.BodyParts.Add(new BodyPartPersist
                {
                    PartId = (int)state.PartId,
                    Level = state.Level,
                    Exp = state.Exp,
                });
            }
        }

        public BodyPartRuntimeState GetPartState(EBodyPart partId)
        {
            return partId != EBodyPart.None && _parts.TryGetValue(partId, out var state) ? state : null;
        }

        // 等级榨取率（百分点）+ 局部 FluidGain（万分比）供内射吸收使用
        public void GetExtractBonuses(EBodyPart partId, out long absorbRatePercent, out long fluidGainPerMyriad)
        {
            absorbRatePercent = 0;
            fluidGainPerMyriad = 0;
            var state = GetPartState(partId);
            if (state == null)
            {
                return;
            }

            absorbRatePercent = BodyPartCatalog.GetAbsorbRatePercent(partId, state.Level);
            fluidGainPerMyriad = GetLocalStat(partId, EPartLocalAttribute.FluidGain);
        }

        public long GetLocalStat(EBodyPart partId, EPartLocalAttribute attr)
        {
            var state = GetPartState(partId);
            if (state == null || attr == EPartLocalAttribute.None)
            {
                return 0;
            }

            return state.LocalStats.Get((int)attr);
        }

        public bool TryAddExp(EBodyPart partId, long delta, out int newLevel)
        {
            newLevel = 0;
            if (delta <= 0)
            {
                return false;
            }

            var state = GetPartState(partId);
            var def = BodyPartCatalog.GetPartDef(partId);
            if (state == null || def == null)
            {
                return false;
            }

            int oldLevel = state.Level;
            state.Exp += delta;
            state.Level = BodyPartCatalog.ResolveLevelByExp(partId, state.Exp);
            if (state.Level <= 0)
            {
                state.Level = 1;
            }

            state.Level = Mathf.Min(state.Level, def.MaxLevel);
            RebuildLocalStats(state);
            newLevel = state.Level;

            if (newLevel != oldLevel || delta > 0)
            {
                NotifyProgressionChanged();
            }

            return true;
        }

        public void AccumulateGlobalBonuses(StatMap targetMap)
        {
            foreach (var kv in _parts)
            {
                var state = kv.Value;
                if (state == null || state.Level <= 0)
                {
                    continue;
                }

                BodyPartCatalog.AccumulateGlobalBonuses(state.PartId, state.Level, targetMap);
            }
        }

        public long GetExpToNextLevel(EBodyPart partId)
        {
            var state = GetPartState(partId);
            var def = BodyPartCatalog.GetPartDef(partId);
            if (state == null || def == null)
            {
                return 0;
            }

            if (state.Level >= def.MaxLevel)
            {
                return 0;
            }

            long nextNeed = BodyPartCatalog.GetNeedExpForLevel(partId, state.Level + 1);
            return Math.Max(0, nextNeed - state.Exp);
        }

        public void RebuildAllLocalStats()
        {
            var equip = _owner?.EquipmentManager;
            foreach (var kv in _parts)
            {
                kv.Value?.RebuildLocalStats(equip);
            }
        }

        void RebuildLocalStats(BodyPartRuntimeState state)
        {
            state?.RebuildLocalStats(_owner?.EquipmentManager);
        }

        void NotifyProgressionChanged()
        {
            if (_owner?.ProgressionSystem?.IsBodyPartBound != true)
            {
                return;
            }

            _progressionProvider.NotifyChanged();
            _owner.ProgressionSystem.ProgressionRoot?.ForceDirty();
            _owner.EquipmentManager?.EnsureAllPartsBudget();

            var player = _owner.logicManager?.playerLogicEntity;
            player?.RefreshProgressionYCAttrs();
        }
    }
}
