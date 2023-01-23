using CraftingList.Crafting;
using CraftingList.Utility;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.UI.CraftingListTab
{

    public class IngredientSummaryListing
    {
        public uint ItemId { get; set; }
        public string Name { get; set; } = "";
        public int Amount { get; set; }

        public bool HasMax { get; set; } // Used for aggregate listings for more than one entry. Allows keeping track of the amount needed by other entries
                                         // while still being able to indicate that there's at least one entry that will go 'max'
        public bool CanBeHQ { get; set; }
    }

    internal class MaterialsSummary
    {


        public static List<IngredientSummaryListing> GetIngredientListFromRecipe(Recipe recipe)
        {

            var ingredients = recipe.UnkData5;

            List<IngredientSummaryListing> ingredientAmounts = new();

            for (int i = 0; i < ingredients.Length; i++)
            {
                var ingredient = ingredients[i];

                if (ingredient.ItemIngredient <= 0)
                    continue;

                var ingredientItem = Service.GetRowFromId((uint)ingredient.ItemIngredient)!;

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
            foreach (var entry in list)
            {
                var recipe = Service.GetRecipeFromResultId(entry.ItemId)!;
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
                        copy.Amount *= int.Parse(entry.NumCrafts);
                    }

                    if (result.TryGetValue(copy.Name, out IngredientSummaryListing? dictResult))
                    {
                        dictResult!.Amount += copy.Amount;
                    }
                    else
                    {
                        result.Add(copy.Name, copy);
                    }
                }

            }

            return result.Values.ToList();
        }
    }
}
