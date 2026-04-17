using System;
using System.Collections.Generic;
using System.Linq;
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

    public enum WorldMapRoomBehavior
    {
        /// <summary>不覆盖区域默认地图</summary>
        Default = 0,
        /// <summary>该房间内禁止打开大地图</summary>
        ForbidOpen = 1,
        /// <summary>使用另一张底图（可选独立边界）</summary>
        UseAlternateMap = 2,
    }

    [Serializable]
    public class WorldMapRoomRule
    {
        [Tooltip("与 LogicEntityBase.BelongRoomId 一致；填 * 表示未匹配到其它规则时的默认")]
        public string roomId = "";

        [Tooltip("同区内多条规则时，数值大优先")]
        public int rulePriority;

        public WorldMapRoomBehavior behavior = WorldMapRoomBehavior.Default;

        [Tooltip("behavior=UseAlternateMap 时使用")]
        public Sprite alternateMapSprite;

        [Tooltip("Resources 路径（无扩展名）；alternateMapSprite 为空时使用")]
        public string alternateMapTextureResourcePath = "";

        [Tooltip("勾选后使用下方独立世界坐标边界，否则沿用区域默认边界")]
        public bool useSeparateBounds;

        public Vector2 alternateWorldMin;
        public Vector2 alternateWorldMax;
    }

    [Serializable]
    public class WorldMapAreaConfig
    {
        [Tooltip("与 GameLogicManager.CurrentArea 一致")]
        public string areaId = "";

        public Sprite mapSprite;

        [Tooltip("Resources 下路径（无 .png），如 WorldMap/fake_map_base_01；mapSprite 已填时忽略")]
        public string mapTextureResourcePath = "";

        public Vector2 worldMin;
        public Vector2 worldMax;

        public List<WorldMapRoomRule> roomRules = new();
    }

    [Serializable]
    public class WorldMapFallbackConfig
    {
        [Tooltip("CurrentArea 未在 areaConfigs 中列出时是否仍允许打开")]
        public bool allowOpenWhenAreaUnknown = true;

        public Sprite mapSprite;

        [Tooltip("未匹配到区域时使用的占位图 Resources 路径（无扩展名）")]
        public string mapTextureResourcePath = "";

        public Vector2 worldMin = new Vector2(-40f, -40f);
        public Vector2 worldMax = new Vector2(40f, 40f);
    }

    [CreateAssetMenu(menuName = "My/WorldMap/Routing Database", fileName = "WorldMapRoutingDatabase")]
    public class WorldMapRoutingDatabase : ScriptableObject
    {
        public List<WorldMapAreaConfig> areaConfigs = new();

        public WorldMapFallbackConfig fallback = new();

        [Tooltip("出现在大地图上的 NPC cfgId（重要 Boss 等）")]
        public List<string> globalNpcBossLandmarkCfgIds = new();

        [Tooltip("出现在大地图上的交互物 cfgId（与交互点自身 ShowOnWorldMap 为或关系）")]
        public List<string> globalInteractLandmarkCfgIds = new();

        public bool IsNpcBossLandmark(string cfgId) =>
            !string.IsNullOrEmpty(cfgId) && globalNpcBossLandmarkCfgIds != null &&
            globalNpcBossLandmarkCfgIds.Contains(cfgId);

        public bool IsGlobalInteractLandmark(string cfgId) =>
            !string.IsNullOrEmpty(cfgId) && globalInteractLandmarkCfgIds != null &&
            globalInteractLandmarkCfgIds.Contains(cfgId);
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

    /// <summary>
    /// 从 Resources 加载 Texture2D 并生成 Sprite（需纹理 Read/Write 开启时使用 Sprite.Create）
    /// </summary>
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

    /// <summary>
    /// 大地图：路由解析、标记收集、与 UI 开关
    /// </summary>
    public static class WorldMapRuntime
    {
        public const string PanelId = "WorldMap";
        private const string DbResourcePath = "Config/WorldMapRoutingDatabase";

        public static WorldMapRoutingDatabase Database { get; private set; }

        public static void EnsureDatabaseLoaded()
        {
            if (Database != null) return;
            Database = Resources.Load<WorldMapRoutingDatabase>(DbResourcePath);
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

            EnsureDatabaseLoaded();
            // CurrentArea 若未维护，则与地图导出名一致使用 AreaManager.MapName
            var areaId = glm.CurrentArea;
            if (string.IsNullOrEmpty(areaId) && glm.AreaManager != null &&
                !string.IsNullOrEmpty(glm.AreaManager.MapName))
            {
                areaId = glm.AreaManager.MapName;
            }

            areaId ??= string.Empty;
            var roomId = player.BelongRoomId ?? string.Empty;

            WorldMapAreaConfig areaCfg = null;
            if (Database != null && Database.areaConfigs != null)
            {
                areaCfg = Database.areaConfigs.FirstOrDefault(a => a.areaId == areaId);
            }

            if (areaCfg == null)
            {
                if (Database == null || Database.fallback.allowOpenWhenAreaUnknown)
                {
                    var fb = Database != null ? Database.fallback : null;
                    var fbSprite = fb != null
                        ? WorldMapTextureResolver.Resolve(fb.mapSprite, fb.mapTextureResourcePath)
                        : null;
                    ctx = new WorldMapViewContext
                    {
                        MapSprite = fbSprite,
                        WorldMin = fb != null ? fb.worldMin : new Vector2(-40f, -40f),
                        WorldMax = fb != null ? fb.worldMax : new Vector2(40f, 40f),
                    };
                    FillMarkers(glm, player, ctx);
                    return true;
                }

                denyHint = "当前区域未配置大地图";
                return false;
            }

            var rule = PickRoomRule(areaCfg, roomId);
            if (rule != null && rule.behavior == WorldMapRoomBehavior.ForbidOpen)
            {
                denyHint = "此处无法打开地图";
                return false;
            }

            var sprite = WorldMapTextureResolver.Resolve(areaCfg.mapSprite, areaCfg.mapTextureResourcePath);
            var wMin = areaCfg.worldMin;
            var wMax = areaCfg.worldMax;

            if (rule != null && rule.behavior == WorldMapRoomBehavior.UseAlternateMap)
            {
                var alt = WorldMapTextureResolver.Resolve(rule.alternateMapSprite, rule.alternateMapTextureResourcePath);
                if (alt != null)
                {
                    sprite = alt;
                    if (rule.useSeparateBounds)
                    {
                        wMin = rule.alternateWorldMin;
                        wMax = rule.alternateWorldMax;
                    }
                }
            }

            ctx = new WorldMapViewContext
            {
                MapSprite = sprite,
                WorldMin = wMin,
                WorldMax = wMax,
            };
            FillMarkers(glm, player, ctx);
            return true;
        }

        private static WorldMapRoomRule PickRoomRule(WorldMapAreaConfig areaCfg, string roomId)
        {
            if (areaCfg.roomRules == null || areaCfg.roomRules.Count == 0) return null;

            var ordered = areaCfg.roomRules.OrderByDescending(r => r.rulePriority).ToList();
            var exact = ordered.FirstOrDefault(r => !string.IsNullOrEmpty(r.roomId) && r.roomId != "*" && r.roomId == roomId);
            if (exact != null) return exact;

            return ordered.FirstOrDefault(r => string.IsNullOrEmpty(r.roomId) || r.roomId == "*");
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
