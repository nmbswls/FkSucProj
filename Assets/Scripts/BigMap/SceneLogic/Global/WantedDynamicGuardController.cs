using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using My.Map.Scene;
using My.MapExport;
using My.Saving;
using UnityEngine;

namespace My
{
    // 通缉星级或区域警戒档位驱动：动态维持临时守卫；高星级/高档位含搜查、退场或路网巡逻
    public sealed class WantedDynamicGuardController
    {
        // 动态守卫补刷逻辑间隔（秒）；GM 说明与测试等待时间参照此值。
        public const float TickPeriodSeconds = 0.75f;

        readonly GameLogicManager _logic;
        readonly Dictionary<long, WantedPressureSession> _sessions = new();
        float _cooldown;

        public WantedDynamicGuardController(GameLogicManager logic)
        {
            _logic = logic;
        }

        int ActiveSpawnSlotCount()
        {
            int count = 0;
            foreach (var session in _sessions.Values)
            {
                if (session != null && session.Phase != EWantedPressurePhase.WalkingAway)
                {
                    count++;
                }
            }

            return count;
        }

        public void ClearAll()
        {
            if (_logic?.AreaManager != null)
            {
                foreach (var entityId in _sessions.Keys)
                {
                    _logic.AreaManager.RequestEntityDestroy(entityId, "wanted_guard_area_clear");
                }
            }

            _sessions.Clear();
        }

        public void Tick(float dt)
        {
            if (_logic?.WantedManager == null || _logic.playerLogicEntity == null || _logic.AreaManager == null)
            {
                return;
            }

            PruneStaleSessions();
            ProcessPressureNpcBrain();

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

            CullFar(cull);
            if (target < ActiveSpawnSlotCount())
            {
                TrimExcess(target);
                return;
            }

            while (ActiveSpawnSlotCount() < target)
            {
                if (!TrySpawnOne(npcId, rMin, rMax, pressureBehavior, patrolPickN))
                {
                    break;
                }
            }
        }

        public bool HasPressureSession(long entityId)
        {
            return _sessions.ContainsKey(entityId);
        }

        public bool TryGetPressureSession(long entityId, out WantedPressureSession session)
        {
            return _sessions.TryGetValue(entityId, out session);
        }

        void ProcessPressureNpcBrain()
        {
            if (_sessions.Count == 0 || _logic?.AreaManager == null)
            {
                return;
            }

            foreach (var pair in _sessions)
            {
                var session = pair.Value;
                if (session == null)
                {
                    continue;
                }

                var e = _logic.AreaManager.GetLogicEntiy(pair.Key, false);
                if (e is not NpcUnitLogicEntity npc)
                {
                    continue;
                }

                if (!WantedPressureNpcBrain.IsAvailable(npc))
                {
                    continue;
                }

                WantedPressureNpcBrain.SyncHomeToFeet(npc);

                if (session.ShouldReplayMacroBehave() && !WantedPressureNpcBrain.HasWantedMacro(npc))
                {
                    if (!TryReplaySessionMacroBehave(npc, session, out var replayed) || !replayed)
                    {
                        if (ShouldDestroyOnMacroReplayFailure(session))
                        {
                            DestroyPressureGuard(npc, "wanted_macro_replay_failed");
                        }
                    }
                    else
                    {
                        WantedPressureNpcBrain.RefreshIdleMacro(npc);
                    }

                    continue;
                }

                if (session.ShouldBeginInvestigation() && !WantedPressureNpcBrain.IsInSearch(npc))
                {
                    WantedPressureNpcBrain.TryBeginInvestigation(_logic, npc);
                    continue;
                }

                if (session.Phase == EWantedPressurePhase.AwaitingInvestigation && session.ResolveKind > 0)
                {
                    TryCompleteInvestigation(npc);
                }
            }
        }

        // Wanted Tick：实体已在 Idle 时下发 post-search macro
        bool TryCompleteInvestigation(NpcUnitLogicEntity npc)
        {
            if (!WantedPressureNpcBrain.IsAvailable(npc))
            {
                return false;
            }

            if (!TryGetPressureSession(npc.Id, out var session)
                || session.Phase != EWantedPressurePhase.AwaitingInvestigation
                || session.ResolveKind <= 0)
            {
                return false;
            }

            if (!WantedPressureNpcBrain.IsIdle(npc))
            {
                return false;
            }

            var db = _logic?.AreaManager?.cacheDatabase;
            if (!WantedPressureMacroBehave.TryApplyResolveKind(npc, session, db, out var applied) || !applied)
            {
                if (ShouldDestroyOnMacroApplyFailure(session))
                {
                    DestroyPressureGuard(npc, "wanted_investigation_macro_failed");
                }

                return false;
            }

            SetSessionPhase(npc.Id, WantedPressureSession.ResolvePhaseAfterInvestigation(session.ResolveKind));
            WantedPressureNpcBrain.RefreshIdleMacro(npc);
            return true;
        }

        static bool ShouldDestroyOnMacroApplyFailure(WantedPressureSession session)
        {
            return session != null && (session.ResolveKind == 1 || session.ResolveKind == 3);
        }

        static bool ShouldDestroyOnMacroReplayFailure(WantedPressureSession session)
        {
            if (session == null)
            {
                return false;
            }

            if (session.Phase == EWantedPressurePhase.WalkingAway)
            {
                return true;
            }

            return session.ResolveKind == 1 || session.ResolveKind == 3;
        }

        void DestroyPressureGuard(NpcUnitLogicEntity npc, string reason)
        {
            if (npc == null || _logic?.AreaManager == null)
            {
                return;
            }

            DestroyPressureGuard(npc.Id, reason);
        }

        void DestroyPressureGuard(long entityId, string reason)
        {
            RemovePressureSession(entityId);
            _logic?.AreaManager?.RequestEntityDestroy(entityId, reason);
        }

        public List<WantedPressureSessionPersist> ExportPressureSessions()
        {
            var list = new List<WantedPressureSessionPersist>(_sessions.Count);
            foreach (var session in _sessions.Values)
            {
                if (session == null) continue;
                list.Add(session.ToPersist());
            }

            return list;
        }

        public void RestorePressureSessions(IReadOnlyList<WantedPressureSessionPersist> persists)
        {
            _sessions.Clear();
            if (persists == null || persists.Count == 0)
            {
                return;
            }

            foreach (var persist in persists)
            {
                var session = WantedPressureSession.FromPersist(persist);
                if (session == null || session.EntityId <= 0)
                {
                    continue;
                }

                _sessions[session.EntityId] = session;
            }
        }

        void SetSessionPhase(long entityId, EWantedPressurePhase phase)
        {
            if (_sessions.TryGetValue(entityId, out var session))
            {
                session.Phase = phase;
            }
        }

        public void RegisterPressureGuard(long entityId, int resolveKind, int patrolPickN, bool beginInvestigationImmediately)
        {
            _sessions[entityId] = new WantedPressureSession
            {
                EntityId = entityId,
                ResolveKind = resolveKind,
                PatrolPickN = patrolPickN > 0 ? patrolPickN : 3,
                Phase = WantedPressureSession.ResolveInitialPhase(resolveKind, beginInvestigationImmediately),
            };
        }

        void PruneStaleSessions()
        {
            if (_sessions.Count == 0 || _logic?.AreaManager == null)
            {
                return;
            }

            var stale = new List<long>();
            foreach (var entityId in _sessions.Keys)
            {
                var e = _logic.AreaManager.GetLogicEntiy(entityId, false);
                if (e == null || e.MarkDestroyed)
                {
                    stale.Add(entityId);
                }
                else if (e is BaseUnitLogicEntity unit && unit.IsDead)
                {
                    stale.Add(entityId);
                }
            }

            for (int i = 0; i < stale.Count; i++)
            {
                RemovePressureSession(stale[i]);
            }
        }

        void RemovePressureSession(long entityId)
        {
            _sessions.Remove(entityId);

            var e = _logic?.AreaManager?.GetLogicEntiy(entityId, false) as BaseUnitLogicEntity;
            e?.ClearMacroBehave(BaseUnitLogicEntity.EMacroBehaveAuthority.Wanted);
        }

        bool TryReplaySessionMacroBehave(NpcUnitLogicEntity npc, WantedPressureSession session, out bool applied)
        {
            var db = _logic?.AreaManager?.cacheDatabase;
            if (session.Phase == EWantedPressurePhase.WalkingAway)
            {
                return WantedPressureMacroBehave.TryApplyWalkingAway(npc, session, db, out applied);
            }

            return WantedPressureMacroBehave.TryApplyResolveKind(npc, session, db, out applied);
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

        // GM / HUD：只读，与 Tick 内选档逻辑一致。pressure_behavior 写入 WantedPressureSession，Search 结束后下发 MacroMoveBehave。
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
                + $"guard_count={tier.GuardCount} npc_cfg_id={tier.NpcCfgId} pressure_behavior={tier.PressureBehavior}(WantedPressureSession.ResolveKind) "
                + $"patrol_pick_n={tier.PatrolPickN} spawn_radius=[{tier.SpawnRadiusMin},{tier.SpawnRadiusMax}] cull_distance={tier.CullDistance}";
        }

        void TrySendGuardWalkAwayOrDestroy(long id, string destroyReasonFallback)
        {
            var e = _logic.AreaManager.GetLogicEntiy(id, false);
            if (e == null || e.MarkDestroyed)
            {
                _logic.AreaManager.RequestEntityDestroy(id, destroyReasonFallback);
                return;
            }

            if (e is not NpcUnitLogicEntity npc)
            {
                _logic.AreaManager.RequestEntityDestroy(id, destroyReasonFallback);
                return;
            }

            if (npc.IsDead)
            {
                _logic.AreaManager.RequestEntityDestroy(id, destroyReasonFallback);
                return;
            }

            if (npc.IsInCombat)
            {
                _logic.AreaManager.RequestEntityDestroy(id, destroyReasonFallback + "_combat");
                return;
            }

            var db = _logic.AreaManager.cacheDatabase;
            if (!TryGetPressureSession(id, out var session))
            {
                _logic.AreaManager.RequestEntityDestroy(id, destroyReasonFallback);
                return;
            }

            if (WantedPressureMacroBehave.TryApplyWalkingAway(npc, session, db, out var applied) && applied)
            {
                SetSessionPhase(id, EWantedPressurePhase.WalkingAway);
                WantedPressureNpcBrain.EnterIdle(npc);
                return;
            }

            DestroyPressureGuard(id, destroyReasonFallback + "_walkaway_failed");
        }

        void CullFar(float cullDistance)
        {
            var p = _logic.playerLogicEntity.Pos;
            float d2 = cullDistance * cullDistance;
            var farIds = new List<long>();

            foreach (var pair in _sessions)
            {
                if (pair.Value.Phase == EWantedPressurePhase.WalkingAway)
                {
                    continue;
                }

                ILogicEntity e = _logic.AreaManager.GetLogicEntiy(pair.Key, false);
                if (e == null)
                {
                    farIds.Add(pair.Key);
                    continue;
                }

                if ((e.Pos - p).sqrMagnitude > d2)
                {
                    farIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < farIds.Count; i++)
            {
                TrySendGuardWalkAwayOrDestroy(farIds[i], "wanted_guard_cull_distance");
            }
        }

        void TrimExcess(int target)
        {
            var p = _logic.playerLogicEntity.Pos;
            while (ActiveSpawnSlotCount() > target)
            {
                long id = FindFarthestSpawnSlotEntityId(p);
                if (id <= 0)
                {
                    break;
                }

                TrySendGuardWalkAwayOrDestroy(id, "wanted_guard_trim");
            }
        }

        long FindFarthestSpawnSlotEntityId(Vector2 p)
        {
            long bestId = 0;
            float bestD = -1f;
            foreach (var pair in _sessions)
            {
                if (pair.Value.Phase == EWantedPressurePhase.WalkingAway)
                {
                    continue;
                }

                ILogicEntity e = _logic.AreaManager.GetLogicEntiy(pair.Key, false);
                float d = e != null ? (e.Pos - p).sqrMagnitude : float.MaxValue;
                if (d > bestD)
                {
                    bestD = d;
                    bestId = pair.Key;
                }
            }

            return bestId;
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
                };
                _logic.AddNewEntityRecord(rec);
                RegisterPressureGuard(
                    rec.Id,
                    pressureBehavior,
                    patrolPickN,
                    pressureBehavior > 0);
                return true;
            }

            return false;
        }
    }
}
