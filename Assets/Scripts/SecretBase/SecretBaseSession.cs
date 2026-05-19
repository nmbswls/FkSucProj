using My;
using UnityEngine;

namespace My.SecretBase
{
    public class SecretBaseSession
    {
        GameLogicManager _logic;
        bool _running;

        public bool IsRunning => _running;

        public void Bind(GameLogicManager logic)
        {
            _logic = logic;
        }

        public void OnWorldSceneLoaded()
        {
            _running = true;
            EnsureSceneRootExists();
            My.UI.SecretBaseHudPanel.TryShow();
        }

        static void EnsureSceneRootExists()
        {
            if (SecretBaseSceneRoot.Instance != null)
            {
                return;
            }

            var go = new GameObject("SecretBaseSceneRoot");
            go.AddComponent<SecretBaseSceneRoot>();
            go.AddComponent<SecretBaseCameraRig>();
            Debug.LogWarning("SecretBaseSession: auto-created SecretBaseSceneRoot (add one in scene for parallax/interactables).");
        }

        public void Shutdown()
        {
            if (!_running)
            {
                return;
            }

            _running = false;
            My.UI.SecretBaseHudPanel.TryHide();
        }

        public void Tick(float dt)
        {
            if (!_running)
            {
                return;
            }

            float axis = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
            {
                axis -= 1f;
            }

            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
            {
                axis += 1f;
            }

            SecretBaseSceneRoot.Instance?.Tick(dt, axis);

            if (UnityEngine.Input.GetMouseButtonDown(0) && Camera.main != null)
            {
                var w = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
                TryClickInteractable(w);
            }
        }

        void TryClickInteractable(Vector3 worldPos)
        {
            var root = SecretBaseSceneRoot.Instance;
            if (root == null)
            {
                return;
            }

            var hits = Object.FindObjectsOfType<SecretBaseInteractable>(false);
            foreach (var h in hits)
            {
                if (h.ContainsWorldPoint(worldPos))
                {
                    h.TryOpenPanel();
                    return;
                }
            }
        }

        public void RequestExit()
        {
            _logic?.ExitSecretBase();
        }
    }
}
