using CraftingList.Crafting;
using CraftingList.Utility;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.UI.CraftingListTab
{
    internal class HQIngredientSelectWindow
    {
        CListEntry? currentEntry = null;
        Recipe? currRecipe = null;

        public void SetEntry(CListEntry entry)
        {
            currentEntry = entry;
            currRecipe = Service.DataManager.GetExcelSheet<Recipe>()!.Where(r => r.ItemResult!.Value!.RowId == entry.ItemId).FirstOrDefault()!;
        }

        public void DrawHQMatSelection()
        {
            if (currentEntry == null)
                return;

            if (ImGui.BeginPopupContextItem($"##popup-{currentEntry.Name}-{Service.Configuration.EntryList.IndexOf(currentEntry)}"))
            {
                if (currRecipe == null)
                {
                    ImGui.EndPopup();
                    return;
                }

                var recipeIngredients = MaterialsSummary.GetIngredientListFromRecipe(currRecipe);
                var maxString = GetLongest(recipeIngredients.Select(i => i.Name));
                bool hasHqItem = false;
                for (int i = 0; i < currRecipe.UnkData5.Length; i++)
                {
                    if (currRecipe.UnkData5[i].ItemIngredient <= 0)
                        continue;

                    var item = Service.GetRowFromId((uint)currRecipe.UnkData5[i].ItemIngredient)!;

                    if (!item.CanBeHq)
                        continue;
                    else
                        hasHqItem = true;

                    HQMat(item.Name,
                        currRecipe.UnkData5[i].AmountIngredient,
                        ref currentEntry.HQSelection[i],
                        ImGui.CalcTextSize(maxString + "0000").X);

                }

                if (!hasHqItem)
                {
                    ImGui.Text("No HQ-able ingredients");
                }
                ImGui.EndPopup();
            }
        }

        public static void HQMat(string name, int amount, ref int outInt, float size)
        {
            ImGui.Text(name);
            ImGui.SameLine();
            ImGui.SetCursorPosX(size);
            ImGui.SetNextItemWidth(25);
            if (ImGui.InputInt($"/{amount}##ingredient_{name}", ref outInt, 0))
            {
                Service.Configuration.Save();
            }
            ImGui.SameLine();

            if (ImGui.Button($"-##hq-{name}", new Vector2(22, 22)))
            {
                outInt--;
                Service.Configuration.Save();

            }
            ImGui.SameLine();

            if (ImGui.Button($"+##hq-{name}", new Vector2(22, 22)))
            {
                outInt++;
                Service.Configuration.Save();

            }

            if (outInt > amount)
            {
                outInt = amount;
            }
            if (outInt < 0)
            {
                outInt = 0;
            }
        }

        public void Enable()
        {
            if (currentEntry == null)
                return;

            ImGui.OpenPopup($"##popup-{currentEntry.Name}-{Service.Configuration.EntryList.IndexOf(currentEntry)}");

        }

        public bool EntryMatches(CListEntry entry)
        {
            return entry == currentEntry;
        }
        public static string GetLongest(IEnumerable<string> strings)
        {
            string max = "";
            foreach (var str in strings)
            {
                if (str.Length > max.Length)
                    max = str;
            }

            return max;
        }
    }
}
