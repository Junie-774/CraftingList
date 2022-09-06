using CraftingList.Crafting;
using CraftingList.SeFunctions;
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

namespace CraftingList.UI
{
    internal class CraftingListTab : ITab, IDisposable
    {
        readonly private IEnumerable<Recipe?> craftableItems;
        readonly private List<string> craftableNames;
        readonly private IEnumerable<Item?> craftingFoods;
        private Recipe? hqMatItem;
        private List<Item> currItemIngredients = new();
        readonly private List<string> foodNames;
        private List<string> newEntryItemNameSearchResults;

        // Two separate lists because we want to present an empty option for a new list entry, but not present an empty option for an existing entry.
        readonly List<string> macroNames;
        readonly List<string> newMacroNames;

        readonly private CListEntry newEntry = new("", 0, "", new CraftingMacro("", 0, 0, 0, 0), 0, false, 0, 0);

        int newEntryItemNameSelection = 0;
        bool newEntryShowItemNameList = false;

        public string Name => "CraftingList";

        private readonly CraftingList plugin;

        int hqMatSelectionCurrEntry = -1;

        public CraftingListTab(CraftingList plugin)
        {
            this.plugin = plugin;
            craftableNames = new List<string>
            {
                ""
            };
            newMacroNames = new List<string>
            {
                ""
            };
            macroNames = new List<string>();
            foodNames = new List<string>();

            var MealIndexes = DalamudApi.DataManager.GetExcelSheet<ItemFood>()!
                .Select(m => m.RowId);

            craftingFoods = DalamudApi.DataManager.GetExcelSheet<Item>()!
                .Where(item => item.ItemAction.Value!.DataHQ[1] != 0 && MealIndexes.Contains(item.ItemAction.Value.DataHQ[1]))
                .Where(meal => meal.ItemAction.Value!.Type == 844 || meal.ItemAction.Value!.Type == 845)
                .Where(meal =>
                {
                    int param = DalamudApi.DataManager.GetExcelSheet<ItemFood>()!
                        .GetRow(meal.ItemAction.Value!.DataHQ[1])!.UnkData1[0].BaseParam;
                    return param == 11 || param == 70 || param == 71;
                });

            craftableItems = DalamudApi.DataManager.GetExcelSheet<Recipe>()!
                .Select(r => r).Where(r => r != null && r.ItemResult.Value != null && r.ItemResult.Value.Name != "");

            foreach (var item in craftableItems)
            {
                craftableNames.Add(item!.ItemResult.Value!.Name);
            }
            foreach (var macro in plugin.Configuration.Macros)
            {
                macroNames.Add(macro.Name);
                newMacroNames.Add(macro.Name);
            }
            foreach (var item in craftingFoods)
            {
                foodNames.Add(item!.Name);
            }
            foodNames.Add("None");
            foodNames.Reverse();
            for (int i = 1; i < foodNames.Count; i++)
            {
                string hqFood = "(HQ) " + foodNames[i];
                foodNames.Insert(i, hqFood);
                i++;
            }
            newEntryItemNameSearchResults = craftableNames.Where(x => x.Contains(newEntry.Name)).ToList();

        }
        public void Draw()
        {
            DrawEntryTable();
            DrawNewListEntry();
            DrawHQMatSelection();
            ImGui.NewLine();
            if (plugin.Crafter.waitingForHQSelection)
            {
                ImGui.Text("Waiting for you to select the HQ mats for your craft, please press the button below when finished.");
                if (ImGui.Button("I've Selected My HQ Mats"))
                {
                    plugin.Crafter.SignalHQMatsSelected();
                }
            }
            else
            {
                if (ImGui.Button("Craft!"))
                {
                    plugin.Crafter.CraftAllItems();
                }
                ImGui.SameLine();
            }
            if (ImGui.Button("Cancel"))
            {
                plugin.Crafter.Cancel("Cancelling craft...", false);
            }
        }

        private void DrawEntryTable()
        {

            float tableSize = (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X);

            ImGui.Columns(6);
            float dynamicAvailWidth = (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) - (18 + 18 + 12) - (ImGui.CalcTextSize("Amount").X + ImGui.CalcTextSize("HQ Mats?").X + ImGui.CalcTextSize("Remove").X);
            ImGui.SetColumnWidth(0, dynamicAvailWidth * 0.4f);
            ImGui.SetColumnWidth(1, 18 + ImGui.CalcTextSize("Amount").X);
            ImGui.SetColumnWidth(2, dynamicAvailWidth * 0.3f);
            ImGui.SetColumnWidth(3, dynamicAvailWidth * 0.3f);
            ImGui.SetColumnWidth(4, 18 + ImGui.CalcTextSize("HQ Mats").X);

            ImGui.Separator();
            ImGui.SetWindowFontScale(1.1f);
            ImGui.SetColumnWidth(5, 12 + ImGui.CalcTextSize("Remove").X);

            ImGui.Text("Item Name");
            ImGui.NextColumn();

            ImGui.Text("Amount");
            ImGui.NextColumn();

            ImGui.Text("Macro");
            ImGui.NextColumn();

            ImGui.Text("Food");
            ImGui.NextColumn();

            ImGui.Text("HQ Mats");

            ImGui.SetWindowFontScale(1f);


            ImGui.Separator();
            ImGui.NextColumn();

            ImGui.NextColumn();

            for (int i = 0; i < plugin.Configuration.EntryList.Count; i++)
            {
                ImGui.Text(plugin.Configuration.EntryList[i].Name);
                ImGui.NextColumn();

                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##NumCrafts" + i, ref plugin.Configuration.EntryList[i].NumCrafts, 50);
                ImGui.NextColumn();

                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo("##Macro" + i, ref plugin.Configuration.EntryList[i].MacroIndex, macroNames.ToArray(), macroNames.Count))
                {
                    var macro = plugin.Configuration.Macros.Where(x => x.Name == macroNames[plugin.Configuration.EntryList[i].MacroIndex]);
                    if (!macro.Any())
                    {
                        PluginLog.Debug("Internal error: Macro name does not match any in macro list. This shouldn't happen.");
                    }
                    else
                    {
                        plugin.Configuration.EntryList[i].Macro = macro.First();
                    }
                    //PluginLog.Debug($"Macro {plugin.Configuration.EntryList[i].MacroIndex}: {plugin.Configuration.EntryList[i].Macro.Name}");
                }
                ImGui.NextColumn();
                /*
                bool HQ = plugin.Configuration.EntryList[i].FoodId > 1000000;
                ImGui.Text((HQ ? "(HQ) " : "")
                    + DalamudApi.DataManager.GetExcelSheet<Item>()!
                        .Where(x => x.RowId == (HQ ? plugin.Configuration.EntryList[i].FoodId - 1000000 : plugin.Configuration.EntryList[i].FoodId)).First().Name
                );*/
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo("##Food" + i, ref plugin.Configuration.EntryList[i].FoodIndex, foodNames.ToArray(), foodNames.Count))
                {
                    var foodName = foodNames[plugin.Configuration.EntryList[i].FoodIndex];
                    bool isFoodHQ = false;

                    if (foodName.Substring(0, 4) == "(HQ)")
                    {
                        isFoodHQ = true;
                        foodName = foodName.Substring(5);

                        uint foodID = plugin.Configuration.EntryList[i].FoodIndex == 0 ? 0 : DalamudApi.DataManager.GetExcelSheet<Item>()!
                            .Where(x => x!.Name == foodName).First().RowId;

                        if (isFoodHQ) foodID += 1000000;

                        plugin.Configuration.EntryList[i].FoodId = foodID;
                    }
                }
                ImGui.NextColumn();

                if (ImGui.Button("Select...##" + i))
                {
                    hqMatItem = craftableItems.Where(item => item!.ItemResult.Value!.RowId == plugin.Configuration.EntryList[i].ItemId).First();
                    setHQItemIngredients(hqMatItem!);
                    hqMatSelectionCurrEntry = i;
                    ImGui.OpenPopup("HQ Mat Selection");
                }
                ImGui.NextColumn();

                if (ImGui.Button("Remove##" + i))
                {
                    plugin.Configuration.EntryList[i].Complete = true;
                }
                ImGui.NextColumn();


                ImGui.Separator();
            }


            plugin.Configuration.EntryList.RemoveAll(x => x.Complete || x.NumCrafts == "0");
            plugin.Configuration.Save();

        }

        private void DrawNewListEntry()
        {
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##Item", ref newEntry.Name, 65535, ImGuiInputTextFlags.AllowTabInput))
            {
                newEntryItemNameSearchResults = craftableNames.Where(x => newEntry.Name == "" || x.ToLower().Contains(newEntry.Name.ToLower())).ToList();
                newEntryShowItemNameList = true;
            }
            ImGui.NextColumn();


            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##Amount", ref newEntry.NumCrafts, 65535);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Number of crafts. Enter \"max\" to craft until you run out of materials or inventory space.");
            }
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            if (ImGui.Combo("##Macro", ref newEntry.MacroIndex, newMacroNames.ToArray(), newMacroNames.Count))
            {
                var macro = plugin.Configuration.Macros.Where(x => x.Name == newMacroNames[newEntry.MacroIndex]);
                if (!macro.Any())
                {
                    PluginLog.Debug("Internal error: Macro name does not match any in macro list. This shouldn't happen.");
                }
                else
                {
                    newEntry.Macro = macro.First();
                }
            }
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            if (ImGui.Combo("##Food", ref newEntry.FoodIndex, foodNames.ToArray(), foodNames.Count))
            {
                var foodName = foodNames[newEntry.FoodIndex];
                bool isFoodHQ = false;

                if (foodName.Substring(0, 4) == "(HQ)")
                {
                    isFoodHQ = true;
                    foodName = foodName.Substring(5);

                    uint foodID = newEntry.FoodIndex == 0 ? 0 : DalamudApi.DataManager.GetExcelSheet<Item>()!
                        .Where(x => x!.Name == foodName).First().RowId;

                    if (isFoodHQ) foodID += 1000000;

                    newEntry.FoodId = foodID;
                }
            }
            ImGui.NextColumn();

            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 18);

            if (ImGui.Button("+", new Vector2(25f, 25f)))
            {
                
                var items = craftableItems.Where(item => item!.ItemResult.Value!.Name == newEntry.Name);

                if (items.Any() &&
                    (newEntry.NumCrafts.ToLower() == "max" || (int.TryParse(newEntry.NumCrafts, out _) && int.Parse(newEntry.NumCrafts) > 0))
                    && newEntry.MacroIndex != 0)
                {

                    newEntry.ItemId = items.First()!.ItemResult.Value!.RowId;
                    newEntry.NumCrafts = newEntry.NumCrafts.ToLower();
                    newEntry.MacroIndex -= 1; //Transition from referring to newMacroName to macroName
                    var entry = new CListEntry(newEntry.Name, newEntry.ItemId, newEntry.NumCrafts, newEntry.Macro, newEntry.FoodId, newEntry.HQMats, newEntry.MacroIndex, newEntry.FoodIndex);
                    plugin.Configuration.EntryList.Add(entry);
                    newEntry.Name = "";
                    newEntry.NumCrafts = "";
                    newEntry.MacroIndex = 0;
                    newEntry.FoodIndex = 0;
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

        public void DrawHQMatSelection()
        {
            if (hqMatSelectionCurrEntry == -1) return;
            if (hqMatItem == null) return;

            CListEntry entry = plugin.Configuration.EntryList[hqMatSelectionCurrEntry];


            bool hasHQIngredients = false;
            string maxString = "";
            foreach (var ingredient in currItemIngredients)
            {
                if (ingredient.RowId != 0)
                {
                    if (ingredient.Name.ToString().Length > maxString.Length)
                    {
                        maxString = ingredient.Name;
                    }
                }
            }

            if (ImGui.BeginPopup("HQ Mat Selection"))
            {

                for (int i = 0; i < hqMatItem!.UnkData5.Length; i++)
                {
                    
                    if (currItemIngredients[i].RowId != 0)
                    {
                        if (currItemIngredients[i].CanBeHq)
                        {
                            
                            ImGui.Text(currItemIngredients[i].Name + " (" + hqMatItem!.UnkData5[i].AmountIngredient + ")");
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(ImGui.CalcTextSize(maxString + "(XXX)").X);
                            ImGui.SetNextItemWidth(100);
                            ImGui.InputInt("##ingredient_" + currItemIngredients[i].Name, ref entry.HQSelection[i], 1);
                            if (entry.HQSelection[i] > hqMatItem!.UnkData5[i].AmountIngredient)
                            {
                                entry.HQSelection[i] = hqMatItem!.UnkData5[i].AmountIngredient;
                            }
                            if (entry.HQSelection[i] < 0)
                            {
                                entry.HQSelection[i] = 0;
                            }
                            
                            hasHQIngredients = true;
                        }
                    }

                }
                if (!hasHQIngredients)
                {
                    ImGui.Text("No HQ ingredients.");
                }
                ImGui.EndPopup();
            }
            else
            {
                hqMatSelectionCurrEntry = -1;
            }
        }

        public void OnConfigChange()
        {
            macroNames.Clear();
            newMacroNames.Clear();
            newMacroNames.Add("");
            foreach (var mac in plugin.Configuration.Macros)
            {
                macroNames.Add(mac.Name);
                newMacroNames.Add(mac.Name);
            }
            foreach (var entry in plugin.Configuration.EntryList)
            {
                entry.MacroIndex = -1;
            }
            
        }
        private void setHQItemIngredients(Recipe recipe)
        {
            currItemIngredients.Clear();
            var itemSheet = DalamudApi.DataManager.GetExcelSheet<Item>();
            for (int i = 0; i < recipe.UnkData5.Length; i++)
            {
                
                int ingredientID = hqMatItem!.UnkData5[i].ItemIngredient;
                var matches = itemSheet!.Where(it => it.RowId == ingredientID);

                if (matches.Any())
                {
                    currItemIngredients.Add(matches.First());
                }
            }
        }
        public void Dispose()
        {

        }
    }
}
