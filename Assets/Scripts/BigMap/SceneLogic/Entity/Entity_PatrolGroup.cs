using Config.Map;
using Config;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using My.Map.Logic;
using My.MapExport;

namespace My.Map
{
    public class PatrolGroupLogicEntity: LogicEntityBase
    {

        public MapPatrolGroupConfig cacheCfg;

        public PatrolGroupLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var record = (LogicEntityRecord4PatrolGroup)bindingRecord;

            this.MoveSpeed = record.MoveSpeed;
            this.WayPointIdx = record.WayPointIdx;
            this.WayPointDistance = record.WayPointDistance;
            this.IsBack = record.IsBack;

            this.WayPointInfos.Clear();
            if (record.PatrolCycleNodeIds != null && record.PatrolCycleNodeIds.Count >= 2)
            {
                var expanded = new List<Vector2>();
                if (PortalPatrolPathBuilder.TryBuildCycleWorldPath(
                        LogicManager.AreaManager.cacheDatabase,
                        record.PatrolPortalNetworkId,
                        record.PatrolCycleNodeIds,
                        expanded)
                    && expanded.Count > 0)
                {
                    this.WayPointInfos.AddRange(expanded);
                }
                else
                {
                    Debug.LogWarning($"[PatrolGroup] Portal patrol path failed for entity {instId}, cfg {cfgId}.");
                }
            }

            if (this.WayPointInfos.Count < 2)
            {
                foreach (var s in record.WayPointList)
                {
                    var p = LogicManager.AreaManager.cacheDatabase.FindNamedPointByName(s);
                    if (p == null)
                    {
                        continue;
                    }

                    this.WayPointInfos.Add(p.Value.Position);
                }
            }

            this.PatrolUnitIds.Clear();
            this.PatrolUnitIds.AddRange(record.PatrolUnitIds);

            if (WayPointInfos.Count > 0 && WayPointIdx >= WayPointInfos.Count)
            {
                WayPointIdx = WayPointInfos.Count - 1;
            }
        }

        public override EEntityType Type => EEntityType.PatrolGroup;

        public override void Initialize()
        {
            base.Initialize();
        }

        public float MoveSpeed { get; set; }
        public int WayPointIdx = 0;
        public float WayPointDistance = 0;
        public List<Vector2> WayPointInfos = new();
        public bool IsBack = false;

        public List<long> PatrolUnitIds = new();

        private Vector2? currMoveDir;
        private float? currMoveDist;

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            if (WayPointInfos.Count < 2)
            {
                return;
            }

            foreach(var uid in PatrolUnitIds)
            {
                var entity = LogicManager.AreaManager.GetLogicEntiy(uid, false);
                if(entity != null && entity is BaseUnitLogicEntity unitEntity)
                {
                    if(unitEntity.IsInCombat)
                    {
                        return;
                    }
                }
            }

            if (currMoveDir == null)
            {
                int currIdx = WayPointIdx;
                int nextIdx = (currIdx + 1) % this.WayPointInfos.Count;

                currMoveDir = (WayPointInfos[nextIdx] - WayPointInfos[currIdx]).normalized;
                currMoveDist = (WayPointInfos[nextIdx] - WayPointInfos[currIdx]).magnitude;
            }

            WayPointDistance += MoveSpeed * dt;


            // 抵达
            if (WayPointDistance >= currMoveDist)
            {
                WayPointIdx = (WayPointIdx + 1) % this.WayPointInfos.Count;

                Pos = WayPointInfos[WayPointIdx];
                WayPointDistance = 0;
                currMoveDir = null;
                currMoveDist = null;
            }
            else
            {
                Pos = WayPointInfos[WayPointIdx] + (currMoveDir.Value* WayPointDistance);
            }
        }
    }
}