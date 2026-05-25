using UnityEngine;

namespace My.Map
{
    // 静态 chunk prefab 被 AppearCond 实例化时播放一次生长/出现动画（Animator 状态名可配）
    public class MapStaticAppearAnim : MonoBehaviour
    {
        public Animator TargetAnimator;
        public string EnterStateName = "grow";
        public float CrossFadeSeconds = 0f;

        void OnEnable()
        {
            var anim = TargetAnimator != null ? TargetAnimator : GetComponentInChildren<Animator>();
            if (anim == null || string.IsNullOrEmpty(EnterStateName))
            {
                return;
            }

            if (CrossFadeSeconds > 0f)
            {
                anim.CrossFade(EnterStateName, CrossFadeSeconds, 0, 0f);
            }
            else
            {
                anim.Play(EnterStateName, 0, 0f);
            }
        }
    }
}
