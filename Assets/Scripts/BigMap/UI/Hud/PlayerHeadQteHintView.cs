using TMPro;
using UnityEngine;

namespace My.UI
{
    // 玩家头顶 QTE 提示：布局与控件必须在 Prefab 中拼装（见项目 basic 规则：Resources/UI/Prefabs），本脚本只做跟随与显隐。
    public class PlayerHeadQteHintView : MonoBehaviour
    {
        public static PlayerHeadQteHintView Instance { get; private set; }

        [Header("Prefab 内拖拽：根节点上建议挂本脚本，anchorRect 为随 RootCanvas 移动的锚点 RectTransform")]
        public RectTransform anchorRect;
        public CanvasGroup panelCanvasGroup;
        public TextMeshProUGUI promptText;
        public TextMeshProUGUI keyHintText;

        [Header("锚点：相对跟随 Transform 的世界偏移")]
        public float headWorldOffsetY = 1.12f;
        public Vector3 extraWorldOffset;

        Transform _follow;
        bool _visible;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            if (anchorRect == null)
            {
                anchorRect = transform as RectTransform;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void LateUpdate()
        {
            if (!_visible || anchorRect == null)
            {
                return;
            }

            RefreshPosition();
        }

        public static void Show(string prompt, string keyDisplay, Transform follow = null)
        {
            if (!TryResolveInstance(out var v))
            {
                return;
            }

            v.ShowInternal(prompt, keyDisplay, follow);
        }

        public static void Show(string prompt, KeyCode key, Transform follow = null)
        {
            Show(prompt, KeyToReadable(key), follow);
        }

        public static void Hide()
        {
            if (Instance == null)
            {
                return;
            }

            Instance.HideInternal();
        }

        static bool TryResolveInstance(out PlayerHeadQteHintView v)
        {
            if (Instance != null && Instance.anchorRect != null)
            {
                v = Instance;
                return true;
            }

            var found = FindObjectOfType<PlayerHeadQteHintView>(true);
            if (found != null)
            {
                if (found.anchorRect == null)
                {
                    found.anchorRect = found.transform as RectTransform;
                }

                Instance = found;
                v = found;
                return v.anchorRect != null;
            }

            Debug.LogWarning(
                "[PlayerHeadQteHintView] No instance in scene. Add Assets/Resources/UI/Prefabs/PlayerHeadQteHint (or your prefab) under RootCanvas / HUD and ensure PlayerHeadQteHintView references are wired. Dynamic UI generation was removed per project UI rules.");
            v = null;
            return false;
        }

        void ShowInternal(string prompt, string keyDisplay, Transform follow)
        {
            if (anchorRect == null)
            {
                return;
            }

            _follow = follow;
            _visible = true;

            if (promptText != null)
            {
                promptText.text = prompt ?? string.Empty;
            }

            if (keyHintText != null)
            {
                bool hasKey = !string.IsNullOrEmpty(keyDisplay);
                keyHintText.gameObject.SetActive(hasKey);
                if (hasKey)
                {
                    keyHintText.text = keyDisplay;
                }
            }

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1f;
            }

            gameObject.SetActive(true);
            RefreshPosition();
        }

        void HideInternal()
        {
            _visible = false;
            _follow = null;
            gameObject.SetActive(false);
        }

        void RefreshPosition()
        {
            Transform follow = _follow;
            if (follow == null && My.MainGameManager.Instance != null &&
                My.MainGameManager.Instance.playerScenePresenter != null)
            {
                follow = My.MainGameManager.Instance.playerScenePresenter.transform;
            }

            if (follow == null)
            {
                return;
            }

            Camera mainCam = Camera.main;
            Canvas rootCanvas = UIManager.Instance != null ? UIManager.Instance.RootCanvas : null;
            if (mainCam == null || rootCanvas == null)
            {
                return;
            }

            Vector3 world = follow.position + new Vector3(0f, headWorldOffsetY, 0f) + extraWorldOffset;
            Vector3 sp = mainCam.WorldToScreenPoint(world);
            if (sp.z < 0f)
            {
                HideInternal();
                return;
            }

            RectTransform parentRect = rootCanvas.transform as RectTransform;
            Camera uiCam = UIManager.Instance.UICamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    new Vector2(sp.x, sp.y),
                    uiCam,
                    out Vector2 local))
            {
                return;
            }

            anchorRect.localPosition = new Vector3(local.x, local.y, 0f);
        }

        static string KeyToReadable(KeyCode key)
        {
            return key switch
            {
                KeyCode.Space => "Space",
                KeyCode.Return => "Enter",
                KeyCode.KeypadEnter => "Enter",
                KeyCode.LeftControl => "LCtrl",
                KeyCode.RightControl => "RCtrl",
                KeyCode.LeftShift => "LShift",
                KeyCode.RightShift => "RShift",
                KeyCode.Mouse0 => "LMB",
                KeyCode.Mouse1 => "RMB",
                KeyCode.Mouse2 => "MMB",
                _ => key.ToString(),
            };
        }
    }
}
