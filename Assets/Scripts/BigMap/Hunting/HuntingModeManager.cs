using My;
using My.Map.Entity;
using My.Map.Scene;
using My.UI;
using UnityEngine;

namespace My.Map.Hunting
{
    /// <summary>
    /// 狩猎模式：按住 Ctrl 维持，松开退出；hover 预览与 pin 锁定分轨。
    /// </summary>
    public class HuntingModeManager : MonoBehaviour
    {
        public const float HuntTimeScale = 0.1f;
        public const float ExecuteMaxDistance = 3f;

        const string HuntHoverReason = "HuntHover";
        const string HuntPinnedReason = "HuntPinned";

        public static HuntingModeManager Instance { get; private set; }

        [SerializeField]
        float pickRadiusPx = 64f;

        public bool Active { get; private set; }

        public SceneNpcPresenter HoverNpc => _hoverNpc;

        public SceneNpcPresenter PinnedNpc => _pinnedNpc;

        public bool HasPinnedTarget => _pinnedNpc != null;

        SceneNpcPresenter _hoverNpc;
        SceneNpcPresenter _pinnedNpc;
        bool _blockReenterUntilHViewRelease;
        float _timeScaleBeforeHunt = 1f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (Active)
            {
                ForceExitInternal();
            }
        }

        void Update()
        {
            if (!Active)
            {
                return;
            }

            TickHoverTarget();
        }

        public bool CanEnter()
        {
            if (_blockReenterUntilHViewRelease)
            {
                return false;
            }

            if (MainGameManager.Instance == null)
            {
                return false;
            }

            if (MainGameManager.Instance.dialoguePlayer != null && MainGameManager.Instance.dialoguePlayer.IsPlaying)
            {
                return false;
            }

            var glm = MainGameManager.Instance.gameLogicManager;
            if (glm == null || glm.MainStage != GameLogicManager.EMainGameStage.Running)
            {
                return false;
            }

            if (OverworldHUDPanel.Instance == null || OverworldHUDPanel.Instance.HudMode != OverworldHUDPanel.EHudMode.Normal)
            {
                return false;
            }

            return true;
        }

        public void Enter()
        {
            if (Active || !CanEnter())
            {
                return;
            }

            Active = true;
            _timeScaleBeforeHunt = LogicTime.timeScale;
            LogicTime.timeScale = HuntTimeScale;

            ApplyHunterVisuals(true);
            ClearAllTargets();
        }

        public void Exit()
        {
            if (!Active)
            {
                return;
            }

            ForceExitInternal();
        }

        public void NotifyHViewCanceledAfterExit()
        {
            _blockReenterUntilHViewRelease = false;
        }

        public void ClearPinnedTarget()
        {
            if (_pinnedNpc == null)
            {
                GetActionRadial()?.Close();
                return;
            }

            SetNpcHighlight(_pinnedNpc, false, HuntPinnedReason);
            _pinnedNpc = null;
            GetActionRadial()?.Close();
            RefreshDetailAfterPinChange();
        }

        public bool TryToggleActionMenu()
        {
            if (!Active)
            {
                return false;
            }

            var radial = GetActionRadial();
            if (radial != null && radial.IsOpen && _pinnedNpc != null)
            {
                var picked = PickNpcUnderCursor();
                if (picked != null && picked == _pinnedNpc)
                {
                    ClearPinnedTarget();
                    return true;
                }

                return false;
            }

            var target = PickNpcUnderCursor();
            if (target == null)
            {
                if (HasPinnedTarget)
                {
                    ClearPinnedTarget();
                    return true;
                }

                return false;
            }

            if (!IsValidHoverNpc(target))
            {
                return false;
            }

            if (radial == null)
            {
                return false;
            }

            PinTarget(target);
            radial.Show(
                _pinnedNpc,
                CanExecuteTarget(_pinnedNpc),
                CanControlTarget(_pinnedNpc));
            return true;
        }

        public bool TryExecuteHoveredTarget()
        {
            if (!Active)
            {
                return false;
            }

            var target = _pinnedNpc != null ? _pinnedNpc : _hoverNpc;
            if (target == null)
            {
                return false;
            }

            return TryExecuteTarget(target);
        }

        public bool TryExecuteTarget(SceneNpcPresenter target)
        {
            if (!CanExecuteTarget(target))
            {
                return false;
            }

            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (player == null)
            {
                return false;
            }

            player.ablilityManager.UseSkill("h_mode_execute", target: target.NpcEntity);

            _blockReenterUntilHViewRelease = true;
            ForceExitInternal();
            return true;
        }

        public bool TryControlTarget(SceneNpcPresenter target)
        {
            if (!CanControlTarget(target))
            {
                return false;
            }

            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (player == null)
            {
                return false;
            }

            player.ablilityManager.UseSkill("h_mode_control", target: target.NpcEntity);

            _blockReenterUntilHViewRelease = true;
            ForceExitInternal();
            return true;
        }

        public static bool IsValidHoverNpc(SceneNpcPresenter npc)
        {
            if (npc == null || npc.UnitEntity == null)
            {
                return false;
            }

            if (npc.UnitEntity.IsDead || npc.UnitEntity.MarkUnsensored)
            {
                return false;
            }

            if (npc.NpcEntity.IsAttaching)
            {
                return false;
            }

            if (npc.NpcEntity.CheckHasState(AttrIdConsts.NoSelect))
            {
                return false;
            }

            return true;
        }

        public static bool IsWithinActionDistance(SceneNpcPresenter npc, float maxDistanceFromPlayer = ExecuteMaxDistance)
        {
            if (npc == null)
            {
                return false;
            }

            var playerPresenter = MainGameManager.Instance?.playerScenePresenter;
            if (playerPresenter == null)
            {
                return false;
            }

            float dist = Vector2.Distance(playerPresenter.transform.position, npc.transform.position);
            return dist <= maxDistanceFromPlayer;
        }

        public static bool CanExecuteTarget(SceneNpcPresenter npc, float maxDistanceFromPlayer = ExecuteMaxDistance)
        {
            if (!IsValidHoverNpc(npc))
            {
                return false;
            }

            if (!IsWithinActionDistance(npc, maxDistanceFromPlayer))
            {
                return false;
            }

            if (!npc.NpcEntity.CheckCanExecute())
            {
                return false;
            }

            return true;
        }

        public static bool CanControlTarget(SceneNpcPresenter npc, float maxDistanceFromPlayer = ExecuteMaxDistance)
        {
            if (!IsValidHoverNpc(npc))
            {
                return false;
            }

            if (!IsWithinActionDistance(npc, maxDistanceFromPlayer))
            {
                return false;
            }

            return npc.NpcEntity.CanAcceptDirectControl(My.Player.GamePlayerIds.Local);
        }

        void ForceExitInternal()
        {
            Active = false;
            LogicTime.timeScale = _timeScaleBeforeHunt > 0f ? _timeScaleBeforeHunt : 1f;

            ApplyHunterVisuals(false);
            ClearAllTargets();
        }

        void ClearAllTargets()
        {
            ClearHoverHighlight();
            if (_pinnedNpc != null)
            {
                SetNpcHighlight(_pinnedNpc, false, HuntPinnedReason);
                _pinnedNpc = null;
            }

            _hoverNpc = null;
            GetDetailView()?.Clear();
            GetActionRadial()?.Close();
        }

        void PinTarget(SceneNpcPresenter npc)
        {
            if (_pinnedNpc == npc)
            {
                return;
            }

            if (_pinnedNpc != null)
            {
                SetNpcHighlight(_pinnedNpc, false, HuntPinnedReason);
            }

            ClearHoverHighlight();
            _hoverNpc = null;
            _pinnedNpc = npc;
            SetNpcHighlight(_pinnedNpc, true, HuntPinnedReason);

            var detailView = GetDetailView();
            if (detailView != null)
            {
                detailView.SetTarget(
                    _pinnedNpc,
                    HuntingNpcDetailView.EDetailMode.Pinned,
                    CanExecuteTarget(_pinnedNpc),
                    CanControlTarget(_pinnedNpc));
            }
        }

        void TickHoverTarget()
        {
            var radial = GetActionRadial();
            bool menuOpen = radial != null && radial.IsOpen;

            if (_pinnedNpc != null && menuOpen)
            {
                RefreshPinnedPresentation();
                return;
            }

            if (IsHuntingOperateUiBlockingHover(GetCursorScreenPos()))
            {
                if (_hoverNpc != null)
                {
                    var view = GetDetailView();
                    view?.RefreshLayout();
                }

                radial?.RefreshLayoutIfOpen();
                return;
            }

            var next = PickNpcUnderCursor();
            if (next == _hoverNpc)
            {
                if (_hoverNpc != null)
                {
                    GetDetailView()?.RefreshLayout();
                    radial?.RefreshLayoutIfOpen();
                }

                return;
            }

            var prev = _hoverNpc;
            _hoverNpc = next;
            ApplyHoverHighlight(prev, _hoverNpc);

            var detailView = GetDetailView();
            if (_hoverNpc == null)
            {
                detailView?.Clear();
            }
            else
            {
                detailView?.SetTarget(
                    _hoverNpc,
                    HuntingNpcDetailView.EDetailMode.Preview,
                    CanExecuteTarget(_hoverNpc),
                    CanControlTarget(_hoverNpc));
            }
        }

        void RefreshPinnedPresentation()
        {
            if (_pinnedNpc == null || !IsValidHoverNpc(_pinnedNpc))
            {
                ClearPinnedTarget();
                return;
            }

            var detailView = GetDetailView();
            detailView?.SetTarget(
                _pinnedNpc,
                HuntingNpcDetailView.EDetailMode.Pinned,
                CanExecuteTarget(_pinnedNpc),
                CanControlTarget(_pinnedNpc));
            detailView?.RefreshLayout();
            GetActionRadial()?.RefreshLayoutIfOpen();
        }

        void RefreshDetailAfterPinChange()
        {
            var detailView = GetDetailView();
            if (_hoverNpc != null)
            {
                detailView?.SetTarget(
                    _hoverNpc,
                    HuntingNpcDetailView.EDetailMode.Preview,
                    CanExecuteTarget(_hoverNpc),
                    CanControlTarget(_hoverNpc));
            }
            else
            {
                detailView?.Clear();
            }
        }

        void ApplyHoverHighlight(SceneNpcPresenter prev, SceneNpcPresenter next)
        {
            if (prev != null && prev != _pinnedNpc)
            {
                SetNpcHighlight(prev, false, HuntHoverReason);
            }

            if (next != null && next != _pinnedNpc)
            {
                SetNpcHighlight(next, true, HuntHoverReason);
            }
        }

        void ClearHoverHighlight()
        {
            if (_hoverNpc != null && _hoverNpc != _pinnedNpc)
            {
                SetNpcHighlight(_hoverNpc, false, HuntHoverReason);
            }
        }

        static void SetNpcHighlight(SceneNpcPresenter npc, bool on, string reason)
        {
            npc?.highlightCtrl?.SetHighlightStatus(on, reason);
        }

        bool IsHuntingOperateUiBlockingHover(Vector2 screenPos)
        {
            var radial = GetActionRadial();
            if (radial != null && radial.IsOpen && radial.ContainsScreenPoint(screenPos))
            {
                return true;
            }

            var detail = GetDetailView();
            if (detail != null && detail.IsPinnedVisible && detail.ContainsScreenPoint(screenPos))
            {
                return true;
            }

            return false;
        }

        Vector2 GetCursorScreenPos()
        {
            var binder = MainGameManager.Instance?.inputBinder;
            return binder != null ? binder.LastPos : (Vector2)UnityEngine.Input.mousePosition;
        }

        SceneNpcPresenter PickNpcUnderCursor()
        {
            if (IsHuntingOperateUiBlockingHover(GetCursorScreenPos()))
            {
                return null;
            }

            var binder = MainGameManager.Instance?.inputBinder;
            var cam = Camera.main;
            var aoi = SceneAOIManager.Instance;
            if (binder == null || cam == null || aoi == null)
            {
                return null;
            }

            Vector2 mouseScreen = binder.LastPos;
            SceneNpcPresenter best = null;
            float bestScreenDist = float.MaxValue;
            float bestWorldDist = float.MaxValue;

            foreach (var presentation in aoi.GetAllActivePresentation())
            {
                if (presentation is not SceneNpcPresenter npc)
                {
                    continue;
                }

                if (!IsValidHoverNpc(npc))
                {
                    continue;
                }

                Vector3 anchor = npc.GetHintAnchorPosition();
                Vector3 screenPos3 = cam.WorldToScreenPoint(anchor);
                if (screenPos3.z <= 0f)
                {
                    continue;
                }

                var screenPos = new Vector2(screenPos3.x, screenPos3.y);
                float screenDist = Vector2.Distance(screenPos, mouseScreen);
                if (screenDist > pickRadiusPx)
                {
                    continue;
                }

                float worldDist = Vector2.Distance(
                    new Vector2(anchor.x, anchor.y),
                    cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, screenPos3.z)));

                if (screenDist < bestScreenDist
                    || (Mathf.Approximately(screenDist, bestScreenDist) && worldDist < bestWorldDist))
                {
                    bestScreenDist = screenDist;
                    bestWorldDist = worldDist;
                    best = npc;
                }
            }

            return best;
        }

        void ApplyHunterVisuals(bool on)
        {
            HuntingHudPanel.Instance?.SetHunterModeState(on);

            if (SceneVolumnManager.Instance != null)
            {
                SceneVolumnManager.Instance.EnterHuntingMode(on);
            }

            URPFeatureController.Instance?.SetHuntingDistortionEffect(on);
        }

        HuntingNpcDetailView GetDetailView()
        {
            return HuntingHudPanel.Instance != null
                ? HuntingHudPanel.Instance.NpcDetail
                : null;
        }

        HuntingNpcActionRadialMenu GetActionRadial()
        {
            return HuntingHudPanel.Instance != null
                ? HuntingHudPanel.Instance.ActionRadial
                : null;
        }
    }
}
