using Config;
using Map.Logic.Events;
using My.Map.Entity;
using My.Map.Unit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace My.Map.Unit
{

    public interface IUnitWithVision
    {
        bool IsTargetVisible(long targetId);
        bool IsTargetWitnessed(long targetId);
    }

    /// <summary>
    /// 注意力列表
    /// </summary>
    public class UnitVisionSystem : IUnitWithVision
    {
        public const float WitnessNeedSec = 1f;
        public const float WitnessForgetSec = 2f;
        const float UpdateInterval = 0.5f;
        const float EntryExpireAfter = 4f;

        protected BaseUnitLogicEntity UnitEntity { get; set; }

        public event Action<long> EventOnMarkVisible;
        public event Action<long> EventOnMarkHidden;

        public class VisibilityEntry
        {
            public long TargetId;
            public bool IsInView;
            // 目击累计量（非 Timer）；默认 spotRate=1 时数值等价于秒
            public float WitnessAccum;
            public float LastSeenTime;
            public float LastUpdateTime;
            public Vector2 LastKnownPos;

            public bool IsWitnessed => WitnessAccum >= WitnessNeedSec;
        }

        public Dictionary<long, VisibilityEntry> VisibleMap = new();

        private float _lastUpdateTime;
        private readonly List<long> cacheListLong = new();
        private readonly HashSet<long> evaluatedIds = new();

        public UnitVisionSystem(BaseUnitLogicEntity unit)
        {
            UnitEntity = unit;
        }

        public void TryUpdateNoticeList()
        {
            if (UnitEntity.Id % 10 != Time.frameCount % 10)
            {
                return;
            }

            if (_lastUpdateTime + UpdateInterval > LogicTime.time)
            {
                return;
            }

            float now = LogicTime.time;
            float dt = _lastUpdateTime <= 0f ? UpdateInterval : now - _lastUpdateTime;
            _lastUpdateTime = now;

            evaluatedIds.Clear();
            UnitEntity.LogicManager.AreaManager.UnitGridIndex.Query(UnitEntity.Pos, 16, cacheListLong);
            foreach (var id in cacheListLong)
            {
                var logicE = UnitEntity.LogicManager.GetLogicEntity(id, false);
                if (logicE == null || logicE is not BaseUnitLogicEntity otherUnit)
                {
                    continue;
                }

                if (UnitEntity.FactionId != EFactionId.None && UnitEntity.FactionId == otherUnit.FactionId)
                {
                    continue;
                }

                if (!UnitEntity.LogicManager.visionSenser.CanUnitSee(UnitEntity.Id, otherUnit.Id))
                {
                    continue;
                }

                if (!VisibleMap.TryGetValue(id, out var noticeRecord))
                {
                    noticeRecord = new VisibilityEntry
                    {
                        TargetId = id,
                        LastSeenTime = -999f,
                        LastUpdateTime = -999f,
                        LastKnownPos = Vector2.zero,
                    };
                    VisibleMap[noticeRecord.TargetId] = noticeRecord;
                }

                EvaluateTarget(now, otherUnit, noticeRecord, dt);
                evaluatedIds.Add(id);
            }

            foreach (var kv in VisibleMap)
            {
                if (evaluatedIds.Contains(kv.Key))
                {
                    continue;
                }

                ApplyOutOfView(kv.Value, now, dt);
            }

            ExpireEntries(now);
        }

        void EvaluateTarget(float now, BaseUnitLogicEntity target, VisibilityEntry entry, float dt)
        {
            var targetPos = target.Pos;
            var dist = (targetPos - UnitEntity.Pos).magnitude;

            var stealth = target.stealthInfo;
            bool stealthBlocks = false;
            if (stealth != null && stealth.stealthId != 0)
            {
                bool ignoreStealth =
                    stealth.SeeUnits != null &&
                    stealth.SeeUnits.TryGetValue(UnitEntity.Id, out var untilTs) &&
                    now < untilTs + 60.0f;

                ignoreStealth |= dist <= 1e-1f;

                if (!ignoreStealth)
                {
                    stealthBlocks = true;
                }
            }

            if (!stealthBlocks)
            {
                ApplyInView(entry, now, targetPos, dt, target);
            }
            else
            {
                ApplyOutOfView(entry, now, dt);
            }
        }

        // 观察者目击加成 vs 目标逃脱加成，万分比对冲
        static float ResolveWitnessSpotRate(BaseUnitLogicEntity observer, BaseUnitLogicEntity target)
        {
            long spotBonus = observer.GetAttr(AttrIdConsts.UnitWitnessSpotRate);
            long escapeBonus = target.GetAttr(AttrIdConsts.UnitWitnessEscapeRate);
            long net = spotBonus - escapeBonus;
            float mul = (10000 + net) / 10000f;
            if (mul < 0.01f)
            {
                mul = 0.01f;
            }

            return mul;
        }

        void ApplyInView(VisibilityEntry entry, float now, Vector2 pos, float dt, BaseUnitLogicEntity target)
        {
            float spotRate = ResolveWitnessSpotRate(UnitEntity, target);
            bool wasInView = entry.IsInView;

            entry.IsInView = true;
            entry.LastSeenTime = now;
            entry.LastKnownPos = pos;
            entry.LastUpdateTime = now;
            entry.WitnessAccum += spotRate * dt;

            if (!wasInView)
            {
                EventOnMarkVisible?.Invoke(entry.TargetId);
            }
        }

        void ApplyOutOfView(VisibilityEntry entry, float now, float dt)
        {
            float hideRate = WitnessNeedSec / WitnessForgetSec;
            bool wasInView = entry.IsInView;

            entry.IsInView = false;
            entry.LastUpdateTime = now;
            entry.WitnessAccum -= hideRate * dt;
            if (entry.WitnessAccum < 0f)
            {
                entry.WitnessAccum = 0f;
            }

            if (wasInView)
            {
                EventOnMarkHidden?.Invoke(entry.TargetId);
            }
        }

        public bool IsTargetVisible(long targetId)
        {
            return VisibleMap.TryGetValue(targetId, out var e) && e.IsInView;
        }

        public bool IsTargetWitnessed(long targetId)
        {
            return VisibleMap.TryGetValue(targetId, out var e) && e.IsWitnessed;
        }

        // 立刻清空目击累积（烟雾等 debuff 首次施加时）
        public void FlushWitnessState()
        {
            foreach (var kv in VisibleMap)
            {
                var entry = kv.Value;
                bool wasInView = entry.IsInView;
                entry.WitnessAccum = 0f;
                entry.IsInView = false;
                entry.LastUpdateTime = LogicTime.time;
                if (wasInView)
                {
                    EventOnMarkHidden?.Invoke(entry.TargetId);
                }
            }
        }

        void ExpireEntries(float now)
        {
            var toRemove = new List<long>();
            foreach (var kv in VisibleMap)
            {
                var e = kv.Value;
                if (!e.IsInView && e.WitnessAccum <= 0f && now - e.LastUpdateTime > EntryExpireAfter)
                {
                    toRemove.Add(kv.Key);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                VisibleMap.Remove(toRemove[i]);
            }
        }
    }
}

namespace My.Map
{

    public abstract partial class BaseUnitLogicEntity
    {
        public UnitVisionSystem VisionSystem { get; set; }

        public virtual void InitVisionSystem()
        {
        }

        public bool IsTargetVisible(long targetId)
        {
            return VisionSystem != null && VisionSystem.IsTargetVisible(targetId);
        }

        public bool IsTargetWitnessed(long targetId)
        {
            return VisionSystem != null && VisionSystem.IsTargetWitnessed(targetId);
        }

        public virtual void OnGazeEnter(long srcId)
        {
        }

        public virtual void OnGazeLeave(long srcId)
        {
        }
    }
}
