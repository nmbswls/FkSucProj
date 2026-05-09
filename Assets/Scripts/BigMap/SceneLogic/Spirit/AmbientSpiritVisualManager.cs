using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using UnityEngine;

namespace My.Map.Logic.Spirit
{
    // 后场氛围影怪（假影怪）：无逻辑实体，仅用与实战影怪一致的外观在玩家外围漂移。
    // 预制体路径：Resources/<AmbientVisualResourcesRoot>/<TbUnitNpc.prefab_name>，
    // 其中 prefab_name 由 spirit_monster_budget 行的 npc_cfg_id 解析得到。
    public sealed class AmbientSpiritVisualManager
    {
        // Resources 子目录名，预制体资源名与 TbUnitNpc.prefab_name 一致（如 h_spirit_small）
        private const string AmbientVisualResourcesRoot = "SpiritMonsterAmbient";

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
        private readonly List<SpiritMonsterTypeBudget> _eligibleBudgetRows = new();
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

        private void CollectEligibleSpiritRows(int desireLevel)
        {
            _eligibleBudgetRows.Clear();
            if (CfgMgr.Cfgs == null)
            {
                return;
            }

            foreach (var row in CfgMgr.Cfgs.TbSpiritMonsterTypeBudget.DataList)
            {
                if (row == null || row.SpawnWeight <= 0)
                {
                    continue;
                }

                if (desireLevel < row.MinDesireLevel)
                {
                    continue;
                }

                var npc = CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(row.NpcCfgId);
                if (npc == null || string.IsNullOrEmpty(npc.PrefabName))
                {
                    continue;
                }

                _eligibleBudgetRows.Add(row);
            }
        }

        private static SpiritMonsterTypeBudget PickWeightedSpawnRow(List<SpiritMonsterTypeBudget> pool)
        {
            int totalW = 0;
            foreach (var r in pool)
            {
                totalW += r.SpawnWeight;
            }

            if (totalW <= 0)
            {
                return null;
            }

            int roll = Random.Range(0, totalW);
            foreach (var r in pool)
            {
                roll -= r.SpawnWeight;
                if (roll < 0)
                {
                    return r;
                }
            }

            return pool[pool.Count - 1];
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

            int lv = _glm.playerLogicEntity.DesireLevel;
            string nextKey = BuildAmbientRebuildKey(lv, desireCfg);
            bool needRebuild = nextKey != _lastAmbientRebuildKey;

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

        private string BuildAmbientRebuildKey(int desireLevel, PlayerDesireLevel c)
        {
            if (c == null)
            {
                return string.Empty;
            }

            CollectEligibleSpiritRows(desireLevel);
            _eligibleBudgetRows.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            System.Text.StringBuilder sig = new System.Text.StringBuilder();
            for (int i = 0; i < _eligibleBudgetRows.Count; i++)
            {
                if (i > 0)
                {
                    sig.Append(',');
                }

                sig.Append(_eligibleBudgetRows[i].Id);
            }

            return $"{desireLevel}|{c.AmbientSpiritCount}|{sig}|{c.AmbientSpiritRadiusMin:F2}|{c.AmbientSpiritRadiusMax:F2}|{c.AmbientSpiritDriftSpeed:F2}";
        }

        private void Rebuild(PlayerDesireLevel desireCfg)
        {
            ClearInstances();

            if (desireCfg.AmbientSpiritCount <= 0 || _glm.playerLogicEntity == null)
            {
                return;
            }

            CollectEligibleSpiritRows(_glm.playerLogicEntity.DesireLevel);
            if (_eligibleBudgetRows.Count == 0)
            {
                return;
            }

            _fallbackParent ??= FindSceneRootFallback();

            float rMin = Mathf.Max(0.1f, Mathf.Min(desireCfg.AmbientSpiritRadiusMin, desireCfg.AmbientSpiritRadiusMax));
            float rMax = Mathf.Max(rMin + 0.1f, Mathf.Max(desireCfg.AmbientSpiritRadiusMin, desireCfg.AmbientSpiritRadiusMax));

            Vector2 anchor = _glm.playerLogicEntity.Pos;

            for (int i = 0; i < desireCfg.AmbientSpiritCount; i++)
            {
                var row = PickWeightedSpawnRow(_eligibleBudgetRows);
                if (row == null)
                {
                    break;
                }

                if (CfgMgr.Cfgs == null)
                {
                    break;
                }

                var npc = CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(row.NpcCfgId);
                if (npc == null || string.IsNullOrEmpty(npc.PrefabName))
                {
                    continue;
                }

                string loadPath = $"{AmbientVisualResourcesRoot}/{npc.PrefabName}";
                var prefab = Resources.Load<GameObject>(loadPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"AmbientSpiritVisualManager prefab missing at Resources/{loadPath}");
                    continue;
                }

                var go = Object.Instantiate(prefab, _fallbackParent);
                go.name = $"{npc.PrefabName}_ambient_{i}";

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
