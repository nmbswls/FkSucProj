using My.Map;
using My.Map.Entity;

namespace My.Map.Fight
{
    public interface IFightAttrProvider
    {
        long SourceEntityId { get; }
        bool TryGetAttr(string attrId, out long value);
        bool TryGetUnitLevel(out int level);
        bool TryGetWorldPos(out UnityEngine.Vector2 pos);
    }

    public sealed class LiveEntityFightAttrProvider : IFightAttrProvider
    {
        readonly ILogicEntity _entity;

        public LiveEntityFightAttrProvider(ILogicEntity entity)
        {
            _entity = entity;
        }

        public long SourceEntityId => _entity?.Id ?? 0;

        public bool TryGetAttr(string attrId, out long value)
        {
            value = 0;
            if (_entity == null)
            {
                return false;
            }

            value = _entity.GetAttr(attrId);
            return true;
        }

        public bool TryGetUnitLevel(out int level)
        {
            level = 0;
            if (_entity is BaseUnitLogicEntity unit)
            {
                level = unit.GetUnitLevel();
                return true;
            }

            return false;
        }

        public bool TryGetWorldPos(out UnityEngine.Vector2 pos)
        {
            pos = default;
            if (_entity == null)
            {
                return false;
            }

            pos = _entity.Pos;
            return true;
        }
    }

    public sealed class CtxFightAttrProvider : IFightAttrProvider
    {
        readonly GameLogicManager.LogicFightEffectContext _ctx;
        ILogicEntity _resolvedSrc;

        public CtxFightAttrProvider(GameLogicManager.LogicFightEffectContext ctx)
        {
            _ctx = ctx;
            if (ctx?.SourceInfo != null && ctx.SourceInfo.SrcEntityId != 0)
            {
                _resolvedSrc = ctx.Env?.GetLogicEntity(ctx.SourceInfo.SrcEntityId, false);
            }
        }

        public long SourceEntityId => _ctx?.SourceInfo?.SrcEntityId ?? 0;

        public bool TryGetAttr(string attrId, out long value)
        {
            value = 0;
            if (_ctx?.CacheAttrVal != null && _ctx.CacheAttrVal.TryGetValue(attrId, out value))
            {
                return true;
            }

            if (_resolvedSrc != null)
            {
                value = _resolvedSrc.GetAttr(attrId);
                return true;
            }

            return false;
        }

        public bool TryGetUnitLevel(out int level)
        {
            level = 0;
            if (_resolvedSrc is BaseUnitLogicEntity unit)
            {
                level = unit.GetUnitLevel();
                return true;
            }

            return false;
        }

        public bool TryGetWorldPos(out UnityEngine.Vector2 pos)
        {
            pos = default;
            if (_resolvedSrc != null)
            {
                pos = _resolvedSrc.Pos;
                return true;
            }

            if (_ctx?.TriggerPos != null)
            {
                pos = _ctx.TriggerPos.Value;
                return true;
            }

            return false;
        }
    }

    public sealed class IntentExtraAttrsFightAttrProvider : IFightAttrProvider
    {
        readonly ResourceDeltaIntent _intent;

        public IntentExtraAttrsFightAttrProvider(ResourceDeltaIntent intent, long sourceEntityId)
        {
            _intent = intent;
            SourceEntityId = sourceEntityId;
        }

        public long SourceEntityId { get; }

        public bool TryGetAttr(string attrId, out long value)
        {
            value = 0;
            if (_intent?.extraAttrs == null)
            {
                return false;
            }

            return _intent.extraAttrs.TryGetValue(attrId, out value);
        }

        public bool TryGetUnitLevel(out int level)
        {
            level = 0;
            if (_intent?.extraAttrs == null)
            {
                return false;
            }

            if (_intent.extraAttrs.TryGetValue(AttrIdConsts.SrcLevel_Pipeline, out var lv))
            {
                level = (int)lv;
                return true;
            }

            return false;
        }

        public bool TryGetWorldPos(out UnityEngine.Vector2 pos)
        {
            pos = default;
            if (_intent?.srcPos != null)
            {
                pos = _intent.srcPos.Value;
                return true;
            }

            return false;
        }
    }
}
