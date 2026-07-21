using System.Collections.Generic;
using Config;
using cfg.demo;
using My.Config;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using My.MapExport;
using My.Player;
using UnityEngine;

namespace My
{
    // 与 AreaManager 平级挂在 GameLogicManager 上：非文明区加载完成后刷秘闻实体
    public sealed class RumorIntelMapSpawn
    {
        const string RumorEventPrefix = "rumor_event:";
        const string RumorIdVariable = "rumor_id";
        const string EventExpireDayVariable = "event_expire_settlement_day";
        const string CultAnchorOutcomeKind = "rumor_cult_anchor";

        readonly GameLogicManager _glm;

        public RumorIntelMapSpawn(GameLogicManager glm)
        {
            _glm = glm;
            _glm?.EventGroupOutcomes?.Register(CultAnchorOutcomeKind, TryResolveCultAnchorOutcome);
        }

        public void ApplyPurchasedRumorsOnMapLoaded()
        {
            if (_glm?.AreaManager == null || _glm.playerDataManager == null)
            {
                return;
            }

            var areaCfg = _glm.AreaManager.cacheMapOverlayCfg;
            if (areaCfg == null)
            {
                return;
            }

            var mapId = _glm.AreaManager.AreaOverlayId;
            if (string.IsNullOrEmpty(mapId) || CfgMgr.Cfgs == null)
            {
                return;
            }

            var rumor = _glm.playerDataManager.RumorIntel;
            rumor.PruneExpiredRumors(_glm.SettlementDayIndex);
            PruneExpiredEventsForCurrentMap();
            var actives = rumor.GetActiveSnapshot(mapId);
            if (actives == null || actives.Count == 0)
            {
                return;
            }

            var rumorPoints = new List<NamedPoint>();
            var db = _glm.AreaManager.cacheDatabase;
            if (db?.NamedPoints != null)
            {
                foreach (var p in db.NamedPoints)
                {
                    if (p.PointType == ENamedPointType.RumorCandidate)
                    {
                        rumorPoints.Add(p);
                    }
                }
            }

            var appliedRumorIds = new HashSet<string>();

            foreach (var a in actives)
            {
                var def = CfgMgr.Cfgs.TbRumorIntel.GetOrDefault(a.RumorId);
                if (def == null)
                {
                    continue;
                }

                var pos = PickPos(rumorPoints);

                switch (def.EffectType)
                {
                    case ERumorEffectType.Chest:
                        if (string.IsNullOrEmpty(def.LootPointCfgId))
                        {
                            Debug.LogWarning($"[RumorIntel] Rumor {a.RumorId} has no loot point config.");
                            continue;
                        }

                        if (global::Config.MapLootPointConfigLoader.Get(def.LootPointCfgId) == null)
                        {
                            Debug.LogWarning(
                                $"[RumorIntel] Rumor {a.RumorId} references unknown loot point {def.LootPointCfgId}.");
                            continue;
                        }

                        _glm.AddNewEntityRecord(new LogicEntityRecord4LootPoint
                        {
                            Id = GameLogicManager.LogicEntityIdInst++,
                            EntityType = EEntityType.LootPoint,
                            CfgId = def.LootPointCfgId,
                            Position = pos,
                            ItemInitialized = false,
                        });
                        appliedRumorIds.Add(a.RumorId);
                        break;
                    case ERumorEffectType.Npc:
                        if (string.IsNullOrEmpty(def.NpcCfgId))
                        {
                            Debug.LogWarning($"[RumorIntel] Rumor {a.RumorId} has no NPC config.");
                            continue;
                        }

                        if (CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(def.NpcCfgId) == null)
                        {
                            Debug.LogWarning(
                                $"[RumorIntel] Rumor {a.RumorId} references unknown NPC {def.NpcCfgId}.");
                            continue;
                        }

                        _glm.AddNewEntityRecord(new LogicEntityRecord4Npc
                        {
                            Id = GameLogicManager.LogicEntityIdInst++,
                            EntityType = EEntityType.Npc,
                            CfgId = def.NpcCfgId,
                            Position = pos,
                            FactionId = EFactionId.Citizen,
                            IsPeace = true,
                            MoveBehaveType = UnitMoveBehaveInfo.EMoveBehaveType.NoMove,
                            EnmityConfId = "default_npc",
                        });
                        appliedRumorIds.Add(a.RumorId);
                        break;
                    case ERumorEffectType.CultInfluence:
                        if (!RumorIntelSystem.MatchesTargetMap(def, mapId))
                        {
                            Debug.LogWarning(
                                $"[RumorIntel] Rumor {a.RumorId} targets {def.TargetOverlayId}, not {mapId}.");
                            continue;
                        }

                        if (string.IsNullOrEmpty(def.EventGroupCfgId)
                            || MapEventGroupCfgLoader.Get(def.EventGroupCfgId) == null)
                        {
                            Debug.LogWarning(
                                $"[RumorIntel] Rumor {a.RumorId} references unknown event group {def.EventGroupCfgId}.");
                            continue;
                        }

                        if (string.IsNullOrEmpty(def.CultActionId))
                        {
                            Debug.LogWarning($"[RumorIntel] Rumor {a.RumorId} has no cult action id.");
                            continue;
                        }

                        var uniqName = $"{RumorEventPrefix}{a.RumorId}";
                        if (TryGetActiveRecord(uniqName, out var existingRecord))
                        {
                            var existingExpireDay = rumor.MarkSpawned(mapId, a.RumorId, def.EventExpireDays);
                            existingRecord.DynamicVariables[RumorIdVariable] = a.RumorId;
                            existingRecord.DynamicVariables[EventExpireDayVariable] = existingExpireDay.ToString();
                            continue;
                        }

                        // A spawned entry without its unique record belongs to a previous exploration.
                        if (a.Spawned)
                        {
                            appliedRumorIds.Add(a.RumorId);
                            continue;
                        }

                        var eventExpireDay = _glm.SettlementDayIndex + Mathf.Max(1, def.EventExpireDays);
                        var initInfo = new EntityInitInfo4EventGroup
                        {
                            CfgId = def.EventGroupCfgId,
                            Position = pos,
                        };
                        initInfo.Variables.Add(RumorIdVariable, a.RumorId);
                        initInfo.Variables.Add("cult_action_id", def.CultActionId);
                        initInfo.Variables.Add("target_overlay_id", mapId);
                        initInfo.Variables.Add(EventExpireDayVariable, eventExpireDay.ToString());

                        var record = _glm.AreaManager.CreateEntityRecordFromInitInfo(initInfo);
                        if (record == null)
                        {
                            Debug.LogWarning($"[RumorIntel] Failed to create event group for rumor {a.RumorId}.");
                            continue;
                        }

                        record.SrcUniqName = uniqName;
                        _glm.AddNewEntityRecord(record);
                        rumor.MarkSpawned(mapId, a.RumorId, def.EventExpireDays);
                        break;
                    default:
                        Debug.LogWarning(
                            $"[RumorIntel] Rumor {a.RumorId} has unsupported effect type {def.EffectType}.");
                        break;
                }
            }

            rumor.ConsumeActiveForMap(mapId, appliedRumorIds);
        }

        bool TryResolveCultAnchorOutcome(EventGroupOutcomeContext context, out string failReason)
        {
            failReason = null;
            var owner = context?.Owner;
            var playerSystem = _glm?.GetPlayerSystem(context?.PlayerId ?? GamePlayerIds.Local);
            var rumorSystem = playerSystem?.RumorIntel;
            var cult = playerSystem?.ProgressionSystem?.DemonCult;
            var mapId = _glm?.AreaManager?.AreaOverlayId;
            var targetMapId = owner?.GetRuntimeVariable("target_overlay_id");
            var rumorId = owner?.GetRuntimeVariable(RumorIdVariable);
            var actionId = context?.ActionId;
            if (string.IsNullOrEmpty(actionId))
            {
                actionId = owner?.GetRuntimeVariable("cult_action_id");
            }

            if (string.IsNullOrEmpty(mapId)
                || (!string.IsNullOrEmpty(targetMapId) && targetMapId != mapId)
                || string.IsNullOrEmpty(rumorId)
                || rumorSystem == null
                || !rumorSystem.IsRumorActive(mapId, rumorId, _glm.SettlementDayIndex))
            {
                failReason = "rumor_event_context_invalid";
                return false;
            }

            var logicAreaId = TownFacilityUtil.ResolveCurrentLogicAreaId(_glm.AreaManager);
            if (cult == null || string.IsNullOrEmpty(logicAreaId))
            {
                failReason = "cult_context_invalid";
                return false;
            }

            if (!cult.TryApplyAnchorAction(
                    logicAreaId,
                    actionId,
                    _glm.SettlementDayIndex,
                    out failReason))
            {
                return false;
            }

            rumorSystem.ConsumeActiveForMap(mapId, new[] { rumorId });
            return true;
        }

        public void PruneExpiredEventsForCurrentMap()
        {
            var rumor = _glm?.playerDataManager?.RumorIntel;
            var records = _glm?.AreaManager?.Repo?.Records;
            var mapId = _glm?.AreaManager?.AreaOverlayId;
            if (rumor == null || records == null || string.IsNullOrEmpty(mapId))
            {
                return;
            }

            var day = _glm.SettlementDayIndex;
            foreach (var record in records.Values)
            {
                if (record is not LogicEntityRecord4EventGroup eventRecord
                    || eventRecord.MarkDestroyed
                    || string.IsNullOrEmpty(eventRecord.SrcUniqName)
                    || !eventRecord.SrcUniqName.StartsWith(RumorEventPrefix))
                {
                    continue;
                }

                eventRecord.DynamicVariables.TryGetValue(RumorIdVariable, out var rumorId);
                eventRecord.DynamicVariables.TryGetValue(EventExpireDayVariable, out var expireText);
                var hasExpireDay = int.TryParse(expireText, out var expireDay);
                if ((hasExpireDay && day < expireDay)
                    || (!hasExpireDay && rumor.IsRumorActive(mapId, rumorId, day)))
                {
                    continue;
                }

                var entity = _glm.GetLogicEntity(eventRecord.Id, false) as LogicEntityBase;
                if (entity != null)
                {
                    entity.DoEntityDestroyed("rumor_event_expired");
                }
                else
                {
                    foreach (var memberEntityId in eventRecord.MemberId2EntityMap.Values)
                    {
                        if (records.TryGetValue(memberEntityId, out var memberRecord))
                        {
                            memberRecord.MarkDestroyed = true;
                        }
                    }
                    eventRecord.MarkDestroyed = true;
                }
            }
        }

        bool TryGetActiveRecord(string uniqName, out LogicEntityRecord4EventGroup eventRecord)
        {
            eventRecord = null;
            var records = _glm?.AreaManager?.Repo?.Records;
            if (records == null || string.IsNullOrEmpty(uniqName))
            {
                return false;
            }

            foreach (var record in records.Values)
            {
                if (record is LogicEntityRecord4EventGroup candidate
                    && !candidate.MarkDestroyed
                    && candidate.SrcUniqName == uniqName)
                {
                    eventRecord = candidate;
                    return true;
                }
            }

            return false;
        }

        Vector2 PickPos(List<NamedPoint> rumorPoints)
        {
            if (rumorPoints.Count > 0)
            {
                var p = rumorPoints[Random.Range(0, rumorPoints.Count)];
                return new Vector2(p.Position.x, p.Position.y);
            }

            Debug.LogWarning("[RumorIntel] No RumorCandidate named points in map export; spawn at player.");
            return _glm.playerLogicEntity != null ? _glm.playerLogicEntity.Pos : Vector2.zero;
        }
    }
}
