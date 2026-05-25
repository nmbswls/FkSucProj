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

            _presentationMoveActive = true;
            CharacterController?.ResetSmoothedMoveVelocity();

            bool controllerWasEnabled = CharacterController != null && CharacterController.enabled;
            if (CharacterController != null)
            {
                CharacterController.enabled = false;
            }

            _presentationMoveTween = transform
                .DOMove(targetWorldPos, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    _presentationMoveActive = false;
                    if (CharacterController != null)
                    {
                        CharacterController.enabled = controllerWasEnabled;
                    }

                    onReach?.Invoke();
                })
                .SetLink(gameObject);
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
