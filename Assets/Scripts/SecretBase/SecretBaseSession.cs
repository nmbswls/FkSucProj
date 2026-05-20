using System.Collections;
using My;
using UnityEngine;

namespace My.SecretBase
{
    public class SecretBaseSession
    {
        GameLogicManager _logic;
        bool _running;
        Coroutine _startCo;

        public bool IsRunning => _running;

        public void Bind(GameLogicManager logic)
        {
            _logic = logic;
        }

        public void OnWorldSceneLoaded()
        {
            if (_running)
            {
                return;
            }

            var host = MainGameManager.Instance;
            if (host == null)
            {
                TryStartWithResolvedRoot();
                return;
            }

            if (_startCo != null)
            {
                host.StopCoroutine(_startCo);
            }

            _startCo = host.StartCoroutine(CoStartWhenSceneReady());
        }

        IEnumerator CoStartWhenSceneReady()
        {
            SecretBaseSceneRoot root = null;
            for (int i = 0; i < 30; i++)
            {
                root = SecretBaseSceneRoot.FindInLoadedScenes();
                if (root != null)
                {
                    break;
                }

                yield return null;
            }

            _startCo = null;
            TryStartWithResolvedRoot(root);
        }

        void TryStartWithResolvedRoot(SecretBaseSceneRoot root = null)
        {
            if (_running)
            {
                return;
            }

            root ??= SecretBaseSceneRoot.FindInLoadedScenes();
            if (root == null)
            {
                Debug.LogError("SecretBaseSession: SecretBaseSceneRoot missing in Main_SecretBase scene.");
                return;
            }

            _running = true;
            root.CameraRig?.BindForSecretBase();
            My.UI.SecretBaseHudPanel.TryShow();
        }

        public void Shutdown()
        {
            if (!_running)
            {
                return;
            }

            _running = false;

            if (_startCo != null && MainGameManager.Instance != null)
            {
                MainGameManager.Instance.StopCoroutine(_startCo);
                _startCo = null;
            }

            var root = SecretBaseSceneRoot.FindInLoadedScenes();
            root?.InteractionHandler?.ClearHover();
            root?.CameraRig?.UnbindFromSecretBase();
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

            var root = SecretBaseSceneRoot.FindInLoadedScenes();
            root?.Tick(dt, axis);

            if (root == null)
            {
                return;
            }

            if (SecretBaseSceneRoot.ScreenToWorldPoint2D(UnityEngine.Input.mousePosition, out var worldPos))
            {
                root.TickInteraction(worldPos, false);
            }
        }

        public void OnScreenPointer(Vector2 screenPos, bool clickDown)
        {
            if (!_running)
            {
                return;
            }

            var root = SecretBaseSceneRoot.FindInLoadedScenes();
            if (root == null)
            {
                return;
            }

            if (!SecretBaseSceneRoot.ScreenToWorldPoint2D(screenPos, out var worldPos))
            {
                return;
            }

            root.TickInteraction(worldPos, clickDown);
        }

        public void RequestExit()
        {
            _logic?.ExitSecretBase();
        }
    }
}
