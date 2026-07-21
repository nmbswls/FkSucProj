using System.Collections.Generic;
using cfg.demo;
using My.Config;
using UnityEngine;

namespace My.Farm
{
    public static class FarmCatalog
    {
        public const string FarmStationFacilityId = "farm_station";
        public const string DefaultLogicAreaId = "homestead_01";
        public const int SeedBasketCapacity = 24;
        public const int ProduceWarehouseCapacity = 40;

        public static CropDef GetCrop(string cropId)
        {
            if (string.IsNullOrEmpty(cropId))
            {
                return null;
            }

            return CfgMgr.Cfgs?.TbCropDef?.GetOrDefault(cropId);
        }

        public static CropDef FindCropBySeedItem(string seedItemId)
        {
            var list = CfgMgr.Cfgs?.TbCropDef?.DataList;
            if (list == null || string.IsNullOrEmpty(seedItemId))
            {
                return null;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].SeedItemId == seedItemId)
                {
                    return list[i];
                }
            }

            return null;
        }

        public static IReadOnlyList<FarmPlot> GetPlotsForArea(string logicAreaId)
        {
            var result = new List<FarmPlot>();
            var list = CfgMgr.Cfgs?.TbFarmPlot?.DataList;
            if (list == null)
            {
                return result;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].LogicAreaId == logicAreaId)
                {
                    result.Add(list[i]);
                }
            }

            result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return result;
        }

        public static List<Vector2Int> ParseCells(string cells)
        {
            var result = new List<Vector2Int>();
            if (string.IsNullOrEmpty(cells))
            {
                return result;
            }

            var parts = cells.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                if (string.IsNullOrWhiteSpace(p))
                {
                    continue;
                }

                var xy = p.Split(',');
                if (xy.Length < 2)
                {
                    continue;
                }

                if (int.TryParse(xy[0].Trim(), out int x) && int.TryParse(xy[1].Trim(), out int y))
                {
                    result.Add(new Vector2Int(x, y));
                }
            }

            return result;
        }

        public static bool IsMature(CropDef crop, int growProgress)
        {
            return crop != null && growProgress >= Mathf.Max(1, crop.GrowDays);
        }

        public static bool IsSprouted(CropDef crop, int growProgress)
        {
            return crop != null && growProgress >= Mathf.Max(0, crop.SproutDay);
        }
    }
}
