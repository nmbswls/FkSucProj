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

            if (moveDuration <= 0f || !isActiveAndEnabled)
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
            if (_rect.parent == target.parent)
            {
                _rect.anchoredPosition = target.anchoredPosition;
                return;
            }

            _rect.position = target.position;
        }

        IEnumerator CoMoveTo(RectTransform target)
        {
            Vector2 start = _rect.anchoredPosition;
            Vector2 end = _rect.parent == target.parent
                ? target.anchoredPosition
                : (Vector2)_rect.parent.InverseTransformPoint(target.position);

            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
                _rect.anchoredPosition = Vector2.Lerp(start, end, t);
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
