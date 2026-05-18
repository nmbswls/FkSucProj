using Map.Entity;
using Map.Logic.Events;
using My.Map;
using My.Map.Entity;
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
        }


        public Dictionary<string, RuntimePathInfo> walkerPathDict = new();
        private List<long> SpawnedWalkerRecords = new();
        public long AreaWalkerLimit = 1;

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
                    }
                    walkerPathDict.Add(runtime.Name, runtime);
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

                    if (unitNpcRec.CurrPathIdx >= recordPath.PointList.Count - 1)
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

                var endP = livePath.PointList[livePath.PointList.Count - 1];
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
            if (LogicTime.time < _refreshWalkerTimer + 3f)
            {
                return;
            }

            _refreshWalkerTimer = LogicTime.time;
            var pathList = walkerPathDict.Values.ToList();
            if(pathList.Count == 0)
            {
                return;
            }

            if (SpawnedWalkerRecords.Count < AreaWalkerLimit)
            {

                //var pathIdx = UnityEngine.Random.Range(0, pathList.Count);
                var pathIdx = 0;
                var path = pathList[pathIdx];

                if (path.PointList.Count < 1)
                {
                    return;
                }

                var firstPoint = path.PointList[0];
                var lastPoint = path.PointList[path.PointList.Count - 1];

                var rec = new LogicEntityRecord4Npc()
                {
                    Id = GameLogicManager.LogicEntityIdInst++,
                    EntityType = EEntityType.Npc,
                    CfgId = "civil",
                    Position = firstPoint,
                    FactionId = EFactionId.Citizen,

                    IsPeace = false,
                    MoveBehaveType = EMoveBehaveType.MoveToThenDespawn,
                    MovePath = null,
                    MoveToDespawnTarget = lastPoint,
                    CurrPathIdx = 0,
                    CurrPathProgress = 0f,

                    EnmityConfId = "default_npc",
                    IsForeigner = true,
                };

                logicManager.AddNewEntityRecord(rec);

                SpawnedWalkerRecords.Add(rec.Id);
            }
        }
    }
}

 