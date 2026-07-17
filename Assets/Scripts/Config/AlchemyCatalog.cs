using System.Collections.Generic;
using cfg.demo;

namespace My.Config
{
    public static class AlchemyCatalog
    {
        public static AlchemyVirtueDef GetVirtueDef(int virtueId)
        {
            if (virtueId <= 0 || CfgMgr.Cfgs?.TbAlchemyVirtueDef == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbAlchemyVirtueDef.GetOrDefault(virtueId);
        }

        public static AlchemyAspectDef GetAspectDef(int aspectId)
        {
            if (aspectId <= 0 || CfgMgr.Cfgs?.TbAlchemyAspectDef == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbAlchemyAspectDef.GetOrDefault(aspectId);
        }

        public static bool IsValidVirtueId(int virtueId) => GetVirtueDef(virtueId) != null;

        public static bool IsValidAspectId(int aspectId) => GetAspectDef(aspectId) != null;

        public static AlchemyMaterial GetMaterial(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || CfgMgr.Cfgs?.TbAlchemyMaterial == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbAlchemyMaterial.GetOrDefault(itemId);
        }

        public static bool IsAlchemyMaterial(string itemId) => GetMaterial(itemId) != null;

        public static IReadOnlyList<AlchemyMaterial> GetMaterialsByTier(int tier)
        {
            var result = new List<AlchemyMaterial>();
            var rows = CfgMgr.Cfgs?.TbAlchemyMaterial?.DataList;
            if (rows == null || tier <= 0)
            {
                return result;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row != null && row.Tier == tier)
                {
                    result.Add(row);
                }
            }

            return result;
        }

        public static AlchemyFurnace GetFurnace(string furnaceId)
        {
            if (string.IsNullOrEmpty(furnaceId) || CfgMgr.Cfgs?.TbAlchemyFurnace == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbAlchemyFurnace.GetOrDefault(furnaceId);
        }

        public static IReadOnlyList<AlchemyFurnace> GetAllFurnaces()
            => CfgMgr.Cfgs?.TbAlchemyFurnace?.DataList;

        public static AlchemyTool GetTool(string toolId)
        {
            if (string.IsNullOrEmpty(toolId) || CfgMgr.Cfgs?.TbAlchemyTool == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbAlchemyTool.GetOrDefault(toolId);
        }

        public static AlchemyRecipe GetRecipe(int recipeId)
            => CfgMgr.Cfgs?.TbAlchemyRecipe?.GetOrDefault(recipeId);

        public static IReadOnlyList<AlchemyRecipe> GetAllRecipes()
            => CfgMgr.Cfgs?.TbAlchemyRecipe?.DataList;

        public static IReadOnlyList<AlchemyRecipe> GetRecipesByTier(int tier)
        {
            var result = new List<AlchemyRecipe>();
            var rows = GetAllRecipes();
            if (rows == null || tier <= 0)
            {
                return result;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row != null && row.Tier == tier)
                {
                    result.Add(row);
                }
            }

            return result;
        }
    }
}
