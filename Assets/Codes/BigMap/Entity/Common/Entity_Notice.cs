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
using static UnityEngine.GraphicsBuffer;

namespace My.Map.Entity
{

    public interface INoticeRecordComp
    {
        bool IsTargetVisible(long targetId);
    }


    /// <summary>
    /// 注意力列表
    /// </summary>
    public class UnitNoticeRecordComp : INoticeRecordComp
    {
        public BaseUnitLogicEntity UnitEntity;


        private float EntryExpireAfter = 2.0f;

        //public class NoticeRecord
        //{
        //    public long Id;
        //    public bool IsEnmity;
        //    public float LastUpdateTime;
        //}

        public class VisibilityEntry
        {
            public long TargetId;
            //public VisibilityStatus Status;
            public bool IsInView = false;
            public float Confidence;     // 0..1，越高越确定
            public float LastSeenTime;   // 最后一次判定为 Visible 的时间
            public float LastUpdateTime; // 最近一次更新（任何状态）
            public Vector2 LastKnownPos; // 最近可见时记录的位置
        }
        /// <summary>
        /// 上一次可见，
        /// </summary>
        //public Dictionary<long, NoticeRecord> NoticeRecords = new();
        public Dictionary<long, VisibilityEntry> VisibleMap = new(); // TargetId => Entry

        public Dictionary<long, int> ExposeRecord;

        private float _lastUpdateTime;


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
            if (UnitEntity.Id % 10 != Time.frameCount % 10)
            {
                return;
            }

            if (_lastUpdateTime + 0.5f > LogicTime.time)
            {
                return;
            }

            _lastUpdateTime = LogicTime.time;


            //VisibilityList.Clear();
            /// 维护了NoticeRecords 
            UnitEntity.LogicManager.AreaManager.UnitGridIndex.Query(UnitEntity.Pos, 16, cacheListLong);
            foreach (var id in cacheListLong)
            {
                var logicE = UnitEntity.LogicManager.GetLogicEntity(id, false);
                if (logicE == null || logicE is not BaseUnitLogicEntity otherUnit)
                {
                    continue;
                }

                // 只关注不同阵营的
                if (UnitEntity.FactionId != EFactionId.None && UnitEntity.FactionId == otherUnit.FactionId)
                {
                    continue;
                }

                if(!UnitEntity.CheckIsEmnityFaction(otherUnit.FactionId))
                {
                    continue;
                }

                if (!UnitEntity.LogicManager.visionSenser.CanUnitSee(UnitEntity.Id, otherUnit.Id))
                {
                    continue;
                }

                // 有记录 更新
                if (!VisibleMap.TryGetValue(id, out var noticeRecord))
                {
                    noticeRecord = new()
                    {
                        TargetId = id,
                        LastSeenTime = -999f,
                        LastUpdateTime = -999f,
                        LastKnownPos = Vector2.zero
                    };
                    VisibleMap[noticeRecord.TargetId] = noticeRecord;
                }

                EvaluateTarget(LogicTime.time, otherUnit, noticeRecord);
            }

            ExpireEntries(LogicTime.time);
        }

        /// <summary>
        /// 目标检查
        /// </summary>
        /// <param name="now"></param>
        /// <param name="target"></param>
        /// <param name="entry"></param>
        private void EvaluateTarget(float now, BaseUnitLogicEntity target, VisibilityEntry entry)
        {
            var targetPos = target.Pos;
            var dist = (targetPos - UnitEntity.Pos).magnitude;

            var cansee = UnitEntity.LogicManager.visionSenser.CanUnitSee(UnitEntity.Id, target.Id);
            if(!cansee)
            {
                MarkHidden(entry, LogicTime.time);
            }
            

            // 隐身覆盖（机制级躲藏）
            var stealth = target.stealthInfo;
            bool stealthBlocks = false;
            if (stealth != null && stealth.stealthId != 0)
            {
                // 该观察者在隐身获取时的无视窗口
                bool ignoreStealth =
                    stealth.SeeUnits != null &&
                    stealth.SeeUnits.TryGetValue(UnitEntity.Id, out var untilTs) &&
                    now < untilTs;

                ignoreStealth |= (dist <= 1e-1);

                if (!ignoreStealth)
                {
                    stealthBlocks = true;
                }
            }

            if (!stealthBlocks && cansee)
            {
                MarkVisible(entry, now, targetPos);
            }
            else
            {
                MarkHidden(entry, now);
            }
        }

        // 查询接口
        public bool IsTargetVisible(long targetId)
        {
            return VisibleMap.TryGetValue(targetId, out var e) && e.IsInView;
        }

        private void MarkVisible(VisibilityEntry e, float now, Vector2 pos)
        {
            e.IsInView = true;
            e.LastSeenTime = now;
            e.LastKnownPos = pos;
            e.LastUpdateTime = now;
        }

        private void MarkHidden(VisibilityEntry e, float now)
        {
            e.IsInView = false;
            e.LastUpdateTime = now;
        }

        private void ExpireEntries(float now)
        {
            var toRemove = new List<long>();
            foreach (var kv in VisibleMap)
            {
                var e = kv.Value;
                if (now - e.LastUpdateTime > EntryExpireAfter)
                    toRemove.Add(kv.Key);
            }
            for (int i = 0; i < toRemove.Count; i++)
                VisibleMap.Remove(toRemove[i]);
        }

    }


}

