using System;
using System.Collections.Generic;
using Map.Entity;
using My.Map;

namespace My.Map.Entity
{
    // RelativeExposeLevel: ParamStr1=目标属性, ParamFloat1=线性每级加成,
    // ParamStr2=非线性映射(如 "0:100,1:200,2:500"), CommonFlag1=暴露真身时是否生效
    internal sealed class BuffDurationRelativeExposeLevelInstance : BuffDurationInstanceBase
    {
        readonly string _dstAttrId;
        readonly float _linearPerLevel;
        readonly Dictionary<int, long> _levelValueMap;
        readonly bool _effectiveWhenExposed;

        Modifier _linkedMod;
        int _lastExposeLevel = -1;
        bool _lastIsExposed;
        bool _lastGateOpen;

        public BuffDurationRelativeExposeLevelInstance(BuffDurationEffet cfg)
        {
            _dstAttrId = cfg.ParamStr1;
            _linearPerLevel = cfg.ParamFloat1;
            _effectiveWhenExposed = cfg.CommonFlag1;
            _levelValueMap = ParseLevelValueMap(cfg.ParamStr2);
        }

        public override void OnBuffInfoChanged(BuffInstance inst)
        {
            RefreshModifier(inst, force: true);
        }

        public override void OnDetached(BuffInstance inst)
        {
            if (_linkedMod != null && inst.BuffOwner != null)
            {
                inst.BuffOwner.ExpireModifierBySource(_linkedMod.source);
            }

            _linkedMod = null;
            _lastExposeLevel = -1;
            _lastIsExposed = false;
            _lastGateOpen = false;
        }

        public override void OnTick(BuffInstance inst, float dt)
        {
            RefreshModifier(inst, force: false);
        }

        void RefreshModifier(BuffInstance inst, bool force)
        {
            if (!IsConfigValid(inst))
            {
                return;
            }

            int exposeLevel = ResolveExposeLevel(inst.BuffOwner);
            bool isExposed = ResolveIsExposed(inst.BuffOwner);
            bool gateOpen = ShouldApply(isExposed);

            if (!force
                && exposeLevel == _lastExposeLevel
                && isExposed == _lastIsExposed
                && gateOpen == _lastGateOpen
                && _linkedMod != null)
            {
                return;
            }

            _lastExposeLevel = exposeLevel;
            _lastIsExposed = isExposed;
            _lastGateOpen = gateOpen;

            long modVal = gateOpen
                ? ComputeModValue(exposeLevel, inst.GetModifierScaleLayer())
                : 0;
            ApplyMod(inst, inst.BuffOwner, modVal);
        }

        bool IsConfigValid(BuffInstance inst)
        {
            if (string.IsNullOrEmpty(_dstAttrId))
            {
                return false;
            }

            return inst.BuffOwner != null;
        }

        bool ShouldApply(bool isExposed)
        {
            if (!isExposed)
            {
                return true;
            }

            return _effectiveWhenExposed;
        }

        static int ResolveExposeLevel(IEntityBuffOwner owner)
        {
            if (owner is not PlayerLogicEntity player || player.LogicManager == null)
            {
                return 0;
            }

            return PlayerGamePlayRule.CalculateClothesExposeLevel(player.LogicManager);
        }

        static bool ResolveIsExposed(IEntityBuffOwner owner)
        {
            return owner is PlayerLogicEntity player && player.IsExposed;
        }

        long ComputeModValue(int exposeLevel, int layer)
        {
            long baseVal;
            if (_levelValueMap.Count > 0)
            {
                if (!_levelValueMap.TryGetValue(exposeLevel, out baseVal))
                {
                    baseVal = 0;
                }
            }
            else
            {
                baseVal = (long)(exposeLevel * _linearPerLevel);
            }

            if (layer != 1)
            {
                baseVal *= layer;
            }

            return baseVal;
        }

        static ModSourceKey MakeSourceKey(BuffInstance inst)
        {
            return new ModSourceKey()
            {
                entityId = inst.CasterId,
                buffId = inst.InstanceId,
                abilityName = BuffDurationInstanceFactory.RelativeExposeLevelModAbilitySlot,
            };
        }

        void ApplyMod(BuffInstance inst, IEntityBuffOwner owner, long modVal)
        {
            if (_linkedMod == null)
            {
                if (modVal == 0)
                {
                    return;
                }

                _linkedMod = owner.AddAttrModifier(MakeSourceKey(inst), _dstAttrId, modVal);
                return;
            }

            if (_linkedMod.value == modVal)
            {
                return;
            }

            if (modVal == 0)
            {
                owner.ExpireModifierBySource(_linkedMod.source);
                _linkedMod = null;
                return;
            }

            _linkedMod.value = modVal;
            owner.UpdateAttrModifier(_linkedMod);
        }

        static Dictionary<int, long> ParseLevelValueMap(string raw)
        {
            var map = new Dictionary<int, long>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return map;
            }

            var entries = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                var pair = entry.Split(new[] { ':', '=' }, 2);
                if (pair.Length != 2)
                {
                    continue;
                }

                if (int.TryParse(pair[0].Trim(), out var level)
                    && long.TryParse(pair[1].Trim(), out var val))
                {
                    map[level] = val;
                }
            }

            return map;
        }
    }
}
