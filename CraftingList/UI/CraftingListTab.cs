using CraftingList.Crafting;
using CraftingList.Crafting.Macro;
using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.UI
{
    internal class CraftingListTab : ITab, IDisposable
    {
        readonly private IEnumerable<Recipe?> craftableItems;
        readonly private List<string> craftableNames;
        private List<string> newEntryItemNameSearchResults;
        private IEnumerable<IngredientSummaryListing> ingredientSummaries;

        // Two separate lists because we want to present an empty option for a new list entry, but not present an empty option for an existing entry.


        readonly private CListEntry newEntry = new("", 0, "", "");

        int newEntryItemNameSelection = 0;
        int newEntryMacroSelection = 0;
        bool newEntryShowItemNameList = false;

        public string Name => "CraftingList";

        private readonly CraftingList plugin;


        public CraftingListTab(CraftingList plugin)
        {
            this.plugin = plugin;
            craftableNames = new List<string>
            {
                ""
            };

            craftableItems = Service.DataManager.GetExcelSheet<Recipe>()!
                .Select(r => r).Where(r => r != null && r.ItemResult.Value != null && r.ItemResult.Value.Name != "");

            foreach (var item in craftableItems)
            {
                craftableNames.Add(item!.ItemResult.Value!.Name);
            }

            newEntryItemNameSearchResults = craftableNames.Where(x => x.Contains(newEntry.Name)).ToList();
            ingredientSummaries = RegenerateIngredientSummary();

        }
        public void Draw()
        {
            DrawEntryTable();
            DrawNewListEntry();
            //DrawHQMatSelection();
            ImGui.NewLine();
            ImGui.Columns(2);
            if (ImGui.Button("Craft!"))
            {
                plugin.Crafter.CraftAllItems();
            }
            ImGui.SameLine();

            if (ImGui.Button("Cancel"))
            {
                plugin.Crafter.Cancel("Cancelling craft...", false);
            }
            ImGui.NewLine();
            ImGui.Checkbox("##HasCraftTimeout", ref plugin.Configuration.HasCraftTimeout);
            ImGui.SameLine();
            ImGui.Text(" Stop after ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(25);
            ImGui.InputInt("##CraftTimeout", ref plugin.Configuration.CraftTimeoutMinutes, 0, 0);
            ImGui.SameLine();
            ImGui.Text(" Minutes");

            ImGui.NextColumn();
            foreach (var ingredient in ingredientSummaries)
            {
                ImGui.Text($"{ingredient.Name}: {ingredient.Amount}{(ingredient.HasMax ? " + max" : "")}");// ({SeInterface.GetItemCountInInevntory(ingredient.ItemId)}");
            }
            ImGui.Columns(1);
        }

        private void DrawEntryTable()
        {
            var macroNames = GetMacroNames();
            float tableSize = (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X);
            ImGui.PushFont(UiBuilder.IconFont);
            float fontSize = ImGui.CalcTextSize(FontAwesomeIcon.TrashAlt.ToIconString()).X;
            ImGui.PopFont();

            ImGui.Columns(5);
            float dynamicAvailWidth = (tableSize) - (18 + 18 + 12) - (ImGui.CalcTextSize("Amount").X + ImGui.CalcTextSize("HQ Mats?").X + fontSize);
            ImGui.SetColumnWidth(0, dynamicAvailWidth * 0.6f);
            ImGui.SetColumnWidth(1, 18 + ImGui.CalcTextSize("Amount").X);
            ImGui.SetColumnWidth(2, dynamicAvailWidth * 0.4f);
            ImGui.SetColumnWidth(3, 18 + ImGui.CalcTextSize("HQ Mats").X);
            ImGui.SetColumnWidth(4, 18 + fontSize);

            ImGui.Separator();
            ImGui.SetWindowFontScale(1.1f);

            ImGui.Text("Item Name");
            ImGui.NextColumn();

            ImGui.Text("Amount");
            ImGui.NextColumn();

            ImGui.Text("Macro");
            ImGui.NextColumn();


            ImGui.Text("HQ Mats");

            ImGui.SetWindowFontScale(1f);


            ImGui.Separator();
            ImGui.NextColumn();
            ImGui.NextColumn();

            int i = 0;
            foreach (var currEntry in plugin.Configuration.EntryList)
            {
                ImGui.Text(currEntry.Name);
                ImGui.NextColumn();

                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText("##NumCrafts" + i, ref currEntry.NumCrafts, 50))
                {
                    ingredientSummaries = RegenerateIngredientSummary();
                    Service.Configuration.Save();

                }
                ImGui.NextColumn();

                ImGui.SetNextItemWidth(-1);
                int macroIndex = macroNames.IndexOf(currEntry.MacroName);
                if (macroIndex == -1 && !currEntry.MacroName.IsNullOrEmpty())
                {
                    PluginLog.Debug($"Error: Entry {currEntry} had macro name which does not match any in list.");
                    Service.ChatManager.PrintMessage("Encountered an internal error. see `/xllog` for details");

                }

                if (ImGui.Combo("##Macro" + i, ref macroIndex, macroNames.ToArray(), macroNames.Count()))
                {
                    currEntry.MacroName = macroNames.ElementAt(macroIndex);
                    Service.Configuration.Save();

                }
                ImGui.NextColumn();
                if (ImGui.Button("Select...##" + i))
                {
                    ImGui.OpenPopup($"##popup-{currEntry.Name}-{i}");
                }
                DrawHQMatSelection(currEntry);

                ImGui.NextColumn();

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 15);
                if (ImGuiAddons.IconButton(FontAwesomeIcon.TrashAlt, "Remove Entry", currEntry.Name + i))
                {
                    plugin.Configuration.EntryList[i].Complete = true;
                    Service.Configuration.Save();

                }
                ImGui.NextColumn();


                ImGui.Separator();
                i++;
            }


            if (Service.Configuration.EntryList.RemoveAll(x => x.Complete || x.NumCrafts == "0") > 0)
            {
                ingredientSummaries = RegenerateIngredientSummary();
            }
        }

        private void DrawNewListEntry()
        {
            var newEntryMacroNames = GenerateNewEntrymacroNames().ToArray();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##Item", ref newEntry.Name, 100, ImGuiInputTextFlags.AllowTabInput))
            {
                newEntryItemNameSearchResults = craftableNames.Where(x => newEntry.Name == "" || x.ToLower().Contains(newEntry.Name.ToLower())).ToList();
                newEntryShowItemNameList = true;
            }
            ImGui.NextColumn();


            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##Amount", ref newEntry.NumCrafts, 100);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Number of crafts. Enter \"max\" to craft until you run out of materials or inventory space.");
            }
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            if (ImGui.Combo("##Macro", ref newEntryMacroSelection, newEntryMacroNames, newEntryMacroNames.Length))
            {
                var name = newEntryMacroNames[newEntryMacroSelection];
                if (!MacroManager.ExistsMacro(name))
                {
                    PluginLog.Debug($"Internal error: Macro name '{name}' does not match any in macro list. This shouldn't happen.");
                }
            }
            ImGui.NextColumn();

            // skip the "delete" button
            ImGui.NextColumn();
            
            ImGui.SetNextItemWidth(-1);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 15);

            if (ImGuiAddons.IconButton(FontAwesomeIcon.Plus, "Add New Entry"))
            {

                var items = craftableItems.Where(item => item!.ItemResult.Value!.Name == newEntry.Name);

                if (items.Any() &&
                    (newEntry.NumCrafts.ToLower() == "max" || (int.TryParse(newEntry.NumCrafts, out _) && int.Parse(newEntry.NumCrafts) > 0))
                    && newEntryMacroNames[newEntryMacroSelection] != "")
                {

                    newEntry.ItemId = items.First()!.ItemResult.Value!.RowId;
                    newEntry.NumCrafts = newEntry.NumCrafts.ToLower();
                    var entry = new CListEntry(newEntry.Name, newEntry.ItemId, newEntry.NumCrafts, newEntryMacroNames[newEntryMacroSelection]);

                    plugin.Configuration.EntryList.Add(entry);
                    newEntryMacroSelection = 0;
                    newEntry.Name = "";
                    newEntry.NumCrafts = "";
                    Service.Configuration.Save();

                }
            }
            ImGui.Separator();
            ImGui.NextColumn();
            ImGui.Columns(1);


            if (newEntryShowItemNameList)
            {
                if (ImGui.ListBox("",
                    ref newEntryItemNameSelection,
                    newEntryItemNameSearchResults.ToArray(),
                    newEntryItemNameSearchResults.Count,
                    20))
                {
                    newEntry.Name = newEntryItemNameSearchResults[newEntryItemNameSelection];
                    newEntryShowItemNameList = false;
                }
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
        public void DrawHQMatSelection(CListEntry entry)
        {
            if (ImGui.BeginPopupContextItem($"##popup-{entry.Name}-{Service.Configuration.EntryList.IndexOf(entry)}"))
            {
                var matches = craftableItems.Where(r => r!.ItemResult.Value!.RowId == entry.ItemId);
                if (!matches.Any()) {
                    ImGui.EndPopup();
                    return;
                }

                var recipe = craftableItems.Where(r => r!.ItemResult.Value!.RowId == entry.ItemId).First()!;

                var recipeIngredients = MaterialsSummary.GetIngredientListFromRecipe(recipe);
                var maxString = GetLongest(recipeIngredients.Select(i => i.Name));
                bool hasHqItem = false;
                for (int i = 0; i < recipe.UnkData5.Length; i++) 
                {
                    if (recipe.UnkData5[i].ItemIngredient <= 0)
                        continue;

                    var item = Service.GetRowFromId((uint)recipe.UnkData5[i].ItemIngredient)!;

                    if (!item.CanBeHq)
                        continue;
                    else
                        hasHqItem = true;
                    
                    HQMat(item.Name,
                        recipe.UnkData5[i].AmountIngredient,
                        ref entry.HQSelection[i],
                        ImGui.CalcTextSize(maxString + "0000").X);

                }

                if (!hasHqItem)
                {
                    ImGui.Text("No HQ-able ingredients");
                }
                ImGui.EndPopup();
            }
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

        public List<string> GetMacroNames()
            => MacroManager.MacroNames;


        public IEnumerable<IngredientSummaryListing> RegenerateIngredientSummary()
        {
            return MaterialsSummary.GetIngredientListFromEntryList(Service.Configuration.EntryList).OrderBy(i => i.ItemId);
        }

        public static void RemoveMacroName(string macroName)
        {

            foreach (var entry in Service.Configuration.EntryList)
            {
                if (entry.MacroName == macroName)
                    entry.MacroName = "";

            }
        }

        public static void UpdateMacroNameInEntries(string oldName, string newName)
        {
            foreach (var entry in Service.Configuration.EntryList)
            {
                if (entry.MacroName == oldName)
                    entry.MacroName = newName;

            }
        }

        public IEnumerable<string> GenerateNewEntrymacroNames()
            => new List<string>() { "" }.Concat(MacroManager.MacroNames);

        public void Dispose()
        {

        }
    }
}
