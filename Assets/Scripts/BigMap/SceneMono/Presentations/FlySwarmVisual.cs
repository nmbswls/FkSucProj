using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Scene
{
    public sealed class FlySwarmVisual : MonoBehaviour
    {
        const int FlyCount = 7;

        readonly List<Transform> _flies = new();
        readonly Dictionary<SpriteRenderer, bool> _hiddenRenderers = new();
        readonly List<Vector3> _baseOffsets = new();

        Transform _root;
        bool _enabled;

        public void SetEnabled(bool enabled, Transform viewRoot, Transform agentView)
        {
            if (_enabled == enabled)
            {
                return;
            }

            _enabled = enabled;
            if (_enabled)
            {
                HideOriginalRenderers(agentView);
                Build(viewRoot != null ? viewRoot : transform);
            }
            else
            {
                RestoreOriginalRenderers();
                if (_root != null)
                {
                    Destroy(_root.gameObject);
                    _root = null;
                }
                _flies.Clear();
                _baseOffsets.Clear();
            }
        }

        public void Tick(float dt)
        {
            if (!_enabled)
            {
                return;
            }

            float t = Time.time;
            for (int i = 0; i < _flies.Count; i++)
            {
                var tr = _flies[i];
                if (tr == null)
                {
                    continue;
                }

                var baseOffset = _baseOffsets[i];
                float wobbleX = Mathf.Sin(t * (3.3f + i * 0.17f) + i * 1.7f) * 0.045f;
                float wobbleY = Mathf.Cos(t * (4.1f + i * 0.13f) + i * 0.9f) * 0.035f;
                tr.localPosition = baseOffset + new Vector3(wobbleX, wobbleY, 0f);
            }
        }

        void HideOriginalRenderers(Transform agentView)
        {
            _hiddenRenderers.Clear();
            if (agentView == null)
            {
                return;
            }

            var renderers = agentView.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                if (sr == null)
                {
                    continue;
                }

                _hiddenRenderers[sr] = sr.enabled;
                sr.enabled = false;
            }
        }

        void RestoreOriginalRenderers()
        {
            foreach (var kv in _hiddenRenderers)
            {
                if (kv.Key != null)
                {
                    kv.Key.enabled = kv.Value;
                }
            }

            _hiddenRenderers.Clear();
        }

        void Build(Transform parent)
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("FlySwarmRuntimeVisual").transform;
            _root.SetParent(parent, false);
            _root.localPosition = Vector3.zero;

            var color = new Color(0.08f, 0.11f, 0.07f, 1f);
            for (int i = 0; i < FlyCount; i++)
            {
                var go = new GameObject($"Fly_{i + 1}");
                go.transform.SetParent(_root, false);

                float angle = i * Mathf.PI * 2f / FlyCount;
                float radius = i % 2 == 0 ? 0.18f : 0.32f;
                var offset = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius + 0.08f, 0f);
                go.transform.localPosition = offset;
                go.transform.localScale = Vector3.one * (i % 3 == 0 ? 0.11f : 0.085f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = RuntimeCircleVisualUtil.CircleSprite;
                sr.color = color;
                sr.sortingOrder = 8 + i;

                _flies.Add(go.transform);
                _baseOffsets.Add(offset);
            }
        }
    }
}
