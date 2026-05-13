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
    // 与 AreaManager 平级挂在 GameLogicManager 上：非文明区加载完成后刷秘闻实体
    public sealed class RumorIntelMapSpawn
    {
        readonly GameLogicManager _glm;

        public RumorIntelMapSpawn(GameLogicManager glm)
        {
            _glm = glm;
        }

        public void ApplyPurchasedRumorsOnMapLoaded()
        {
            if (_glm?.AreaManager == null || _glm.playerDataManager == null)
            {
                return;
            }

            var areaCfg = _glm.AreaManager.cacheMapCfg;
            if (areaCfg == null || areaCfg.IsCivilArea)
            {
                return;
            }

            var mapId = _glm.AreaManager.MapName;
            if (string.IsNullOrEmpty(mapId) || CfgMgr.Cfgs == null)
            {
                return;
            }

            var rumor = _glm.playerDataManager.RumorIntel;
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
                        break;
                    case ERumorEffectType.Npc:
                        if (string.IsNullOrEmpty(def.NpcCfgId))
                        {
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
                        break;
                }
            }

            rumor.ConsumeAllActiveForMap(mapId);
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
