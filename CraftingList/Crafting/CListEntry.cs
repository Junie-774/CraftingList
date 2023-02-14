using CraftingList.Utility;
using Dalamud.Logging;
using Newtonsoft.Json;
using System.Linq;

namespace CraftingList.Crafting
{
    public unsafe class CListEntry
    {
        public bool Complete { get; set; } = false;

        public string Name;
        public int EntryId;

        public int RecipeId;
        public string NumCrafts;
        
        public bool running = false;
        public int[] HQSelection = new int[6];
        public bool SpecifiedHQ = false;

        //Dummy variables to store stuff for the UI

        public string MacroName;

        [JsonConstructor]

        public CListEntry(int recipeId, string numCrafts, string macroName, bool specifiedHq, int[] hqSelection)
        {

            this.Name = (recipeId >= 0 && Service.Recipes != null) ? Service.Recipes[recipeId].ItemResult.Value!.Name : "???";
            this.RecipeId = recipeId;
            this.NumCrafts = numCrafts == "max" || int.TryParse(numCrafts, out _) ? numCrafts : "0";
            
            this.MacroName = macroName;
            this.SpecifiedHQ = specifiedHq;
            this.HQSelection = hqSelection;
        }
        public void Decrement()
        {
            if (!int.TryParse(NumCrafts, out int numCrafts))
                return;
            NumCrafts = (numCrafts - 1).ToString();
            if (NumCrafts == "0") Complete = true;
            Service.Configuration.Save();
        }

        public void Decrement(int numCompleted)
        {
            if (!int.TryParse(NumCrafts, out int numCrafts))
                return;

            NumCrafts = (numCrafts - numCompleted).ToString();
            if (NumCrafts == "0") Complete = true;
            Service.Configuration.Save();
        }

        public static int[] EmptyHQSelection()
        {
            return new int[] { 0, 0, 0, 0, 0, 0 };
        }

        public static bool IsValidNumCrafts(string numCrafts)
        {
            return numCrafts.ToLower() == "max" || (int.TryParse(numCrafts, out int result) && result > 0);
        }

        public static int GetCraftNum(string numCrafts)
        {
            if (int.TryParse(numCrafts, out int result))
                return result;
            return -1;
        }

    }
}
