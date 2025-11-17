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
        public EMapLogicEventType Type { get { return EMapLogicEventType.Common;} }

        public string Name;
        public int Param1;
        public int Param2;
        public long Param3;
        public long Param4;
        public string Param5;
        public string Param6;
    }

    public partial struct MLEAttractEvent : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public EMapLogicEventType Type { get { return EMapLogicEventType.Common; } }

        public Vector2 Pos;
        public IAttractSource AttractSource;
        public float Power;
    }
    

    #endregion

    #region buff

    public partial class MLEApplyBuff : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public EMapLogicEventType Type { get { return EMapLogicEventType.AddBuff; } }
        public long CasterId;
        public long TargetId;
        public string BuffId;
        public int Layer;
    }

    //public interface IApplyBuffHandler : IMapLogicEventHandler<MLEApplyBuff> { }

    //public sealed class ApplyBuffAdapter : IMapLogicEventHandler<MLEApplyBuff>
    //{
    //    private readonly Action<MLEApplyBuff> _fn;
    //    public ApplyBuffAdapter(Action<MLEApplyBuff> fn) { _fn = fn; }
    //    public void Handle(in MLEApplyBuff evt) => _fn(evt);
    //}


    #endregion

    #region hit

    public partial struct MLEUnitOnHit : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public EMapLogicEventType Type { get { return EMapLogicEventType.AddBuff; } }
        public SourceKey SrcKey;
        public long OnHitId;
        public long Damage;
        public int Flags;
    }

    //public interface IUnitOnHitHandler : IMapLogicEventHandler<MLEUnitOnHit> { }

    //public sealed class MLEUnitOnHitAdapter : IMapLogicEventHandler<MLEUnitOnHit>
    //{
    //    private readonly Action<MLEUnitOnHit> _fn;
    //    public MLEUnitOnHitAdapter(Action<MLEUnitOnHit> fn) { _fn = fn; }
    //    public void Handle(in MLEUnitOnHit evt) => _fn(evt);
    //}


    #endregion
}