using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace My.Map
{
    /// <summary>
    /// 氛围感
    /// </summary>
    public class AmbientBubble : MonoBehaviour
    {
        [Header("UI Components")]
        public TextMeshProUGUI contentText;
        public CanvasGroup canvasGroup;
        public RectTransform rectTransform;

        [Header("Animation Settings")]
        public float floatDistance = 50f; // 向上漂浮的距离
        public float lifeTime = 4f;       // 存活总时间
        public float fadeTime = 0.5f;     // 淡入淡出时间

        // 初始化方法
        public void Setup(string text, Vector2 startPos, float scale)
        {
            contentText.text = text;
            rectTransform.anchoredPosition = startPos;
            transform.localScale = Vector3.one * scale;

            // 初始设为全透明
            canvasGroup.alpha = 0;

            // 开始动画序列
            AnimateBubble();
        }

        void AnimateBubble()
        {
            // 使用 DoTween 的写法 (推荐)
            // 1. 淡入
            canvasGroup.DOFade(1f, fadeTime);

            // 2. 持续漂浮移动 (模拟空气流动，稍微随机一点方向)
            float randomX = Random.Range(-20f, 20f);
            rectTransform.DOAnchorPos(
                rectTransform.anchoredPosition + new Vector2(randomX, floatDistance),
                lifeTime).SetEase(Ease.Linear);

            // 3. 各种微小的旋转晃动增加动态感
            transform.DORotate(new Vector3(0, 0, Random.Range(-5f, 5f)), lifeTime);

            // 4. 生命周期结束前淡出并销毁
            Sequence seq = DOTween.Sequence();
            seq.AppendInterval(lifeTime - fadeTime); // 等待
            seq.Append(canvasGroup.DOFade(0f, fadeTime)); // 淡出
            seq.OnComplete(() => Destroy(gameObject)); // 销毁
        }

        // 如果没有 DoTween，需要用 Coroutine 手写 Lerp 插值，这里省略手写版以保持简洁
    }
}
