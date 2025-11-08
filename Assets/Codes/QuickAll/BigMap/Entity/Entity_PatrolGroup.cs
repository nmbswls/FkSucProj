using Config.Map;
using Config;
using Map.Logic.Chunk;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Map.Entity
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
            foreach (var s in record.WayPointList)
            {
                var p = LogicManager.AreaManager.cacheDatabase.FindNamedPointByName(s);
                if (string.IsNullOrEmpty(p.Name)) continue;
                this.WayPointInfos.Add(p.Position);
            }

            this.PatrolUnitIds.Clear();
            this.PatrolUnitIds.AddRange(record.PatrolUnitIds);

            if(WayPointIdx >= this.WayPointInfos.Count)
            {
                WayPointIdx = this.WayPointInfos.Count - 1;
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

        public override void Tick(float now, float dt)
        {
            foreach(var uid in PatrolUnitIds)
            {
                var entity = LogicManager.AreaManager.GetLogicEntiy(uid, false);
                if(entity != null && entity is BaseUnitLogicEntity unitEntity)
                {
                    if(unitEntity.IsInBattle)
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


            // µÖ´ï
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