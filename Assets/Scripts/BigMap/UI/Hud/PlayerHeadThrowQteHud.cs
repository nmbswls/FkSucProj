using My;
using My.Map;
using My.Map.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace My.UI
{
    public sealed class PlayerHeadThrowQteSessionData
    {
        public ThrowContext ThrowCtx;
        public string Prompt;
        public Transform Follow;
    }

    // 投技头顶时段输入提示：表现（跟随、圆环）与输入判定；生命周期由 UIManager + PanelBase 管理
    [DefaultExecutionOrder(500)]
    public sealed class PlayerHeadThrowQteHud : PanelBase
    {
        public const string PanelIdConst = "PlayerHeadThrowQteHud";

        public static PlayerHeadThrowQteHud Instance
        {
            get
            {
                var panel = UIManager.Instance?.GetShowingPanel(PanelIdConst);
                return panel as PlayerHeadThrowQteHud;
            }
        }

        [Header("Layout")]
        [SerializeField] private RectTransform anchorRect;
        [SerializeField] private RectTransform shrinkingRing;
        [SerializeField] private RectTransform targetRing;
        [SerializeField] private TextMeshProUGUI promptText;

        [Header("Follow")]
        [SerializeField] private float headWorldOffsetY = 1.12f;
        [SerializeField] private Vector3 extraWorldOffset;

        [Header("Ring scale (normalized time 0 -> 1)")]
        [SerializeField] private float ringScaleStart = 1f;
        [SerializeField] private float ringScaleEnd = 0.26f;

        ThrowContext _throwCtx;
        TimelineHoldSession _session;
        Transform _follow;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = PanelIdConst;
            }

            layer = UILayer.HUD;

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (anchorRect == null)
            {
                anchorRect = transform as RectTransform;
            }
        }

        public static void ShowSession(ThrowContext throwCtx, string prompt, Transform follow)
        {
            if (throwCtx?.ActiveHold == null || throwCtx.ActiveHold.Resolved)
            {
                return;
            }

            if (UIManager.Instance == null)
            {
                Debug.LogError("[PlayerHeadThrowQteHud] UIManager not ready.");
                return;
            }

            UIManager.Instance.ShowPanel(
                PanelIdConst,
                new PlayerHeadThrowQteSessionData
                {
                    ThrowCtx = throwCtx,
                    Prompt = prompt,
                    Follow = follow,
                });
        }


        public override void Setup(object data = null)
        {
            if (data is PlayerHeadThrowQteSessionData session)
            {
                ApplySession(session.ThrowCtx, session.Prompt, session.Follow);
            }
            else
            {
                ClearSession();
            }
        }

        public override void Show()
        {
            base.Show();

            if (_throwCtx?.ActiveHold != null)
            {
                _throwCtx.ActiveHold.LastSampledNormalizedProgress = _throwCtx.ActiveHold.GetNormalizedProgress();
                UpdateRingVisuals();
                RefreshFollowPosition();
            }
        }

        public override void Hide()
        {
            ClearSession();
            base.Hide();
        }

        public override void Teardown()
        {
            ClearSession();
            base.Teardown();
        }

        void ApplySession(ThrowContext throwCtx, string prompt, Transform follow)
        {
            _throwCtx = throwCtx;
            _session = throwCtx.ActiveHold;
            _follow = follow;

            if (promptText != null)
            {
                promptText.text = prompt ?? string.Empty;
            }

            if (shrinkingRing != null)
            {
                float s0 = ringScaleStart;
                shrinkingRing.localScale = new Vector3(s0, s0, 1f);
            }
        }

        void ClearSession()
        {
            _throwCtx = null;
            _session = null;
            _follow = null;
        }

        void Update()
        {
            if (!IsVisible || _session == null || _throwCtx == null)
            {
                return;
            }

            if (_throwCtx.ActiveHold != _session || _session.Resolved)
            {
                UIManager.Instance?.HidePanel(PanelIdConst);
                return;
            }

            TickHoldInput();
        }

        void LateUpdate()
        {
            if (!IsVisible || _session == null || _throwCtx == null)
            {
                return;
            }

            if (_throwCtx.ActiveHold != _session || _session.Resolved)
            {
                UIManager.Instance?.HidePanel(PanelIdConst);
                return;
            }

            RefreshFollowPosition();
            UpdateRingVisuals();
        }

        void TickHoldInput()
        {
            var q = _session;
            float pNow = q.GetNormalizedProgress();

            if (PollConfirmSpaceDown())
            {
                bool inWin = q.IsInHitWindow(pNow)
                             || q.IsInHitWindow(q.LastSampledNormalizedProgress)
                             || q.SegmentIntersectsHitWindow(q.LastSampledNormalizedProgress, pNow);
                _throwCtx.CompleteActiveHold(inWin);
                UIManager.Instance?.HidePanel(PanelIdConst);
                return;
            }

            if (LogicTime.time >= q.TimeoutAtLogicTime)
            {
                _throwCtx.CompleteActiveHold(false);
                UIManager.Instance?.HidePanel(PanelIdConst);
                return;
            }

            q.LastSampledNormalizedProgress = pNow;
        }

        static bool PollConfirmSpaceDown()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }

            return UnityEngine.Input.GetKeyDown(KeyCode.Space);
        }

        void UpdateRingVisuals()
        {
            if (_session == null || shrinkingRing == null || targetRing == null)
            {
                return;
            }

            float p = _session.GetNormalizedProgress();
            float ringCenterN = TimelineHoldSession.HitWindowCenterNormalized;
            float sShrink = Mathf.Lerp(ringScaleStart, ringScaleEnd, p);
            float sTarget = Mathf.Lerp(ringScaleStart, ringScaleEnd, ringCenterN);
            shrinkingRing.localScale = new Vector3(sShrink, sShrink, 1f);
            targetRing.localScale = new Vector3(sTarget, sTarget, 1f);
        }

        void RefreshFollowPosition()
        {
            var ui = UIManager.Instance;
            if (ui == null || anchorRect == null)
            {
                return;
            }

            Transform follow = _follow;
            if (follow == null && MainGameManager.Instance != null && MainGameManager.Instance.playerScenePresenter != null)
            {
                follow = MainGameManager.Instance.playerScenePresenter.transform;
            }

            if (follow == null)
            {
                return;
            }

            Camera mainCam = Camera.main;
            if (mainCam == null || ui.RootCanvas == null)
            {
                return;
            }

            Vector3 world = follow.position + new Vector3(0f, headWorldOffsetY, 0f) + extraWorldOffset;
            Vector3 sp = mainCam.WorldToScreenPoint(world);
            if (sp.z < 0f)
            {
                UIManager.Instance?.HidePanel(PanelIdConst);
                return;
            }

            RectTransform parentRect = anchorRect.parent as RectTransform;
            if (parentRect == null)
            {
                parentRect = ui.RootCanvas.transform as RectTransform;
            }

            Camera cam = ui.UICamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    new Vector2(sp.x, sp.y),
                    cam,
                    out Vector2 local))
            {
                return;
            }

            anchorRect.localPosition = new Vector3(local.x, local.y, 0f);
        }
    }
}
