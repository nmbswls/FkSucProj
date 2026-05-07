using Map.Entity;
using Map.Logic.Events;
using My.Map.Entity;
using My.Map.Fight;
using My.MapExport;
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
                    if(rec == null || rec is not LogicEntityRecord4Npc unitNpcRec)
                    {
                        SpawnedWalkerRecords.RemoveAt(i);
                        RequestEntityDestroy(recId, "err");
                        continue;
                    }

                    var path = GetRuntimePath(unitNpcRec.MovePath);
                    if (unitNpcRec.CurrPathIdx >= path.PointList.Count - 1)
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

                    if (entity == null || entity is not NpcUnitLogicEntity unitNpc)
                    {
                        Debug.LogError($"TickRefreshWalker invalid {recId}");
                        SpawnedWalkerRecords.RemoveAt(i);
                        RequestEntityDestroy(recId, "err");
                        continue;
                    }

                    if (unitNpc.IsInCombat)
                    {
                        continue;
                    }

                    if (unitNpc.MoveBehaveInfo.MoveBehaveMode != UnitMoveBehaveInfo.EMoveBehaveType.MovePath
                        || string.IsNullOrEmpty(unitNpc.MoveBehaveInfo.MovePath))
                    {
                        SpawnedWalkerRecords.RemoveAt(i);
                        RequestEntityDestroy(recId, "err");
                        continue;
                    }

                    walkerPathDict.TryGetValue(unitNpc.MoveBehaveInfo.MovePath, out var path);
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

                var rec = new LogicEntityRecord4Npc()
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
                    IsForeigner = true,
                };

                logicManager.AddNewEntityRecord(rec);

                SpawnedWalkerRecords.Add(rec.Id);
            }
        }


        #region 处理影怪刷新

        private float lastRefreshSpiritTime; // 上次更新时间

        private List<long> _pendingSpriteEntites = new(); // 存放用于表演的假hspirit
        private List<long> _runningSpriteEntites = new(); // 存放实例化的hspirit

        private float lastClearHSpiritsTime;
        /// <summary>
        /// 检查精灵怪物
        /// </summary>
        protected void TickRefreshSpiritMonster()
        {
            if(LogicTime.time - lastRefreshSpiritTime < 1.0f)
            {
                return;
            }

            lastRefreshSpiritTime = LogicTime.time;

            if(logicManager.PlayerHumanMode)
            {
                return;
            }

            if(_runningSpriteEntites.Count > 0)
            {
                return;
            }

            if(lastClearHSpiritsTime != 0 && LogicTime.time - lastClearHSpiritsTime < 15.0f)
            {
                return;
            }

            var monsterCfg = PlayerGamePlayRule.GetHSpiritByPlayerStatus(logicManager.playerLogicEntity.DesireLevel, logicManager.playerDataManager.Level);

            var initInfo = new EntityInitInfo4Npc();
            initInfo.CfgId = monsterCfg;
            initInfo.Position = logicManager.playerLogicEntity.Pos + new Vector2(2, 2);

            var record = CreateEntityRecordFromInitInfo(initInfo);
            if (record == null)
            {
                Debug.LogError($"TickRefreshSpiritMonster spawn fail.");
                return;
            }

            logicManager.AddNewEntityRecord(record);

            _runningSpriteEntites.Add(record.Id);
        }

        protected void OnHSpiritClear(long entityId)
        {
            if (_runningSpriteEntites.Contains(entityId))
            {
                _runningSpriteEntites.Remove(entityId);

                if(_runningSpriteEntites.Count == 0)
                {
                    lastClearHSpiritsTime = LogicTime.time;
                }

                Repo.Records.TryGetValue(entityId, out var rawRec);

                var restore = PlayerGamePlayRule.GetHSpiritRestoreSan(rawRec.CfgId);
                Debug.Log("影怪击杀 恢复"+ restore);
                {
                    logicManager.playerLogicEntity.ApplyResourceChange(AttrIdConsts.PlayerSanity, restore, false, FightStruct.EDmgFlag.None, null);
                }
            }
        }



        #endregion
    }
}

 