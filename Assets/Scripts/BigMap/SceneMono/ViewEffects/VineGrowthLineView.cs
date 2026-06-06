using System;
using DG.Tweening;
using UnityEngine;

namespace My.Map.View
{
    // 竖直藤蔓：base 底边固定 (0,0)；head/body 顶边锚定，随生长上移；body Tiled 向下拉长。
    public class VineGrowthLineView : MonoBehaviour
    {
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

            if (bodyRenderer != null)
            {
                bodyRenderer.drawMode = SpriteDrawMode.Tiled;
            }

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
            float topLocalY = EvaluateTopLocalY(_currentProgress);
            return transform.TransformPoint(new Vector3(0f, topLocalY, 0f));
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
                SetRendererEnabled(baseMaskRenderer, false);
                SetRendererEnabled(bodyRenderer, false);
                SetRendererEnabled(tipRenderer, false);
                ProgressChanged?.Invoke();
                return;
            }

            float totalLocalH = WorldToLocalLength(_growLengthWorld * _currentProgress);
            float baseLocalH = GetFullLocalHeight(baseMaskRenderer);
            float tipLocalH = GetFullLocalHeight(tipRenderer);
            float bodyLocalH = Mathf.Max(0f, totalLocalH - baseLocalH - tipLocalH);

            // 从上往下排：head 顶 = totalLocalH，body 顶边接 head 底边并 Tiled 向下长，base 底边固定 0
            float topY = totalLocalH;

            if (tipRenderer != null && tipLocalH > 0f)
            {
                bool showTip = totalLocalH > baseLocalH + 0.001f;
                SetRendererEnabled(tipRenderer, showTip);
                if (showTip)
                {
                    LayoutFromTop(tipRenderer, topY, tipLocalH, tiled: false);
                    topY -= tipLocalH;
                }
            }

            if (bodyRenderer != null)
            {
                bool showBody = bodyLocalH > 0.001f;
                SetRendererEnabled(bodyRenderer, showBody);
                if (showBody)
                {
                    LayoutFromTop(bodyRenderer, topY, bodyLocalH, tiled: true);
                }
            }

            if (baseMaskRenderer != null && baseLocalH > 0f)
            {
                SetRendererEnabled(baseMaskRenderer, true);
                LayoutFromBottom(baseMaskRenderer, 0f, baseLocalH);
            }

            ProgressChanged?.Invoke();
        }

        float EvaluateTopLocalY(float progress)
        {
            progress = Mathf.Clamp01(progress);
            return Mathf.Max(0f, WorldToLocalLength(_growLengthWorld * progress));
        }

        float WorldToLocalLength(float worldLength)
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

        static float GetFullLocalHeight(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return 0f;
            }

            if (renderer.drawMode == SpriteDrawMode.Tiled)
            {
                return renderer.size.y * Mathf.Abs(renderer.transform.localScale.y);
            }

            return renderer.sprite.bounds.size.y * Mathf.Abs(renderer.transform.localScale.y);
        }

        // 顶边锚定在 topY，Tiled 增大 size.y 向下方延伸，localPosition.y 随 topY 上移
        static void LayoutFromTop(SpriteRenderer renderer, float topY, float localHeight, bool tiled)
        {
            var tr = renderer.transform;
            var sprite = renderer.sprite;
            if (sprite == null)
            {
                return;
            }

            float scaleY = tr.localScale.y;
            if (tiled)
            {
                renderer.drawMode = SpriteDrawMode.Tiled;
                var size = renderer.size;
                size.y = localHeight / Mathf.Max(0.0001f, Mathf.Abs(scaleY));
                renderer.size = size;
            }

            // 贴图上缘 = localPosition.y + bounds.max.y * scaleY
            tr.localPosition = new Vector3(0f, topY - sprite.bounds.max.y * scaleY, 0f);
            tr.localRotation = Quaternion.identity;
        }

        // 底边锚定在 bottomY（base 始终贴地）
        static void LayoutFromBottom(SpriteRenderer renderer, float bottomY, float localHeight)
        {
            var tr = renderer.transform;
            var sprite = renderer.sprite;
            if (sprite == null)
            {
                return;
            }

            float scaleY = tr.localScale.y;
            tr.localPosition = new Vector3(0f, bottomY - sprite.bounds.min.y * scaleY, 0f);
            tr.localRotation = Quaternion.identity;
        }
    }
}
