using System.Collections;
using UnityEngine;

namespace My.UI.BodyPart
{
    // 立绘上的选中标记：跟随当前部位热区位置
    public sealed class BodyPartFocusMarkView : MonoBehaviour
    {
        [SerializeField] RectTransform markRect;
        [SerializeField] float moveDuration = 0.12f;

        RectTransform _rect;
        Coroutine _moveRoutine;
        RectTransform _lastTarget;
        Vector3 _lastWorldPos;
        bool _hasPlaced;

        void Awake()
        {
            _rect = markRect != null ? markRect : transform as RectTransform;
        }

        public void FocusTo(RectTransform target, bool visible)
        {
            if (_rect == null)
            {
                return;
            }

            if (!visible || target == null)
            {
                StopMove();
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (_lastTarget == target && _hasPlaced)
            {
                StopMove();
                SnapTo(target);
                return;
            }

            bool animate = _hasPlaced && moveDuration > 0f && isActiveAndEnabled;
            _lastTarget = target;

            if (!animate)
            {
                StopMove();
                SnapTo(target);
                return;
            }

            StopMove();
            _moveRoutine = StartCoroutine(CoMoveTo(target));
        }

        void SnapTo(RectTransform target)
        {
            _rect.position = target.position;
            _lastWorldPos = _rect.position;
            _hasPlaced = true;
        }

        IEnumerator CoMoveTo(RectTransform target)
        {
            Vector3 startWorld = _hasPlaced ? _lastWorldPos : _rect.position;
            Vector3 endWorld = target.position;
            _rect.position = startWorld;

            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
                _rect.position = Vector3.Lerp(startWorld, endWorld, t);
                yield return null;
            }

            SnapTo(target);
            _moveRoutine = null;
        }

        void StopMove()
        {
            if (_moveRoutine == null)
            {
                return;
            }

            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }

        void OnDisable()
        {
            StopMove();
        }
    }
}
