using Map.Entity;
using Map.Logic.Events;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static My.Map.UnitMoveBehaveInfo;
using static My.MapExport.MapExportDatabase;

namespace My.Map.Logic
{

    /// <summary>
    /// 管理区域
    /// </summary>
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

            TickRefreshWaler();
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
                // 先只处理loaded
                if (!Repo.IsLoaded(recId))
                {
                    Repo.Records.TryGetValue(recId, out var rec);
                    if(rec == null || rec is not LogicEntityRecord4UnitBase unitRec)
                    {
                        SpawnedWalkerRecords.RemoveAt(i);
                        RequestEntityDestroy(recId, "err");
                        continue;
                    }

                    var path = GetRuntimePath(unitRec.MovePath);
                    if (unitRec.CurrPathIdx >= path.PointList.Count - 1)
                    {
                        Debug.Log("TickCleanWalker unloaded rec destroyed");
                        SpawnedWalkerRecords.RemoveAt(i);
                        RequestEntityDestroy(recId, "walker_reach");
                        continue;
                    }
                }
                else
                {
                    Repo.Loaded.TryGetValue(recId, out var entity);

                    if (entity == null || entity is not BaseUnitLogicEntity unitEntity)
                    {
                        Debug.LogError($"TickRefreshWalker invalid {recId}");
                        SpawnedWalkerRecords.RemoveAt(i);
                        RequestEntityDestroy(recId, "err");
                        continue;
                    }

                    if (unitEntity.combatStateComp.CombatState != EntityCombatStateComp.ECombatState.NotCombat)
                    {
                        continue;
                    }

                    if (unitEntity.MoveBehaveInfo.MoveBehaveMode != UnitMoveBehaveInfo.EMoveBehaveType.MovePath
                        || string.IsNullOrEmpty(unitEntity.MoveBehaveInfo.MovePath))
                    {
                        SpawnedWalkerRecords.RemoveAt(i);
                        RequestEntityDestroy(recId, "err");
                        continue;
                    }

                    walkerPathDict.TryGetValue(unitEntity.MoveBehaveInfo.MovePath, out var path);
                    var endP = path.PointList[path.PointList.Count - 1];
                    var diff = endP - entity.Pos;

                    // 检查到达
                    if (diff.magnitude < 0.3f)
                    {
                        SpawnedWalkerRecords.RemoveAt(i);
                        RequestEntityDestroy(recId, "walker_reach");
                        continue;
                    }
                }
            }
        }
    
        private void TickRefreshWaler()
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

                var firstPoint = path.PointList.First();

                var rec = new LogicEntityRecord4UnitBase()
                {
                    Id = GameLogicManager.LogicEntityIdInst++,
                    EntityType = EEntityType.Npc,
                    CfgId = "civil",
                    Position = firstPoint,
                    FactionId = EFactionId.Citizen,

                    IsPeace = false,
                    MoveBehaveType = EMoveBehaveType.MovePath,
                    MovePath = path.Name,

                    EnmityConfId = "default_npc",
                };

                logicManager.AddNewEntityRecord(rec);

                SpawnedWalkerRecords.Add(rec.Id);
            }
        }
    }
}

 