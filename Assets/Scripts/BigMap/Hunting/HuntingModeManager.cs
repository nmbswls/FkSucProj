using My;
using My.Map.Entity;
using My.Map.Scene;
using My.UI;
using UnityEngine;

namespace My.Map.Hunting
{
    /// <summary>
    /// 狩猎模式：按住 Ctrl 维持，松开退出；鼠标悬浮 NPC 详情与处决。
    /// </summary>
    public class HuntingModeManager : MonoBehaviour
    {
        public const float HuntTimeScale = 0.1f;
        public const float ExecuteMaxDistance = 3f;

        public static HuntingModeManager Instance { get; private set; }

        public bool Active { get; private set; }

        public SceneNpcPresenter HoverNpc => _hoverNpc;

        private SceneNpcPresenter _hoverNpc;
        private bool _blockReenterUntilHViewRelease;
        private float _timeScaleBeforeHunt = 1f;
        private Collider2D[] _rayHits = new Collider2D[8];
        private int _mapTargetLayer = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _mapTargetLayer = LayerMask.NameToLayer("MapTarget");
        }

        private void OnDestroy()
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

        private void Update()
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
            GetDetailView()?.SetActiveRoot(true);
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

        private void ForceExitInternal()
        {
            Active = false;
            LogicTime.timeScale = _timeScaleBeforeHunt > 0f ? _timeScaleBeforeHunt : 1f;

            ApplyHunterVisuals(false);
            _hoverNpc = null;
            GetDetailView()?.Clear();
            GetDetailView()?.SetActiveRoot(false);
        }

        private void ApplyHunterVisuals(bool on)
        {
            HuntingHudPanel.Instance?.SetHunterModeState(on);

            if (SceneVolumnManager.Instance != null)
            {
                SceneVolumnManager.Instance.EnterHuntingMode(on);
            }

            URPFeatureController.Instance?.SetHuntingDistortionEffect(on);
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

        public static bool CanExecuteTarget(SceneNpcPresenter npc, float maxDistanceFromPlayer = ExecuteMaxDistance)
        {
            if (!IsValidHoverNpc(npc))
            {
                return false;
            }

            if (!npc.NpcEntity.CheckCanExecute())
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

        public bool TryExecuteHoveredTarget()
        {
            if (!Active || _hoverNpc == null)
            {
                return false;
            }

            if (!CanExecuteTarget(_hoverNpc))
            {
                return false;
            }

            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (player == null)
            {
                return false;
            }

            player.ablilityManager.UseSkill("h_mode_execute", target: _hoverNpc.NpcEntity);

            _blockReenterUntilHViewRelease = true;
            ForceExitInternal();
            return true;
        }

        private void TickHoverTarget()
        {
            var next = RaycastNpcUnderMouse();
            if (next == _hoverNpc)
            {
                GetDetailView()?.RefreshLayout();
                return;
            }

            _hoverNpc = next;
            var view = GetDetailView();
            if (_hoverNpc == null)
            {
                view?.Clear();
            }
            else
            {
                view?.SetTarget(_hoverNpc, CanExecuteTarget(_hoverNpc));
            }
        }

        private SceneNpcPresenter RaycastNpcUnderMouse()
        {
            var binder = MainGameManager.Instance?.inputBinder;
            var cam = Camera.main;
            if (binder == null || cam == null || _mapTargetLayer < 0)
            {
                return null;
            }

            var playerPresenter = MainGameManager.Instance.playerScenePresenter;
            float z = playerPresenter != null ? playerPresenter.transform.position.z : 0f;
            Vector3 screen = binder.LastPos;
            screen.z = Mathf.Abs(cam.transform.position.z - z);
            Vector2 world = cam.ScreenToWorldPoint(screen);

            int count = Physics2D.OverlapPointNonAlloc(world, _rayHits, 1 << _mapTargetLayer);
            SceneNpcPresenter best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = _rayHits[i];
                if (col == null)
                {
                    continue;
                }

                var npc = col.GetComponentInParent<SceneNpcPresenter>();
                if (!IsValidHoverNpc(npc))
                {
                    continue;
                }

                float d = Vector2.SqrMagnitude((Vector2)col.transform.position - world);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = npc;
                }
            }

            return best;
        }

        private HuntingNpcDetailView GetDetailView()
        {
            return HuntingHudPanel.Instance != null
                ? HuntingHudPanel.Instance.NpcDetail
                : null;
        }
    }
}
