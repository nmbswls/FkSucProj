using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using UnityEngine;
using My.Map;

namespace Map.Logic.Events
{

    #region Common Event

    public partial struct MLECommonGameEvent : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public string Name;
        public int Param1;
        public int Param2;
        public long Param3;
        public long Param4;
        public float Param5;
        public float Param6;
    }

    public interface ICommonGameEventHandler : IMapLogicEventHandler<MLECommonGameEvent> { }

    public sealed class CommonGameEventAdapter : IMapLogicEventHandler<MLECommonGameEvent>
    {
        private readonly Action<MLECommonGameEvent> _fn;
        public CommonGameEventAdapter(Action<MLECommonGameEvent> fn) { _fn = fn; }
        public void Handle(in MLECommonGameEvent evt) => _fn(evt);
    }


    #endregion

    #region buff

    public partial struct MLEApplyBuff : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public long CasterId;
        public long TargetId;
        public string BuffId;
        public int Layer;
    }

    public interface IApplyBuffHandler : IMapLogicEventHandler<MLEApplyBuff> { }

    public sealed class ApplyBuffAdapter : IMapLogicEventHandler<MLEApplyBuff>
    {
        private readonly Action<MLEApplyBuff> _fn;
        public ApplyBuffAdapter(Action<MLEApplyBuff> fn) { _fn = fn; }
        public void Handle(in MLEApplyBuff evt) => _fn(evt);
    }


    #endregion

    #region hit

    public partial struct MLEUnitOnHit : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public SourceKey SrcKey;
        public long OnHitId;
        public long Damage;
        public int Flags;
    }

    public interface IUnitOnHitHandler : IMapLogicEventHandler<MLEUnitOnHit> { }

    public sealed class MLEUnitOnHitAdapter : IMapLogicEventHandler<MLEUnitOnHit>
    {
        private readonly Action<MLEUnitOnHit> _fn;
        public MLEUnitOnHitAdapter(Action<MLEUnitOnHit> fn) { _fn = fn; }
        public void Handle(in MLEUnitOnHit evt) => _fn(evt);
    }


    #endregion


    public partial struct MLESkillCastStarted : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public int CasterId;
        public int SkillId;
        public Vector2 TargetPos;
    }


    public static class SkillCastStartedExtensions
    {
        public static void Publish(this MapLogicEventBus bus, int casterId, int skillId, Vector2 targetPos)
        {
            var evt = new MLESkillCastStarted
            {
                Ctx = new MapLogicEventContext { CorrelationId = Guid.NewGuid(), Reliable = true },
                CasterId = casterId,
                SkillId = skillId,
                TargetPos = targetPos
            };
            bus.Publish(evt);
        }
    }

    public interface ISkillCastStartedHandler : IMapLogicEventHandler<MLESkillCastStarted> { }

    public sealed class SkillCastStartedAdapter : IMapLogicEventHandler<MLESkillCastStarted>
    {
        private readonly Action<MLESkillCastStarted> _fn;
        public SkillCastStartedAdapter(Action<MLESkillCastStarted> fn) { _fn = fn; }
        public void Handle(in MLESkillCastStarted evt) => _fn(evt);
    }

    public static class SkillCastStartedSubscribe
    {
        public static MapLogicSubscription OnSkillCastStarted(this MapLogicEventBus bus, Action<MLESkillCastStarted> fn, int priority = 0)
        {
            return bus.Subscribe(new SkillCastStartedAdapter(fn), priority);
        }
    }

}