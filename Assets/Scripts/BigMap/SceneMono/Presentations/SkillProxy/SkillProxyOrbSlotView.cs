using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace My.Map.Scene
{
    // 单个弹药槽显示：active/inactive 两态切换 + alpha 淡入淡出。
    public class SkillProxyOrbSlotView : MonoBehaviour
    {
        [SerializeField] private GameObject activeVisual;
        [SerializeField] private GameObject inactiveVisual;

        // 若未在 Inspector 指定，Awake 时从 activeVisual 子节点自动收集
        [SerializeField] private SpriteRenderer[] fadeRenderers;

        readonly List<Tween> _fadeTweens = new();

        void Awake()
        {
            if ((fadeRenderers == null || fadeRenderers.Length == 0) && activeVisual != null)
            {
                fadeRenderers = activeVisual.GetComponentsInChildren<SpriteRenderer>(true);
            }
        }

        public void SetActive(bool active)
        {
            if (activeVisual != null)
            {
                activeVisual.SetActive(active);
            }

            if (inactiveVisual != null)
            {
                inactiveVisual.SetActive(!active);
            }
        }

        public void SetAlpha(float alpha)
        {
            if (fadeRenderers == null)
            {
                return;
            }

            foreach (var sr in fadeRenderers)
            {
                if (sr == null)
                {
                    continue;
                }

                var c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }

        // 返回第一个可用 renderer 的 alpha；无则返回 1。
        public float GetAlpha()
        {
            if (fadeRenderers != null)
            {
                foreach (var sr in fadeRenderers)
                {
                    if (sr != null)
                    {
                        return sr.color.a;
                    }
                }
            }

            return 1f;
        }

        // 对所有 fadeRenderers 同步执行 alpha tween；完成后调用 onComplete。
        public void PlayFade(float targetAlpha, float duration, Action onComplete = null)
        {
            KillFadeTweens();

            if (fadeRenderers == null || fadeRenderers.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            bool callbackFired = false;
            int remaining = 0;

            foreach (var sr in fadeRenderers)
            {
                if (sr != null)
                {
                    remaining++;
                }
            }

            if (remaining == 0)
            {
                onComplete?.Invoke();
                return;
            }

            foreach (var sr in fadeRenderers)
            {
                if (sr == null)
                {
                    continue;
                }

                var captured = sr;
                var t = DOTween.To(
                    () => captured.color.a,
                    a =>
                    {
                        var c = captured.color;
                        c.a = a;
                        captured.color = c;
                    },
                    targetAlpha,
                    duration)
                    .SetEase(Ease.OutCubic)
                    .OnComplete(() =>
                    {
                        remaining--;
                        if (remaining <= 0 && !callbackFired)
                        {
                            callbackFired = true;
                            onComplete?.Invoke();
                        }
                    });
                _fadeTweens.Add(t);
            }
        }

        public void KillFadeTweens()
        {
            foreach (var t in _fadeTweens)
            {
                t?.Kill();
            }

            _fadeTweens.Clear();
        }

        void OnDestroy()
        {
            KillFadeTweens();
        }
    }
}
