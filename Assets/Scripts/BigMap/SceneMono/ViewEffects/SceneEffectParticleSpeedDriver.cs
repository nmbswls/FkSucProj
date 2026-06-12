using UnityEngine;

namespace My
{
    // 按 progress 驱动粒子 simulationSpeed
    [RequireComponent(typeof(MapSceneEffectCtrl))]
    public class SceneEffectParticleSpeedDriver : SceneEffectProgressConsumerBase
    {
        [SerializeField] ParticleSystem[] particleSystems;
        [SerializeField] AnimationCurve speedByProgress = AnimationCurve.Linear(0f, 0.5f, 1f, 2.5f);

        protected override void OnEffectShown()
        {
            if (particleSystems == null || particleSystems.Length == 0)
            {
                particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            }

            ApplySpeed(0f);
        }

        protected override void OnProgressChanged(float progress01)
        {
            ApplySpeed(progress01);
        }

        void ApplySpeed(float progress01)
        {
            if (particleSystems == null)
            {
                return;
            }

            float speed = speedByProgress != null && speedByProgress.length > 0
                ? speedByProgress.Evaluate(progress01)
                : 1f;

            for (int i = 0; i < particleSystems.Length; i++)
            {
                var ps = particleSystems[i];
                if (ps == null)
                {
                    continue;
                }

                var main = ps.main;
                main.simulationSpeed = speed;
            }
        }
    }
}
