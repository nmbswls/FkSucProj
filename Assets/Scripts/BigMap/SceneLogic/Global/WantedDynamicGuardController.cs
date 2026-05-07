using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using My.Map.Scene;
using UnityEngine;

namespace My
{
    // 通缉星级达到配置档位后，在玩家附近动态维持临时守卫；过远回收并从近处补刷
    public sealed class WantedDynamicGuardController
    {
        const float TickPeriod = 0.75f;

        readonly GameLogicManager _logic;
        readonly List<long> _guardIds = new();
        float _cooldown;

        public WantedDynamicGuardController(GameLogicManager logic)
        {
            _logic = logic;
        }

        public void ClearAll()
        {
            if (_logic?.AreaManager == null)
            {
                _guardIds.Clear();
                return;
            }

            for (int i = 0; i < _guardIds.Count; i++)
            {
                _logic.AreaManager.RequestEntityDestroy(_guardIds[i], "wanted_guard_area_clear");
            }

            _guardIds.Clear();
        }

        public void Tick(float dt)
        {
            if (_logic?.WantedManager == null || _logic.playerLogicEntity == null || _logic.AreaManager == null)
            {
                return;
            }

            _cooldown -= dt;
            if (_cooldown > 0f)
            {
                return;
            }

            _cooldown = TickPeriod;

            var tier = SelectTier();
            int target = tier?.GuardCount ?? 0;
            string npcId = string.IsNullOrEmpty(tier?.NpcCfgId) ? "default_guard_01" : tier.NpcCfgId;
            float rMin = tier?.SpawnRadiusMin ?? 6f;
            float rMax = tier?.SpawnRadiusMax ?? 12f;
            if (rMax < rMin)
            {
                (rMin, rMax) = (rMax, rMin);
            }

            float cull = tier?.CullDistance ?? 36f;

            PruneDead();
            CullFar(cull);
            if (target < _guardIds.Count)
            {
                TrimExcess(target);
                return;
            }

            while (_guardIds.Count < target)
            {
                if (!TrySpawnOne(npcId, rMin, rMax))
                {
                    break;
                }
            }
        }

        WantedGuardSpawnTier SelectTier()
        {
            var table = CfgMgr.Cfgs?.TbWantedGuardSpawnTier;
            if (table?.DataList == null || table.DataList.Count == 0)
            {
                return null;
            }

            int star = _logic.WantedManager.GetWantedStarLevel();
            WantedGuardSpawnTier best = null;
            foreach (var row in table.DataList)
            {
                if (row == null)
                {
                    continue;
                }

                if (star < row.MinWantedStarLevel)
                {
                    continue;
                }

                if (best == null || row.MinWantedStarLevel > best.MinWantedStarLevel)
                {
                    best = row;
                }
            }

            return best;
        }

        void PruneDead()
        {
            for (int i = _guardIds.Count - 1; i >= 0; i--)
            {
                ILogicEntity e = _logic.AreaManager.GetLogicEntiy(_guardIds[i], false);
                if (e == null || e.MarkDestroyed)
                {
                    _guardIds.RemoveAt(i);
                }
            }
        }

        void CullFar(float cullDistance)
        {
            var p = _logic.playerLogicEntity.Pos;
            float d2 = cullDistance * cullDistance;
            for (int i = _guardIds.Count - 1; i >= 0; i--)
            {
                ILogicEntity e = _logic.AreaManager.GetLogicEntiy(_guardIds[i], false);
                if (e == null)
                {
                    _guardIds.RemoveAt(i);
                    continue;
                }

                if ((e.Pos - p).sqrMagnitude > d2)
                {
                    long id = _guardIds[i];
                    _guardIds.RemoveAt(i);
                    _logic.AreaManager.RequestEntityDestroy(id, "wanted_guard_cull_distance");
                }
            }
        }

        void TrimExcess(int target)
        {
            var p = _logic.playerLogicEntity.Pos;
            while (_guardIds.Count > target)
            {
                int farIdx = FindFarthestIndex(p);
                long id = _guardIds[farIdx];
                _guardIds.RemoveAt(farIdx);
                _logic.AreaManager.RequestEntityDestroy(id, "wanted_guard_trim");
            }
        }

        int FindFarthestIndex(Vector2 p)
        {
            int best = 0;
            float bestD = -1f;
            for (int i = 0; i < _guardIds.Count; i++)
            {
                ILogicEntity e = _logic.AreaManager.GetLogicEntiy(_guardIds[i], false);
                float d = e != null ? (e.Pos - p).sqrMagnitude : float.MaxValue;
                if (d > bestD)
                {
                    bestD = d;
                    best = i;
                }
            }

            return best;
        }

        bool TrySpawnOne(string cfgId, float rMin, float rMax)
        {
            var player = _logic.playerLogicEntity;
            for (int attempt = 0; attempt < 14; attempt++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float r = Random.Range(rMin, rMax);
                Vector2 cand = player.Pos + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
                if (!MapWorldEmptySpotUtil.TryFindEmptySpotNear(cand, 3f, 0.4f, player.Id, null, out var spot))
                {
                    continue;
                }

                var rec = new LogicEntityRecord4Npc
                {
                    Id = GameLogicManager.LogicEntityIdInst++,
                    EntityType = EEntityType.Npc,
                    CfgId = cfgId,
                    Position = spot,
                    FactionId = EFactionId.Citizen,
                    IsPeace = false,
                    MoveBehaveType = UnitMoveBehaveInfo.EMoveBehaveType.NoMove,
                    EnmityConfId = "default_guard",
                };
                _logic.AddNewEntityRecord(rec);
                _guardIds.Add(rec.Id);
                return true;
            }

            return false;
        }
    }
}
