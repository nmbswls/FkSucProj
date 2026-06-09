using System.Collections.Generic;
using UnityEngine;

namespace My.MapExport
{
    // Editor / 运行时 AreaRoot 下 MapVariant 相关节点的固定命名与解析
    public static class MapVariantSceneHierarchy
    {
        public const string MapVariantRootName = "MapVariantRoot";
        public const string LegacyStaticRootName = "StaticRoot";
        public const string LegacyStaticPrefabRootName = "StaticPrefabRoot";

        public const string GridRootName = "GridRoot";
        public const string RoomFolderName = "Room";
        public const string CommonFolderName = "Common";
        public const string DecorateFolderName = "Decorate";
        public const string TriggerFolderName = "Trigger";
        public const string DynamicRootName = "DynamicRoot";

        public const string LegacyStaticOverlayFolderName = "StaticOverlay";
        public const string LegacyTriggerAreaName = "TriggerArea";
        public const string LegacyRoomRootName = "RoomRoot";

        // MapVariantRoot 下基础设施目录（不参与静态 prefab 扫描）
        public static readonly HashSet<string> VariantInfrastructureFolderNames = new HashSet<string>
        {
            GridRootName,
            RoomFolderName,
            LegacyStaticOverlayFolderName,
            "Roads",
            "Edge",
        };

        // MapVariantRoot 下静态导出子层
        public static readonly string[] StaticExportLayerNames =
        {
            DecorateFolderName,
            TriggerFolderName,
        };

        public static Transform ResolveMapVariantRoot(Transform areaRoot)
        {
            if (areaRoot == null)
            {
                return null;
            }

            var root = areaRoot.Find(MapVariantRootName);
            if (root != null)
            {
                return root;
            }

            root = areaRoot.Find(LegacyStaticRootName);
            if (root != null)
            {
                return root;
            }

            return areaRoot.Find(LegacyStaticPrefabRootName);
        }

        public static Transform ResolveDynamicRoot(Transform areaRoot)
        {
            return areaRoot != null ? areaRoot.Find(DynamicRootName) : null;
        }

        public static Transform ResolveGridRoot(Transform areaRoot)
        {
            var mapVariantRoot = ResolveMapVariantRoot(areaRoot);
            if (mapVariantRoot != null)
            {
                var gridRoot = mapVariantRoot.Find(GridRootName);
                if (gridRoot != null)
                {
                    return gridRoot;
                }
            }

            return areaRoot != null ? areaRoot.Find(GridRootName) : null;
        }

        public static bool IsVariantInfrastructureFolder(string folderName)
        {
            return !string.IsNullOrEmpty(folderName) && VariantInfrastructureFolderNames.Contains(folderName);
        }

        // 兼容旧命名
        public static bool IsVariantLevelFolder(string folderName) => IsVariantInfrastructureFolder(folderName);
    }
}
