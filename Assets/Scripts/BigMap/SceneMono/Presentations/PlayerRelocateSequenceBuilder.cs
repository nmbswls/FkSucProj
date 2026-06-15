using System;
using DG.Tweening;
using UnityEngine;

namespace My.Map.Scene
{
    public struct FakeJump2DContext
    {
        public Transform Root;
        public Transform ViewRoot;
        public Vector3 ViewBaseLocalPos;
    }

    public static class PlayerRelocateSequenceBuilder
    {
        public static void AppendRootWalk(
            Sequence seq,
            Transform root,
            Vector3 targetWorld,
            float duration,
            Ease ease = Ease.OutQuad)
        {
            seq.Append(root.DOMove(targetWorld, duration).SetEase(ease));
        }

        public static Tween CreateLinearProgressTween(float duration, Action<float> onProgress)
        {
            float t = 0f;
            return DOTween.To(
                () => t,
                v =>
                {
                    t = v;
                    onProgress(v);
                },
                1f,
                duration).SetEase(Ease.Linear);
        }

        public static void AppendFakeJump2D(
            Sequence seq,
            FakeJump2DContext ctx,
            Vector3 fromWorld,
            Vector3 toWorld,
            float arcPeak,
            float duration,
            Action<Sequence, float> appendShadowTween = null)
        {
            ctx.Root.position = fromWorld;

            seq.Append(ctx.Root.DOMove(toWorld, duration).SetEase(Ease.InOutQuad));

            if (ctx.ViewRoot != null)
            {
                seq.Join(CreateLinearProgressTween(duration, t =>
                {
                    float yOff = 4f * arcPeak * t * (1f - t);
                    var localPos = ctx.ViewBaseLocalPos;
                    localPos.y = ctx.ViewBaseLocalPos.y + yOff;
                    ctx.ViewRoot.localPosition = localPos;
                }));
            }

            appendShadowTween?.Invoke(seq, duration);
        }
    }
}
