using System.Collections;
using System.Collections.Generic;
using My;
using My.Map;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.Scene
{
    // 禁行区检测、对白衔接后的平滑拉回、逻辑暂停等（与主文件 partial 合并）
    public partial class PlayerScenePresenter
    {
        [Header("Forbid zone exit")]
        [Tooltip("false：进内圈立刻拉回 LastValidMovePos（旧行为）")]
        [SerializeField] private bool useSmoothForbidExit = true;
        [SerializeField] private float forbidExitMoveDuration = 0.45f;
        [Tooltip("等待对白命令期间暂停逻辑时间；拉回插值仍用 unscaled 时间")]
        [SerializeField] private bool pauseLogicDuringForbidExit;
        [Tooltip(">0：未收到对白命令则若干秒 real time 后自动开始拉回，防卡死")]
        [SerializeField] private float forbidCueFallbackRealtime;
        [SerializeField] private bool forbidExitAlwaysInstant;
        [SerializeField] private bool forbidExitInstantWhenEnemyAlerting;

        private bool _inForbidInnerZone;
        private readonly List<ForbidZoneChecker> _forbidCheckerScratch = new(8);

        private enum EForbidExitPhase
        {
            Idle,
            AwaitingDialogCue,
            Sliding,
        }

        private EForbidExitPhase _forbidExitPhase;
        private Coroutine _forbidSlideRoutine;
        private float _awaitCueElapsedRealtime;
        private bool _forbidExitLogicPauseHeld;

        private void OnDestroy()
        {
            CancelForbidExitPipeline();
        }

        // 对白命令 DialogCommandData4PlayerForbidZoneExitSlide：提示结束后开始平滑拉回
        public void DialogCommand_CompleteForbidZoneExitCue(float overrideMoveDuration = 0f)
        {
            if (_forbidExitPhase != EForbidExitPhase.AwaitingDialogCue)
                return;
            float dur = overrideMoveDuration > 0f ? overrideMoveDuration : forbidExitMoveDuration;
            StartForbidSlideInternal(dur);
        }

        public override bool CheckCanActiveMove()
        {
            if (_inForbidInnerZone)
                return false;
            return base.CheckCanActiveMove();
        }

        private static bool ForbidZoneCondEnabled(ForbidZoneChecker checker)
        {
            if (checker.EnableCondition == null || checker.EnableCondition.Count == 0)
                return true;
            var glm = MainGameManager.Instance.gameLogicManager;
            foreach (var cond in checker.EnableCondition)
            {
                if (!glm.CheckCommonCond(cond))
                    return false;
            }
            return true;
        }

        private void GatherForbidZonesAt(Vector2 worldPos)
        {
            _forbidCheckerScratch.Clear();
            int zn = Physics2D.OverlapPointNonAlloc(worldPos, zoneTriggerCache, 1 << LayerMask.NameToLayer("Zone"));
            for (int i = 0; i < zn; i++)
            {
                var col = zoneTriggerCache[i];
                if (col == null) continue;
                var checker = col.GetComponentInParent<ForbidZoneChecker>();
                if (checker == null) continue;
                if (!ForbidZoneCondEnabled(checker)) continue;
                if (_forbidCheckerScratch.Contains(checker)) continue;
                _forbidCheckerScratch.Add(checker);
            }
        }

        private void TickForbiddenAreaMove()
        {
            if (_forbidExitPhase == EForbidExitPhase.Sliding)
                return;

            Vector2 worldPos = rb != null ? rb.position : (Vector2)transform.position;
            GatherForbidZonesAt(worldPos);

            bool inInner = false;
            for (int i = 0; i < _forbidCheckerScratch.Count; i++)
            {
                var fz = _forbidCheckerScratch[i];
                if (fz.InnerCol != null && fz.InnerCol.OverlapPoint(worldPos))
                {
                    inInner = true;
                    break;
                }
            }

            bool inOuter = false;
            if (!inInner)
            {
                for (int i = 0; i < _forbidCheckerScratch.Count; i++)
                {
                    var fz = _forbidCheckerScratch[i];
                    if (fz.OuterCol != null && fz.OuterCol.OverlapPoint(worldPos))
                    {
                        inOuter = true;
                        break;
                    }
                }
            }

            if (!inInner && _forbidExitPhase == EForbidExitPhase.AwaitingDialogCue)
            {
                ReleaseForbidExitLogicPauseIfHeld();
                _forbidExitPhase = EForbidExitPhase.Idle;
                _awaitCueElapsedRealtime = 0f;
            }

            bool inExitFlow = _forbidExitPhase != EForbidExitPhase.Idle;
            _inForbidInnerZone = inInner || inExitFlow;

            if (_forbidExitPhase == EForbidExitPhase.AwaitingDialogCue)
            {
                if (forbidCueFallbackRealtime > 0f && LastValidMovePos.HasValue)
                {
                    _awaitCueElapsedRealtime += Time.unscaledDeltaTime;
                    if (_awaitCueElapsedRealtime >= forbidCueFallbackRealtime)
                        StartForbidSlideInternal(forbidExitMoveDuration);
                }
            }

            if (inInner)
            {
                if (LastValidMovePos.HasValue)
                {
                    if (!useSmoothForbidExit || ShouldUseInstantForbidExit())
                    {
                        ApplyInstantForbidExit(LastValidMovePos.Value);
                    }
                    else if (_forbidExitPhase == EForbidExitPhase.Idle)
                    {
                        EnterForbidExitAwaitingCue();
                    }
                }
                return;
            }

            if (!inOuter && _forbidExitPhase == EForbidExitPhase.Idle)
            {
                LastValidMovePos = worldPos;
            }
        }

        private bool ShouldUseInstantForbidExit()
        {
            if (forbidExitAlwaysInstant)
                return true;
            if (!forbidExitInstantWhenEnemyAlerting)
                return false;
            var am = MainGameManager.Instance?.gameLogicManager?.AreaManager;
            if (am == null)
                return false;
            foreach (var _ in am.GetAlertingLogicEntities())
                return true;
            return false;
        }

        private void ApplyInstantForbidExit(Vector2 restoreWorld)
        {
            ApplyPlayerWorldPos(restoreWorld);
            CharacterController?.ResetSmoothedMoveVelocity();
        }

        private void ApplyPlayerWorldPos(Vector2 world)
        {
            if (rb != null)
                rb.position = world;
            transform.position = new Vector3(world.x, world.y, transform.position.z);
            PlayerEntity.SetPosition(MainGameManager.Instance.GetLogicPosFromWorldPos(world));
        }

        private void EnterForbidExitAwaitingCue()
        {
            _forbidExitPhase = EForbidExitPhase.AwaitingDialogCue;
            _awaitCueElapsedRealtime = 0f;
            if (pauseLogicDuringForbidExit && !_forbidExitLogicPauseHeld)
            {
                LogicTime.RequestPause("ForbidZoneExit");
                _forbidExitLogicPauseHeld = true;
            }
        }

        private void StartForbidSlideInternal(float duration)
        {
            if (!LastValidMovePos.HasValue)
            {
                ReleaseForbidExitLogicPauseIfHeld();
                _forbidExitPhase = EForbidExitPhase.Idle;
                return;
            }

            if (_forbidSlideRoutine != null)
            {
                StopCoroutine(_forbidSlideRoutine);
                _forbidSlideRoutine = null;
            }

            _forbidExitPhase = EForbidExitPhase.Sliding;
            CharacterController?.ResetSmoothedMoveVelocity();
            _forbidSlideRoutine = StartCoroutine(CoForbidSlideTo(LastValidMovePos.Value, Mathf.Max(0.05f, duration)));
        }

        private void ReleaseForbidExitLogicPauseIfHeld()
        {
            if (_forbidExitLogicPauseHeld)
            {
                LogicTime.ReleasePause("ForbidZoneExit");
                _forbidExitLogicPauseHeld = false;
            }
        }

        private IEnumerator CoForbidSlideTo(Vector2 targetWorld, float duration)
        {
            Vector2 start = rb != null ? rb.position : (Vector2)transform.position;
            float el = 0f;
            try
            {
                while (el < duration)
                {
                    el += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(el / duration);
                    u = u * u * (3f - 2f * u);
                    ApplyPlayerWorldPos(Vector2.Lerp(start, targetWorld, u));
                    yield return null;
                }
                ApplyPlayerWorldPos(targetWorld);
            }
            finally
            {
                _forbidExitPhase = EForbidExitPhase.Idle;
                _forbidSlideRoutine = null;
                CharacterController?.ResetSmoothedMoveVelocity();
                ReleaseForbidExitLogicPauseIfHeld();
            }
        }

        private void CancelForbidExitPipeline()
        {
            if (_forbidSlideRoutine != null)
            {
                StopCoroutine(_forbidSlideRoutine);
                _forbidSlideRoutine = null;
            }
            ReleaseForbidExitLogicPauseIfHeld();
            _forbidExitPhase = EForbidExitPhase.Idle;
        }
    }
}
