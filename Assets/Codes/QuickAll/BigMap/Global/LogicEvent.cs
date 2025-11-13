using System.Collections.Generic;
using System;
using UnityEngine;
using My.Map;

namespace Map.Logic.Events
{
    public interface IMapLogicEvent 
    {
        EMapLogicEventType Type { get; }
        MapLogicEventContext Ctx { get;  }
    }

    public enum EMapLogicEventType
    { 
        Invalid,
        Common,
        AddBuff,
        OnHit,
    }


    public struct MapLogicEventContext
    {
        public int FrameId;
        public Guid CorrelationId;
        public ILogicEntity SourceEntity;
        public bool Reliable;
        public Vector2 HappenPos;
        public long TargetId;
    }

    //public enum Delivery { Immediate, QueuedFrame, Deferred }

    public interface IMapLogicEventHandler
    {

        void Handle(in IMapLogicEvent evt);
    }

    public class MapLogicEventAdapter : IMapLogicEventHandler
    {
        private readonly Action<IMapLogicEvent> _fn;
        public MapLogicEventAdapter(Action<IMapLogicEvent> fn) { _fn = fn; }
        public void Handle(in IMapLogicEvent evt) => _fn(evt);
    }

    public sealed class MapLogicSubscription
    {
        public readonly EMapLogicEventType EventType;
        public readonly int Priority;
        public readonly IMapLogicEventHandler Handler;
        public MapLogicSubscription(EMapLogicEventType type, int priority, IMapLogicEventHandler handler)
        {
            EventType = type; Priority = priority; Handler = handler;
        }
    }

    public sealed class MapLogicEventBus
    {

        private readonly Dictionary<EMapLogicEventType, List<MapLogicSubscription>> subs = new();
        private readonly Queue<object> frameQueue = new();
        private readonly List<(object evt, int framesLeft)> deferred = new();

        public MapLogicSubscription Subscribe(EMapLogicEventType type, IMapLogicEventHandler handler, int priority = 0)
        {
            var sub = new MapLogicSubscription(type, priority, handler);
            if (!subs.TryGetValue(type, out var list)) { list = new List<MapLogicSubscription>(); subs[type] = list; }
            list.Add(sub);
            list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            return sub;
        }

        public void Unsubscribe(MapLogicSubscription s)
        {
            if (subs.TryGetValue(s.EventType, out var list)) list.Remove(s);
        }

        public void Publish(in IMapLogicEvent evt) 
        {
            Dispatch(evt);
        }

        private void Dispatch(in IMapLogicEvent evt)
        {
            if (!subs.TryGetValue(evt.Type, out var list)) return;
            foreach (var s in list)
            {
                s.Handler.Handle(evt);
            }
        }

    }
}