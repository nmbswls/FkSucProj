using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player.Bag;

namespace My.Player.Alchemy
{
    public static class AlchemyOwnershipUtil
    {
        public static bool IsFurnaceOwned(PlayerInventorySystem inv, string furnaceId)
        {
            if (string.IsNullOrEmpty(furnaceId) || inv == null)
            {
                return false;
            }

            if (furnaceId == AlchemyConstants.HandCraftFurnaceId)
            {
                return true;
            }

            var ownerItemId = ResolveFurnaceOwnerItemId(furnaceId);
            return !string.IsNullOrEmpty(ownerItemId) && inv.CheckHaveItem(ownerItemId, 1);
        }

        public static List<string> GetSelectableFurnaceIds(PlayerInventorySystem inv)
        {
            var result = new List<string> { AlchemyConstants.HandCraftFurnaceId };
            var furnaces = AlchemyCatalog.GetAllFurnaces();
            if (furnaces == null)
            {
                return result;
            }

            for (int i = 0; i < furnaces.Count; i++)
            {
                var furnace = furnaces[i];
                if (furnace == null || furnace.FurnaceId == AlchemyConstants.HandCraftFurnaceId)
                {
                    continue;
                }

                if (IsFurnaceOwned(inv, furnace.FurnaceId))
                {
                    result.Add(furnace.FurnaceId);
                }
            }

            return result;
        }

        public static bool IsToolOwned(PlayerInventorySystem inv, string toolId)
        {
            return !string.IsNullOrEmpty(toolId) && inv != null && inv.CheckHaveItem(toolId, 1);
        }

        public static List<string> GetOwnedToolIds(PlayerInventorySystem inv)
        {
            var result = new List<string>();
            var tools = CfgMgr.Cfgs?.TbAlchemyTool?.DataList;
            if (tools == null || inv == null)
            {
                return result;
            }

            for (int i = 0; i < tools.Count; i++)
            {
                var tool = tools[i];
                if (tool != null && IsToolOwned(inv, tool.ToolId))
                {
                    result.Add(tool.ToolId);
                }
            }

            return result;
        }

        public static string ResolveFurnaceOwnerItemId(string furnaceId)
        {
            if (string.IsNullOrEmpty(furnaceId))
            {
                return null;
            }

            var furnace = AlchemyCatalog.GetFurnace(furnaceId);
            if (furnace != null && !string.IsNullOrEmpty(furnace.OwnerItemId))
            {
                return furnace.OwnerItemId;
            }

            if (furnace != null && furnace.FurnaceId == AlchemyConstants.HandCraftFurnaceId)
            {
                return null;
            }

            if (ItemCatalog.GetItemDef(furnaceId) != null)
            {
                return furnaceId;
            }

            var prefixed = "item_" + furnaceId;
            if (ItemCatalog.GetItemDef(prefixed) != null)
            {
                return prefixed;
            }

            return furnace?.OwnerItemId;
        }
    }
}
