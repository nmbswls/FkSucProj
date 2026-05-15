using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using My.Map.Scene;
using My.MapExport;
using UnityEngine;

namespace My
{
    // 通缉星级或区域警戒档位驱动：动态维持临时守卫；高星级/高档位含搜查、退场或路网巡逻
    public sealed class WantedDynamicGuardController
    {
        // 动态守卫补刷逻辑间隔（秒）；GM 说明与测试等待时间参照此值。
        public const float TickPeriodSeconds = 0.75f;

        readonly GameLogicManager _logic;
        readonly List<long> _guardIds = new();
        readonly List<long> _postSearchPolicyPendingIds = new();
        readonly HashSet<long> _postSearchPolicyPendingSet = new();
        float _cooldown;

        public WantedDynamicGuardController(GameLogicManager logic)
        {
            _logic = logic;
        }

        public void ClearAll()
        {
            _postSearchPolicyPendingIds.Clear();
            _postSearchPolicyPendingSet.Clear();
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

            ProcessPendingPostSearchPolicies();

            _cooldown -= dt;
            if (_cooldown > 0f)
            {
                return;
            }

            _cooldown = TickPeriodSeconds;

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

        public void EnqueuePostSearchPolicyPending(long npcEntityId)
        {
            if (!_postSearchPolicyPendingSet.Add(npcEntityId))
            {
                return;
            }

            _postSearchPolicyPendingIds.Add(npcEntityId);
        }

        public void CancelPostSearchPolicyPending(long npcEntityId)
        {
            if (!_postSearchPolicyPendingSet.Remove(npcEntityId))
            {
                return;
            }

            _postSearchPolicyPendingIds.Remove(npcEntityId);
        }

        void ProcessPendingPostSearchPolicies()
        {
            if (_postSearchPolicyPendingIds.Count == 0)
            {
                return;
            }

            var batch = new long[_postSearchPolicyPendingIds.Count];
            _postSearchPolicyPendingIds.CopyTo(batch);
            _postSearchPolicyPendingIds.Clear();
            _postSearchPolicyPendingSet.Clear();

            foreach (var id in batch)
            {
                TryApplyPostSearchPolicyForEntity(id);
            }
        }

        void TryApplyPostSearchPolicyForEntity(long id)
        {
            var e = _logic.AreaManager.GetLogicEntiy(id, false);
            if (e is not NpcUnitLogicEntity npc)
            {
                return;
            }

            if (npc.MarkDestroyed || npc.IsDead || npc.AIBrain == null)
            {
                return;
            }

            if (!npc.AIBrain.PostSearchPolicyPending || npc.AIBrain.CurrentState != npc.AIBrain.StateSearch)
            {
                return;
            }

            var rec = npc.NpcRecord;
            if (rec == null)
            {
                npc.AIBrain.PostSearchPolicyPending = false;
                npc.AIBrain.ChangeState(npc.AIBrain.StateReturn);
                return;
            }

            int kind = rec.PostInvestigationResolveKind;
            if (kind <= 0)
            {
                npc.AIBrain.PostSearchPolicyPending = false;
                npc.AIBrain.ChangeState(npc.AIBrain.StateReturn);
                return;
            }

            var db = _logic.AreaManager.cacheDatabase;
            int nPick = Mathf.Max(2, rec.PostInvestigationPatrolPickN > 0 ? rec.PostInvestigationPatrolPickN : 3);

            bool applied = false;
            switch (kind)
            {
                case 1:
                    if (DynamicPressureGuardUtil.TryPickRandomNamedPointPosition(db, ENamedPointType.GuardSpawner, out var exitPos))
                    {
                        npc.MoveBehaveInfo.MoveToDespawnTarget = exitPos;
                        npc.MoveBehaveInfo.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.MoveToThenDespawn;
                        applied = true;
                    }

                    break;
                case 2:
                    npc.MoveBehaveInfo.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.NoMove;
                    applied = true;
                    break;
                case 3:
                {
                    var ids = npc.MoveBehaveInfo.PatrolCycleNodeIds;
                    ids.Clear();
                    if (DynamicPressureGuardUtil.TrySamplePatrolCycleIds(
                            db,
                            npc.MoveBehaveInfo.PatrolPortalNetworkId,
                            nPick,
                            ids,
                            out var resolvedNet))
                    {
                        npc.MoveBehaveInfo.PatrolPortalNetworkId = resolvedNet;
                        npc.MoveBehaveInfo.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.Patrol;
                        applied = true;
                    }

                    break;
                }
            }

            npc.AIBrain.PostSearchPolicyPending = false;
            if (applied)
            {
                npc.AIBrain.ChangeState(npc.AIBrain.StateIdle);
            }
            else
            {
                npc.AIBrain.ChangeState(npc.AIBrain.StateReturn);
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

        // GM / HUD：只读，与 Tick 内选档逻辑一致。pressure_behavior 写入 NpcRecord，Search 结束后由本控制器下发移动策略。
        public string DebugFormatSelectedTier(out int alertPressureTier, out int wantedStarLevel)
        {
            alertPressureTier = 0;
            wantedStarLevel = 0;
            if (_logic?.WantedManager == null || _logic.AreaManager == null)
            {
                return "incomplete (WantedManager or AreaManager null)";
            }

            wantedStarLevel = _logic.WantedManager.GetWantedStarLevel();
            var tier = SelectPressureTier(out alertPressureTier);
            if (tier == null)
            {
                return "no tier row matched (wanted star + alert tier vs min_wanted_star_level / min_alert_tier; note min_alert_tier=999 skips alert branch)";
            }

            return
                $"tier_id={tier.TierId} min_wanted_star={tier.MinWantedStarLevel} min_alert_tier={tier.MinAlertTier} "
                + $"guard_count={tier.GuardCount} npc_cfg_id={tier.NpcCfgId} pressure_behavior={tier.PressureBehavior}(NpcRecord.PostInvestigationResolveKind) "
                + $"patrol_pick_n={tier.PatrolPickN} spawn_radius=[{tier.SpawnRadiusMin},{tier.SpawnRadiusMax}] cull_distance={tier.CullDistance}";
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
