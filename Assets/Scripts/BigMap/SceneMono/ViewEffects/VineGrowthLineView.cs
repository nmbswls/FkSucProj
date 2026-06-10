using System;
using DG.Tweening;
using UnityEngine;

namespace My.Map.View
{
    // 配置 VineGrowLength = 从 vine_line 原点 (y=0) 到顶端的总高度（世界单位）。
    // base 为底部装饰不参与高度预算；body 用 Tiled + flipY + scaleY<0 反转扩展方向，Transform 钉顶边；head 接在 body 顶边。
    public class VineGrowthLineView : MonoBehaviour
    {
        const float BodyTiledScaleY = -1f;

        [SerializeField] Transform seedAnchor;
        [SerializeField] SpriteRenderer baseMaskRenderer;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] SpriteRenderer tipRenderer;
        [SerializeField] float defaultGrowLength = 1.23f;

        float _growLengthWorld;
        float _currentProgress;
        Tween _growTween;

        public float GrowLengthWorld => _growLengthWorld;
        public float CurrentProgress => _currentProgress;
        public event Action ProgressChanged;

        void Awake()
        {
            if (baseMaskRenderer == null)
            {
                baseMaskRenderer = transform.Find("base")?.GetComponent<SpriteRenderer>();
            }

            if (bodyRenderer == null)
            {
                bodyRenderer = transform.Find("body")?.GetComponent<SpriteRenderer>();
            }

            if (tipRenderer == null)
            {
                tipRenderer = transform.Find("head")?.GetComponent<SpriteRenderer>();
            }

            SetupBodyTiledFlip(bodyRenderer);

            _growLengthWorld = Mathf.Max(0.01f, defaultGrowLength);
        }

        public void Configure(float growLengthWorld)
        {
            _growLengthWorld = Mathf.Max(0.01f, growLengthWorld);
            if (_currentProgress > 0f)
            {
                ApplyProgress(_currentProgress);
            }
        }

        public Vector3 GetTopWorldPosition()
        {
            float localTopY = EvaluateTargetLocalTopY(_currentProgress);
            return transform.TransformPoint(new Vector3(0f, localTopY, 0f));
        }

        public void SetProgress(float t)
        {
            ApplyProgress(Mathf.Clamp01(t));
        }

        public void PlayGrow(float duration, Action onComplete = null)
        {
            KillActiveTween();
            ApplyProgress(0f);
            _growTween = DOTween.To(() => 0f, ApplyProgress, 1f, Mathf.Max(0.01f, duration))
                .SetEase(Ease.OutQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void SetInstantFull()
        {
            KillActiveTween();
            ApplyProgress(1f);
        }

        public void SetHidden()
        {
            KillActiveTween();
            ApplyProgress(0f);
        }

        public void KillActiveTween()
        {
            if (_growTween != null && _growTween.IsActive())
            {
                _growTween.Kill();
            }

            _growTween = null;
        }

        void OnDisable()
        {
            KillActiveTween();
        }

        void ApplyProgress(float t)
        {
            _currentProgress = Mathf.Clamp01(t);

            if (_currentProgress <= 0.0001f)
            {
                SetRendererEnabled(bodyRenderer, false);
                SetRendererEnabled(tipRenderer, false);

                if (baseMaskRenderer != null && GetSegmentLocalHeight(baseMaskRenderer) > 0f)
                {
                    SetRendererEnabled(baseMaskRenderer, true);
                    LayoutFromBottom(baseMaskRenderer, 0f);
                }
                else
                {
                    SetRendererEnabled(baseMaskRenderer, false);
                }

                ProgressChanged?.Invoke();
                return;
            }

            float targetLocalTopY = EvaluateTargetLocalTopY(_currentProgress);
            float tipLocalH = GetSegmentLocalHeight(tipRenderer);
            float baseLocalH = GetSegmentLocalHeight(baseMaskRenderer);

            // 总高度 = body + head；未长到能容纳 head 前只伸 body
            bool showTip = tipLocalH <= 0.001f || targetLocalTopY >= tipLocalH - 0.001f;
            float bodyLocalH = showTip
                ? Mathf.Max(0f, targetLocalTopY - tipLocalH)
                : targetLocalTopY;

            if (baseMaskRenderer != null && baseLocalH > 0f)
            {
                SetRendererEnabled(baseMaskRenderer, true);
                LayoutFromBottom(baseMaskRenderer, 0f);
            }

            // body 顶边 Y；底边在生长过程中始终落在 vine_line 原点 y=0
            float bodyTopLocalY = bodyLocalH;

            if (bodyRenderer != null)
            {
                bool showBody = bodyLocalH > 0.001f;
                SetRendererEnabled(bodyRenderer, showBody);
                if (showBody)
                {
                    ApplyTiledBodyLayout(bodyRenderer, bodyTopLocalY, bodyLocalH);
                }
            }

            if (tipRenderer != null && tipLocalH > 0f)
            {
                SetRendererEnabled(tipRenderer, showTip);
                if (showTip)
                {
                    LayoutFromBottom(tipRenderer, bodyTopLocalY);
                }
            }

            ProgressChanged?.Invoke();
        }

        float EvaluateTargetLocalTopY(float progress)
        {
            progress = Mathf.Clamp01(progress);
            return Mathf.Max(0f, WorldLengthToLocalY(_growLengthWorld * progress));
        }

        float WorldLengthToLocalY(float worldLength)
        {
            return worldLength / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        }

        static void SetRendererEnabled(SpriteRenderer renderer, bool enabled)
        {
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }

        static float GetSegmentLocalHeight(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return 0f;
            }

            return renderer.sprite.bounds.size.y * Mathf.Abs(renderer.transform.localScale.y);
        }

        // flipY + scaleY<0：视觉不变，Tiled 增高时从 Transform（顶边）向 y=0 延伸。
        static void SetupBodyTiledFlip(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.flipY = true;

            var tr = renderer.transform;
            //tr.localScale = new Vector3(tr.localScale.x, BodyTiledScaleY, tr.localScale.z);
        }

        static void ApplyTiledBodyLayout(SpriteRenderer renderer, float topLocalY, float localHeight)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return;
            }

            SetupBodyTiledFlip(renderer);

            var tr = renderer.transform;
            float absScaleY = Mathf.Max(0.0001f, Mathf.Abs(tr.localScale.y));
            var size = renderer.size;
            size.y = localHeight / absScaleY;
            renderer.size = size;

            // body 贴图需 Top Pivot；Transform 即顶边，只改 Size 即向下填藤身
            tr.localPosition = new Vector3(tr.localPosition.x, topLocalY, tr.localPosition.z);
            tr.localRotation = Quaternion.identity;
        }

        static void LayoutFromBottom(SpriteRenderer renderer, float bottomY)
        {
            var tr = renderer.transform;
            var sprite = renderer.sprite;
            if (sprite == null)
            {
                return;
            }

            float scaleY = tr.localScale.y;
            tr.localPosition = new Vector3(tr.localPosition.x, bottomY - sprite.bounds.min.y * scaleY, tr.localPosition.z);
            tr.localRotation = Quaternion.identity;
        }
    }
}
