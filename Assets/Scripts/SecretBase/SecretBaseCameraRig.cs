using Cinemachine;
using My;
using UnityEngine;

namespace My.SecretBase
{
    // 横板卷轴：驱动 Main_Root 上的 MainMapVCam，不占用子场景内的 Main Camera。
    public class SecretBaseCameraRig : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private Transform scrollAnchor;
        [SerializeField] private float scrollSpeed = 8f;
        [SerializeField] private float minX;
        [SerializeField] private float maxX = 40f;

        private float _scrollX;
        private Transform _savedFollow;
        private Transform _savedLookAt;
        private bool _bound;

        public float ScrollX => _scrollX;

        public void ConfigureBounds(float min, float max)
        {
            minX = min;
            maxX = max;
            _scrollX = Mathf.Clamp(_scrollX, minX, maxX);
            ApplyScrollPosition();
        }

        public void ResetScroll(float x = 0f)
        {
            _scrollX = Mathf.Clamp(x, minX, maxX);
            ApplyScrollPosition();
        }

        public void BindForSecretBase()
        {
            var vcam = ResolveVirtualCamera();
            if (vcam == null)
            {
                Debug.LogError("SecretBaseCameraRig: MainMapVCam not found.");
                return;
            }

            if (_bound)
            {
                return;
            }

            _savedFollow = vcam.Follow;
            _savedLookAt = vcam.LookAt;
            vcam.Follow = null;
            vcam.LookAt = null;
            vcam.PreviousStateIsValid = false;

            _bound = true;
            ApplyScrollPosition();
        }

        public void UnbindFromSecretBase()
        {
            if (!_bound)
            {
                return;
            }

            var vcam = ResolveVirtualCamera();
            if (vcam != null)
            {
                vcam.Follow = _savedFollow;
                vcam.LookAt = _savedLookAt;
                vcam.PreviousStateIsValid = false;
            }

            _savedFollow = null;
            _savedLookAt = null;
            _bound = false;
        }

        public void Tick(float dt, float axisInput)
        {
            if (!_bound || Mathf.Abs(axisInput) < 0.01f)
            {
                return;
            }

            _scrollX = Mathf.Clamp(_scrollX + axisInput * scrollSpeed * dt, minX, maxX);
            ApplyScrollPosition();
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

        CinemachineVirtualCamera ResolveVirtualCamera()
        {
            if (virtualCamera != null)
            {
                return virtualCamera;
            }

            return MainGameManager.Instance != null ? MainGameManager.Instance.MainMapVCam : null;
        }

        void ApplyScrollPosition()
        {
            var vcam = ResolveVirtualCamera();
            if (vcam == null)
            {
                return;
            }

            var anchor = scrollAnchor != null ? scrollAnchor : transform;
            var p = vcam.transform.position;
            p.x = _scrollX;
            p.y = anchor.position.y;
            p.z = anchor.position.z;
            vcam.transform.position = p;
        }
    }
}
