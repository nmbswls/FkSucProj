using DG.Tweening;
using UnityEngine;

namespace My.Map.Scene
{
    public abstract partial class SceneUnitPresenter
    {
        struct RelocateShadowSnapshot
        {
            public Vector3 LocalScale;
            public Color BaseColor;
            public bool Captured;
        }

        RelocateShadowSnapshot _relocateShadowSnapshot;
        Tween _relocateShadowTween;
        float _relocateShadowFade = 1f;
        float _relocateShadowScaleMul = 1f;

        protected void EnsureRelocateShadowReady()
        {
            if (ViewRoot == null)
            {
                ViewRoot = transform.Find(UnitPresentationPaths.View);
                if (ViewRoot == null)
                {
                    ViewRoot = transform.Find(UnitPresentationPaths.ViewLegacy);
                }
            }

            if (ShadowView == null && ViewRoot != null)
            {
                ShadowView = ViewRoot.Find(UnitPresentationPaths.Shadow);
            }

            if (_shadowView == null && ShadowView != null)
            {
                AssignShadowViewRenderer(ShadowView.GetComponent<SpriteRenderer>());
            }
        }

        protected void CaptureRelocateShadowSnapshot()
        {
            EnsureRelocateShadowReady();
            if (ShadowView == null || _shadowView == null)
            {
                _relocateShadowSnapshot = default;
                return;
            }

            _relocateShadowSnapshot = new RelocateShadowSnapshot
            {
                LocalScale = ShadowView.localScale,
                BaseColor = _shadowView.color,
                Captured = true,
            };
            _relocateShadowFade = 1f;
            _relocateShadowScaleMul = 1f;
        }

        protected void ApplyRelocateShadowVisual(float visible01, float scaleMul)
        {
            if (!_relocateShadowSnapshot.Captured || ShadowView == null || _shadowView == null)
            {
                return;
            }

            _relocateShadowFade = Mathf.Clamp01(visible01);
            _relocateShadowScaleMul = Mathf.Max(0f, scaleMul);
            ShadowView.localScale = _relocateShadowSnapshot.LocalScale * _relocateShadowScaleMul;

            var c = _relocateShadowSnapshot.BaseColor;
            c.a = _relocateShadowSnapshot.BaseColor.a * _relocateShadowFade;
            _shadowView.color = c;
        }

        protected Tween TweenRelocateShadowVisual(float targetVisible01, float targetScaleMul, float duration)
        {
            KillRelocateShadowTween();
            if (!_relocateShadowSnapshot.Captured)
            {
                return null;
            }

            float fade = _relocateShadowFade;
            float scale = _relocateShadowScaleMul;
            var seq = DOTween.Sequence();
            seq.Join(DOTween.To(
                () => fade,
                v =>
                {
                    fade = v;
                    ApplyRelocateShadowVisual(fade, scale);
                },
                targetVisible01,
                duration));
            seq.Join(DOTween.To(
                () => scale,
                v =>
                {
                    scale = v;
                    ApplyRelocateShadowVisual(fade, scale);
                },
                targetScaleMul,
                duration));
            _relocateShadowTween = seq;
            return seq;
        }

        protected void KillRelocateShadowTween()
        {
            if (_relocateShadowTween != null && _relocateShadowTween.IsActive())
            {
                _relocateShadowTween.Kill();
            }

            _relocateShadowTween = null;
        }

        protected void RestoreRelocateShadowVisual()
        {
            KillRelocateShadowTween();
            if (!_relocateShadowSnapshot.Captured || ShadowView == null || _shadowView == null)
            {
                return;
            }

            ShadowView.localScale = _relocateShadowSnapshot.LocalScale;
            _shadowView.color = _relocateShadowSnapshot.BaseColor;
            _relocateShadowFade = 1f;
            _relocateShadowScaleMul = 1f;
            _relocateShadowSnapshot = default;
        }
    }
}
