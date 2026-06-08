using System.Collections.Generic;
using My.Map.Scene;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.Map.Hunting
{
    public class HuntingNpcActionRadialMenu : MonoBehaviour
    {
        public enum EActionSlot
        {
            Execute,
            Control,
        }

        struct ActionSlotData
        {
            public EActionSlot Slot;
            public string Label;
            public bool Interactable;
        }

        public RectTransform MenuRoot;
        public Button ExecuteButton;
        public RectTransform OptionsContainer;
        public Button OptionButtonTemplate;

        public float OptionRadius = 90f;
        public float OptionStartAngleDeg = 90f;
        public float OptionAngleStepDeg = 60f;

        readonly List<Button> _optionButtons = new();
        SceneNpcPresenter _target;
        Camera _uiCam;

        public bool IsOpen { get; private set; }

#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        bool _editorPreviewActive;

        public bool IsEditorPreviewActive => _editorPreviewActive;
#endif

        void Awake()
        {
            EnsureSetup();

            if (Application.isPlaying)
            {
                if (ExecuteButton != null)
                {
                    ExecuteButton.onClick.AddListener(OnExecuteClicked);
                }

                SetOpen(false);
            }
#if UNITY_EDITOR
            else if (_editorPreviewActive)
            {
                RefreshEditorPreviewLayout();
            }
            else
            {
                SetOpen(false);
            }
#endif
        }

        void EnsureSetup()
        {
            if (MenuRoot == null)
            {
                MenuRoot = transform as RectTransform;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCam = canvas.worldCamera;
            }

            if (OptionButtonTemplate != null)
            {
                OptionButtonTemplate.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (ExecuteButton != null)
            {
                ExecuteButton.onClick.RemoveListener(OnExecuteClicked);
            }
        }

        public void Show(SceneNpcPresenter target, bool canExecute, bool canControl)
        {
            _target = target;

            if (ExecuteButton != null)
            {
                ExecuteButton.interactable = canExecute;
            }

            RebuildOptions(BuildDefaultOptions(canControl));
            RefreshLayout();
            SetOpen(true);
        }

        public void Close()
        {
            _target = null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _editorPreviewActive = false;
            }
#endif
            SetOpen(false);
            ClearOptions();
        }

#if UNITY_EDITOR
        // 编辑器内预览假菜单，便于调整 OptionRadius 等布局参数。
        public void ShowEditorPreview(bool canExecute = true, bool canControl = true)
        {
            _editorPreviewActive = true;
            EnsureSetup();
            _target = null;

            if (ExecuteButton != null)
            {
                ExecuteButton.interactable = canExecute;
            }

            RebuildOptions(BuildDefaultOptions(canControl));
            RefreshEditorPreviewLayout();
            SetOpen(true);

            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        public void HideEditorPreview()
        {
            _editorPreviewActive = false;
            Close();
        }

        public void RefreshEditorPreviewLayout()
        {
            if (!_editorPreviewActive || MenuRoot == null)
            {
                return;
            }

            MenuRoot.localPosition = Vector3.zero;
            RepositionAllOptionButtons();
        }

        void RepositionAllOptionButtons()
        {
            int count = _optionButtons.Count;
            float step = count <= 1 ? 0f : OptionAngleStepDeg;
            for (int i = 0; i < count; i++)
            {
                var btn = _optionButtons[i];
                if (btn != null)
                {
                    PlaceOptionButton(btn.transform as RectTransform, i, step);
                }
            }
        }
#endif

        static List<ActionSlotData> BuildDefaultOptions(bool canControl)
        {
            return new List<ActionSlotData>
            {
                new ActionSlotData
                {
                    Slot = EActionSlot.Control,
                    Label = "操控",
                    Interactable = canControl,
                },
            };
        }

        // 轮盘打开时由输入层调用：点击菜单外区域则关闭并消耗本次点击。
        public bool TryHandleClick(Vector2 screenPos)
        {
            if (!IsOpen)
            {
                return false;
            }

            if (ContainsScreenPoint(screenPos))
            {
                return true;
            }

            HuntingModeManager.Instance?.ClearPinnedTarget();
            return true;
        }

        public bool ContainsScreenPoint(Vector2 screenPos, float paddingPx = 10f)
        {
            if (!IsOpen)
            {
                return false;
            }

            var pad = Vector4.one * paddingPx;

            if (MenuRoot != null
                && RectTransformUtility.RectangleContainsScreenPoint(MenuRoot, screenPos, _uiCam, pad))
            {
                return true;
            }

            if (ExecuteButton != null
                && RectTransformUtility.RectangleContainsScreenPoint(
                    ExecuteButton.transform as RectTransform, screenPos, _uiCam, pad))
            {
                return true;
            }

            foreach (var btn in _optionButtons)
            {
                if (btn == null)
                {
                    continue;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(
                        btn.transform as RectTransform, screenPos, _uiCam, pad))
                {
                    return true;
                }
            }

            return false;
        }

        public void RefreshLayoutIfOpen()
        {
            if (IsOpen)
            {
                RefreshLayout();
            }
        }

        void OnExecuteClicked()
        {
            if (_target == null || ExecuteButton == null || !ExecuteButton.interactable)
            {
                return;
            }

            var manager = HuntingModeManager.Instance;
            if (manager == null)
            {
                return;
            }

            if (manager.TryExecuteTarget(_target))
            {
                Close();
            }
        }

        void OnOptionClicked(EActionSlot slot)
        {
            var manager = HuntingModeManager.Instance;
            if (manager == null || _target == null)
            {
                return;
            }

            bool handled = slot switch
            {
                EActionSlot.Control => manager.TryControlTarget(_target),
                _ => false,
            };

            if (handled)
            {
                Close();
            }
        }

        void RebuildOptions(List<ActionSlotData> options)
        {
            ClearOptions();
            if (OptionButtonTemplate == null || OptionsContainer == null)
            {
                return;
            }

            int count = options.Count;
            float step = count <= 1 ? 0f : OptionAngleStepDeg;

            for (int i = 0; i < count; i++)
            {
                var slot = options[i];
                var btn = Instantiate(OptionButtonTemplate, OptionsContainer);
                btn.gameObject.SetActive(true);
                btn.interactable = slot.Interactable;

                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = slot.Label;
                }

                PlaceOptionButton(btn.transform as RectTransform, i, step);

                var captured = slot.Slot;
                btn.onClick.AddListener(() => OnOptionClicked(captured));
                _optionButtons.Add(btn);
            }
        }

        void PlaceOptionButton(RectTransform rt, int index, float angleStepDeg)
        {
            if (rt == null)
            {
                return;
            }

            float angleDeg = OptionStartAngleDeg - index * angleStepDeg;
            float rad = angleDeg * Mathf.Deg2Rad;
            rt.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * OptionRadius;
        }

        void RefreshLayout()
        {
            if (_target == null || MenuRoot == null || UIManager.Instance == null)
            {
                return;
            }

            var hintPos = _target.GetHintAnchorPosition();
            var gameplayCam = Camera.main;
            if (gameplayCam == null)
            {
                return;
            }

            Vector3 screenPos = gameplayCam.WorldToScreenPoint(hintPos);
            var rootRt = UIManager.Instance.RootCanvas.transform as RectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootRt, screenPos, UIManager.Instance.UICamera, out Vector2 localInRoot))
            {
                return;
            }

            var positionParent = MenuRoot.parent as RectTransform;
            if (positionParent == null)
            {
                return;
            }

            Vector3 worldOnCanvas = rootRt.TransformPoint(new Vector3(localInRoot.x, localInRoot.y, 0f));
            MenuRoot.localPosition = positionParent.InverseTransformPoint(worldOnCanvas);
        }

        void SetOpen(bool open)
        {
            IsOpen = open;
            if (MenuRoot != null)
            {
                MenuRoot.gameObject.SetActive(open);
            }

            if (ExecuteButton != null)
            {
                ExecuteButton.gameObject.SetActive(true);
            }
        }

        void ClearOptions()
        {
            foreach (var btn in _optionButtons)
            {
                if (btn != null)
                {
                    if (Application.isPlaying)
                    {
                        btn.onClick.RemoveAllListeners();
                        Destroy(btn.gameObject);
                    }
#if UNITY_EDITOR
                    else
                    {
                        DestroyImmediate(btn.gameObject);
                    }
#endif
                }
            }

            _optionButtons.Clear();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!Application.isPlaying && _editorPreviewActive)
            {
                RefreshEditorPreviewLayout();
            }
        }
#endif
    }
}
