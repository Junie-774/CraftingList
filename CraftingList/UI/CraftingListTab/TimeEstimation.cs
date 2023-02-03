using CraftingList.Crafting;
using CraftingList.Crafting.Macro;
using CraftingList.Utility;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.UI.CraftingListTab
{
    internal class TimeEstimation
    {

        public static int EstimateMacroDurationMS(CraftingMacro macro)
        {
            if (macro == null) return 0;
            return (macro is PluginMacro) ? EstimateMacroDurationMS((PluginMacro) macro) : EstimateMacroDurationMS((IngameMacro) macro);
        }

        public static int EstimateMacroDurationMS(PluginMacro macro)
        {
            int total = 0;
            foreach (var command in MacroManager.Parse(macro.Text))
            {
                total += command.WaitMS;
            }

            return total;
        }

        public static int EstimateMacroDurationMS(IngameMacro macro)
        {
            int total = 0;
            foreach (var command in MacroManager.Parse(IngameMacro.GetMacroText(macro.Macro1Num)))
                total += command.WaitMS;
            
            foreach (var command in MacroManager.Parse(IngameMacro.GetMacroText(macro.Macro2Num)))
                total += command.WaitMS;

            return total;
        }

        public static int EstimateEntryDurationMS(CListEntry entry)
        {
            
            int setupTime = 6000;
            int timePerCraft = 0;
            int numRestarts = 0;
            int numCrafts = 0;
            if (entry.NumCrafts.ToLower() == "max")
            {
                
                var recipe = Service.DataManager.GetExcelSheet<Recipe>()!.Where(r => r.ItemResult!.Value!.RowId == entry.ItemId).FirstOrDefault()!;
                var ingredients = MaterialsSummary.GetIngredientListFromRecipe(recipe);
                List<int> nItemsCraftablePerIngredient = new();
                for(int i = 0; i < ingredients.Count; i++)
                {
                    nItemsCraftablePerIngredient.Add(SeInterface.GetItemCountInInevntory(ingredients[i].ItemId) / (ingredients[i].Amount));
                }

                numCrafts = nItemsCraftablePerIngredient.Min();

            }
            else
            {
                if (!int.TryParse(entry.NumCrafts, out _))
                    return 0;
                numCrafts = int.Parse(entry.NumCrafts);
            }

            if (entry.MacroName == "<Quick Synth>")
            {
                timePerCraft = 3000;
                numRestarts = numCrafts / 99;
            }
            else
            {
                numRestarts = numCrafts;
                timePerCraft = EstimateMacroDurationMS(MacroManager.GetMacro(entry.MacroName)!);
            }

            return (numRestarts * 3000) + (timePerCraft * numCrafts) + setupTime;

        }
    }
}
