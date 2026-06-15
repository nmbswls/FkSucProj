using System;
using DG.Tweening;
using My.Map;
using My.Map.View;
using UnityEngine;

namespace My.Map.Scene
{
    public partial class PlayerScenePresenter
    {
        const float ShadowMulHidden = 0.15f;
        const float ShadowMulClimb = 0.55f;
        const float ShadowMulJump = 0.20f;

        Tween _relocateSessionTween;
        Tween _relocateShadowTween;
        bool _relocateSessionActive;
        int _ghostRelocateFxId;

        bool _relocateShadowBound;
        Vector3 _relocateShadowBaseScale;
        Color _relocateShadowBaseColor;
        float _relocateShadowMul = 1f;

        bool _relocateViewBound;
        Vector3 _relocateViewBaseLocalPos;

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
                case PlayerRelocateTransitStyle.FakeJump2D:
                    PlayFakeJump2DRelocate(
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
            TweenRelocateShadow(ShadowMulHidden, PlayerRelocateTimings.GhostFadeOut, standalone: true);

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
                    TweenRelocateShadow(1f, PlayerRelocateTimings.GhostPlayerFadeIn, standalone: true);
                },
                onComplete: () =>
                {
                    _ghostRelocateFxId = 0;
                    FinishRelocateSession(onComplete);
                });
        }

        void PlayFakeJump2DRelocate(
            Vector2? entryLogicPos,
            Vector2? takeoffLogicPos,
            Vector2 landLogicPos,
            Action onComplete)
        {
            BeginRelocateSession();

            var takeoffLogic = takeoffLogicPos ?? landLogicPos;
            var takeoffWorld = MapLogicPosition.LogicToWorld(takeoffLogic);
            var landWorld = MapLogicPosition.LogicToWorld(landLogicPos);
            float arcPeak = PlayerRelocateTimings.ResolveJumpArcPeak(takeoffLogic, landLogicPos);

            var seq = DOTween.Sequence();
            if (entryLogicPos.HasValue)
            {
                PlayerRelocateSequenceBuilder.AppendRootWalk(
                    seq,
                    transform,
                    takeoffWorld,
                    PlayerRelocateTimings.JumpWalkEntry,
                    Ease.OutQuad);
            }

            var jumpCtx = new FakeJump2DContext
            {
                Root = transform,
                ViewRoot = ViewRoot,
                ViewBaseLocalPos = _relocateViewBaseLocalPos,
            };
            PlayerRelocateSequenceBuilder.AppendFakeJump2D(
                seq,
                jumpCtx,
                takeoffWorld,
                landWorld,
                arcPeak,
                PlayerRelocateTimings.JumpArc,
                (s, d) => JoinRelocateShadowTween(s, ShadowMulHidden, d));

            seq.OnComplete(() => FinishRelocateSession(onComplete));
            seq.SetLink(gameObject);
            _relocateSessionTween = seq;
        }

        void PlayVineClimbRelocate(
            Vector2? entryLogicPos,
            Vector2? midLogicPos,
            Vector2 finalLogicPos,
            Action onComplete)
        {
            BeginRelocateSession();

            float climbDuration = PlayerRelocateTimings.VineClimb;
            float pauseDuration = PlayerRelocateTimings.VinePause;
            float jumpDuration = PlayerRelocateTimings.VineJump;

            var endWorld = MapLogicPosition.LogicToWorld(midLogicPos ?? finalLogicPos);
            var landWorld = MapLogicPosition.LogicToWorld(finalLogicPos);
            float jumpPower = Mathf.Max(0.15f, Mathf.Abs(landWorld.y - endWorld.y));

            var seq = DOTween.Sequence();
            if (entryLogicPos.HasValue)
            {
                var entryWorld = MapLogicPosition.LogicToWorld(entryLogicPos.Value);
                PlayerRelocateSequenceBuilder.AppendRootWalk(
                    seq,
                    transform,
                    entryWorld,
                    PlayerRelocateTimings.VineEntry,
                    Ease.OutQuad);
            }

            seq.Append(transform.DOMove(endWorld, climbDuration).SetEase(Ease.Linear));
            JoinRelocateShadowTween(seq, ShadowMulClimb, climbDuration);

            if (pauseDuration > 0f)
            {
                seq.AppendInterval(pauseDuration);
            }

            seq.Append(transform.DOJump(landWorld, jumpPower, 1, jumpDuration).SetEase(Ease.InOutQuad));
            JoinRelocateShadowTween(seq, ShadowMulJump, jumpDuration);

            seq.OnComplete(() => FinishRelocateSession(onComplete));
            seq.SetLink(gameObject);
            _relocateSessionTween = seq;
        }

        void BindRelocateShadow()
        {
            if (ShadowView == null && ViewRoot != null)
            {
                ShadowView = ViewRoot.Find(UnitPresentationPaths.Shadow);
            }

            if (_shadowView == null && ShadowView != null)
            {
                AssignShadowViewRenderer(ShadowView.GetComponent<SpriteRenderer>());
            }

            _relocateShadowBound = ShadowView != null && _shadowView != null;
            if (!_relocateShadowBound)
            {
                return;
            }

            _relocateShadowBaseScale = ShadowView.localScale;
            _relocateShadowBaseColor = _shadowView.color;
            _relocateShadowMul = 1f;
        }

        void BindRelocateViewOffset()
        {
            if (ViewRoot == null)
            {
                ViewRoot = transform.Find(UnitPresentationPaths.View);
                if (ViewRoot == null)
                {
                    ViewRoot = transform.Find(UnitPresentationPaths.ViewLegacy);
                }
            }

            _relocateViewBound = ViewRoot != null;
            if (!_relocateViewBound)
            {
                return;
            }

            _relocateViewBaseLocalPos = ViewRoot.localPosition;
        }

        void ApplyRelocateShadowMul(float mul)
        {
            if (!_relocateShadowBound)
            {
                return;
            }

            _relocateShadowMul = mul;
            ShadowView.localScale = _relocateShadowBaseScale * mul;
            var c = _relocateShadowBaseColor;
            c.a = _relocateShadowBaseColor.a * mul;
            _shadowView.color = c;
        }

        Tween TweenRelocateShadow(float targetMul, float duration, bool standalone = false)
        {
            if (!_relocateShadowBound)
            {
                return null;
            }

            float mul = _relocateShadowMul;
            var tween = DOTween.To(
                () => mul,
                v =>
                {
                    mul = v;
                    ApplyRelocateShadowMul(v);
                },
                targetMul,
                duration);
            if (standalone)
            {
                _relocateShadowTween?.Kill();
                _relocateShadowTween = tween;
            }

            return tween;
        }

        void JoinRelocateShadowTween(Sequence seq, float targetMul, float duration)
        {
            var tween = TweenRelocateShadow(targetMul, duration);
            if (tween != null)
            {
                seq.Join(tween);
            }
        }

        void RestoreRelocateShadow()
        {
            _relocateShadowTween?.Kill();
            _relocateShadowTween = null;
            if (!_relocateShadowBound)
            {
                return;
            }

            ShadowView.localScale = _relocateShadowBaseScale;
            _shadowView.color = _relocateShadowBaseColor;
            _relocateShadowBound = false;
            _relocateShadowMul = 1f;
        }

        void RestoreRelocateViewOffset()
        {
            if (!_relocateViewBound || ViewRoot == null)
            {
                return;
            }

            ViewRoot.localPosition = _relocateViewBaseLocalPos;
            _relocateViewBound = false;
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
            BindRelocateViewOffset();
            BindRelocateShadow();
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
            RestoreRelocateViewOffset();
            RestoreRelocateShadow();

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

            RestoreRelocateViewOffset();
            RestoreRelocateShadow();

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
