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
    /// π‹¿Ì«¯”Ú
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
    }
}

 