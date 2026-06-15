using System;
using DG.Tweening;
using Map.Scene;
using My.Map;
using My.Map.View;
using UnityEngine;

namespace My.Map.Scene
{
    public partial class PlayerScenePresenter
    {
        const float RelocateShadowHiddenScale = 0.15f;
        const float RelocateShadowClimbScale = 0.55f;
        const float RelocateShadowJumpScale = 0.20f;

        Tween _relocateSessionTween;
        bool _relocateSessionActive;
        int _ghostRelocateFxId;

        public bool IsRelocateSessionActive => _relocateSessionActive;

        public void PlayRelocate(PlayerRelocateSpec spec, Action onComplete)
        {
            CancelRelocateSession();

            switch (spec.TransitStyle)
            {
                case PlayerRelocateTransitStyle.WaitOnly:
                    PlayWaitOnlyRelocate(onComplete);
                    break;
                case PlayerRelocateTransitStyle.GhostOrb:
                    PlayGhostOrbRelocate(MapLogicPosition.LogicToWorld(spec.FinalLogicPos), onComplete);
                    break;
                case PlayerRelocateTransitStyle.VineClimb:
                    PlayVineClimbRelocate(
                        spec.EntryLogicPos,
                        spec.MidLogicPos,
                        spec.FinalLogicPos,
                        onComplete);
                    break;
                default:
                    PlayWaitOnlyRelocate(onComplete);
                    break;
            }
        }

        void PlayWaitOnlyRelocate(Action onComplete)
        {
            BeginRelocateSession();
            TweenRelocateShadowVisual(0.85f, 0.85f, PlayerRelocateTimings.WaitOnlyDefault * 0.5f);
            _relocateSessionTween = DOVirtual
                .DelayedCall(PlayerRelocateTimings.WaitOnlyDefault, () => FinishRelocateSession(onComplete))
                .SetLink(gameObject);
        }

        void PlayGhostOrbRelocate(Vector3 targetWorldPos, Action onComplete)
        {
            var playerSr = ResolvePlayerSpriteRenderer();
            if (playerSr == null)
            {
                onComplete?.Invoke();
                return;
            }

            BeginRelocateSession();
            TweenRelocateShadowVisual(0f, RelocateShadowHiddenScale, PlayerRelocateTimings.GhostFadeOut);

            float fxLifetime = PlayerRelocateTimings.GetGhostTotal() + 0.5f;
            var ctx = MapSceneEffectManager.Instance.ShowSceneEffect(
                transform.position,
                fxLifetime,
                "PlayerSpecialMove",
                null);
            if (ctx == null)
            {
                FinishRelocateSession(onComplete);
                return;
            }

            _ghostRelocateFxId = ctx.UniqId;
            var fx = ctx.EffectGo.GetComponent<PlayerGhostMoveFxCtrl>();
            if (fx == null)
            {
                MapSceneEffectManager.Instance.ForceDestroy(_ghostRelocateFxId);
                _ghostRelocateFxId = 0;
                FinishRelocateSession(onComplete);
                return;
            }

            _relocateSessionTween = fx.PlayMoveFx(
                playerSr,
                transform,
                targetWorldPos,
                PlayerRelocateTimings.Ghost,
                onReachTarget: () => { transform.position = targetWorldPos; },
                onPlayerFadeInStart: () =>
                {
                    TweenRelocateShadowVisual(1f, 1f, PlayerRelocateTimings.GhostPlayerFadeIn);
                },
                onComplete: () =>
                {
                    _ghostRelocateFxId = 0;
                    FinishRelocateSession(onComplete);
                });
        }

        void PlayVineClimbRelocate(
            Vector2? entryLogicPos,
            Vector2? midLogicPos,
            Vector2 finalLogicPos,
            Action onComplete)
        {
            BeginRelocateSession();

            float entryDuration = PlayerRelocateTimings.VineEntry;
            float climbDuration = PlayerRelocateTimings.VineClimb;
            float pauseDuration = PlayerRelocateTimings.VinePause;
            float jumpDuration = PlayerRelocateTimings.VineJump;

            var endWorld = MapLogicPosition.LogicToWorld(
                midLogicPos ?? finalLogicPos);
            var landWorld = MapLogicPosition.LogicToWorld(finalLogicPos);
            float jumpPower = Mathf.Max(0.15f, Mathf.Abs(landWorld.y - endWorld.y));

            var seq = DOTween.Sequence();
            if (entryLogicPos.HasValue)
            {
                var entryWorld = MapLogicPosition.LogicToWorld(entryLogicPos.Value);
                seq.Append(transform.DOMove(entryWorld, entryDuration).SetEase(Ease.OutQuad));
            }

            seq.Append(transform.DOMove(endWorld, climbDuration).SetEase(Ease.Linear));
            var climbShadowTween = TweenRelocateShadowVisual(0.6f, RelocateShadowClimbScale, climbDuration);
            if (climbShadowTween != null)
            {
                seq.Join(climbShadowTween);
            }

            if (pauseDuration > 0f)
            {
                seq.AppendInterval(pauseDuration);
            }

            seq.Append(transform.DOJump(landWorld, jumpPower, 1, jumpDuration).SetEase(Ease.InOutQuad));
            var jumpShadowTween = TweenRelocateShadowVisual(0f, RelocateShadowJumpScale, jumpDuration);
            if (jumpShadowTween != null)
            {
                seq.Join(jumpShadowTween);
            }
            seq.OnComplete(() => FinishRelocateSession(onComplete));
            seq.SetLink(gameObject);
            _relocateSessionTween = seq;
        }

        SpriteRenderer ResolvePlayerSpriteRenderer()
        {
            var agentView = AgentView;
            if (agentView != null)
            {
                return agentView.GetComponentInChildren<SpriteRenderer>();
            }

            return transform.Find(UnitPresentationPaths.View)?.Find(UnitPresentationPaths.Agent)
                ?.GetComponentInChildren<SpriteRenderer>();
        }

        void BeginRelocateSession()
        {
            _relocateSessionActive = true;
            CaptureRelocateShadowSnapshot();
            CharacterController?.ResetSmoothedMoveVelocity();

            if (CharacterController != null)
            {
                CharacterController.enabled = false;
            }
        }

        void FinishRelocateSession(Action onComplete)
        {
            onComplete?.Invoke();

            _relocateSessionActive = false;
            _relocateSessionTween = null;
            RestoreRelocateShadowVisual();

            if (CharacterController != null)
            {
                CharacterController.enabled = true;
            }
        }

        public void CancelRelocateSession()
        {
            if (_relocateSessionTween != null && _relocateSessionTween.IsActive())
            {
                _relocateSessionTween.Kill();
            }

            _relocateSessionTween = null;
            _relocateSessionActive = false;

            if (_ghostRelocateFxId != 0)
            {
                MapSceneEffectManager.Instance.ForceDestroy(_ghostRelocateFxId);
                _ghostRelocateFxId = 0;
            }

            var playerSr = ResolvePlayerSpriteRenderer();
            if (playerSr != null)
            {
                playerSr.enabled = true;
                var c = playerSr.color;
                c.a = 1f;
                playerSr.color = c;
            }

            RestoreRelocateShadowVisual();

            if (CharacterController != null)
            {
                CharacterController.enabled = true;
            }
        }

        void CancelRelocateSessionOnDestroy()
        {
            CancelRelocateSession();
        }
    }
}
