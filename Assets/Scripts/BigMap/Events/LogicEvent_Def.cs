using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using UnityEngine;
using My.Map;
using My.Map.Entity;

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

    public partial struct MLEPlayerKillEvent : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public EMapLogicEventType Type { get { return EMapLogicEventType.UnitDie; } }

    }


    public partial struct MLEUnitDie : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public EMapLogicEventType Type { get { return EMapLogicEventType.UnitDie; } }

        public long EntityId;
        public Vector2 Pos;
        public ResourceDeltaIntent? LastIntent;
    }

    public partial struct MLEUnitCantAlert : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public EMapLogicEventType Type { get { return EMapLogicEventType.UnitCantAlert; } }

        public long EntityId;
    }

    // NPC 失神（非致命击倒等）；玩家侧由 PlayerSystemManager 桥接
    public partial struct MLEUnitUnsensored : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public EMapLogicEventType Type { get { return EMapLogicEventType.UnitUnsensored; } }

        public long EntityId;
        public string NpcCfgId;
        public string RaceId;
        public long SrcEntityId;
    }
    


    public partial struct MLEObjWithOwnerDestroyedEvent : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public EMapLogicEventType Type { get { return EMapLogicEventType.ObjWithOwnerDestroyed; } }

        public long EntityId;
        public string ObjCfgId;
        public Vector2 Pos;
    }

    public partial struct MLECostPendingAlertEvent : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public EMapLogicEventType Type { get { return EMapLogicEventType.CostPendingAlert; } }

        public long Value;
    }

    public partial struct MLEPlayerFaQingStatusChangeEvent : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public EMapLogicEventType Type { get { return EMapLogicEventType.PlayerFaQingStatusChange; } }
    }

    public partial struct MLEPlayerExposeStatusChangeEvent : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public EMapLogicEventType Type { get { return EMapLogicEventType.PlayerExposeStatusChange; } }
    }

    public partial struct MLERefreshGroupSwapEvent : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public EMapLogicEventType Type { get { return EMapLogicEventType.RefreshGroupSwap; } }

        public string GroupKey;
        public long OldEntityId;
        public long NewEntityId;
        public float MaxRetainSeconds;

        public bool IsBindNewEntity { get { return NewEntityId != 0; } }
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
        public ModSourceKey SrcKey;
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

    public partial struct MLEVariableChangeEvent : IMapLogicEvent
    {
        public MapLogicEventContext Ctx { get; set; }
        public EMapLogicEventType Type { get { return EMapLogicEventType.VariableChange; } }

        public string Name;
        public int BeforeVal;
        public int AfterVal;
    }
}
