using System;
using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using My.Config;
using My.Map.Entity;
using My.UI;
using UnityEngine;
using My;

namespace My.Map
{
    public enum WorldMapLandmarkKind
    {
        None = 0,
        Player = 1,
        MajorInteract = 2,
        MajorBoss = 3,
    }

    [Serializable]
    public struct WorldMapMarkerData
    {
        public Vector2 worldPos;
        public WorldMapLandmarkKind kind;
        public string label;
        public long sourceEntityId;
    }

    public sealed class WorldMapViewContext
    {
        public string MapOverlayId;
        public Sprite MapSprite;
        public Vector2 WorldMin;
        public Vector2 WorldMax;
        public bool UsesWorldCoords;
        public bool HasMapLayer;
        public readonly List<WorldMapMarkerData> Markers = new();
    }

    public static class WorldMapTextureResolver
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite Resolve(Sprite direct, string resourcesPathWithoutExtension)
        {
            if (direct != null)
            {
                return direct;
            }

            if (string.IsNullOrEmpty(resourcesPathWithoutExtension))
            {
                return null;
            }

            if (Cache.TryGetValue(resourcesPathWithoutExtension, out var cached) && cached != null)
            {
                return cached;
            }

            var tex = Resources.Load<Texture2D>(resourcesPathWithoutExtension);
            if (tex == null)
            {
                Debug.LogWarning($"[WorldMap] Resources.Load<Texture2D> failed: '{resourcesPathWithoutExtension}'");
                return null;
            }

            var sp = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            Cache[resourcesPathWithoutExtension] = sp;
            return sp;
        }

        public static Sprite ResolveMiniMapThumb(string thumbName)
        {
            if (string.IsNullOrEmpty(thumbName))
            {
                return null;
            }

            return Resources.Load<Sprite>($"MiniMap/{thumbName}");
        }

        public static string NormalizeBigMapPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (path.Contains("/"))
            {
                return path;
            }

            return $"MiniMap/{path}";
        }
    }

    public static class WorldMapRuntime
    {
        public const string PanelId = "WorldMap";

        public static bool IsNpcBossLandmark(string cfgId) =>
            !string.IsNullOrEmpty(cfgId) &&
            IdInCsv(CfgMgr.Cfgs?.TbWorldMapGlobal?.GlobalNpcBossLandmarkCfgIds, cfgId);

        public static bool IsGlobalInteractLandmark(string cfgId) =>
            !string.IsNullOrEmpty(cfgId) &&
            IdInCsv(CfgMgr.Cfgs?.TbWorldMapGlobal?.GlobalInteractLandmarkCfgIds, cfgId);

        static bool IdInCsv(string csv, string id)
        {
            if (string.IsNullOrEmpty(csv))
            {
                return false;
            }

            foreach (var part in csv.Split(','))
            {
                if (part.Trim() == id)
                {
                    return true;
                }
            }

            return false;
        }

        static bool RoomFilterMatches(string cfgRoomId, string playerRoomId)
        {
            if (string.IsNullOrEmpty(cfgRoomId) || cfgRoomId == "*")
            {
                return true;
            }

            return cfgRoomId == playerRoomId;
        }

        static WorldMapBigMapLayer PickBigMapLayer(IReadOnlyList<WorldMapBigMapLayer> list, string roomId)
        {
            if (list == null || list.Count == 0)
            {
                return null;
            }

            var winner = list
                .Where(r => RoomFilterMatches(r.RoomId, roomId))
                .OrderByDescending(r => r.RulePriority)
                .FirstOrDefault();
            if (winner != null)
            {
                return winner;
            }

            return list.OrderByDescending(r => r.RulePriority).FirstOrDefault();
        }

        static string ResolveRoomIdForMapView(GameLogicManager glm, PlayerLogicEntity player, string mapOverlayId)
        {
            if (player == null || glm?.AreaManager == null)
            {
                return string.Empty;
            }

            var currentMapId = glm.AreaManager.AreaOverlayId;
            if (string.IsNullOrEmpty(currentMapId) || currentMapId != mapOverlayId)
            {
                return string.Empty;
            }

            return player.BelongRoomId ?? string.Empty;
        }

        public static bool HasBigMapLayer(string mapOverlayId)
        {
            var mapCfg = CfgMgr.Cfgs?.TbAreaOverlayStateInfo?.GetOrDefault(mapOverlayId);
            var layers = mapCfg?.BelongVariantInfo?.MapLayers;
            return layers != null && layers.Count > 0;
        }

        static Sprite ResolveBigMapSprite(string bigPath, WorldMapGlobal global)
        {
            var sprite = WorldMapTextureResolver.Resolve(
                null,
                WorldMapTextureResolver.NormalizeBigMapPath(bigPath));
            if (sprite != null || global == null)
            {
                return sprite;
            }

            return WorldMapTextureResolver.Resolve(
                null,
                WorldMapTextureResolver.NormalizeBigMapPath(global.FallbackBigMapTextureResourcePath));
        }

        public static bool CanOpenMap(GameLogicManager glm, out string denyHint)
        {
            denyHint = null;
            if (glm == null)
            {
                denyHint = "Game not ready";
                return false;
            }

            if (glm.MainStage != GameLogicManager.EMainGameStage.Running)
            {
                denyHint = "当前无法打开地图";
                return false;
            }

            if (glm.IsDialogPlayering)
            {
                denyHint = "对话中无法打开地图";
                return false;
            }

            if (glm.IsInfiltrationRun)
            {
                denyHint = "潜入中无法打开地图";
                return false;
            }

            if (glm.playerLogicEntity == null && !glm.IsInSecretBase)
            {
                denyHint = "Player not ready";
                return false;
            }

            return true;
        }

        public static void CollectBrowsableMaps(GameLogicManager glm, List<AreaOverlayStateInfo> outMaps)
        {
            outMaps.Clear();
            var tb = CfgMgr.Cfgs?.TbAreaOverlayStateInfo;
            if (tb?.DataList == null)
            {
                return;
            }

            var dayPeriod = glm?.DayPeriod ?? GameLogicManager.EDayPeriod.Day;
            foreach (var m in tb.DataList)
            {
                if (m == null || string.IsNullOrEmpty(m.Id))
                {
                    continue;
                }

                var variant = m.BelongVariantInfo;
                if (variant == null || !variant.ShowInMap)
                {
                    continue;
                }

                if (m.DayPeriodLimit != 0)
                {
                    if (m.DayPeriodLimit == 1 && dayPeriod != GameLogicManager.EDayPeriod.Day)
                    {
                        continue;
                    }

                    if (m.DayPeriodLimit == 2 && dayPeriod != GameLogicManager.EDayPeriod.Night)
                    {
                        continue;
                    }
                }

                if (glm != null && !glm.CheckCommonCondsAll(variant.ShowConds))
                {
                    continue;
                }

                outMaps.Add(m);
            }

            outMaps.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        }

        public static bool CanTeleportToSavePoint(
            GameLogicManager glm,
            AreaOverlayStateInfo map,
            SavePoint savePoint,
            out string reason)
        {
            reason = null;
            if (glm == null || map == null || savePoint == null)
            {
                reason = "invalid_args";
                return false;
            }

            if (glm.IsInfiltrationRun)
            {
                reason = "infiltration_run";
                return false;
            }

            if (!map.CanTeleport)
            {
                reason = "map_no_teleport";
                return false;
            }

            if (map.IsDangerArea)
            {
                reason = "danger_area";
                return false;
            }

            if (!SavePointUnlockHelper.IsActivated(glm, savePoint.SavePointId))
            {
                reason = "save_point_locked";
                return false;
            }

            return true;
        }

        public static void TryToggle()
        {
            var mg = MainGameManager.Instance;
            if (mg == null || mg.gameLogicManager == null)
            {
                return;
            }

            var glm = mg.gameLogicManager;
            if (UIManager.Instance != null && UIManager.Instance.IsPanelVisible(PanelId))
            {
                UIManager.Instance.HidePanel(PanelId);
                return;
            }

            if (!CanOpenMap(glm, out var denyHint))
            {
                if (!string.IsNullOrEmpty(denyHint) && glm.playerLogicEntity != null)
                {
                    var p = glm.playerLogicEntity.Pos;
                    FakeHintTextManager.ShowWorld(denyHint, new Vector3(p.x, p.y, 0f));
                }

                return;
            }

            var selectMapId = glm.AreaManager?.AreaOverlayId;
            UIManager.Instance?.ShowPanel(
                PanelId,
                new WorldMapPanel.OpenArgs { SelectMapId = selectMapId },
                UILayer.Overlay);
        }

        public static bool TryBuildViewContext(
            GameLogicManager glm,
            string mapOverlayId,
            bool liveMarkers,
            out WorldMapViewContext ctx)
        {
            ctx = null;
            if (string.IsNullOrEmpty(mapOverlayId))
            {
                return false;
            }

            var cfgs = CfgMgr.Cfgs;
            var mapCfg = cfgs?.TbAreaOverlayStateInfo?.GetOrDefault(mapOverlayId);
            if (mapCfg == null)
            {
                return false;
            }

            var player = glm?.playerLogicEntity;
            var roomId = ResolveRoomIdForMapView(glm, player, mapOverlayId);
            var global = cfgs?.TbWorldMapGlobal.Data;
            var bigWinner = PickBigMapLayer(mapCfg.BelongVariantInfo?.MapLayers, roomId);

            if (bigWinner == null)
            {
                ctx = new WorldMapViewContext
                {
                    MapOverlayId = mapOverlayId,
                    HasMapLayer = false,
                };
                return true;
            }

            var bigPath = bigWinner.BigMapTexturePath;
            if (string.IsNullOrEmpty(bigPath) && global != null)
            {
                bigPath = global.FallbackBigMapTextureResourcePath;
            }

            ctx = new WorldMapViewContext
            {
                MapOverlayId = mapOverlayId,
                MapSprite = ResolveBigMapSprite(bigPath, global),
                WorldMin = new Vector2(bigWinner.WorldMinX, bigWinner.WorldMinY),
                WorldMax = new Vector2(bigWinner.WorldMaxX, bigWinner.WorldMaxY),
                UsesWorldCoords = true,
                HasMapLayer = true,
            };

            if (liveMarkers && player != null && glm != null)
            {
                FillMarkers(glm, player, ctx);
            }

            return true;
        }

        static void FillMarkers(GameLogicManager glm, PlayerLogicEntity player, WorldMapViewContext ctx)
        {
            ctx.Markers.Add(new WorldMapMarkerData
            {
                worldPos = player.Pos,
                kind = WorldMapLandmarkKind.Player,
                label = "Player",
                sourceEntityId = player.Id,
            });

            foreach (var kv in glm.AreaManager.Repo.Records)
            {
                var e = glm.GetLogicEntity(kv.Key, false) as LogicEntityBase;
                if (e == null || !e.IsActive || e.MarkDestroyed)
                {
                    continue;
                }

                if (e.Id == player.Id)
                {
                    continue;
                }

                var kind = e.WorldMapLandmark;
                if (kind == WorldMapLandmarkKind.None || kind == WorldMapLandmarkKind.Player)
                {
                    continue;
                }

                ctx.Markers.Add(new WorldMapMarkerData
                {
                    worldPos = e.Pos,
                    kind = kind,
                    label = e.WorldMapLandmarkLabel,
                    sourceEntityId = e.Id,
                });
            }
        }
    }
}
