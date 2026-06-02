using System;
using DG.Tweening;
using UnityEngine;

namespace My.Map.View
{
    // 藤蔓 LineRenderer 纯表现：从 seedAnchor 沿 up 方向生长固定长度。
    public class VineGrowthLineView : MonoBehaviour
    {
        [SerializeField] LineRenderer line;
        [SerializeField] Transform seedAnchor;
        [SerializeField] int segmentCount = 16;
        [SerializeField] float lineWidth = 0.12f;
        [SerializeField] float defaultGrowLength = 1.23f;

        Tween _growTween;
        Vector3[] _pointBuffer;
        float _growLengthWorld;

        void Awake()
        {
            if (line == null)
            {
                line = GetComponent<LineRenderer>();
            }

            if (line != null)
            {
                line.useWorldSpace = false;
                line.widthMultiplier = lineWidth;
            }

            _growLengthWorld = Mathf.Max(0.01f, defaultGrowLength);
        }

        public float GrowLengthWorld => _growLengthWorld;

        public void Configure(float growLengthWorld)
        {
            _growLengthWorld = Mathf.Max(0.01f, growLengthWorld);
        }

        public Vector3 GetTopWorldPosition()
        {
            return EvaluatePathWorld(1f);
        }

        public void SetProgress(float t)
        {
            t = Mathf.Clamp01(t);
            if (line == null || seedAnchor == null)
            {
                return;
            }

            if (t <= 0f)
            {
                line.positionCount = 0;
                line.enabled = false;
                return;
            }

            line.enabled = true;
            int count = Mathf.Max(2, segmentCount);
            EnsureBuffer(count);
            SamplePath(t, _pointBuffer, count);
            line.positionCount = count;
            line.SetPositions(_pointBuffer);
        }

        public void PlayGrow(float duration, Action onComplete = null)
        {
            KillActiveTween();
            if (line != null)
            {
                line.enabled = true;
            }

            SetProgress(0f);
            _growTween = DOTween.To(() => 0f, SetProgress, 1f, Mathf.Max(0.01f, duration))
                .SetEase(Ease.OutQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void SetInstantFull()
        {
            KillActiveTween();
            SetProgress(1f);
        }

        public void SetHidden()
        {
            KillActiveTween();
            SetProgress(0f);
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

        void EnsureBuffer(int count)
        {
            if (_pointBuffer == null || _pointBuffer.Length != count)
            {
                _pointBuffer = new Vector3[count];
            }
        }

        void SamplePath(float t, Vector3[] outPoints, int count)
        {
            var root = line.transform;
            for (int i = 0; i < count; i++)
            {
                float pathT = (count <= 1) ? t : (i / (float)(count - 1)) * t;
                var world = EvaluatePathWorld(pathT);
                outPoints[i] = root.InverseTransformPoint(world);
            }
        }

        Vector3 EvaluatePathWorld(float pathT)
        {
            pathT = Mathf.Clamp01(pathT);
            var origin = seedAnchor.position;
            var direction = seedAnchor.up.sqrMagnitude > 0.0001f ? seedAnchor.up.normalized : Vector3.up;
            return origin + direction * (_growLengthWorld * pathT);
        }
    }
}
