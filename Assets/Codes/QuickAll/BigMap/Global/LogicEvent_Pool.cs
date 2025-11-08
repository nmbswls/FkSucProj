using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Map.Logic.Events
{

    public interface IPoolableMapLogicEvent : IMapLogicEvent { void Reset(); }
    public sealed class MapLogicEventPool<T> where T : struct, IPoolableMapLogicEvent
    {
        private readonly Stack<T> pool = new();
        public T Rent() => pool.Count > 0 ? pool.Pop() : default;
        public void Return(T e) { pool.Push(e); }
    }

}


