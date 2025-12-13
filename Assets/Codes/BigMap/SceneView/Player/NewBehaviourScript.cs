using UnityEngine;
using TMPro;
using System;

namespace My.Map.View
{
    [RequireComponent(typeof(TMP_Text))]
    public class FloatingRumorText : MonoBehaviour
    {
        [Header("Runtime")]
        public float lifetime = 2f;
        public float floatSpeed = 1f;
        public float fadeInTime = 0.1f;
        public float fadeOutTime = 0.4f;
        public float swayAmplitude = 0.1f;
        public float swayFrequency = 1.5f;
        public Vector3 initialScale = Vector3.one;
        public bool lookAtCamera = true;

        public Action onFinished;

        private TMP_Text _text;
        private Color _baseColor;
        private float _age;
        private Vector3 _startPos;
        private Vector3 _randomSwayDir;
        private Transform _mainCam;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _baseColor = _text.color;
            _randomSwayDir = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f)) * Vector3.right;
            _mainCam = Camera.main ? Camera.main.transform : null;
        }

        public void Play(
            float lifetime,
            float floatSpeed,
            float fadeInTime,
            float fadeOutTime,
            float swayAmplitude,
            float swayFrequency,
            Vector3 initialScale,
            bool lookAtCamera
        )
        {
            this.lifetime = lifetime;
            this.floatSpeed = floatSpeed;
            this.fadeInTime = Mathf.Max(0.01f, fadeInTime);
            this.fadeOutTime = Mathf.Max(0.01f, fadeOutTime);
            this.swayAmplitude = swayAmplitude;
            this.swayFrequency = swayFrequency;
            this.initialScale = initialScale;
            this.lookAtCamera = lookAtCamera;

            _age = 0f;
            _startPos = transform.position;
            _text.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0f);
            transform.localScale = initialScale;
            gameObject.SetActive(true);
            enabled = true;
        }

        private void OnEnable()
        {
            // 若从对象池激活，重置必要状态
            if (_text == null) _text = GetComponent<TMP_Text>();
            _baseColor = _text.color;
            if (_mainCam == null && Camera.main) _mainCam = Camera.main.transform;
            _randomSwayDir = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f)) * Vector3.right;
        }

        private void Update()
        {
            _age += Time.deltaTime;

            // 上浮
            float upOffset = floatSpeed * _age;
            // 摆动（水平轻微偏移）
            float sway = Mathf.Sin(_age * swayFrequency * Mathf.PI * 2f) * swayAmplitude;
            Vector3 swayOffset = _randomSwayDir * sway;

            transform.position = _startPos + Vector3.up * upOffset + swayOffset;

            // 朝向相机（仅旋转Y以保持平面）
            if (lookAtCamera && _mainCam != null)
            {
                // 使文本始终朝向镜头（Billboard）
                Vector3 toCam = _mainCam.position - transform.position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(toCam);
                }
            }

            // 渐隐/渐显
            float alpha = 1f;
            if (_age < fadeInTime)
            {
                alpha = Mathf.Clamp01(_age / fadeInTime);
            }
            else if (_age > lifetime - fadeOutTime)
            {
                float t = (_age - (lifetime - fadeOutTime)) / fadeOutTime;
                alpha = Mathf.Clamp01(1f - t);
            }

            var c = _text.color;
            _text.color = new Color(c.r, c.g, c.b, alpha);

            // 缓慢缩放（可选：在消失前略微缩小）
            float shrinkStart = lifetime - fadeOutTime;
            if (_age > shrinkStart)
            {
                float t = (_age - shrinkStart) / fadeOutTime;
                transform.localScale = Vector3.Lerp(initialScale, initialScale * 0.9f, t);
            }

            // 结束逻辑
            if (_age >= lifetime)
            {
                onFinished?.Invoke();
                gameObject.SetActive(false);
                enabled = false;
            }
        }
    }
}

