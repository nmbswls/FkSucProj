using System.Collections.Generic;
using System;

namespace Map.Logic.Events
{
    public interface IMapLogicEvent { MapLogicEventContext Ctx { get; set; } }

    public struct MapLogicEventContext
    {
        public int FrameId;
        public Guid CorrelationId;
        public long SourceId;
        public bool Reliable;
    }

    //public enum Delivery { Immediate, QueuedFrame, Deferred }

    public interface IMapLogicEventHandler<T> where T : IMapLogicEvent
    {
        void Handle(in T evt);
    }

    public sealed class MapLogicSubscription
    {
        public readonly Type EventType;
        public readonly int Priority;
        public readonly Delegate Handler;
        public MapLogicSubscription(Type type, int priority, Delegate handler)
        {
            EventType = type; Priority = priority; Handler = handler;
        }
    }

    public sealed class MapLogicEventBus
    {
        private readonly Dictionary<Type, List<MapLogicSubscription>> subs = new();
        private readonly Queue<object> frameQueue = new();
        private readonly List<(object evt, int framesLeft)> deferred = new();

        public MapLogicSubscription Subscribe<T>(IMapLogicEventHandler<T> handler, int priority = 0) where T : struct, IMapLogicEvent
        {
            Action<T> del = e => handler.Handle(e); //   ≈‰ in
            var sub = new MapLogicSubscription(typeof(T), priority, del);
            if (!subs.TryGetValue(typeof(T), out var list)) { list = new List<MapLogicSubscription>(); subs[typeof(T)] = list; }
            list.Add(sub);
            list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            return sub;
        }

        public void Unsubscribe(MapLogicSubscription s)
        {
            if (subs.TryGetValue(s.EventType, out var list)) list.Remove(s);
        }

        public void Publish<T>(in T evt) where T : struct, IMapLogicEvent
        {
            Dispatch(evt);
        }

        private void Dispatch<T>(in T evt) where T : struct, IMapLogicEvent
        {
            if (!subs.TryGetValue(typeof(T), out var list)) return;
            foreach (var s in list)
            {
                if (s.Handler is Action<T> a) a(evt);
            }
        }
    }
}