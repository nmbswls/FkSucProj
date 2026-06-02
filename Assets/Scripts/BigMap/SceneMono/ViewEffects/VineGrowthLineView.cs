using System;
using DG.Tweening;
using UnityEngine;

namespace My.Map.View
{
    // 藤蔓 LineRenderer 纯表现：路径采样与生长 Tween，不依赖逻辑层。
    public class VineGrowthLineView : MonoBehaviour
    {
        [SerializeField] LineRenderer line;
        [SerializeField] Transform seedAnchor;
        [SerializeField] Transform targetAnchor;
        [SerializeField] Transform[] pathAnchors;
        [SerializeField] int segmentCount = 16;
        [SerializeField] float lineWidth = 0.12f;

        Tween _growTween;
        Vector3[] _pointBuffer;

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
        }

        public void SetProgress(float t)
        {
            t = Mathf.Clamp01(t);
            if (line == null || seedAnchor == null || targetAnchor == null)
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
            var a = seedAnchor.position;
            var b = targetAnchor.position;

            if (pathAnchors == null || pathAnchors.Length == 0)
            {
                return Vector3.Lerp(a, b, pathT);
            }

            if (pathAnchors.Length == 1)
            {
                return QuadraticBezier(a, pathAnchors[0].position, b, pathT);
            }

            var c1 = pathAnchors[0].position;
            var c2 = pathAnchors[pathAnchors.Length - 1].position;
            return CubicBezier(a, c1, c2, b, pathT);
        }

        static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            float u2 = u * u;
            float t2 = t * t;
            return u2 * u * p0 + 3f * u2 * t * p1 + 3f * u * t2 * p2 + t2 * t * p3;
        }
    }
}
