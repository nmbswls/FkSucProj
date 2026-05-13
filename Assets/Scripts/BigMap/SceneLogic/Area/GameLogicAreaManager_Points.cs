using Map.Entity;
using Map.Logic.Events;
using My.Map.Entity;
using My.MapExport;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace My.Map.Logic
{

    /// <summary>
    /// 管理区域
    /// </summary>
    public partial class GameLogicAreaManager
    {
        public List<NamedPoint> emptyGuardSpawners = new();

        public void InitGuardSpawnPoints()
        {
            var namePoints = cacheDatabase.NamedPoints;
            foreach (var p in namePoints)
            {
                if (p.PointType == ENamedPointType.GuardSpawner)
                {
                    emptyGuardSpawners.Add(p);
                }
            }
        }

        /// <summary>
        /// 构建重要巡逻点
        /// 结果是一张图
        /// 搜集所有
        /// </summary>
        public void BuildMapImportantPatrolPoint()
        {

        }
    }
}

 