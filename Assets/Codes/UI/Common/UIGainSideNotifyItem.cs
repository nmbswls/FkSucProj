

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My
{

    [RequireComponent(typeof(CanvasGroup))]
    public class UIGainSideNotifyItem : MonoBehaviour
    {
        [Header("UI Refs")]
        public TextMeshProUGUI messageText;
        public Image iconImage;

        [Header("Animation Settings")]
        public float slideDuration = 0.3f; // 滑入耗时
        public float lifeTime = 3.0f;      // 停留时间
        public float fadeOutDuration = 0.5f;
        public float targetHeight = 80f;   // 你条目的实际高度

        public RectTransform viewRoot;
        private CanvasGroup canvasGroup;
        private LayoutElement layoutElement;

        public void Initialize(string text, Sprite icon)
        {
            messageText.text = text;
            iconImage.sprite = icon;

            // 获取组件
            //rectTrans = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            layoutElement = GetComponent<LayoutElement>();

            // --- 1. 初始状态设置 (不可见、在右侧、高度为0) ---

            // A. 透明度设为 0
            canvasGroup.alpha = 0f;

            // B. 位置偏移到屏幕右侧外 (假设条目宽400，偏移500确保出去)
            // 注意：LayoutGroup 控制位置，但我们可以控制 anchoredPosition 的偏移
            viewRoot.anchoredPosition = new Vector2(100f, 0f);

            // C. 高度设为 0 (这是让老消息平滑上浮的关键)
            layoutElement.preferredHeight = 0f;
            layoutElement.minHeight = 0f;

            // --- 2. 开始动画流程 ---
            StartCoroutine(AnimateRoutine());
        }

        /// <summary>
        /// 被管理器强制移除时调用（比如超过最大数量）
        /// </summary>
        public void ForceExit()
        {
            // 停止之前的生命周期协程
            StopAllCoroutines();

            // 开启快速淡出协程
            StartCoroutine(FastFadeOutRoutine());
        }


        private IEnumerator FastFadeOutRoutine()
        {
            float timer = 0f;
            float duration = 0.2f; // 快速消失时间
            float startAlpha = canvasGroup.alpha;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / duration);
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator AnimateRoutine()
        {
            // === 阶段一：滑入 + 撑开高度 ===
            float timer = 0f;

            while (timer < slideDuration)
            {
                timer += Time.deltaTime;
                float t = timer / slideDuration;
                // 使用平滑曲线 (EaseOutBack 会有一点点回弹，很有动感)
                float curveT = 1f - Mathf.Pow(1f - t, 3f);

                // 1. 撑开高度：从 0 -> targetHeight
                // 随着高度变大，LayoutGroup 会自动把上面的老消息往上推
                layoutElement.preferredHeight = Mathf.Lerp(0f, targetHeight, curveT);

                // 2. 透明度：0 -> 1
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

                // 3. 位移：300 -> 0 (回到 LayoutGroup 分配的位置)
                viewRoot.anchoredPosition = Vector2.Lerp(new Vector2(100f, 0), Vector2.zero, curveT);

                yield return null;
            }

            // 确保最终状态正确
            layoutElement.preferredHeight = targetHeight;
            viewRoot.anchoredPosition = Vector2.zero;
            canvasGroup.alpha = 1f;

            // === 阶段二：停留展示 ===
            yield return new WaitForSeconds(lifeTime);

            // === 阶段三：淡出消失 ===
            timer = 0f;
            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                float t = timer / fadeOutDuration;

                // 透明度变 0
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

                // 可选：稍微往上飘一点，或者往右缩回去
                // rectTrans.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(0, 50f), t);

                yield return null;
            }

            // === 结束：销毁 ===
            Destroy(gameObject);
        }
    }


}