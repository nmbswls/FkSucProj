using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Logic
{
    [Serializable]
    public class SerializableDict<TKey, TValue>
    {
        public List<TKey> keys = new List<TKey>();
        public List<TValue> values = new List<TValue>();
        public void Add(TKey k, TValue v) { keys.Add(k); values.Add(v); }
        public bool TryGetValue(TKey k, out TValue v)
        {
            int idx = keys.IndexOf(k);
            if (idx >= 0) { v = values[idx]; return true; }
            v = default; return false;
        }
    }

}
