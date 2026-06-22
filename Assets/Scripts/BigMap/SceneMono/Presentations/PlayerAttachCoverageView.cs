using System.Collections.Generic;
using My.Config;
using UnityEngine;

namespace My.Map.Scene
{
    public sealed class PlayerAttachCoverageView : MonoBehaviour
    {
        readonly List<Transform> _dots = new();
        readonly List<Vector3> _baseOffsets = new();

        string _attachId;
        int _sameTypeCount = -1;

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
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            _dots.Clear();
            _baseOffsets.Clear();

            int dotCount = PlayerAttachCatalog.GetCoverageCircleCount(_attachId, _sameTypeCount);
            if (dotCount <= 0)
            {
                return;
            }

            float radius = PlayerAttachCatalog.GetCoverageRadius(_attachId);
            var color = RuntimeCircleVisualUtil.ParseColor(
                PlayerAttachCatalog.GetCoverageColor(_attachId),
                new Color(0.08f, 0.08f, 0.08f, 0.9f));
            color.a = Mathf.Clamp(color.a <= 0f ? 0.85f : color.a, 0.2f, 1f);

            for (int i = 0; i < dotCount; i++)
            {
                var go = new GameObject($"Coverage_{i + 1}");
                go.transform.SetParent(transform, false);

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
    }
}
