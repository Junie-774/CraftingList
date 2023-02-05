using CraftingList.Crafting;
using CraftingList.Utility;
using Dalamud.Interface.Colors;
using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace CraftingList.UI.CraftingListTab
{

    public class IngredientSummaryListing
    {
        public uint ItemId { get; set; }
        public string Name { get; set; } = "";
        public int Amount { get; set; }
        public bool HasMax { get; set; } // Used for aggregate listings for more than one entry. Allows keeping track of the amount needed by other entries
        public bool CanBeHQ { get; set; }
        public bool NeedsHQ { get; set; }
    }

    public class IngredientSummary
    {
        public IEnumerable<IngredientSummaryListing> Ingredients { get; set; } = new List<IngredientSummaryListing>();

        public void UpdateIngredients()
        {
            Ingredients = GetIngredientListFromEntryList(Service.Configuration.EntryList).OrderBy(i => i.ItemId);
        }

        public void DisplayListings()
        {
            foreach (var ingredient in Ingredients)
            {
                DisplayListing(ingredient);
            }
        }
        
        public static void DisplayListing(IngredientSummaryListing ingredient)
        {
            int inInventory = SeInterface.GetItemCountInInevntory(ingredient.ItemId, ingredient.NeedsHQ);// + SeInterface.GetItemCountInInevntory(ingredient.ItemId, true);
            if (inInventory >= ingredient.Amount)
            {
                ImGuiAddons.IconTextColored(ImGuiColors.HealerGreen, FontAwesomeIcon.Check);
                ImGui.SameLine();
            }
            else
            {
                ImGui.Dummy(new Vector2(17, 0));
                ImGui.SameLine();
            }
            ImGui.Text($"{ingredient.Name}: {ingredient.Amount}{(ingredient.HasMax ? " + max" : "")}");
            ImGui.SameLine();
            Vector4 color = ImGuiColors.DalamudGrey;

            if (inInventory < ingredient.Amount)
                color = ImGuiColors.DalamudRed;

            ImGui.TextColored(color, $" ({inInventory} in inventory) ");

            
        }

        public static List<IngredientSummaryListing> GetIngredientListFromRecipe(Recipe recipe)
        {

            var ingredients = recipe.UnkData5;

            List<IngredientSummaryListing> ingredientAmounts = new();


            for (int i = 0; i < ingredients.Length; i++)
            {
                var ingredient = ingredients[i];

                if (ingredient.ItemIngredient <= 0)
                    continue;

                var ingredientItem = Service.Items[ingredient.ItemIngredient];

                ingredientAmounts.Add(new IngredientSummaryListing
                {
                    ItemId = (uint)ingredient.ItemIngredient,
                    Name = ingredientItem.Name,
                    Amount = ingredient.AmountIngredient,
                    CanBeHQ = ingredientItem.CanBeHq
                });
            }

            return ingredientAmounts;
        }

        public static List<IngredientSummaryListing> GetIngredientListFromEntryList(IEnumerable<CListEntry> list)
        {
            Dictionary<string, IngredientSummaryListing> result = new();
            /*
            foreach (var entry in list)
            {
                var recipe = Service.Recipes[entry.RecipeId];
                foreach (var ingredient in GetIngredientListFromRecipe(recipe))
                {
                    var copy = ingredient;
                    if (entry.NumCrafts.ToLower() == "max")
                    {
                        copy.Amount = 0;
                        copy.HasMax = true;
                    }
                    else
                    {
                        if (!int.TryParse(entry.NumCrafts, out var numCrafts))
                        {
                            copy.Amount = 0;
                        }
                        copy.Amount *= numCrafts;
                    }

                    if (result.TryGetValue(copy.Name, out IngredientSummaryListing? dictResult))
                    {
                        dictResult!.Amount += copy.Amount;
                        dictResult!.HasMax |= copy.HasMax;
                    }
                    else
                    {
                        result.Add(copy.Name, copy);
                    }
                }

            }
            */

            foreach (var entry in list)
            {
                if (entry.NumCrafts.ToLower() == "max")
                {
                    continue;
                }
                var recipe = Service.Recipes[entry.RecipeId];
                var entryIngredientList = GetIngredientListFromRecipe(recipe);
                for(int i = 0; i < entryIngredientList.Count; i++)
                {
                    var copy = entryIngredientList[i];

                    if (entryIngredientList[i].CanBeHQ)
                    {
                        IngredientSummaryListing hqIngredientCopy = new()
                        {
                            ItemId = copy.ItemId,
                            Name = copy.Name,
                            Amount = copy.Amount,
                            HasMax = copy.HasMax,
                            CanBeHQ = copy.CanBeHQ,
                            NeedsHQ = copy.NeedsHQ,
                        };
                        hqIngredientCopy.Name = "" + hqIngredientCopy.Name;
                        hqIngredientCopy.NeedsHQ = true;
                        hqIngredientCopy.Amount = entry.HQSelection[i] * CListEntry.GetCraftNum(entry.NumCrafts);

                        if (hqIngredientCopy.Amount > 0)
                            AddToDict(result, hqIngredientCopy);

                        copy.Amount -= entry.HQSelection[i];
                        
                    }
                    copy.Amount *= CListEntry.GetCraftNum(entry.NumCrafts);

                    if (copy.Amount > 0)
                        AddToDict(result, copy);

                }

            }
            return result.Values.ToList();
        }

        private static void AddToDict(Dictionary<string, IngredientSummaryListing> dict, IngredientSummaryListing listing)
        {
            if (dict.TryGetValue(listing.Name, out IngredientSummaryListing? result))
            {
                result!.Amount = listing.Amount;
                result.HasMax |= listing.HasMax;
            }
            else
            {
                dict.Add(listing.Name, listing);
            }
        }
    }
}
