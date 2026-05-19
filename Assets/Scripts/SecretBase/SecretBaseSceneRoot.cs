using UnityEngine;

namespace My.SecretBase
{
    // 场景内挂载：卷轴边界、视差层、交互点、家具根节点。
    public class SecretBaseSceneRoot : MonoBehaviour
    {
        public static SecretBaseSceneRoot Instance { get; private set; }

        [SerializeField] private SecretBaseCameraRig cameraRig;
        [SerializeField] private SecretBaseParallaxLayer[] parallaxLayers;
        [SerializeField] private Transform furnitureRoot;
        [SerializeField] private float scrollMinX;
        [SerializeField] private float scrollMaxX = 40f;
        [SerializeField] private SecretBaseInteractable[] interactables;

        public SecretBaseCameraRig CameraRig => cameraRig;
        public Transform FurnitureRoot => furnitureRoot != null ? furnitureRoot : transform;

        void Awake()
        {
            Instance = this;
            if (cameraRig != null)
            {
                cameraRig.ConfigureBounds(scrollMinX, scrollMaxX);
                cameraRig.ResetScroll(scrollMinX);
            }

            if (interactables == null || interactables.Length == 0)
            {
                interactables = GetComponentsInChildren<SecretBaseInteractable>(true);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Tick(float dt, float horizontalAxis)
        {
            if (cameraRig == null)
            {
                return;
            }

            cameraRig.Tick(dt, horizontalAxis);
            cameraRig.ApplyParallax(parallaxLayers);
        }
    }
}
