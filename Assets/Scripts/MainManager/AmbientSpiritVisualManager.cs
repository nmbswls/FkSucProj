using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Map;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.View
{
    // 后场氛围影怪：玩家附近徘徊「或」在给定世界矩形外缘横穿闪过 → 隐身再现身；掠过范围由玩家理智与欲望环半径决定（不绑相机）。
    public sealed class AmbientSpiritVisualManager
    {
        private const string AmbientVisualResourcesRoot = "SpiritMonsterAmbient";

        private enum AmbientSpiritPhase
        {
            Wander,
            Sweep,
            Hidden,
        }

        private sealed class AmbientSpiritEntry
        {
            public GameObject Instance;
            public Animator CachedAnimator;
            public string VisualPrefabKey = string.Empty;

            public AmbientSpiritPhase Phase;
            public float PhaseEndLogicTime;

            public Vector2 BaseOffsetRel;
            public Vector2 WanderSeed;

            public float WanderStaggerNoise;

            public Vector2 SweepStartWorld;
            public Vector2 SweepEndWorld;
            public float SweepStartLogicTime;
            public float SweepDuration;
        }

        private readonly GameLogicManager _glm;
        private readonly Transform _visualRoot;

        private readonly List<AmbientSpiritEntry> _entries = new();
        private readonly List<SpiritMonsterTypeBudget> _eligibleBudgetRows = new();
        private string _lastAmbientRebuildKey = string.Empty;

        private const float WanderMin = 1.25f;
        private const float WanderMax = 2.75f;

        // 徘徊层内半径：仅由 max 推导，避免与横扫/矩形策略叠加重复配置 min
        private const float WanderInnerRadiusFromMax = 0.42f;

        private const float HiddenMin = 0.22f;
        private const float HiddenMax = 0.92f;

        public AmbientSpiritVisualManager(GameLogicManager glm, Transform visualRoot)
        {
            _glm = glm;
            _visualRoot = visualRoot;
        }

        private PlayerDesireLevel TryGetPlayerDesireCfg()
        {
            if (_glm.playerLogicEntity == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbPlayerDesireLevel.GetOrDefault(_glm.playerLogicEntity.DesireLevel);
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
                if (row == null || row.SpawnWeight <= 0 || string.IsNullOrEmpty(row.NpcBaseType))
                {
                    continue;
                }

                if (desireLevel < row.MinDesireLevel)
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
            if (nextKey != _lastAmbientRebuildKey)
            {
                Rebuild(desireCfg);
                _lastAmbientRebuildKey = nextKey;
            }

            if (_entries.Count == 0 || _eligibleBudgetRows.Count == 0)
            {
                return;
            }

            Vector2 anchor = _glm.playerLogicEntity.Pos;
            float drift = Mathf.Max(0.25f, desireCfg.AmbientSpiritDriftSpeed);
            float rMax = Mathf.Max(0.2f, desireCfg.AmbientSpiritRadiusMax);
            float rMin = Mathf.Max(0.1f, rMax * WanderInnerRadiusFromMax);

            float lt = LogicTime.time;
            foreach (var e in _entries)
            {
                if (e?.Instance == null)
                {
                    continue;
                }

                TickOneEntry(e, anchor, drift, lt, rMin, rMax);
            }
        }

        private void TickOneEntry(AmbientSpiritEntry e, Vector2 anchor, float drift, float logicTimeNow, float rMin,
            float rMax)
        {
            switch (e.Phase)
            {
                case AmbientSpiritPhase.Hidden:
                    if (logicTimeNow >= e.PhaseEndLogicTime)
                    {
                        BeginAppearCycle(e, anchor, rMin, rMax, drift);
                    }

                    break;

                case AmbientSpiritPhase.Wander:
                    {
                        float wAmp = Mathf.Lerp(0.12f, 0.4f, Mathf.Clamp01(drift / 4f));

                        float t = LogicTime.time * (1.05f + 0.42f * drift) + e.WanderStaggerNoise;

                        Vector2 jig = new(
                            Mathf.Sin(t * 2.05f + e.WanderSeed.x) +
                            Mathf.Cos(t * 1.31f + e.WanderSeed.y * 0.41f),
                            Mathf.Cos(t * 1.91f + e.WanderSeed.y) +
                            Mathf.Sin(t * 1.57f + e.WanderSeed.x * 0.33f));

                        jig *= 0.225f * wAmp;

                        ApplyWorldPos(e, anchor + e.BaseOffsetRel + jig);
                        SetAnimatorGhostSpeed(e.CachedAnimator, Mathf.Max(0.22f, drift * 0.48f));

                        if (logicTimeNow >= e.PhaseEndLogicTime)
                        {
                            EnterHidden(e, logicTimeNow);
                        }

                        break;
                    }

                case AmbientSpiritPhase.Sweep:
                    {
                        float u = e.SweepDuration <= 1e-5f
                            ? 1f
                            : Mathf.Clamp01((logicTimeNow - e.SweepStartLogicTime) / e.SweepDuration);

                        // 正交横穿：线性插值更接近「一道影子闪过」
                        Vector2 pos = Vector2.Lerp(e.SweepStartWorld, e.SweepEndWorld, u);

                        ApplyWorldPos(e, pos);

                        SetAnimatorGhostSpeed(e.CachedAnimator, Mathf.Max(1.25f, drift * 2.1f));

                        if (u >= 1f - 1e-4f || logicTimeNow >= e.PhaseEndLogicTime + 0.05f)
                        {
                            EnterHidden(e, logicTimeNow);
                        }

                        break;
                    }
            }
        }

        private static void ApplyWorldPos(AmbientSpiritEntry e, Vector2 posWorld)
        {
            if (e?.Instance == null)
            {
                return;
            }

            var tr = e.Instance.transform;
            tr.position = new Vector3(posWorld.x, posWorld.y, tr.position.z);
        }

        private static void SetAnimatorGhostSpeed(Animator anim, float speed)
        {
            if (anim != null && anim.isInitialized)
            {
                anim.speed = Mathf.Clamp(speed, 0.12f, 3.2f);
            }
        }

        private static void EnterHidden(AmbientSpiritEntry e, float logicTimeNow)
        {
            e.Phase = AmbientSpiritPhase.Hidden;
            e.PhaseEndLogicTime = logicTimeNow + Random.Range(HiddenMin, HiddenMax);
            if (e.Instance != null)
            {
                e.Instance.SetActive(false);
            }
        }

        private Vector2 Polar(float angRad, float rad)
        {
            return new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad)) * rad;
        }

        // 以玩家为中心的世界矩形：半边长 = 基础(随理智)+欲望环；起终点在边外 margin，横穿模拟「掠过屏幕」。
        private void BeginScreenCrossFlashSweep(AmbientSpiritEntry e, Vector2 anchor, float rMin, float rMax, float drift)
        {
            float san01 = 1f;
            if (_glm.playerLogicEntity != null)
            {
                san01 = Mathf.Clamp01(_glm.playerLogicEntity.GetAttr(AttrIdConsts.PlayerSanity) / 100_000f);
            }

            float halfSpan = Mathf.Lerp(17f, 10f, san01) + rMax * 0.4f;
            halfSpan = Mathf.Max(halfSpan, rMin + 4f);

            float vxMin = anchor.x - halfSpan;
            float vxMax = anchor.x + halfSpan;
            float vyMin = anchor.y - halfSpan;
            float vyMax = anchor.y + halfSpan;

            float vw = vxMax - vxMin;
            float vh = vyMax - vyMin;
            float marginX = Mathf.Max(0.4f, vw * 0.08f);
            float marginY = Mathf.Max(0.4f, vh * 0.08f);

            float yLo = vyMin + marginY * 0.3f;
            float yHi = vyMax - marginY * 0.3f;
            float xLo = vxMin + marginX * 0.3f;
            float xHi = vxMax - marginX * 0.3f;

            int mode = Random.Range(0, 10);

            Vector2 a;
            Vector2 b;

            if (mode <= 4)
            {
                float y0 = Random.Range(yLo, yHi);
                float y1 = Mathf.Clamp(y0 + Random.Range(-vh * 0.14f, vh * 0.14f), vyMin + marginY * 0.1f, vyMax - marginY * 0.1f);
                if (Random.value < 0.55f)
                {
                    a = new Vector2(vxMax + marginX, y0);
                    b = new Vector2(vxMin - marginX, y1);
                }
                else
                {
                    a = new Vector2(vxMin - marginX, y0);
                    b = new Vector2(vxMax + marginX, y1);
                }
            }
            else if (mode <= 7)
            {
                float x0 = Random.Range(xLo, xHi);
                float x1 = Mathf.Clamp(x0 + Random.Range(-vw * 0.12f, vw * 0.12f), vxMin + marginX * 0.1f, vxMax - marginX * 0.1f);
                if (Random.value < 0.5f)
                {
                    a = new Vector2(x0, vyMax + marginY);
                    b = new Vector2(x1, vyMin - marginY);
                }
                else
                {
                    a = new Vector2(x0, vyMin - marginY);
                    b = new Vector2(x1, vyMax + marginY);
                }
            }
            else
            {
                a = new Vector2(vxMax + marginX, Random.Range(yLo, yHi));
                b = new Vector2(vxMin - marginX, Random.Range(yLo, yHi));
            }

            e.SweepStartWorld = a;
            e.SweepEndWorld = b;
            e.SweepStartLogicTime = LogicTime.time;

            float chord = Vector2.Distance(a, b);
            float speed = Random.Range(34f, 56f) * Mathf.Lerp(0.88f, 1.32f, Mathf.Clamp01(drift / 4f));
            e.SweepDuration = Mathf.Clamp(chord / speed, 0.26f, 1.08f);

            ApplyWorldPos(e, e.SweepStartWorld);

            e.PhaseEndLogicTime = e.SweepStartLogicTime + e.SweepDuration + 0.15f;

            SetAnimatorGhostSpeed(e.CachedAnimator, Mathf.Max(1.15f, drift * 2.05f));
        }

        private void BeginAppearCycle(AmbientSpiritEntry e, Vector2 anchor, float rMin, float rMax, float drift)
        {
            if (CfgMgr.Cfgs == null || _eligibleBudgetRows.Count == 0)
            {
                e.PhaseEndLogicTime = LogicTime.time + 0.35f;
                return;
            }

            var row = PickWeightedSpawnRow(_eligibleBudgetRows);
            if (row == null || !EnsurePrefab(e, row.NpcBaseType))
            {
                e.Phase = AmbientSpiritPhase.Hidden;
                e.PhaseEndLogicTime = LogicTime.time + Random.Range(HiddenMin, HiddenMax);
                return;
            }

            float rBand = Mathf.Lerp(rMin, rMax, 0.14f + Random.value * 0.72f);
            float ang = Random.Range(0f, Mathf.PI * 2f);

            bool doWander = Random.value < 0.53f;

            if (doWander)
            {
                e.Phase = AmbientSpiritPhase.Wander;
                e.BaseOffsetRel = Polar(ang, rBand);

                ApplyWorldPos(e, anchor + e.BaseOffsetRel + Polar(Random.Range(0f, Mathf.PI * 2f), Random.Range(0.02f, 0.1f)));

                float dwell = Random.Range(WanderMin, WanderMax);

                dwell += Mathf.Repeat(Mathf.Abs(e.WanderStaggerNoise), 0.42f);

                e.PhaseEndLogicTime = LogicTime.time + dwell;
                SetAnimatorGhostSpeed(e.CachedAnimator, Mathf.Max(0.28f, 1f));
            }
            else
            {
                e.Phase = AmbientSpiritPhase.Sweep;
                BeginScreenCrossFlashSweep(e, anchor, rMin, rMax, drift);
            }

            if (e.Instance != null && !e.Instance.activeSelf)
            {
                e.Instance.SetActive(true);
            }
        }

        private bool EnsurePrefab(AmbientSpiritEntry e, string visualKey)
        {
            if (string.IsNullOrEmpty(visualKey))
            {
                return false;
            }

            if (string.Equals(e.VisualPrefabKey, visualKey, System.StringComparison.Ordinal) && e.Instance != null)
            {
                return true;
            }

            if (e.Instance != null)
            {
                Object.Destroy(e.Instance);

                e.Instance = null;

                e.CachedAnimator = null;

                e.VisualPrefabKey = string.Empty;
            }

            string path = $"{AmbientVisualResourcesRoot}/{visualKey}";

            var prefab = Resources.Load<GameObject>(path);

            if (prefab == null)
            {

                Debug.LogWarning($"AmbientSpiritVisualManager prefab missing at Resources/{path}");

                return false;
            }

            var go = Object.Instantiate(prefab, _visualRoot);

            go.name = $"{visualKey}_ambient";

            e.Instance = go;

            e.CachedAnimator = go.GetComponentInChildren<Animator>();

            e.VisualPrefabKey = visualKey;

            return true;
        }

        private string BuildAmbientRebuildKey(int desireLevel, PlayerDesireLevel c)
        {
            if (c == null)
            {

                return string.Empty;
            }

            CollectEligibleSpiritRows(desireLevel);

            _eligibleBudgetRows.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            var sig = new System.Text.StringBuilder();

            for (int i = 0; i < _eligibleBudgetRows.Count; i++)
            {

                if (i > 0)
                {

                    sig.Append(',');

                }

                sig.Append(_eligibleBudgetRows[i].Id);

            }

            return
                $"{desireLevel}|{c.AmbientSpiritCount}|{sig}|{c.AmbientSpiritRadiusMax:F2}|{c.AmbientSpiritDriftSpeed:F2}";
        }

        private void Rebuild(PlayerDesireLevel desireCfg)
        {
            ClearInstances();

            if (_visualRoot == null)
            {

                Debug.LogError("AmbientSpiritVisualManager: visual root is null.");

                return;
            }

            if (desireCfg.AmbientSpiritCount <= 0 || _glm.playerLogicEntity == null)
            {

                return;
            }

            CollectEligibleSpiritRows(_glm.playerLogicEntity.DesireLevel);

            if (_eligibleBudgetRows.Count == 0)
            {

                return;
            }

            float lt = LogicTime.time;

            int n = desireCfg.AmbientSpiritCount;

            for (int i = 0; i < n; i++)
            {

                var row = PickWeightedSpawnRow(_eligibleBudgetRows);

                if (row == null)
                {

                    break;
                }

                var ent = new AmbientSpiritEntry
                {

                    WanderStaggerNoise = Random.Range(-Mathf.PI, Mathf.PI) + i * 0.37f,

                    WanderSeed = Random.insideUnitCircle * Mathf.PI,

                };

                if (!EnsurePrefab(ent, row.NpcBaseType))
                {

                    continue;
                }

                ent.Phase = AmbientSpiritPhase.Hidden;

                float frac = i / Mathf.Max(1f, n - 1f);

                float firstPop = Mathf.Lerp(0.04f + HiddenMin * Random.value, Mathf.Min(1.8f + HiddenMin, HiddenMax + 2.2f * frac),

                    Random.Range(0.25f, 0.92f));

                ent.PhaseEndLogicTime = lt + firstPop;

                if (ent.Instance != null)

                {

                    ent.Instance.SetActive(false);

                    ent.Instance.name = $"{row.NpcBaseType}_ambient_{i}";

                }

                _entries.Add(ent);

            }
        }

        private void ClearInstances()
        {

            foreach (var e in _entries)
            {

                if (e?.Instance != null)
                {

                    Object.Destroy(e.Instance);

                }

            }

            _entries.Clear();

        }

    }

}
