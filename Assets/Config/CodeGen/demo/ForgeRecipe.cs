
//------------------------------------------------------------------------------
// Hand-maintained (aligned with Luban export).
//------------------------------------------------------------------------------

using Luban;
using SimpleJSON;

namespace cfg.demo
{
    [System.Serializable]
    public sealed partial class ForgeRecipe : Luban.BeanBase
    {
        public ForgeRecipe(JSONNode _buf)
        {
            { if (!_buf["id"].IsNumber) { throw new SerializationException(); } Id = _buf["id"]; }
            { if (!_buf["recipe_type"].IsNumber) { throw new SerializationException(); } RecipeType = (EForgeRecipeType)_buf["recipe_type"].AsInt; }
            { if (!_buf["sort"].IsNumber) { throw new SerializationException(); } Sort = _buf["sort"]; }
            { if (!_buf["display_name"].IsString) { throw new SerializationException(); } DisplayName = _buf["display_name"]; }
            { if (!_buf["result_item_id"].IsString) { throw new SerializationException(); } ResultItemId = _buf["result_item_id"]; }
            { if (!_buf["icon_sprite"].IsString) { throw new SerializationException(); } IconSprite = _buf["icon_sprite"]; }
            { var __json0 = _buf["materials"]; if (!__json0.IsArray) { throw new SerializationException(); } Materials = new System.Collections.Generic.List<ForgeMaterialCost>(__json0.Count); foreach (JSONNode __e0 in __json0.Children) { ForgeMaterialCost __v0; { if (!__e0.IsObject) { throw new SerializationException(); } __v0 = ForgeMaterialCost.DeserializeForgeMaterialCost(__e0); } Materials.Add(__v0); } }
            { if (!_buf["unlock_mode"].IsNumber) { throw new SerializationException(); } UnlockMode = (EForgeUnlockMode)_buf["unlock_mode"].AsInt; }
            { if (!_buf["unlock_param"].IsString) { throw new SerializationException(); } UnlockParam = _buf["unlock_param"]; }
            { if (!_buf["unlock_item_min_count"].IsNumber) { throw new SerializationException(); } UnlockItemMinCount = _buf["unlock_item_min_count"]; }
        }

        public static ForgeRecipe DeserializeForgeRecipe(JSONNode _buf)
        {
            return new ForgeRecipe(_buf);
        }

        public int Id;
        public EForgeRecipeType RecipeType;
        public int Sort;
        public string DisplayName;
        public string ResultItemId;
        public string IconSprite;
        public System.Collections.Generic.List<ForgeMaterialCost> Materials;
        public EForgeUnlockMode UnlockMode;
        public string UnlockParam;
        public long UnlockItemMinCount;

        public const int __ID__ = 914827364;
        public override int GetTypeId() => __ID__;

        public void ResolveRef(Tables tables)
        {
        }

        public override string ToString()
        {
            return "{ id:" + Id + ",recipeType:" + RecipeType + ",resultItemId:" + ResultItemId + "}";
        }
    }
}
