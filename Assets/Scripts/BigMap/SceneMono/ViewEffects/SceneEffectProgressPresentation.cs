using UnityEngine;

namespace My
{
    // 按 MapSceneEffectCtrl 的 progress 驱动粒子/分档显隐
    [RequireComponent(typeof(MapSceneEffectCtrl))]
    public class SceneEffectProgressPresentation : MonoBehaviour
    {
        [SerializeField] ParticleSystem[] particleSystems;
        [SerializeField] AnimationCurve simulationSpeedByProgress = AnimationCurve.Linear(0f, 0.5f, 1f, 2.5f);
        [SerializeField] ProgressStage[] stages;

        int _lastStageIndex = -1;
        MapSceneEffectCtrl _effectCtrl;

        [System.Serializable]
        public struct ProgressStage
        {
            [Range(0f, 1f)]
            public float AtProgress;
            public GameObject Root;
        }

        void Awake()
        {
            _effectCtrl = GetComponent<MapSceneEffectCtrl>();
        }

        void OnEnable()
        {
            if (_effectCtrl == null)
            {
                return;
            }

            _effectCtrl.OnShown += HandleShown;
            _effectCtrl.OnProgressChanged += Apply;
        }

        void OnDisable()
        {
            if (_effectCtrl == null)
            {
                return;
            }

            _effectCtrl.OnShown -= HandleShown;
            _effectCtrl.OnProgressChanged -= Apply;
        }

        void HandleShown()
        {
            ResetPresentation();
        }

        public void ResetPresentation()
        {
            _lastStageIndex = -1;
            if (particleSystems == null || particleSystems.Length == 0)
            {
                particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            }

            Apply(0f);
        }

        public void Apply(float progress01)
        {
            ApplyParticleSpeed(progress01);
            ApplyStages(progress01);
        }

        void ApplyParticleSpeed(float progress01)
        {
            if (particleSystems == null)
            {
                return;
            }

            float speed = simulationSpeedByProgress != null && simulationSpeedByProgress.length > 0
                ? simulationSpeedByProgress.Evaluate(progress01)
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

        void ApplyStages(float progress01)
        {
            if (stages == null || stages.Length == 0)
            {
                return;
            }

            int stageIndex = -1;
            for (int i = 0; i < stages.Length; i++)
            {
                if (progress01 + 1e-5f >= stages[i].AtProgress)
                {
                    stageIndex = i;
                }
            }

            if (stageIndex == _lastStageIndex)
            {
                return;
            }

            _lastStageIndex = stageIndex;
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i].Root == null)
                {
                    continue;
                }

                bool active = i == stageIndex;
                stages[i].Root.SetActive(active);
                if (active)
                {
                    PlayStageParticles(stages[i].Root);
                }
            }
        }

        static void PlayStageParticles(GameObject root)
        {
            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                if (ps == null)
                {
                    continue;
                }

                ps.Clear(true);
                ps.Play(true);
            }
        }
    }
}
