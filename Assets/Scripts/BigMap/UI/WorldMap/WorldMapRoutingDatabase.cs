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
        /// <summary>与逻辑实体 Id 对应，用于大地图打开时持续刷新位置</summary>
        public long sourceEntityId;
    }

    public sealed class WorldMapViewContext
    {
        public Sprite MapSprite;
        public Vector2 WorldMin;
        public Vector2 WorldMax;
        public readonly List<WorldMapMarkerData> Markers = new();
    }

    // 从 Resources 加载 Texture2D 并生成 Sprite（需纹理 Read/Write 开启时使用 Sprite.Create）
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

    /// <summary>大地图：路由解析、标记收集、与 UI 开关（数据来自 Luban map.xlsx）</summary>
    public static class WorldMapRuntime
    {
        public const string PanelId = "WorldMap";

        public static bool IsNpcBossLandmark(string cfgId) =>
            !string.IsNullOrEmpty(cfgId) && IdInCsv(CfgMgr.Cfgs?.TbWorldMapSettings?.GlobalNpcBossLandmarkCfgIds, cfgId);

        public static bool IsGlobalInteractLandmark(string cfgId) =>
            !string.IsNullOrEmpty(cfgId) && IdInCsv(CfgMgr.Cfgs?.TbWorldMapSettings?.GlobalInteractLandmarkCfgIds, cfgId);

        private static bool IdInCsv(string csv, string id)
        {
            if (string.IsNullOrEmpty(csv)) return false;
            foreach (var part in csv.Split(','))
            {
                if (part.Trim() == id) return true;
            }

            return false;
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
            var areaId = glm.CurrentArea;
            if (string.IsNullOrEmpty(areaId) && glm.AreaManager != null &&
                !string.IsNullOrEmpty(glm.AreaManager.MapName))
            {
                areaId = glm.AreaManager.MapName;
            }

            areaId ??= string.Empty;
            var roomId = player.BelongRoomId ?? string.Empty;

            WorldMapArea areaRow = null;
            if (cfgs != null)
            {
                areaRow = cfgs.TbWorldMapArea.GetOrDefault(areaId);
            }

            if (areaRow == null)
            {
                var allowUnknown = cfgs?.TbWorldMapSettings == null ||
                                   cfgs.TbWorldMapSettings.AllowOpenWhenAreaUnknown;
                if (allowUnknown)
                {
                    var st = cfgs?.TbWorldMapSettings;
                    var fbPath = st != null ? st.FallbackMapTextureResourcePath : string.Empty;
                    var fbSprite = WorldMapTextureResolver.Resolve(null, fbPath);
                    var wMin = st != null
                        ? new Vector2(st.FallbackWorldMinX, st.FallbackWorldMinY)
                        : new Vector2(-40f, -40f);
                    var wMax = st != null
                        ? new Vector2(st.FallbackWorldMaxX, st.FallbackWorldMaxY)
                        : new Vector2(40f, 40f);
                    ctx = new WorldMapViewContext
                    {
                        MapSprite = fbSprite,
                        WorldMin = wMin,
                        WorldMax = wMax,
                    };
                    FillMarkers(glm, player, ctx);
                    return true;
                }

                denyHint = "当前区域未配置大地图";
                return false;
            }

            var rule = PickRoomRule(cfgs, areaId, roomId);
            if (rule != null && rule.Behavior == EWorldMapRoomBehavior.ForbidOpen)
            {
                denyHint = "此处无法打开地图";
                return false;
            }

            var sprite = WorldMapTextureResolver.Resolve(null, areaRow.MapTextureResourcePath);
            var wMinArea = new Vector2(areaRow.WorldMinX, areaRow.WorldMinY);
            var wMaxArea = new Vector2(areaRow.WorldMaxX, areaRow.WorldMaxY);

            if (rule != null && rule.Behavior == EWorldMapRoomBehavior.UseAlternateMap)
            {
                var alt = WorldMapTextureResolver.Resolve(null, rule.AlternateMapTextureResourcePath);
                if (alt != null)
                {
                    sprite = alt;
                    if (rule.UseSeparateBounds)
                    {
                        wMinArea = new Vector2(rule.AlternateWorldMinX, rule.AlternateWorldMinY);
                        wMaxArea = new Vector2(rule.AlternateWorldMaxX, rule.AlternateWorldMaxY);
                    }
                }
            }

            ctx = new WorldMapViewContext
            {
                MapSprite = sprite,
                WorldMin = wMinArea,
                WorldMax = wMaxArea,
            };
            FillMarkers(glm, player, ctx);
            return true;
        }

        private static WorldMapRoomRule PickRoomRule(Tables cfgs, string areaId, string roomId)
        {
            if (cfgs == null) return null;
            var list = cfgs.TbWorldMapRoomRule.DataList;
            if (list == null || list.Count == 0) return null;

            var ordered = list
                .Where(r => r.AreaId == areaId)
                .OrderByDescending(r => r.RulePriority)
                .ToList();
            if (ordered.Count == 0) return null;

            var exact = ordered.FirstOrDefault(r =>
                !string.IsNullOrEmpty(r.RoomId) && r.RoomId != "*" && r.RoomId == roomId);
            if (exact != null) return exact;

            return ordered.FirstOrDefault(r => string.IsNullOrEmpty(r.RoomId) || r.RoomId == "*");
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
