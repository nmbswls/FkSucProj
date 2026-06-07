using System;
using DG.Tweening;
using UnityEngine;

namespace My.Map.Scene
{
    public partial class PlayerScenePresenter
    {
        Tween _presentationMoveTween;
        bool _presentationMoveActive;

        public bool IsPresentationMoveActive => _presentationMoveActive;

        public void PlayPresentationMove(Vector3 targetWorldPos, float duration, Action onReach)
        {
            CancelPresentationMove();

            if (duration <= 0f)
            {
                duration = 0.5f;
            }

            BeginPresentationMove();
            _presentationMoveTween = transform
                .DOMove(targetWorldPos, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() => FinishPresentationMove(onReach))
                .SetLink(gameObject);
        }

        public void PlayVineClimbMove(
            Vector3 apexWorldPos,
            Vector3 landWorldPos,
            float climbDuration,
            float pauseDuration,
            float jumpDuration,
            Action onComplete)
        {
            CancelPresentationMove();

            climbDuration = Mathf.Max(0.01f, climbDuration);
            pauseDuration = Mathf.Max(0f, pauseDuration);
            jumpDuration = Mathf.Max(0.01f, jumpDuration);

            BeginPresentationMove();

            float jumpPower = Mathf.Max(0.15f, Mathf.Abs(landWorldPos.y - apexWorldPos.y));
            var seq = DOTween.Sequence();
            seq.Append(transform.DOMove(apexWorldPos, climbDuration).SetEase(Ease.OutQuad));
            if (pauseDuration > 0f)
            {
                seq.AppendInterval(pauseDuration);
            }

            seq.Append(transform.DOJump(landWorldPos, jumpPower, 1, jumpDuration).SetEase(Ease.InOutQuad));
            seq.OnComplete(() => FinishPresentationMove(onComplete));
            seq.SetLink(gameObject);
            _presentationMoveTween = seq;
        }

        void BeginPresentationMove()
        {
            _presentationMoveActive = true;
            CharacterController?.ResetSmoothedMoveVelocity();

            if (CharacterController != null)
            {
                CharacterController.enabled = false;
            }
        }

        void FinishPresentationMove(Action onReach)
        {
            // 先回调（藤蔓攀爬在此同步 TeleportTo），仍保持 presentation 保护相机
            onReach?.Invoke();

            _presentationMoveActive = false;
            _presentationMoveTween = null;

            if (CharacterController != null)
            {
                CharacterController.enabled = true;
            }
        }

        public void CancelPresentationMove()
        {
            if (_presentationMoveTween != null && _presentationMoveTween.IsActive())
            {
                _presentationMoveTween.Kill();
            }

            _presentationMoveTween = null;
            _presentationMoveActive = false;
        }

        void CancelPresentationMoveOnDestroy()
        {
            CancelPresentationMove();
        }
    }
}
