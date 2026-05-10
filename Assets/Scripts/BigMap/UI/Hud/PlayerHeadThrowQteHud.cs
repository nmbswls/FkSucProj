using My;
using My.Map;
using My.Map.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace My.UI
{
    // 投技头顶时段输入提示：表现（跟随、圆环）与输入判定在此；结算调用 ThrowContext.CompleteActiveHold
    [DefaultExecutionOrder(500)] // 晚于默认 LogicTimeManager(0)，保证本帧 LogicTime 已推进后再采样进度与输入
    public sealed class PlayerHeadThrowQteHud : MonoBehaviour
    {
        const string ResourcePath = "UI/Prefabs/PlayerHeadQteHint";

        static PlayerHeadThrowQteHud _instance;

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
            if (anchorRect == null)
            {
                anchorRect = transform as RectTransform;
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static void ShowSession(ThrowContext throwCtx, string prompt, Transform follow)
        {
            if (throwCtx?.ActiveHold == null || throwCtx.ActiveHold.Resolved)
            {
                return;
            }

            EnsureInstance();
            if (_instance == null)
            {
                return;
            }

            _instance.ApplySession(throwCtx, prompt, follow);
        }

        public static void Hide()
        {
            if (_instance == null)
            {
                return;
            }

            _instance.ClearSession();
            _instance.gameObject.SetActive(false);
        }

        static void EnsureInstance()
        {
            if (_instance != null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError("[ThrowHoldHud] Missing prefab at Resources/" + ResourcePath);
                return;
            }

            Transform parent = null;
            if (UIManager.Instance != null)
            {
                parent = UIManager.Instance.GetLayerRoot(UILayer.HUD);
            }

            var go = Instantiate(prefab, parent, false);
            _instance = go.GetComponent<PlayerHeadThrowQteHud>();
            if (_instance == null)
            {
                Debug.LogError("[ThrowHoldHud] Prefab root must have PlayerHeadThrowQteHud");
                Destroy(go);
            }
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

            gameObject.SetActive(true);
            // 与首帧 TickHoldInput 读取的区间一致，避免上一 session 残留 0 导致 Segment 判定异常
            throwCtx.ActiveHold.LastSampledNormalizedProgress = throwCtx.ActiveHold.GetNormalizedProgress();
            UpdateRingVisuals();
            RefreshFollowPosition();
        }

        void ClearSession()
        {
            _throwCtx = null;
            _session = null;
            _follow = null;
        }

        void Update()
        {
            if (_session == null || _throwCtx == null)
            {
                return;
            }

            if (_throwCtx.ActiveHold != _session || _session.Resolved)
            {
                ClearSession();
                gameObject.SetActive(false);
                return;
            }

            TickHoldInput();
        }

        void LateUpdate()
        {
            if (_session == null || _throwCtx == null)
            {
                return;
            }

            if (_throwCtx.ActiveHold != _session || _session.Resolved)
            {
                ClearSession();
                gameObject.SetActive(false);
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
                Hide();
                return;
            }

            if (LogicTime.time >= q.TimeoutAtLogicTime)
            {
                _throwCtx.CompleteActiveHold(false);
                Hide();
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

            // p：整段 Timeout 上的 0~1。收缩环 s(p)=Lerp(start,end,p)。目标环固定在「命中窗时间中点」的 s，故仅当 p==HitWindowCenterNormalized 时两环半径一致。
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
                ClearSession();
                gameObject.SetActive(false);
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
