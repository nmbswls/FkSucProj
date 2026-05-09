using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using UnityEngine;

namespace My.Map.Logic.Spirit
{
    // 后场氛围影怪：无逻辑实体 / Presenter，仅在玩家外围漂移播动画。
    public sealed class AmbientSpiritVisualManager
    {
        private readonly GameLogicManager _glm;

        private struct Entry
        {
            public GameObject Instance;
            public float PhaseRad;
            public float Radius;
            public float AngleSpeedSign;
            public Animator CachedAnimator;
        }

        private readonly List<Entry> _entries = new();
        private string _lastAmbientRebuildKey = string.Empty;
        private Transform _fallbackParent;

        public AmbientSpiritVisualManager(GameLogicManager glm)
        {
            _glm = glm;
        }

        private PlayerDesireLevel TryGetPlayerDesireCfg()
        {
            if (_glm.playerLogicEntity == null)
            {
                return null;
            }
            return CfgMgr.Cfgs.TbPlayerDesireLevel.GetOrDefault(_glm.playerLogicEntity.DesireLevel);
        }

        private static Transform FindSceneRootFallback()
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var r in roots)
            {
                if (r.CompareTag("MapRoot"))
                {
                    return r.transform;
                }
            }
            return roots.Length > 0 ? roots[0].transform : null;
        }

        public void Shutdown()
        {
            ClearInstances();
            _lastAmbientRebuildKey = string.Empty;
        }

        public void Tick(float dt)
        {
            if (_glm.PlayerHumanMode)
            {
                if (_entries.Count != 0)
                {
                    ClearInstances();
                    _lastAmbientRebuildKey = string.Empty;
                }
                return;
            }

            if (_glm.playerLogicEntity == null)
            {
                return;
            }

            var desireCfg = TryGetPlayerDesireCfg();
            if (desireCfg == null)
            {
                if (_entries.Count != 0)
                {
                    ClearInstances();
                }

                _lastAmbientRebuildKey = string.Empty;
                return;
            }

            string nextKey = BuildAmbientRebuildKey(_glm.playerLogicEntity.DesireLevel, desireCfg);
            bool needRebuild = nextKey != _lastAmbientRebuildKey;

            if (!string.IsNullOrEmpty(desireCfg.AmbientSpiritPrefab))
            {
                _fallbackParent ??= FindSceneRootFallback();
            }

            if (needRebuild)
            {
                Rebuild(desireCfg);
                _lastAmbientRebuildKey = nextKey;
            }
            if (_entries.Count == 0 || _glm.playerLogicEntity == null)
            {
                return;
            }

            Vector2 anchor = _glm.playerLogicEntity.Pos;

            for (int idx = 0; idx < _entries.Count; idx++)
            {
                var ent = _entries[idx];
                if (ent.Instance == null)
                {
                    continue;
                }

                float w = Mathf.Max(0.05f, desireCfg.AmbientSpiritDriftSpeed);
                float nextAngle = LogicTime.time * (0.55f + ent.Radius * 0.015f) * w * ent.AngleSpeedSign + ent.PhaseRad;
                var offset = new Vector2(Mathf.Cos(nextAngle), Mathf.Sin(nextAngle)) * ent.Radius;

                Vector3 wp = new(anchor.x + offset.x, anchor.y + offset.y, ent.Instance.transform.position.z);
                ent.Instance.transform.position = wp;

                if (ent.CachedAnimator != null && ent.CachedAnimator.isInitialized)
                {
                    ent.CachedAnimator.speed = Mathf.Max(0.2f, w);
                }
            }
        }

        private static string BuildAmbientRebuildKey(int desireLevel, PlayerDesireLevel c)
        {
            if (c == null)
            {
                return string.Empty;
            }

            return $"{desireLevel}|{c.AmbientSpiritCount}|{c.AmbientSpiritPrefab}|{c.AmbientSpiritRadiusMin:F2}|{c.AmbientSpiritRadiusMax:F2}|{c.AmbientSpiritDriftSpeed:F2}";
        }

        private void Rebuild(PlayerDesireLevel desireCfg)
        {
            ClearInstances();

            if (desireCfg.AmbientSpiritCount <= 0 || string.IsNullOrEmpty(desireCfg.AmbientSpiritPrefab))
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(desireCfg.AmbientSpiritPrefab);
            if (prefab == null)
            {
                Debug.LogWarning($"AmbientSpiritVisualManager prefab missing at Resources/{desireCfg.AmbientSpiritPrefab}");
                return;
            }

            float rMin = Mathf.Max(0.1f, Mathf.Min(desireCfg.AmbientSpiritRadiusMin, desireCfg.AmbientSpiritRadiusMax));
            float rMax = Mathf.Max(rMin + 0.1f, Mathf.Max(desireCfg.AmbientSpiritRadiusMin, desireCfg.AmbientSpiritRadiusMax));

            Transform parentObj = _fallbackParent;
            Vector2 anchor = _glm.playerLogicEntity != null ? _glm.playerLogicEntity.Pos : Vector2.zero;

            for (int i = 0; i < desireCfg.AmbientSpiritCount; i++)
            {
                var go = Object.Instantiate(prefab, parentObj);
                go.name = $"{prefab.name}_ambient_{i}";

                float phase = Random.Range(0f, Mathf.PI * 2f);
                float rad = Random.Range(rMin, rMax);
                float spin = Random.value > 0.5f ? 1f : -1f;

                Vector3 wp = new(anchor.x + Mathf.Cos(phase) * rad, anchor.y + Mathf.Sin(phase) * rad, 0f);
                go.transform.position = wp;

                var anim = go.GetComponentInChildren<Animator>();

                _entries.Add(new Entry
                {
                    Instance = go,
                    PhaseRad = phase,
                    Radius = rad,
                    AngleSpeedSign = spin,
                    CachedAnimator = anim,
                });
            }
        }

        private void ClearInstances()
        {
            foreach (var e in _entries)
            {
                if (e.Instance != null)
                {
                    Object.Destroy(e.Instance);
                }
            }

            _entries.Clear();
        }
    }
}
