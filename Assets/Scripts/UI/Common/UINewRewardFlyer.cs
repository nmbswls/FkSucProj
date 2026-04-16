using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace My
{
    public class UINewRewardFlyer : MonoBehaviour
    {
        [Header("Refs")]
        public Image iconImage;

        [Header("Settings")]
        public float popDuration = 0.5f;   // 中央弹出停留时间
        public float flyDuration = 0.6f;   // 飞行耗时
        public float scalePunch = 1.5f;    // 弹出时的最大缩放

        private Action onCompleteCallback;

        public void Initialize(Sprite sprite, Vector3 startPos, Vector3 targetPos, Action onComplete)
        {
            iconImage.sprite = sprite;
            transform.position = startPos; // 注意使用 World Position
            this.onCompleteCallback = onComplete;

            StartCoroutine(AnimateRoutine(startPos, targetPos));
        }

        private IEnumerator AnimateRoutine(Vector3 startPos, Vector3 endPos)
        {
            // --- 阶段 1: 中央弹出 (Pop) ---
            float timer = 0f;
            transform.localScale = Vector3.zero;

            // 简单的弹性放大效果
            while (timer < popDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / popDuration;

                // 使用一个自定义的曲线模拟弹跳: 0 -> 1.5 -> 1.0
                float scale = 0f;
                if (progress < 0.5f) scale = Mathf.Lerp(0, scalePunch, progress * 2);
                else scale = Mathf.Lerp(scalePunch, 1f, (progress - 0.5f) * 2);

                transform.localScale = Vector3.one * scale;
                yield return null;
            }
            transform.localScale = Vector3.one;

            // --- 阶段 2: 贝塞尔曲线飞行 (Fly) ---
            timer = 0f;

            // 计算一个控制点，使路径呈弧形
            // 控制点取在中点上方或侧方一定距离
            Vector3 midPoint = (startPos + endPos) / 2;
            Vector3 controlPoint = midPoint + (Vector3.up * 2f) + (Vector3.right * 1f); // 根据屏幕调整数值

            while (timer < flyDuration)
            {
                timer += Time.deltaTime;
                float t = timer / flyDuration;

                // 二阶贝塞尔曲线公式
                // B(t) = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
                Vector3 pos = Mathf.Pow(1 - t, 2) * startPos +
                              2 * (1 - t) * t * controlPoint +
                              Mathf.Pow(t, 2) * endPos;

                transform.position = pos;

                // 飞行过程中逐渐变小
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.5f, t);

                yield return null;
            }

            // --- 结束 ---
            onCompleteCallback?.Invoke(); // 通知管理器我到了
            Destroy(gameObject);
        }
    }
}

