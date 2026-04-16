

using System.Collections;
using UnityEngine;

namespace My
{
    public class HitStopManager : MonoBehaviour
    {
        public static HitStopManager Instance 
        { 
            get 
            {
                return MainGameManager.Instance.HitStopManager;
            } 
        }

        private bool isWaiting = false;
        private float restoreTimeScale = 1f;

        void Awake()
        {
        }

        /// <summary>
        /// 触发顿帧
        /// </summary>
        /// <param name="duration">顿帧持续的真实时间（秒）</param>
        /// <param name="slowScale">顿帧时的流速，建议0.0~0.1</param>
        public void TriggerHitStop(float duration, float slowScale = 0.05f)
        {
            if (isWaiting)
            {
                // 如果已经在顿帧中，停止当前的恢复协程，开启一个新的
                // 或者更简单的逻辑：只重置恢复时间，让当前的协程多跑一会
                // 这里我们采用"重置协程"的方式，确保最新的 slowScale 生效
                StopAllCoroutines();
            }

            StartCoroutine(DoHitStop(duration, slowScale));
        }

        IEnumerator DoHitStop(float duration, float slowScale)
        {
            isWaiting = true;

            // 记录原始时间流速（防止原本就在慢动作里，比如子弹时间）
            // 如果原本就是1，那就恢复到1。如果原本是0.5（全局减速），就恢复到0.5
            // 但为了防止嵌套混乱，通常建议直接恢复到 1，或者用一个变量单独存 BaseTimeScale
            if (Time.timeScale > 0.2f)
            {
                restoreTimeScale = Time.timeScale;
            }

            Time.timeScale = slowScale;

            // 使用 Realtime 等待，不受 TimeScale 影响
            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = restoreTimeScale;
            isWaiting = false;
        }
    }
}