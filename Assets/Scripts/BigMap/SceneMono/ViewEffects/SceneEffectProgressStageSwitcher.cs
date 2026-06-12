using UnityEngine;

namespace My
{
    // 按 progress 切换分档子节点显隐，并在激活时重播该档粒子
    [RequireComponent(typeof(MapSceneEffectCtrl))]
    public class SceneEffectProgressStageSwitcher : SceneEffectProgressConsumerBase
    {
        [SerializeField] ProgressStage[] stages;

        int _lastStageIndex = -1;

        public GameObject ActiveStageRoot { get; private set; }

        [System.Serializable]
        public struct ProgressStage
        {
            [Range(0f, 1f)]
            public float AtProgress;
            public GameObject Root;
        }

        protected override void OnEffectShown()
        {
            ResetStages();
        }

        protected override void OnProgressChanged(float progress01)
        {
            ApplyStages(progress01);
        }

        public void ResetStages()
        {
            _lastStageIndex = -1;
            ActiveStageRoot = null;
            ApplyStages(0f);
        }

        void ApplyStages(float progress01)
        {
            if (stages == null || stages.Length == 0)
            {
                return;
            }

            int stageIndex = ResolveStageIndex(progress01);
            if (stageIndex == _lastStageIndex)
            {
                return;
            }

            _lastStageIndex = stageIndex;
            ActiveStageRoot = stageIndex >= 0 && stageIndex < stages.Length
                ? stages[stageIndex].Root
                : null;

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

        int ResolveStageIndex(float progress01)
        {
            int stageIndex = -1;
            for (int i = 0; i < stages.Length; i++)
            {
                if (progress01 + 1e-5f >= stages[i].AtProgress)
                {
                    stageIndex = i;
                }
            }

            return stageIndex;
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
