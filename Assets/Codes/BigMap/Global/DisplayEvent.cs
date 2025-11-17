//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace Global.Display.Event
//{
//    public interface IDisplayEvent { DisplayContext Ctx { get; set; } }
//    public struct DisplayContext
//    {
//        public int FrameId;
//        public Guid CorrelationId;
//        public bool Reliable;
//        public DisplayPhase Phase;
//    }
//    public enum DisplayPhase { Immediate, Update, Fixed, EndOfFrame }

//    public interface IDisplayHandler<T> where T : struct, IDisplayEvent
//    {
//        void Handle(in T evt);
//    }

//    // 订阅句柄
//    public sealed class DisplaySubscription
//    {
//        public readonly Type EventType;
//        public readonly object Handler; // 存接口实例即可
//        public DisplaySubscription(Type type, object handler)
//        {
//            EventType = type; Handler = handler;
//        }
//    }

//    public sealed class DisplayBus
//    {
//        private readonly Dictionary<Type, List<object>> handlers = new();
//        private readonly Queue<object> qUpdate = new();
//        private readonly Queue<object> qFixed = new();
//        private readonly Queue<object> qEOF = new();

//        // 返回可用于解绑的句柄
//        public DisplaySubscription Subscribe<T>(IDisplayHandler<T> handler) where T : struct, IDisplayEvent
//        {
//            var t = typeof(T);
//            if (!handlers.TryGetValue(t, out var list))
//            {
//                list = new List<object>();
//                handlers[t] = list;
//            }
//            list.Add(handler);
//            return new DisplaySubscription(t, handler);
//        }

//        public void Unsubscribe(DisplaySubscription sub)
//        {
//            if (sub == null) return;
//            if (handlers.TryGetValue(sub.EventType, out var list))
//            {
//                list.Remove(sub.Handler);
//                if (list.Count == 0) handlers.Remove(sub.EventType);
//            }
//        }

//        public void Publish<T>(in T evt) where T : struct, IDisplayEvent
//        {
//            switch (evt.Ctx.Phase)
//            {
//                case DisplayPhase.Immediate: Dispatch(evt); break;
//                case DisplayPhase.Update: qUpdate.Enqueue(evt); break;
//                case DisplayPhase.Fixed: qFixed.Enqueue(evt); break;
//                case DisplayPhase.EndOfFrame: qEOF.Enqueue(evt); break;
//            }
//        }

//        private void Dispatch<T>(in T evt) where T : struct, IDisplayEvent
//        {
//            if (!handlers.TryGetValue(typeof(T), out var list)) return;
//            // 强类型调用，无反射
//            foreach (var h in list)
//            {
//                if (h is IDisplayHandler<T> dh) dh.Handle(evt);
//            }
//        }

//        public void PumpUpdate() { while (qUpdate.Count > 0) DispatchDynamic(qUpdate.Dequeue()); }
//        public void PumpFixed() { while (qFixed.Count > 0) DispatchDynamic(qFixed.Dequeue()); }
//        public void PumpEndOfFrame() { while (qEOF.Count > 0) DispatchDynamic(qEOF.Dequeue()); }

//        // 动态路径用于不同具体事件类型，仍避免反射查找方法，走接口调用
//        private void DispatchDynamic(object evt)
//        {
//            var t = evt.GetType();
//            if (!handlers.TryGetValue(t, out var list)) return;

//            // 使用运行时泛型辅助：通过静态字典路由（可由生成器生成），这里用反射兜底
//            foreach (var h in list)
//            {
//                var iface = typeof(IDisplayHandler<>).MakeGenericType(t);
//                if (iface.IsInstanceOfType(h))
//                {
//                    var mi = iface.GetMethod("Handle");
//                    mi?.Invoke(h, new[] { evt });
//                }
//            }
//        }
//    }

//}