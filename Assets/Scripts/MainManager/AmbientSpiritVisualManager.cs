using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Map;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.View
{
    // 后场氛围影怪：仅在「刷新出现」瞬时用玩家附近坐标生成世界锚点，之后与普通单位一样按自有世界坐标运动；不出现快速掠过屏幕。
    public sealed class AmbientSpiritVisualManager
    {
        private const string AmbientVisualResourcesRoot = "SpiritMonsterAmbient";

        private enum AmbientSpiritPhase
        {
            IdleIntro,
            WalkOut,
            Hidden,
        }

        private sealed class AmbientSpiritEntry
        {
            public GameObject Instance;
            public Animator CachedAnimator;
            public string VisualPrefabKey = string.Empty;

            public AmbientSpiritPhase Phase;
            public float PhaseEndLogicTime;

            // 出现当帧写入的世界坐标锚点（不再每帧跟随玩家）
            public Vector2 SpawnWorld;
            public Vector2 WanderSeed;
            public float WanderStaggerNoise;

            public Vector2 MoveFromWorld;
            public Vector2 DepartTargetWorld;
            public float MoveStartLogicTime;
            public float MoveDuration;
        }

        private readonly GameLogicManager _glm;
        private readonly Transform _visualRoot;

        private readonly List<AmbientSpiritEntry> _entries = new();
        private readonly List<SpiritMonsterTypeBudget> _eligibleBudgetRows = new();
        private string _lastAmbientRebuildKey = string.Empty;

        private const float IdleAnimMin = 0.85f;
        private const float IdleAnimMax = 2.15f;

        // 离场位移：相对当前世界位置的一段步行距离（世界坐标，与玩家后续移动无关）
        private const float DepartDistMin = 2.1f;
        private const float DepartDistMax = 6.6f;

        // 徘徊内圈 = 外环 * 比例；外环由 Tick 内 rMax 推导（与理智等无关的占位，保持原结构）
        private const float WanderInnerRadiusFromMax = 0.42f;

        private const float HiddenMin = 0.22f;
        private const float HiddenMax = 0.92f;

        public AmbientSpiritVisualManager(GameLogicManager glm, Transform visualRoot)
        {
            _glm = glm;
            _visualRoot = visualRoot;
        }

        private PlayerSanCorruptLevel TryGetPlayerSanCorruptCfg()
        {
            if (_glm.playerLogicEntity == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbPlayerSanCorruptLevel.GetOrDefault(_glm.playerLogicEntity.SanCorruptLevel);
        }

        private void CollectEligibleSpiritRows(int sanCorruptLevel)
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

                if (sanCorruptLevel < row.MinSanCorruptLevel)
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
            if (_glm.GameSession.PlayerHumanMode)
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

            var sanCorruptCfg = TryGetPlayerSanCorruptCfg();
            if (sanCorruptCfg == null)
            {
                if (_entries.Count != 0)
                {
                    ClearInstances();
                }

                _lastAmbientRebuildKey = string.Empty;
                return;
            }

            int lv = _glm.playerLogicEntity.SanCorruptLevel;
            string nextKey = BuildAmbientRebuildKey(lv, sanCorruptCfg);
            if (nextKey != _lastAmbientRebuildKey)
            {
                Rebuild(sanCorruptCfg);
                _lastAmbientRebuildKey = nextKey;
            }

            if (_entries.Count == 0 || _eligibleBudgetRows.Count == 0)
            {
                return;
            }

            Vector2 anchor = _glm.playerLogicEntity.Pos;
            float drift = Mathf.Max(0.25f, sanCorruptCfg.AmbientSpiritDriftSpeed);

            float rMax = 4.0f;
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

                case AmbientSpiritPhase.IdleIntro:
                    {
                        float wAmp = Mathf.Lerp(0.06f, 0.14f, Mathf.Clamp01(drift / 4f));
                        float t = LogicTime.time * (1.05f + 0.35f * drift) + e.WanderStaggerNoise;

                        Vector2 jig = new(
                            Mathf.Sin(t * 2.05f + e.WanderSeed.x) +
                            Mathf.Cos(t * 1.31f + e.WanderSeed.y * 0.41f),
                            Mathf.Cos(t * 1.91f + e.WanderSeed.y) +
                            Mathf.Sin(t * 1.57f + e.WanderSeed.x * 0.33f));

                        jig *= 0.12f * wAmp;

                        ApplyWorldPos(e, e.SpawnWorld + jig);
                        SetAnimatorGhostSpeed(e.CachedAnimator, Mathf.Clamp(0.2f + drift * 0.22f, 0.18f, 0.62f));

                        if (logicTimeNow >= e.PhaseEndLogicTime)
                        {
                            BeginDepartWalk(e, logicTimeNow, drift);
                        }

                        break;
                    }

                case AmbientSpiritPhase.WalkOut:
                    {
                        float u = e.MoveDuration <= 1e-5f
                            ? 1f
                            : Mathf.Clamp01((logicTimeNow - e.MoveStartLogicTime) / e.MoveDuration);

                        Vector2 pos = Vector2.Lerp(e.MoveFromWorld, e.DepartTargetWorld, u);

                        ApplyWorldPos(e, pos);

                        SetAnimatorGhostSpeed(e.CachedAnimator, Mathf.Clamp(0.38f + drift * 0.18f, 0.32f, 1.05f));

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

        private void BeginDepartWalk(AmbientSpiritEntry e, float logicTimeNow, float drift)
        {
            Vector2 from = e.SpawnWorld;
            if (e.Instance != null)
            {
                var p = e.Instance.transform.position;
                from = new Vector2(p.x, p.y);
            }

            e.MoveFromWorld = from;
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(DepartDistMin, DepartDistMax);
            e.DepartTargetWorld = from + Polar(ang, dist);

            float chord = Vector2.Distance(e.MoveFromWorld, e.DepartTargetWorld);
            float walkSpeed = Mathf.Lerp(1.25f, 2.65f, Mathf.Clamp01(drift / 4f));
            e.MoveDuration = Mathf.Clamp(chord / Mathf.Max(0.2f, walkSpeed), 0.5f, 6f);
            e.MoveStartLogicTime = logicTimeNow;
            e.Phase = AmbientSpiritPhase.WalkOut;
            e.PhaseEndLogicTime = logicTimeNow + e.MoveDuration + 0.12f;
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

            e.SpawnWorld = anchor + Polar(ang, rBand) + Polar(Random.Range(0f, Mathf.PI * 2f), Random.Range(0.02f, 0.12f));

            ApplyWorldPos(e, e.SpawnWorld);

            e.Phase = AmbientSpiritPhase.IdleIntro;
            float dwell = Random.Range(IdleAnimMin, IdleAnimMax);
            dwell += Mathf.Repeat(Mathf.Abs(e.WanderStaggerNoise), 0.42f);
            e.PhaseEndLogicTime = LogicTime.time + dwell;
            SetAnimatorGhostSpeed(e.CachedAnimator, Mathf.Clamp(0.22f + drift * 0.12f, 0.2f, 0.55f));

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

        static int ResolveAmbientSpiritCount(int sanLevel)
        {
            if (sanLevel <= 0)
            {
                return 0;
            }

            if (sanLevel == 1)
            {
                return 3;
            }

            if (sanLevel == 2)
            {
                return 5;
            }

            if (sanLevel == 3)
            {
                return 8;
            }

            return 10;
        }

        private string BuildAmbientRebuildKey(int sanLevel, PlayerSanCorruptLevel c)
        {
            if (c == null)
            {

                return string.Empty;
            }

            CollectEligibleSpiritRows(sanLevel);

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
                $"{sanLevel}|{sig}";
        }

        private void Rebuild(PlayerSanCorruptLevel sanCfg)
        {
            ClearInstances();

            if (_visualRoot == null)
            {

                Debug.LogError("AmbientSpiritVisualManager: visual root is null.");

                return;
            }

            int n = ResolveAmbientSpiritCount(_glm.playerLogicEntity.SanCorruptLevel);

            if (n <= 0 || _glm.playerLogicEntity == null)
            {

                return;
            }

            CollectEligibleSpiritRows(_glm.playerLogicEntity.SanCorruptLevel);

            if (_eligibleBudgetRows.Count == 0)
            {

                return;
            }

            float lt = LogicTime.time;

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
