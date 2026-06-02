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

        public SerializableDict<TKey, TValue> Clone()
        {
            var clone = new SerializableDict<TKey, TValue>();
            clone.keys.AddRange(keys);
            clone.values.AddRange(values);
            return clone;
        }

        public void CopyTo(Dictionary<TKey, TValue> target, bool clearFirst = false)
        {
            if (target == null)
            {
                return;
            }

            if (clearFirst)
            {
                target.Clear();
            }

            int count = Math.Min(keys.Count, values.Count);
            for (int i = 0; i < count; i++)
            {
                target[keys[i]] = values[i];
            }
        }
    }

}
