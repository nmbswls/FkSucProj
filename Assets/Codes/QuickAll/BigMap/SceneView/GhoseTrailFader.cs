using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    public class GhoseTrailFader : MonoBehaviour
    {
        public float life = 0.3f;

        private float _elapsed;
        private SpriteRenderer _sr;
        private Color _baseColor;
        private bool _inited;

        void OnEnable()
        {
            if (_sr == null)
                _sr = GetComponent<SpriteRenderer>();

            if (_sr == null)
            {
                Debug.LogError("GhoseTrailFader requires SpriteRenderer.");
                enabled = false;
                return;
            }

            // 使用当前颜色作为基色
            _baseColor = _sr.color;
            _elapsed = 0f;
            _inited = true;
        }

        // 可在生成时传入初始颜色（与目标一致）
        public void ResetLife(float l, Color initialColor)
        {
            life = l;
            _elapsed = 0f;
            _baseColor = initialColor;
            if (_sr != null)
            {
                _sr.color = _baseColor;
            }
            _inited = true;
        }

        void Update()
        {
            if (!_inited) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / life);

            var c = _sr.color;
            c.a = Mathf.Lerp(_baseColor.a, 0f, t); // Alpha 从初始 -> 0
            _sr.color = c;

            if (_elapsed >= life)
            {
                // 到点交给Spawner的协程回收
            }
        }
    }
}
