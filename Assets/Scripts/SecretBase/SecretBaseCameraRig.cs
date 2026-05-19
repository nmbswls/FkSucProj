using UnityEngine;

namespace My.SecretBase
{
    // 横板据点相机：跟随 scrollX，并对视差层施加偏移。
    public class SecretBaseCameraRig : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float scrollSpeed = 8f;
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 40f;

        private float _scrollX;

        public float ScrollX => _scrollX;

        public void ConfigureBounds(float min, float max)
        {
            minX = min;
            maxX = max;
            _scrollX = Mathf.Clamp(_scrollX, minX, maxX);
            ApplyCameraPosition();
        }

        public void ResetScroll(float x = 0f)
        {
            _scrollX = Mathf.Clamp(x, minX, maxX);
            ApplyCameraPosition();
        }

        public void Tick(float dt, float axisInput)
        {
            if (Mathf.Abs(axisInput) < 0.01f)
            {
                return;
            }

            _scrollX = Mathf.Clamp(_scrollX + axisInput * scrollSpeed * dt, minX, maxX);
            ApplyCameraPosition();
        }

        void ApplyCameraPosition()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            var p = targetCamera.transform.position;
            p.x = _scrollX;
            targetCamera.transform.position = p;
        }

        public void ApplyParallax(SecretBaseParallaxLayer[] layers)
        {
            if (layers == null)
            {
                return;
            }

            foreach (var layer in layers)
            {
                if (layer == null)
                {
                    continue;
                }

                layer.ApplyOffset(_scrollX);
            }
        }
    }
}
