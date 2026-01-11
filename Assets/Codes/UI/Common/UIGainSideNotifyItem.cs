

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My
{

    [RequireComponent(typeof(CanvasGroup))]
    public class UIGainSideNotifyItem : MonoBehaviour
    {
        public TextMeshProUGUI contentText;
        public Image iconImage;
        public CanvasGroup canvasGroup;

        [Header("Settings")]
        public float slideDuration = 0.3f;
        public float lifeTime = 3.0f;

        public void Setup(string text, Sprite icon)
        {
            contentText.text = text;
            iconImage.sprite = icon;

            // 初始状态：透明且稍微偏右（假设在右侧堆叠）
            canvasGroup.alpha = 0;
            // 也可以通过 LayoutElement 的 minHeight 做展开动画，这里简单处理透明度
            StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            // 1. Fade In (配合 LayoutGroup 自动排版，这里只做透明度渐变)
            float timer = 0f;
            while (timer < slideDuration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = timer / slideDuration;
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // 2. Wait
            yield return new WaitForSeconds(lifeTime);

            // 3. Fade Out
            timer = 0f;
            while (timer < slideDuration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = 1 - (timer / slideDuration);
                yield return null;
            }

            Destroy(gameObject);
        }
    }


}