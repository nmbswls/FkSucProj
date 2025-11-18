using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTimeDriver : MonoBehaviour
{
    private class Tween
    {
        public Action<float> update; // 传入 0..1 的进度
        public Action completed;
        public float duration;
        public float elapsed;
        public bool alive;
    }

    private readonly List<Tween> tweens = new List<Tween>();

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = tweens.Count - 1; i >= 0; i--)
        {
            var t = tweens[i];
            if (!t.alive) { tweens.RemoveAt(i); continue; }
            t.elapsed += dt;
            float p = t.duration <= 0f ? 1f : Mathf.Clamp01(t.elapsed / t.duration);
            t.update?.Invoke(p);
            if (p >= 1f)
            {
                t.completed?.Invoke();
                t.alive = false;
                tweens.RemoveAt(i);
            }
        }
    }

    public void Run(float duration, Action<float> onUpdate, Action onComplete = null)
    {
        var tw = new Tween
        {
            duration = duration,
            elapsed = 0f,
            update = onUpdate,
            completed = onComplete,
            alive = true
        };
        tweens.Add(tw);
    }
}