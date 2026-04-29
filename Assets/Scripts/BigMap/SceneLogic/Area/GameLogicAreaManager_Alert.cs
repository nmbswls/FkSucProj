using Map.Entity;
using Map.Logic.Events;
using My.Map.Entity;
using My.MapExport;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

namespace My.Map.Logic
{

    /// <summary>
    /// 管理区域
    /// </summary>
    public partial class GameLogicAreaManager
    {
        //public class AlertRecord
        //{
        //    public long SrcEntityId;
        //    public float HappenTime;
        //    public Vector2 HappenPos;

        //    public bool IsValid;
        //}

        //public float GlobalAlertMinInterval = 5.0f;
        //public float UnitAlertTryInterval = 12.0f;
        //public float AlertDuration = 5.0f;

        public bool PlayerInAlertArea { get; set; } = true;

        public long MaxAlertValue = 10000;
        public long AreaAlertValue = 0;


        protected Dictionary<long, WeakReference<BaseUnitLogicEntity>> alertingLogicEntities = new();
        private Dictionary<long, List<(float, float)>> entityPendingAlerts = new();

        public float EvilAlertBalanceInterval = 0.25f;
        public float EvilApplyDelay = 5.0f;

        private float _lastAddUnitAlertTimer = 0f; 
        private float _lastClearDiedAlertTimer = 0f;
        private float _lastApplyPendingTimer = 0f;

        protected void TickEvilAlerts()
        {
            TickAddPendingEvilAlerts();
            TickSafeClearDeadEntities();
            TickPendingEvilApply();
        }

        /// <summary>
        /// 获取合法entity
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BaseUnitLogicEntity> GetAlertingLogicEntities()
        {
            foreach(var oneVal in alertingLogicEntities.Values)
            {
                if(oneVal.TryGetTarget(out var entity))
                {
                    yield return entity;
                }
            }
        }

        private void TickAddPendingEvilAlerts()
        {
            if (_lastAddUnitAlertTimer == 0)
            {
                _lastAddUnitAlertTimer = LogicTime.time;
            }

            if (LogicTime.time - _lastAddUnitAlertTimer < EvilAlertBalanceInterval)
            {
                return;
            }

            var addTimes = (int)((LogicTime.time - _lastAddUnitAlertTimer) / EvilAlertBalanceInterval);

            var speed = CalculateAddEvilSpeedByCnt();
            float addVal = (long)(speed * EvilAlertBalanceInterval * addTimes);


            _lastAddUnitAlertTimer += EvilAlertBalanceInterval * addTimes;

            foreach (var alerting in alertingLogicEntities)
            {
                if (!entityPendingAlerts.TryGetValue(alerting.Key, out var pendingList))
                {
                    pendingList = new();
                    entityPendingAlerts[alerting.Key] = pendingList;
                }

                pendingList.Add(new (addVal, LogicTime.time));
            }
        }

        private void TickSafeClearDeadEntities()
        {
            if (_lastClearDiedAlertTimer == 0)
            {
                _lastClearDiedAlertTimer = LogicTime.time;
            }

            if (LogicTime.time - _lastClearDiedAlertTimer < 5.0f)
            {
                return;
            }

            _lastClearDiedAlertTimer = LogicTime.time;

            List<long> needClear = null;
            foreach (var alerting in alertingLogicEntities)
            {
                if (!alerting.Value.TryGetTarget(out var entity))
                {
                    if (needClear == null) needClear = new();
                    needClear.Add(alerting.Key);
                    continue;
                }

                if(entity.IsDead)
                {
                    if (needClear == null) needClear = new();
                    needClear.Add(alerting.Key);
                    continue;
                }
            }

            if (needClear != null && needClear.Count > 0)
            {
                foreach(var oneClear in needClear)
                {
                    alertingLogicEntities.Remove(oneClear);
                }
            }
        }

        private void TickPendingEvilApply()
        {
            if (_lastApplyPendingTimer == 0)
            {
                _lastApplyPendingTimer = LogicTime.time;
            }

            if (LogicTime.time - _lastApplyPendingTimer < 0.3f)
            {
                return;
            }

            _lastApplyPendingTimer = LogicTime.time;

            // 结算并清理警戒度
            foreach (var oneKey in entityPendingAlerts.Keys.ToList())
            {
                var ll = entityPendingAlerts[oneKey];
                float sum = 0;
                while(ll.Count > 0)
                {
                    if (LogicTime.time - ll[0].Item2 < EvilApplyDelay)
                    {
                        break;
                    }

                    sum += ll[0].Item1;

                    ll.RemoveAt(0);
                }


                AreaAlertValue += (long)sum;

                if(ll.Count == 0)
                {
                    entityPendingAlerts.Remove(oneKey);
                }
            }
        }

        public long CalculateAddEvilSpeedByCnt()
        {
            int cnt = alertingLogicEntities.Count;
            if(cnt == 0)
            {
                return 0;
            }
            if (cnt < 1)
            {
                return 50;
            }

            if (cnt < 3)
            {
                return 100;
            }

            if(cnt < 10)
            {
                return 200;
            }

            return 500;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityId"></param>
        public void EntityTryRegisterAlert(BaseUnitLogicEntity unitEntity)
        {
            alertingLogicEntities[unitEntity.Id] = new WeakReference<BaseUnitLogicEntity>(unitEntity);
        }


        public void EntityTryUnregisterAlert(long unitId)
        {
            alertingLogicEntities.Remove(unitId);
        }

        /// <summary>
        /// 添加邪恶值
        /// </summary>
        /// <param name="addVal"></param>
        public void TryAddEvilAlertDirect(long addVal)
        {
            AreaAlertValue += 5;

            // 计算衰减
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public long GetTempAlertValue()
        {
            float tempSum = 0;
            foreach (var pendingList in entityPendingAlerts.Values)
            {
                foreach(var onePending in pendingList)
                {
                    tempSum += onePending.Item1;
                }
            }

            return (long)tempSum;
        }

        public void ClearUnitRelateAlert(long entityId)
        {
            if(entityPendingAlerts.TryGetValue(entityId, out var pendingList))
            {
                float sum = 0;
                foreach(var pair in pendingList)
                {
                    sum += pair.Item1;
                }

                long totalLost = (long)sum;
                if(totalLost > 0)
                {
                    logicManager.LogicEventBus.Publish(new MLECostPendingAlertEvent()
                    {
                        Ctx = new()
                        {
                        },
                        Value = totalLost,
                    });
                }
                entityPendingAlerts.Remove(entityId);
            }
        }
    }
}

 