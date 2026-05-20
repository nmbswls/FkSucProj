using Cinemachine;
using My;
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

        SecretBaseInteractionHandler _interactionHandler;

        public SecretBaseCameraRig CameraRig => cameraRig;
        public Transform FurnitureRoot => furnitureRoot != null ? furnitureRoot : transform;
        public SecretBaseInteractionHandler InteractionHandler => _interactionHandler;

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

            _interactionHandler = new SecretBaseInteractionHandler(interactables);
        }

        void OnDestroy()
        {
            _interactionHandler?.ClearHover();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static SecretBaseSceneRoot FindInLoadedScenes()
        {
            if (Instance != null)
            {
                return Instance;
            }

            return Object.FindObjectOfType<SecretBaseSceneRoot>(true);
        }

        // 优先 Cinemachine 输出相机，与大地图一致。
        public static Camera ResolveViewCamera()
        {
            var mgr = MainGameManager.Instance;
            if (mgr != null)
            {
                if (mgr.CineBrain != null && mgr.CineBrain.OutputCamera != null)
                {
                    return mgr.CineBrain.OutputCamera;
                }

                if (mgr.MainMapVCam != null)
                {
                    var brain = mgr.MainMapVCam.GetComponent<CinemachineBrain>();
                    if (brain != null && brain.OutputCamera != null)
                    {
                        return brain.OutputCamera;
                    }
                }
            }

            return Camera.main;
        }

        public static bool ScreenToWorldPoint2D(Vector2 screenPos, out Vector2 world, float worldZ = 0f)
        {
            var cam = ResolveViewCamera();
            if (cam == null)
            {
                world = default;
                return false;
            }

            var p = new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z - worldZ));
            world = cam.ScreenToWorldPoint(p);
            return true;
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

        public void TickInteraction(Vector2 worldPos, bool clickDown)
        {
            if (_interactionHandler == null)
            {
                return;
            }

            _interactionHandler.TickHover(worldPos);
            if (clickDown)
            {
                _interactionHandler.TryClick(worldPos);
            }
        }
    }
}
