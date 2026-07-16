using Map.Entity;
using Map.Logic.Events;
using My.Config;
using My.Map;
using My.Map.Entity;
using My.MapExport;
using cfg.demo;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static My.Map.UnitMoveBehaveInfo;
using static My.MapExport.MapExportDatabase;

namespace My.Map.Logic
{
    public partial class GameLogicAreaManager
    {

        public class RuntimePathInfo
        {
            public string Name;
            public string Tag;
            public List<Vector2> PointList = new();
            public List<ENamedPointType> PointTypes = new();
            public HashSet<int> SpawnPointIndices = new();
            public int DespawnPointIndex = -1;

            public int GetEndIndex() => DespawnPointIndex >= 0
                ? DespawnPointIndex
                : Mathf.Max(0, PointList.Count - 1);
        }


        public Dictionary<string, RuntimePathInfo> walkerPathDict = new();
        private List<long> SpawnedWalkerRecords = new();
        TownWalkerPopulation WalkerPopulationConfig
        {
            get
            {
                var table = CfgMgr.Cfgs?.TbTownWalkerPopulation;
                var logicAreaId = TownFacilityUtil.ResolveCurrentLogicAreaId(this);
                var state = logicManager?.worldPersistState?.GetLogicAreaHomesteadState(logicAreaId);
                if (table == null || string.IsNullOrEmpty(logicAreaId))
                {
                    return null;
                }

                int prosperity = state?.Prosperity ?? 0;
                TownWalkerPopulation selected = null;
                foreach (var row in table.DataList)
                {
                    if (row == null || row.LogicAreaId != logicAreaId
                        || row.MinProsperity > prosperity
                        || selected != null && row.MinProsperity <= selected.MinProsperity)
                    {
                        continue;
                    }

                    selected = row;
                }

                return selected;
            }
        }

        int WalkerLimit => Mathf.Max(0, WalkerPopulationConfig?.WalkerLimit ?? cacheMapOverlayCfg?.WalkerLimit ?? 0);
        float WalkerSpawnInterval => Mathf.Max(0.25f, WalkerPopulationConfig?.WalkerSpawnInterval ?? cacheMapOverlayCfg?.WalkerSpawnInterval ?? 1.5f);
        float WalkerHotRadius => Mathf.Max(2f, cacheMapOverlayCfg?.WalkerHotRadius ?? 12f);
        float WalkerHiddenRadius => Mathf.Max(0.5f, cacheMapOverlayCfg?.WalkerHiddenRadius ?? 3f);

        string WalkerNormalCfgId => string.IsNullOrEmpty(cacheMapOverlayCfg?.WalkerNormalCfgId)
            ? "civil" : cacheMapOverlayCfg.WalkerNormalCfgId;
        string WalkerAdvancedCfgId => string.IsNullOrEmpty(cacheMapOverlayCfg?.WalkerAdvancedCfgId)
            ? "civil_advanced" : cacheMapOverlayCfg.WalkerAdvancedCfgId;
        string WalkerEliteCfgId => string.IsNullOrEmpty(cacheMapOverlayCfg?.WalkerEliteCfgId)
            ? "civil_elite" : cacheMapOverlayCfg.WalkerEliteCfgId;
        int WalkerNormalWeight => Mathf.Max(0, WalkerPopulationConfig?.WalkerNormalWeight ?? cacheMapOverlayCfg?.WalkerNormalWeight ?? 6);
        int WalkerAdvancedWeight => Mathf.Max(0, WalkerPopulationConfig?.WalkerAdvancedWeight ?? cacheMapOverlayCfg?.WalkerAdvancedWeight ?? 3);
        int WalkerEliteWeight => Mathf.Max(0, WalkerPopulationConfig?.WalkerEliteWeight ?? cacheMapOverlayCfg?.WalkerEliteWeight ?? 1);

        public void InitWalkerPath()
        {
            walkerPathDict.Clear();
            foreach(var one in SpawnedWalkerRecords)
            {
                RequestEntityDestroy(one, "walker_remove"); 
            } 
            SpawnedWalkerRecords.Clear();

            foreach (var path in cacheDatabase.NamedPaths)
            {
                if(path.Tag.StartsWith("walker"))
                {
                    var runtime = new RuntimePathInfo();
                    runtime.Name = path.Name;
                    runtime.Tag = path.Tag;
                    foreach(var pName in path.Points)
                    {
                        var p = cacheDatabase.FindNamedPointByName(pName);
                        if (p == null) 
                        { 
                            continue; 
                        }
                        runtime.PointList.Add(p.Value.Position);
                        runtime.PointTypes.Add(p.Value.PointType);
                        if (IsWalkerSpawnPoint(p.Value.PointType))
                        {
                            runtime.SpawnPointIndices.Add(runtime.PointList.Count - 1);
                        }
                        if (p.Value.PointType == ENamedPointType.WalkerDespawn)
                        {
                            runtime.DespawnPointIndex = runtime.PointList.Count - 1;
                        }
                    }
                    if (runtime.PointList.Count >= 2)
                    {
                        if (runtime.DespawnPointIndex < 0)
                        {
                            runtime.DespawnPointIndex = runtime.PointList.Count - 1;
                        }
                        walkerPathDict[runtime.Name] = runtime;
                    }
                }
            }

            // Portal networks can also provide background routes. The exported node
            // order is used as a deterministic cycle; named paths remain supported.
            if (cacheDatabase.PortalNetworks != null)
            {
                foreach (var network in cacheDatabase.PortalNetworks)
                {
                    if (network?.Nodes == null || network.Nodes.Count < 2)
                    {
                        continue;
                    }

                    var typedNodes = network.Nodes.Where(n => IsWalkerPointType(n.PointType)).ToList();
                    if (typedNodes.Count < 2)
                    {
                        continue;
                    }

                    var cycleNodes = typedNodes.Select(n => n.NodeId)
                        .Where(n => !string.IsNullOrEmpty(n)).ToList();
                    var points = new List<Vector2>();
                    if (cycleNodes.Count < 2 || !PortalPatrolPathBuilder.TryBuildCycleWorldPath(
                            cacheDatabase, network.NetworkId, cycleNodes, points) || points.Count < 2)
                    {
                        continue;
                    }

                    var portalPath = new RuntimePathInfo
                    {
                        Name = $"walker_portal_{network.NetworkId}",
                        Tag = "walker_portal",
                        PointList = points,
                    };
                    portalPath.PointTypes.AddRange(Enumerable.Repeat(
                        ENamedPointType.WalkerTransit, points.Count));
                    foreach (var node in typedNodes)
                    {
                        int pointIndex = FindNearestPointIndex(points, node.Position);
                        if (pointIndex < 0)
                        {
                            continue;
                        }

                        portalPath.PointTypes[pointIndex] = node.PointType;
                        if (IsWalkerSpawnPoint(node.PointType))
                        {
                            portalPath.SpawnPointIndices.Add(pointIndex);
                        }
                        if (node.PointType == ENamedPointType.WalkerDespawn)
                        {
                            portalPath.DespawnPointIndex = pointIndex;
                        }
                    }
                    if (portalPath.DespawnPointIndex < 0)
                    {
                        portalPath.DespawnPointIndex = portalPath.PointList.Count - 1;
                    }
                    walkerPathDict[portalPath.Name] = portalPath;
                }
            }
        }
        public RuntimePathInfo GetRuntimePath(string name)
        {
            walkerPathDict.TryGetValue(name, out var val);
            return val;
        }

        public Vector2? GetRuntimePathPoint(string name, int idx)
        {
            walkerPathDict.TryGetValue(name, out var val);
            if (val == null) return null;
            if(idx < 0 || idx >= val.PointList.Count) return null;
            return val.PointList[idx];
        }

        public int GetRuntimePathEndIndex(string name)
        {
            return GetRuntimePath(name)?.GetEndIndex() ?? -1;
        }

        static bool IsWalkerSpawnPoint(ENamedPointType pointType) =>
            pointType == ENamedPointType.WalkerSpawn || pointType == ENamedPointType.WalkerStart;

        static bool IsWalkerPointType(ENamedPointType pointType) =>
            IsWalkerSpawnPoint(pointType)
            || pointType == ENamedPointType.WalkerTransit
            || pointType == ENamedPointType.WalkerDespawn;

        static int FindNearestPointIndex(List<Vector2> points, Vector3 target)
        {
            int result = -1;
            float best = 0.01f * 0.01f;
            for (int i = 0; i < points.Count; i++)
            {
                float distance = (points[i] - (Vector2)target).sqrMagnitude;
                if (distance <= best)
                {
                    best = distance;
                    result = i;
                }
            }
            return result;
        }

        private float _cleanWalkerTimer = 0;
        private float _refreshWalkerTimer = 0;
        public void TickRefreshWalker()
        {
            TickCleanWalker();

            TickRefreshWalkerSpawn();
        }

        // 仅记录、尚未 Spawn 的行人：用路径索引判断是否走完；与 TickLowFreqTickRecord 里推进 CurrPathIdx 配合
        bool TryGetValidWalkerRuntimePath(string movePathKey, out RuntimePathInfo runtimePath)
        {
            if (string.IsNullOrEmpty(movePathKey))
            {
                runtimePath = null;
                return false;
            }

            runtimePath = GetRuntimePath(movePathKey);
            return runtimePath != null && runtimePath.PointList.Count > 0;
        }

        void RemoveSpawnedWalkerSlot(int listIndex, long recId, string reason)
        {
            SpawnedWalkerRecords.RemoveAt(listIndex);
            RequestEntityDestroy(recId, reason);
        }

        private void TickCleanWalker()
        {
            if (LogicTime.time < _cleanWalkerTimer + 0.5f)
            {
                return;
            }

            _cleanWalkerTimer = LogicTime.time;

            int walkerLimit = WalkerLimit;
            for (int i = SpawnedWalkerRecords.Count - 1; i >= walkerLimit; i--)
            {
                RemoveSpawnedWalkerSlot(i, SpawnedWalkerRecords[i], "walker_population_limit");
            }

            for (int i = SpawnedWalkerRecords.Count - 1; i >= 0; i--)
            {
                long recId = SpawnedWalkerRecords[i];

                if (!Repo.IsLoaded(recId))
                {
                    Repo.Records.TryGetValue(recId, out var rec);
                    if (rec == null || rec is not LogicEntityRecord4Npc unitNpcRec)
                    {
                        RemoveSpawnedWalkerSlot(i, recId, "walker_err");
                        continue;
                    }

                    if (unitNpcRec.MarkDestroyed)
                    {
                        SpawnedWalkerRecords.RemoveAt(i);
                        continue;
                    }

                    if (unitNpcRec.IsForeigner
                        && unitNpcRec.MoveBehaveType == UnitMoveBehaveInfo.EMoveBehaveType.MoveToThenDespawn)
                    {
                        continue;
                    }

                    if (unitNpcRec.MoveBehaveType != UnitMoveBehaveInfo.EMoveBehaveType.MovePath)
                    {
                        RemoveSpawnedWalkerSlot(i, recId, "walker_err_mode");
                        continue;
                    }

                    if (!TryGetValidWalkerRuntimePath(unitNpcRec.MovePath, out var recordPath))
                    {
                        RemoveSpawnedWalkerSlot(i, recId, "walker_err_path");
                        continue;
                    }

                    if (unitNpcRec.CurrPathIdx >= recordPath.GetEndIndex())
                    {
                        RemoveSpawnedWalkerSlot(i, recId, "walker_reach");
                        continue;
                    }

                    continue;
                }

                Repo.Loaded.TryGetValue(recId, out var entity);

                if (entity == null || entity is not NpcUnitLogicEntity unitNpc)
                {
                    RemoveSpawnedWalkerSlot(i, recId, "walker_err");
                    continue;
                }

                if (unitNpc.MarkDestroyed || unitNpc.IsDead)
                {
                    SpawnedWalkerRecords.RemoveAt(i);
                    continue;
                }

                if (unitNpc.IsInCombat)
                {
                    continue;
                }

                if (unitNpc.NpcRecord.IsForeigner
                    && unitNpc.MoveBehaveInfo.MoveBehaveMode == UnitMoveBehaveInfo.EMoveBehaveType.MoveToThenDespawn)
                {
                    continue;
                }

                if (unitNpc.MoveBehaveInfo.MoveBehaveMode != UnitMoveBehaveInfo.EMoveBehaveType.MovePath
                    || string.IsNullOrEmpty(unitNpc.MoveBehaveInfo.MovePath))
                {
                    RemoveSpawnedWalkerSlot(i, recId, "walker_err_mode");
                    continue;
                }

                if (!TryGetValidWalkerRuntimePath(unitNpc.MoveBehaveInfo.MovePath, out var livePath))
                {
                    RemoveSpawnedWalkerSlot(i, recId, "walker_err_path");
                    continue;
                }

                var endP = livePath.PointList[livePath.GetEndIndex()];
                var diff = endP - entity.Pos;

                if (diff.magnitude < 0.3f)
                {
                    RemoveSpawnedWalkerSlot(i, recId, "walker_reach");
                    continue;
                }
            }
        }
    
        private void TickRefreshWalkerSpawn()
        {
            if (LogicTime.time < _refreshWalkerTimer + WalkerSpawnInterval)
            {
                return;
            }

            _refreshWalkerTimer = LogicTime.time;
            var pathList = walkerPathDict.Values.ToList();
            if(pathList.Count == 0)
            {
                return;
            }

            if (WalkerLimit <= 0 || SpawnedWalkerRecords.Count >= WalkerLimit)
            {
                return;
            }

            if (!TryPickWalkerSpawn(pathList, out var path, out var pointIndex))
            {
                return;
            }

            var spawnPoint = path.PointList[pointIndex];
            var lastPoint = path.PointList[path.GetEndIndex()];

            var rec = new LogicEntityRecord4Npc()
            {
                Id = GameLogicManager.LogicEntityIdInst++,
                EntityType = EEntityType.Npc,
                CfgId = PickWalkerNpcCfgId(),
                Position = spawnPoint,
                FactionId = EFactionId.Citizen,

                IsPeace = false,
                MoveBehaveType = EMoveBehaveType.MovePath,
                MovePath = path.Name,
                MoveToDespawnTarget = lastPoint,
                CurrPathIdx = pointIndex,
                CurrPathProgress = 0f,

                EnmityConfId = "default_npc",
                IsForeigner = true,
            };

            logicManager.AddNewEntityRecord(rec);

            SpawnedWalkerRecords.Add(rec.Id);
        }

        private string PickWalkerNpcCfgId()
        {
            int normalWeight = WalkerNormalWeight;
            int advancedWeight = WalkerAdvancedWeight;
            int eliteWeight = WalkerEliteWeight;
            int totalWeight = normalWeight + advancedWeight + eliteWeight;
            if (totalWeight <= 0)
            {
                return WalkerNormalCfgId;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            if (roll < normalWeight) return WalkerNormalCfgId;
            if (roll < normalWeight + advancedWeight) return WalkerAdvancedCfgId;
            return WalkerEliteCfgId;
        }

        private bool TryPickWalkerSpawn(List<RuntimePathInfo> paths, out RuntimePathInfo selectedPath, out int selectedIndex)
        {
            selectedPath = null;
            selectedIndex = -1;

            var player = logicManager.playerLogicEntity;
            if (player == null)
            {
                selectedPath = paths[UnityEngine.Random.Range(0, paths.Count)];
                selectedIndex = UnityEngine.Random.Range(0, selectedPath.PointList.Count - 1);
                return true;
            }

            var candidates = new List<(RuntimePathInfo path, int index, float weight)>();
            var view = player.GetViewRangeAndAngle();
            foreach (var path in paths)
            {
                for (int i = 0; i < path.PointList.Count - 1; i++)
                {
                    if (path.SpawnPointIndices.Count > 0 && !path.SpawnPointIndices.Contains(i))
                    {
                        continue;
                    }

                    var point = path.PointList[i];
                    var distance = Vector2.Distance(player.Pos, point);
                    if (distance > WalkerHotRadius || distance < WalkerHiddenRadius)
                    {
                        continue;
                    }

                    bool visible = logicManager.visionSenser != null
                        && logicManager.visionSenser.SimpleCanSee(
                            player.Pos, player.CurrentLook, point, view.Item1, view.Item2);
                    if (visible)
                    {
                        continue;
                    }

                    candidates.Add((path, i, 1f / (1f + distance)));
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            float totalWeight = candidates.Sum(c => c.weight);
            float roll = UnityEngine.Random.value * totalWeight;
            foreach (var candidate in candidates)
            {
                roll -= candidate.weight;
                if (roll <= 0f)
                {
                    selectedPath = candidate.path;
                    selectedIndex = candidate.index;
                    return true;
                }
            }

            var last = candidates[candidates.Count - 1];
            selectedPath = last.path;
            selectedIndex = last.index;
            return true;
        }
    }
}
