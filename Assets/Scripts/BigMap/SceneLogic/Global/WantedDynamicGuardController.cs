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
    // 通缉星级或区域警戒档位驱动：动态维持临时守卫；高星级/高档位含搜查、退场或路网巡逻
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

            var tier = SelectPressureTier(out _);
            int target = tier?.GuardCount ?? 0;
            string npcId = string.IsNullOrEmpty(tier?.NpcCfgId) ? "default_guard_01" : tier.NpcCfgId;
            float rMin = tier?.SpawnRadiusMin ?? 6f;
            float rMax = tier?.SpawnRadiusMax ?? 12f;
            if (rMax < rMin)
            {
                (rMin, rMax) = (rMax, rMin);
            }

            float cull = tier?.CullDistance ?? 36f;
            int pressureBehavior = tier?.PressureBehavior ?? 0;
            int patrolPickN = tier?.PatrolPickN > 0 ? tier.PatrolPickN : 3;

            PruneDead();
            CullFar(cull);
            if (target < _guardIds.Count)
            {
                TrimExcess(target);
                return;
            }

            while (_guardIds.Count < target)
            {
                if (!TrySpawnOne(npcId, rMin, rMax, pressureBehavior, patrolPickN))
                {
                    break;
                }
            }
        }

        WantedGuardSpawnTier SelectPressureTier(out int alertTier)
        {
            alertTier = _logic.AreaManager.GetAlertPressureTier();
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

                bool byWanted = star >= row.MinWantedStarLevel;
                bool byAlert = alertTier >= row.MinAlertTier;
                if (!byWanted && !byAlert)
                {
                    continue;
                }

                if (best == null || row.TierId > best.TierId)
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
                else if(e is BaseUnitLogicEntity unit && unit.IsDead)
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

        bool IsSpotOutsidePlayerFov(Vector2 spot)
        {
            var senser = _logic.visionSenser;
            var player = _logic.playerLogicEntity;
            if (senser == null || player == null)
            {
                return true;
            }

            var prm = player.GetViewRangeAndAngle();
            return !senser.SimpleCanSee(player.Pos, player.CurrentLook, spot, prm.Item1, prm.Item2);
        }

        bool TrySpawnOne(string cfgId, float rMin, float rMax, int pressureBehavior, int patrolPickN)
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

                if (!IsSpotOutsidePlayerFov(spot))
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
                    PostInvestigationResolveKind = pressureBehavior,
                    PostInvestigationPatrolPickN = patrolPickN > 0 ? patrolPickN : 3,
                    SpawnWithImmediateInvestigation = pressureBehavior > 0,
                };
                _logic.AddNewEntityRecord(rec);
                _guardIds.Add(rec.Id);
                return true;
            }

            return false;
        }
    }
}
