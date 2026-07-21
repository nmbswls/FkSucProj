using System.Collections.Generic;
using UnityEngine;

namespace My.Farm
{
    // 场景农田原点注册表：由 FarmPlotAreaProvider 在启用时登记
    public static class FarmPlotAreaRegistry
    {
        static readonly Dictionary<string, Dictionary<string, Vector2>> OriginsByArea = new();

        public static void Register(string logicAreaId, string plotId, Vector2 worldOrigin)
        {
            if (string.IsNullOrEmpty(logicAreaId) || string.IsNullOrEmpty(plotId))
            {
                return;
            }

            if (!OriginsByArea.TryGetValue(logicAreaId, out var map))
            {
                map = new Dictionary<string, Vector2>();
                OriginsByArea[logicAreaId] = map;
            }

            map[plotId] = worldOrigin;
        }

        public static void Unregister(string logicAreaId, string plotId)
        {
            if (string.IsNullOrEmpty(logicAreaId) || string.IsNullOrEmpty(plotId))
            {
                return;
            }

            if (!OriginsByArea.TryGetValue(logicAreaId, out var map))
            {
                return;
            }

            map.Remove(plotId);
            if (map.Count == 0)
            {
                OriginsByArea.Remove(logicAreaId);
            }
        }

        public static bool TryGetOrigin(string logicAreaId, out Dictionary<string, Vector2> origins)
        {
            if (!string.IsNullOrEmpty(logicAreaId) && OriginsByArea.TryGetValue(logicAreaId, out origins))
            {
                return true;
            }

            origins = new Dictionary<string, Vector2>();
            return false;
        }
    }
}
