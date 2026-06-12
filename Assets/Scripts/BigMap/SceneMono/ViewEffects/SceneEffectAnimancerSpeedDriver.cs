using Animancer;
using UnityEngine;

namespace My
{
    // 按 progress 驱动本节点及子节点上的 SoloAnimation / Animancer 播放速度；挂到实际播放动画的 stage 上
    public class SceneEffectAnimancerSpeedDriver : SceneEffectProgressConsumerBase
    {
        [SerializeField] AnimationCurve speedByProgress = AnimationCurve.Linear(0f, 0.5f, 1f, 2.5f);

        protected override void OnEnable()
        {
            base.OnEnable();
            if (EffectCtrl != null)
            {
                ApplySpeed(EffectCtrl.Progress01);
            }
        }

        protected override void OnEffectShown()
        {
            ApplySpeed(0f);
        }

        protected override void OnProgressChanged(float progress01)
        {
            ApplySpeed(progress01);
        }

        void ApplySpeed(float progress01)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            float speed = speedByProgress != null && speedByProgress.length > 0
                ? speedByProgress.Evaluate(progress01)
                : 1f;
            speed = Mathf.Max(0f, speed);

            SceneEffectAnimancerDriveUtil.DriveOnRoot(gameObject, EffectCtrl, speed);
        }
    }

    static class SceneEffectAnimancerDriveUtil
    {
        public static void DriveOnRoot(GameObject root, MapSceneEffectCtrl effectCtrl, float speed)
        {
            if (root == null)
            {
                return;
            }

            var soloAnimations = root.GetComponentsInChildren<SoloAnimation>(true);
            for (int i = 0; i < soloAnimations.Length; i++)
            {
                DriveSoloAnimation(soloAnimations[i], speed);
            }

            var animancers = root.GetComponentsInChildren<AnimancerComponent>(true);
            for (int i = 0; i < animancers.Length; i++)
            {
                var animancer = animancers[i];
                if (animancer == null)
                {
                    continue;
                }

                if (animancer.GetComponent<SoloAnimation>() != null)
                {
                    continue;
                }

                var clip = ResolveAnimancerClip(animancer.gameObject, effectCtrl);
                if (clip == null)
                {
                    continue;
                }

                DriveAnimancerComponent(animancer, clip, speed);
            }
        }

        static AnimationClip ResolveAnimancerClip(GameObject owner, MapSceneEffectCtrl effectCtrl)
        {
            var solo = owner.GetComponent<SoloAnimation>();
            if (solo != null && solo.Clip != null)
            {
                return solo.Clip;
            }

            if (effectCtrl != null && effectCtrl.ShowClip != null)
            {
                return effectCtrl.ShowClip;
            }

            return null;
        }

        static void DriveSoloAnimation(SoloAnimation solo, float speed)
        {
            if (solo == null || solo.Clip == null)
            {
                return;
            }

            if (!solo.IsInitialized)
            {
                solo.Play();
            }

            solo.Speed = speed;
        }

        static void DriveAnimancerComponent(AnimancerComponent animancer, AnimationClip clip, float speed)
        {
            if (animancer == null || clip == null)
            {
                return;
            }

            var state = animancer.States.Current;
            if (state == null || state.Clip != clip)
            {
                state = animancer.Play(clip, 0f, FadeMode.FromStart);
            }

            if (state == null)
            {
                return;
            }

            state.Speed = speed;
        }
    }
}
