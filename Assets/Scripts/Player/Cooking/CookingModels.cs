using System.Collections.Generic;
using cfg.demo;

namespace My.Player.Cooking
{
    public sealed class CookingIngredientQuote
    {
        public string ItemId;
        public long RequiredCount;
        public long OwnedCount;
        public bool IsEnough => OwnedCount >= RequiredCount;
    }

    public sealed class CookingCraftQuote
    {
        public CookingRecipe Recipe;
        public int BatchCount;
        public long OutputCount;
        public string QualityResultItemId;
        public float QualityChance;
        public bool IsUnlocked;
        public bool IsConfigValid;
        public bool HasMaterials;
        public bool HasOutputSpace;
        public ECookingActionResult Result;
        public IReadOnlyList<CookingIngredientQuote> Ingredients;
        public bool CanCraft => Result == ECookingActionResult.Success;
    }

    public enum ECookingActionResult
    {
        Success = 0,
        InvalidRequest,
        UnknownRecipe,
        Locked,
        InvalidConfig,
        InsufficientItems,
        InventoryFull,
        UnexpectedInventoryFailure,
    }
}
