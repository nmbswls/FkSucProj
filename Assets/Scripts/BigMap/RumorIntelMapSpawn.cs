using System.Collections.Generic;
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
    // 非文明区地图加载后：消耗已购秘闻并刷宝箱/NPC（选点：ENamedPointType.RumorCandidate）
    public static class RumorIntelMapSpawn
    {
        public static void ApplyPurchasedRumorsOnMapLoaded(GameLogicManager glm)
        {
            if (glm?.AreaManager == null || glm.playerDataManager == null)
            {
                return;
            }

            var areaCfg = glm.AreaManager.cacheMapCfg;
            if (areaCfg == null || areaCfg.IsCivilArea)
            {
                return;
            }

            var mapId = glm.AreaManager.MapName;
            if (string.IsNullOrEmpty(mapId) || CfgMgr.Cfgs == null)
            {
                return;
            }

            var progress = glm.playerDataManager.RumorIntel;
            var day = glm.SettlementDayIndex;
            var actives = progress.GetActiveSnapshot(mapId, day);
            if (actives == null || actives.Count == 0)
            {
                return;
            }

            var rumorPoints = new List<NamedPoint>();
            var db = glm.AreaManager.cacheDatabase;
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

            foreach (var a in actives)
            {
                var def = CfgMgr.Cfgs.TbRumorIntel.GetOrDefault(a.RumorId);
                if (def == null)
                {
                    continue;
                }

                var pos = PickPos(rumorPoints, glm);

                switch (def.EffectType)
                {
                    case ERumorEffectType.Chest:
                        if (string.IsNullOrEmpty(def.LootPointCfgId))
                        {
                            continue;
                        }

                        glm.AddNewEntityRecord(new LogicEntityRecord4LootPoint
                        {
                            Id = GameLogicManager.LogicEntityIdInst++,
                            EntityType = EEntityType.LootPoint,
                            CfgId = def.LootPointCfgId,
                            Position = pos,
                            ItemInitialized = false,
                        });
                        break;
                    case ERumorEffectType.Npc:
                        if (string.IsNullOrEmpty(def.NpcCfgId))
                        {
                            continue;
                        }

                        glm.AddNewEntityRecord(new LogicEntityRecord4Npc
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
                        break;
                }
            }

            progress.ConsumeAllActiveForMap(mapId, day);
        }

        static Vector2 PickPos(List<NamedPoint> rumorPoints, GameLogicManager glm)
        {
            if (rumorPoints.Count > 0)
            {
                var p = rumorPoints[Random.Range(0, rumorPoints.Count)];
                return new Vector2(p.Position.x, p.Position.y);
            }

            Debug.LogWarning("[RumorIntel] No RumorCandidate named points in map export; spawn at player.");
            return glm.playerLogicEntity != null ? glm.playerLogicEntity.Pos : Vector2.zero;
        }
    }
}
