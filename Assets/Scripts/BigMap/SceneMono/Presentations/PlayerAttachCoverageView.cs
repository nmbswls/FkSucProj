using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Scene
{
    public sealed class PlayerAttachCoverageView : MonoBehaviour
    {
        readonly List<Transform> _dots = new();
        readonly List<Vector3> _baseOffsets = new();

        string _attachId;
        int _sameTypeCount = -1;
        Transform _coverageRoot;

        public void Configure(string attachId, int sameTypeCount)
        {
            sameTypeCount = Mathf.Max(0, sameTypeCount);
            if (_attachId == attachId && _sameTypeCount == sameTypeCount)
            {
                return;
            }

            _attachId = attachId;
            _sameTypeCount = sameTypeCount;
            Rebuild();
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < _dots.Count; i++)
            {
                var tr = _dots[i];
                if (tr == null)
                {
                    continue;
                }

                float pulse = Mathf.Sin(t * 2.4f + i * 0.73f) * 0.018f;
                tr.localPosition = _baseOffsets[i] + new Vector3(pulse, -pulse * 0.5f, 0f);
            }
        }

        void Rebuild()
        {
            var root = GetOrCreateCoverageRoot();
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }

            _dots.Clear();
            _baseOffsets.Clear();

            var effectCfg = ResolveEffectCfg(_attachId, _sameTypeCount);
            int dotCount = effectCfg.DotCount;
            root.gameObject.SetActive(dotCount > 0);
            if (dotCount <= 0)
            {
                return;
            }

            float radius = effectCfg.Radius;
            var color = RuntimeCircleVisualUtil.ParseColor(
                effectCfg.Color,
                new Color(0.08f, 0.08f, 0.08f, 0.9f));
            color.a = Mathf.Clamp(color.a <= 0f ? 0.85f : color.a, 0.2f, 1f);

            for (int i = 0; i < dotCount; i++)
            {
                var go = new GameObject($"Coverage_{i + 1}");
                go.transform.SetParent(root, false);

                float normalized = dotCount <= 1 ? 0f : i / (float)(dotCount - 1);
                float angle = i * 2.3999632f;
                float r = radius * Mathf.Sqrt(normalized);
                var offset = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r + 0.05f, 0f);
                go.transform.localPosition = offset;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.07f, 0.11f, (i % 3) / 2f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = RuntimeCircleVisualUtil.CircleSprite;
                sr.color = color;
                sr.sortingOrder = 30 + i;

                _dots.Add(go.transform);
                _baseOffsets.Add(offset);
            }
        }

        Transform GetOrCreateCoverageRoot()
        {
            if (_coverageRoot != null)
            {
                return _coverageRoot;
            }

            _coverageRoot = transform.Find("__AttachCoverage");
            if (_coverageRoot != null)
            {
                return _coverageRoot;
            }

            var go = new GameObject("__AttachCoverage");
            go.transform.SetParent(transform, false);
            _coverageRoot = go.transform;
            return _coverageRoot;
        }

        static CoverageEffectCfg ResolveEffectCfg(string attachId, int sameTypeCount)
        {
            switch (attachId)
            {
                case "forest_fly_attach":
                    return new CoverageEffectCfg
                    {
                        DotCount = ResolveForestFlyDotCount(sameTypeCount),
                        Radius = 0.42f,
                        Color = "#1f2a1d",
                    };
                default:
                    return default;
            }
        }

        static int ResolveForestFlyDotCount(int sameTypeCount)
        {
            if (sameTypeCount <= 0)
            {
                return 0;
            }

            if (sameTypeCount == 1)
            {
                return 5;
            }

            if (sameTypeCount == 2)
            {
                return 10;
            }

            return 16;
        }

        struct CoverageEffectCfg
        {
            public int DotCount;
            public float Radius;
            public string Color;
        }
    }
}
