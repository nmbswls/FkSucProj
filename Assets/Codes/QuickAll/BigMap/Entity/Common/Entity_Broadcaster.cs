using Config;
using Map.Logic.Events;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

namespace My.Map.Entity
{

    public interface INoticeRecordComp
    {
        bool CheckNoticeEntity(long entityId);
    }


    /// <summary>
    /// 注意力列表
    /// </summary>
    public class UnitNoticeRecordComp : INoticeRecordComp
    {
        public BaseUnitLogicEntity UnitEntity;

        public class NoticeRecord
        {
            public long Id;
            public bool IsEnmity;
            public float LastUpdateTime;
        }
        /// <summary>
        /// 上一次可见，
        /// </summary>
        public Dictionary<long, NoticeRecord> NoticeRecords = new();



        private float _clearInvalidTimer = 0;
        private List<long> cacheListLong = new();

        public void Initialize(BaseUnitLogicEntity unit)
        {
            this.UnitEntity = unit;
        }


        /// <summary>
        /// 更新注意力列表
        /// </summary>
        public void TryUpdateNoticeList()
        {
            // 分针轮询
            if(UnitEntity.Id % 100 != Time.frameCount % 100)
            {
                return;
            }

            //VisibilityList.Clear();
            /// 维护了NoticeRecords 
            UnitEntity.LogicManager.AreaManager.UnitGridIndex.Query(UnitEntity.Pos, 16, cacheListLong);
            foreach(var id in cacheListLong)
            {
                var logicE = UnitEntity.LogicManager.GetLogicEntity(id, false);
                if (logicE == null || logicE is not BaseUnitLogicEntity otherUnit)
                {
                    continue;
                }

                // 只关注不同阵营的
                if(UnitEntity.FactionId != EFactionId.None && UnitEntity.FactionId == otherUnit.FactionId)
                {
                    continue;
                }

                if(!UnitEntity.LogicManager.visionSenser.CanSee(UnitEntity.Pos, UnitEntity.FaceDir, otherUnit.Pos, 5.0f, 60))
                {
                    continue;
                }

                // 有记录 更新
                if(!NoticeRecords.TryGetValue(id, out var noticeRecord))
                {
                    noticeRecord = new()
                    {
                        Id = id,
                        LastUpdateTime = LogicTime.time,
                        IsEnmity = false
                    };
                }
            }

            if(_clearInvalidTimer + 10.0f < LogicTime.time)
            {
                _clearInvalidTimer = LogicTime.time;

                foreach(var key in NoticeRecords.Keys.ToList())
                {
                    if(NoticeRecords[key].LastUpdateTime < LogicTime.time - 10.0f)
                    {
                        NoticeRecords.Remove(key);
                    }
                }
            }
        }


        /// <summary>
        /// 个体自身的
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="power"></param>
        /// <param name="reason"></param>
        public void OnFightStatusTrigger(long srcId, float power, int reason)
        {

        }

        public bool CheckNoticeEntity(long entityId)
        {
            return false;
        }
    }


}

