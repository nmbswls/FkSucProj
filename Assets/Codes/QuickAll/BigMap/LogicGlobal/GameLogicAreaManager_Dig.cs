using Map.Entity;
using Map.Logic.Events;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static ChunkMapExportDatabase;

namespace My.Map.Logic
{

    /// <summary>
    /// 管理区域
    /// </summary>
    public partial class GameLogicAreaManager
    {
        public Dictionary<string, long> DigPointOccupyInfo = new();
        private List<string> emptySlots = new();

        public void InitDigPoints()
        {
            var namePoints = cacheDatabase.GetAllDigPoints();
            foreach(var p in namePoints)
            {
                emptySlots.Add(p.Name);
            }
        }

        /// <summary>
        /// 创建一个点
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="digId"></param>
        /// <param name="digInfo"></param>
        public void CreateOneDig(Vector2 pos, string digId, string dropId)
        {
            if(emptySlots.Count == 0)
            {
                return;
            }

            var idx = UnityEngine.Random.Range(0, emptySlots.Count);
            var pName = emptySlots[idx];

            var p = cacheDatabase.FindNamedPointByName(pName);
            logicManager.CreateNewEntityRecord(new LogicEntityRecord4LootPoint()
            {
                Id = GameLogicManager.LogicEntityIdInst++,
                EntityType = EEntityType.LootPoint,
                CfgId = digId,
                Position = p.Position,

                DynamicDropId = dropId,
            });
        }
    }
}

 