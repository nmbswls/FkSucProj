using System;
using System.Collections.Generic;
using System.Linq;
using cfg;
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
        // 与逻辑实体 Id 对应，用于大地图打开时持续刷新位置
        public long sourceEntityId;
    }

    public sealed class WorldMapViewContext
    {
        public Sprite MapSprite;
        public Vector2 WorldMin;
        public Vector2 WorldMax;
        public readonly List<WorldMapMarkerData> Markers = new();
    }

    public static class WorldMapTextureResolver
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite Resolve(Sprite direct, string resourcesPathWithoutExtension)
        {
            if (direct != null) return direct;
            if (string.IsNullOrEmpty(resourcesPathWithoutExtension)) return null;
            if (Cache.TryGetValue(resourcesPathWithoutExtension, out var cached) && cached != null)
                return cached;

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
    }

    // Luban：TbWorldMapGlobal + TbWorldMapBigMapLayer（map.xlsx 两 sheet）
    public static class WorldMapRuntime
    {
        public const string PanelId = "WorldMap";

        public static bool IsNpcBossLandmark(string cfgId) =>
            !string.IsNullOrEmpty(cfgId) &&
            IdInCsv(CfgMgr.Cfgs?.TbWorldMapGlobal?.GlobalNpcBossLandmarkCfgIds, cfgId);

        public static bool IsGlobalInteractLandmark(string cfgId) =>
            !string.IsNullOrEmpty(cfgId) &&
            IdInCsv(CfgMgr.Cfgs?.TbWorldMapGlobal?.GlobalInteractLandmarkCfgIds, cfgId);

        private static bool IdInCsv(string csv, string id)
        {
            if (string.IsNullOrEmpty(csv)) return false;
            foreach (var part in csv.Split(','))
            {
                if (part.Trim() == id) return true;
            }

            return false;
        }

        private static bool RoomFilterMatches(string cfgRoomId, string playerRoomId)
        {
            if (string.IsNullOrEmpty(cfgRoomId) || cfgRoomId == "*") return true;
            return cfgRoomId == playerRoomId;
        }

        private static WorldMapBigMapLayer PickBigMapLayer(IReadOnlyList<WorldMapBigMapLayer> list, string sceneName, string roomId)
        {
            if (list == null || string.IsNullOrEmpty(sceneName)) return null;
            return list
                .Where(r => r.SceneName == sceneName)
                .Where(r => RoomFilterMatches(r.RoomId, roomId))
                .OrderByDescending(r => r.RulePriority)
                .FirstOrDefault();
        }

        public static void TryToggle()
        {
            var mg = MainGameManager.Instance;
            if (mg == null || mg.gameLogicManager == null) return;
            var glm = mg.gameLogicManager;
            if (glm.MainStage != GameLogicManager.EMainGameStage.Running) return;
            if (glm.IsDialogPlayering) return;

            if (UIManager.Instance != null && UIManager.Instance.IsPanelVisible(PanelId))
            {
                UIManager.Instance.HidePanel(PanelId);
                return;
            }

            if (!TryBuildViewContext(glm, out WorldMapViewContext ctx, out string denyHint))
            {
                if (!string.IsNullOrEmpty(denyHint) && glm.playerLogicEntity != null)
                {
                    var p = glm.playerLogicEntity.Pos;
                    FakeHintTextManager.ShowWorld(denyHint, new Vector3(p.x, p.y, 0f));
                }
                return;
            }

            UIManager.Instance?.ShowPanel(PanelId, ctx, UILayer.Overlay);
        }

        public static bool TryBuildViewContext(GameLogicManager glm, out WorldMapViewContext ctx, out string denyHint)
        {
            ctx = null;
            denyHint = null;
            var player = glm.playerLogicEntity;
            if (player == null)
            {
                denyHint = "Player not ready";
                return false;
            }

            var cfgs = CfgMgr.Cfgs;
            var mapId = glm.CurrentArea;
            if (string.IsNullOrEmpty(mapId) && glm.AreaManager != null &&
                !string.IsNullOrEmpty(glm.AreaManager.MapName))
            {
                mapId = glm.AreaManager.MapName;
            }

            mapId ??= string.Empty;
            var roomId = player.BelongRoomId ?? string.Empty;

            var global = cfgs?.TbWorldMapGlobal;
            var bigList = cfgs?.TbWorldMapBigMapLayer?.DataList;

            var mapCfg = CfgMgr.Cfgs.TbMapAreaInfo.GetOrDefault(mapId);

            var bigWinner = PickBigMapLayer(bigList, mapCfg.SceneName, roomId);

            if (bigWinner == null)
            {
                var allowUnknown = global == null || global.AllowOpenWhenAreaUnknown;
                if (!allowUnknown)
                {
                    denyHint = "当前区域未配置大地图";
                    return false;
                }

                var texPath = global != null ? global.FallbackBigMapTextureResourcePath : string.Empty;
                var wMin = global != null
                    ? new Vector2(global.FallbackWorldMinX, global.FallbackWorldMinY)
                    : new Vector2(-40f, -40f);
                var wMax = global != null
                    ? new Vector2(global.FallbackWorldMaxX, global.FallbackWorldMaxY)
                    : new Vector2(40f, 40f);
                ctx = new WorldMapViewContext
                {
                    MapSprite = WorldMapTextureResolver.Resolve(null, texPath),
                    WorldMin = wMin,
                    WorldMax = wMax,
                };
                FillMarkers(glm, player, ctx);
                return true;
            }

            if (bigWinner.ForbidOpenWorldMap)
            {
                denyHint = "此处无法打开地图";
                return false;
            }

            var bigPath = bigWinner.BigMapTextureResourcePath;
            if (string.IsNullOrEmpty(bigPath) && global != null)
                bigPath = global.FallbackBigMapTextureResourcePath;

            ctx = new WorldMapViewContext
            {
                MapSprite = WorldMapTextureResolver.Resolve(null, bigPath),
                WorldMin = new Vector2(bigWinner.WorldMinX, bigWinner.WorldMinY),
                WorldMax = new Vector2(bigWinner.WorldMaxX, bigWinner.WorldMaxY),
            };
            FillMarkers(glm, player, ctx);
            return true;
        }

        private static void FillMarkers(GameLogicManager glm, PlayerLogicEntity player, WorldMapViewContext ctx)
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
                if (e == null || !e.IsActive || e.MarkDestroyed) continue;
                if (e.Id == player.Id) continue;

                var kind = e.WorldMapLandmark;
                if (kind == WorldMapLandmarkKind.None || kind == WorldMapLandmarkKind.Player) continue;

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
