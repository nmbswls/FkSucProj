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
using UnityEngine.UIElements;

namespace My.Map.Logic
{

    /// <summary>
    /// 管理区域
    /// </summary>
    public partial class GameLogicAreaManager
    {
        public class AlertRecord
        {
            public long SrcEntityId;
            public float HappenTime;
            public Vector2 HappenPos;

            public bool IsValid;
        }

        public float AlertTryInterval = 8.0f;
        public float AlertDuration = 5.0f;

        public long AreaAlertValue = 0;

        protected Dictionary<long, float> EntityLastTryAlertTimes = new();
        protected List<AlertRecord> alertRecords = new();

        protected void TickEvilAlerts()
        {
            float currTime = LogicTime.time;

            for (int i = alertRecords.Count - 1; i >= 0; i--)
            {
                if (currTime - alertRecords[i].HappenTime > AlertDuration)
                {
                    alertRecords[i].IsValid = true;

                    TryAddEvilAlert(5);
                    alertRecords.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityId"></param>
        public void EntityTryAlert(long entityId)
        {
            var entity = GetLogicEntiy(entityId);
            if(entity == null)
            {
                return;
            }

            EntityLastTryAlertTimes.TryGetValue(entityId, out var lastAlertTime);
            if(lastAlertTime != 0 && LogicTime.time -  lastAlertTime < AlertTryInterval)
            {
                return;
            }

            if(!cacheMapAreaCfg.AlwaysAlert)
            {
                var inAlert = logicManager.visionSenser.CheckIsInAlertArea(entity.Pos);
                if(!inAlert)
                {
                    return;
                }
            }

            EntityLastTryAlertTimes[entityId] = LogicTime.time;

            alertRecords.Add(new AlertRecord()
            {
                SrcEntityId = entityId,
                HappenPos = entity.Pos,
                HappenTime = LogicTime.time,
            });
        }

        /// <summary>
        /// 清理alert信息
        /// </summary>
        /// <param name="entityId"></param>
        public void TryClearPendingAlert(long entityId)
        {
            bool changed = false;
            for (int i = alertRecords.Count - 1; i >= 0; i--)
            {
                var alertRecord = alertRecords[i];
                if(alertRecord.SrcEntityId == entityId)
                {
                    alertRecords.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
        }

        /// <summary>
        /// 添加邪恶值
        /// </summary>
        /// <param name="addVal"></param>
        public void TryAddEvilAlert(long addVal)
        {
            AreaAlertValue += 5;

            // 计算衰减
        }
    }
}

 