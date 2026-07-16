using System.Collections.Generic;
using cfg.demo;
using My.Config;

namespace My.Player.Alchemy
{
    public static class AlchemyMixResolver
    {
        public static bool TryResolve(
            string furnaceId,
            IReadOnlyList<string> activeToolIds,
            IReadOnlyList<AlchemyInputSlot> materialSlots,
            int progressLevel,
            out AlchemyMixState mixState,
            out string failReasonEn)
        {
            mixState = null;
            failReasonEn = "";

            var furnace = AlchemyCatalog.GetFurnace(furnaceId);
            if (furnace == null)
            {
                failReasonEn = "Unknown alchemy furnace.";
                return false;
            }

            if (!HasRequiredTools(furnace, activeToolIds, out failReasonEn))
            {
                return false;
            }

            int maxSlots = furnace.MaxMaterialSlots + AlchemyUnlockUtil.ResolveExtraMaterialSlots(progressLevel, activeToolIds);
            if (!ValidateMaterialSlots(materialSlots, maxSlots, out failReasonEn))
            {
                return false;
            }

            mixState = BuildMixState(furnace, activeToolIds, materialSlots);
            return true;
        }

        public static bool MatchesRecipe(AlchemyMixState mixState, AlchemyRecipe recipe)
        {
            if (mixState == null || recipe == null)
            {
                return false;
            }

            if (recipe.VirtueRequirements != null)
            {
                for (int i = 0; i < recipe.VirtueRequirements.Count; i++)
                {
                    var req = recipe.VirtueRequirements[i];
                    if (req == null || req.VirtueId <= 0 || req.Value <= 0)
                    {
                        continue;
                    }

                    if (mixState.GetVirtue(req.VirtueId) < req.Value)
                    {
                        return false;
                    }
                }
            }

            if (recipe.AspectRequirements != null)
            {
                for (int i = 0; i < recipe.AspectRequirements.Count; i++)
                {
                    var req = recipe.AspectRequirements[i];
                    if (req == null || req.AspectId <= 0 || req.Value <= 0)
                    {
                        continue;
                    }

                    if (mixState.GetAspect(req.AspectId) < req.Value)
                    {
                        return false;
                    }
                }
            }

            if (recipe.FixedMaterials != null)
            {
                for (int i = 0; i < recipe.FixedMaterials.Count; i++)
                {
                    var req = recipe.FixedMaterials[i];
                    if (req == null || string.IsNullOrEmpty(req.ItemId) || req.Count <= 0)
                    {
                        continue;
                    }

                    if (mixState.GetMaterialCount(req.ItemId) < req.Count)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static List<AlchemyRecipe> FindMatchingRecipes(AlchemyMixState mixState, bool onlyUnlocked = true)
        {
            var result = new List<AlchemyRecipe>();
            var recipes = AlchemyCatalog.GetAllRecipes();
            if (mixState == null || recipes == null)
            {
                return result;
            }

            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                if (recipe == null)
                {
                    continue;
                }

                if (onlyUnlocked && !AlchemyUnlockUtil.IsRecipeUnlocked(recipe))
                {
                    continue;
                }

                if (MatchesRecipe(mixState, recipe))
                {
                    result.Add(recipe);
                }
            }

            return result;
        }

        static bool HasRequiredTools(AlchemyFurnace furnace, IReadOnlyList<string> activeToolIds, out string failReasonEn)
        {
            failReasonEn = "";
            if (furnace.RequiredTools == null || furnace.RequiredTools.Count == 0)
            {
                return true;
            }

            if (activeToolIds == null || activeToolIds.Count == 0)
            {
                failReasonEn = "Missing required alchemy tools.";
                return false;
            }

            for (int i = 0; i < furnace.RequiredTools.Count; i++)
            {
                var required = furnace.RequiredTools[i];
                if (string.IsNullOrEmpty(required))
                {
                    continue;
                }

                bool found = false;
                for (int j = 0; j < activeToolIds.Count; j++)
                {
                    if (activeToolIds[j] == required)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    failReasonEn = "Missing required alchemy tools.";
                    return false;
                }
            }

            return true;
        }

        static bool ValidateMaterialSlots(IReadOnlyList<AlchemyInputSlot> materialSlots, int maxSlots, out string failReasonEn)
        {
            failReasonEn = "";
            if (materialSlots == null || materialSlots.Count == 0)
            {
                failReasonEn = "No alchemy materials provided.";
                return false;
            }

            if (materialSlots.Count > maxSlots)
            {
                failReasonEn = "Too many alchemy material slots.";
                return false;
            }

            for (int i = 0; i < materialSlots.Count; i++)
            {
                var slot = materialSlots[i];
                if (string.IsNullOrEmpty(slot.ItemId))
                {
                    failReasonEn = "Invalid alchemy material slot.";
                    return false;
                }

                if (AlchemyCatalog.GetMaterial(slot.ItemId) == null)
                {
                    failReasonEn = "Item is not an alchemy material.";
                    return false;
                }
            }

            return true;
        }

        static AlchemyMixState BuildMixState(
            AlchemyFurnace furnace,
            IReadOnlyList<string> activeToolIds,
            IReadOnlyList<AlchemyInputSlot> materialSlots)
        {
            var mix = new AlchemyMixState();
            var materialVirtues = new Dictionary<int, int>();

            for (int i = 0; i < materialSlots.Count; i++)
            {
                var slot = materialSlots[i];
                var def = AlchemyCatalog.GetMaterial(slot.ItemId);
                if (def == null)
                {
                    continue;
                }

                mix.AddMaterialCount(slot.ItemId, slot.Count);
                AccumulateVirtues(def.Virtues, slot.Count, mix, materialVirtues);
                AccumulateAspects(def.Aspects, slot.Count, mix);
            }

            if (furnace.AmplifyVirtueId > 0 && furnace.AmplifyPercent > 0
                && materialVirtues.TryGetValue(furnace.AmplifyVirtueId, out var materialAmount))
            {
                mix.AmplifyVirtueFromMaterials(furnace.AmplifyVirtueId, materialAmount, furnace.AmplifyPercent);
            }

            AccumulateVirtues(furnace.BaseVirtues, 1, mix, null);
            AccumulateAspects(furnace.BaseAspects, 1, mix);

            if (activeToolIds != null)
            {
                for (int i = 0; i < activeToolIds.Count; i++)
                {
                    var tool = AlchemyCatalog.GetTool(activeToolIds[i]);
                    if (tool == null)
                    {
                        continue;
                    }

                    AccumulateVirtues(tool.BonusVirtues, 1, mix, null);
                    AccumulateAspects(tool.BonusAspects, 1, mix);
                }
            }

            return mix;
        }

        static void AccumulateVirtues(
            IReadOnlyList<AlchemyVirtueValue> values,
            int multiplier,
            AlchemyMixState mix,
            Dictionary<int, int> materialOnly)
        {
            if (values == null || multiplier <= 0)
            {
                return;
            }

            for (int i = 0; i < values.Count; i++)
            {
                var entry = values[i];
                if (entry == null || entry.VirtueId <= 0 || entry.Value == 0)
                {
                    continue;
                }

                if (!AlchemyCatalog.IsValidVirtueId(entry.VirtueId))
                {
                    continue;
                }

                int amount = entry.Value * multiplier;
                mix.AddVirtue(entry.VirtueId, amount);
                if (materialOnly != null)
                {
                    materialOnly.TryGetValue(entry.VirtueId, out var current);
                    materialOnly[entry.VirtueId] = current + amount;
                }
            }
        }

        static void AccumulateAspects(IReadOnlyList<AlchemyAspectValue> values, int multiplier, AlchemyMixState mix)
        {
            if (values == null || multiplier <= 0)
            {
                return;
            }

            for (int i = 0; i < values.Count; i++)
            {
                var entry = values[i];
                if (entry == null || entry.AspectId <= 0 || entry.Value == 0)
                {
                    continue;
                }

                if (!AlchemyCatalog.IsValidAspectId(entry.AspectId))
                {
                    continue;
                }

                mix.AddAspect(entry.AspectId, entry.Value * multiplier);
            }
        }
    }
}
