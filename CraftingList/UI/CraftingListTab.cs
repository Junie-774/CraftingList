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
        private Recipe? hqMatItem;
        private List<Item> currItemIngredients = new();
        private List<string> newEntryItemNameSearchResults;

        // Two separate lists because we want to present an empty option for a new list entry, but not present an empty option for an existing entry.
        readonly List<string> macroNames;
        readonly List<string> newMacroNames;

        readonly private CListEntry newEntry = new("", 0, "", 0);

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

            craftableItems = DalamudApi.DataManager.GetExcelSheet<Recipe>()!
                .Select(r => r).Where(r => r != null && r.ItemResult.Value != null && r.ItemResult.Value.Name != "");

            foreach (var item in craftableItems)
            {
                craftableNames.Add(item!.ItemResult.Value!.Name);
            }
            PopulateMacroNames();

            newEntryItemNameSearchResults = craftableNames.Where(x => x.Contains(newEntry.Name)).ToList();

        }
        public void Draw()
        {
            DrawEntryTable();
            DrawNewListEntry();
            DrawHQMatSelection();
            ImGui.NewLine();

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
        }

        private void DrawEntryTable()
        {

            float tableSize = (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X);

            ImGui.Columns(5);
            float dynamicAvailWidth = (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) - (18 + 18 + 12) - (ImGui.CalcTextSize("Amount").X + ImGui.CalcTextSize("HQ Mats?").X + ImGui.CalcTextSize("Remove").X);
            ImGui.SetColumnWidth(0, dynamicAvailWidth * 0.6f);
            ImGui.SetColumnWidth(1, 18 + ImGui.CalcTextSize("Amount").X);
            ImGui.SetColumnWidth(2, dynamicAvailWidth * 0.4f);
            ImGui.SetColumnWidth(3, 18 + ImGui.CalcTextSize("HQ Mats").X);

            ImGui.Separator();
            ImGui.SetWindowFontScale(1.1f);
            ImGui.SetColumnWidth(4, 12 + ImGui.CalcTextSize("Remove").X);

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

            for (int i = 0; i < DalamudApi.Configuration.EntryList.Count; i++)
            {
                ImGui.Text(plugin.Configuration.EntryList[i].Name);
                ImGui.NextColumn();

                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##NumCrafts" + i, ref plugin.Configuration.EntryList[i].NumCrafts, 50);
                ImGui.NextColumn();

                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo("##Macro" + i, ref plugin.Configuration.EntryList[i].MacroIndex, macroNames.ToArray(), macroNames.Count))
                {
                    var macro = DalamudApi.Configuration.PluginMacros.Where(x => x.Name == macroNames[plugin.Configuration.EntryList[i].MacroIndex]);
                    if (!macro.Any())
                    {
                        PluginLog.Debug("Internal error: Macro name does not match any in macro list. This shouldn't happen.");
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
                var macro = plugin.Configuration.PluginMacros.Where(x => x.Name == newMacroNames[newEntry.MacroIndex]);
                if (!macro.Any())
                {
                    PluginLog.Debug("Internal error: Macro name does not match any in macro list. This shouldn't happen.");
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
                    && newEntry.MacroIndex > 0)
                {

                    newEntry.ItemId = items.First()!.ItemResult.Value!.RowId;
                    newEntry.NumCrafts = newEntry.NumCrafts.ToLower();
                    newEntry.MacroIndex -= 1; //Transition from referring to newMacroName to macroName
                    var entry = new CListEntry(newEntry.Name, newEntry.ItemId, newEntry.NumCrafts, newEntry.MacroIndex);
                    plugin.Configuration.EntryList.Add(entry);
                    newEntry.Name = "";
                    newEntry.NumCrafts = "";
                    newEntry.MacroIndex = 0;
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

                            ImGui.Text(currItemIngredients[i].Name); // + " (" + hqMatItem!.UnkData5[i].AmountIngredient + ")");
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(ImGui.CalcTextSize(maxString + "(XXX)").X);
                            ImGui.SetNextItemWidth(25);
                            ImGui.InputInt($"/{hqMatItem!.UnkData5[i].AmountIngredient}##ingredient_{currItemIngredients[i].Name}", ref entry.HQSelection[i], 0);
                            ImGui.SameLine();
                            if (ImGui.Button("+", new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight())))
                            {
                                entry.HQSelection[i]++;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("-", new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight())))
                            {
                                entry.HQSelection[i]--;
                            }
                            ImGui.PopItemWidth();
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

            PopulateMacroNames();
            
            foreach (var entry in plugin.Configuration.EntryList)
            {
                entry.MacroIndex = -1;
            }
            
        }

        void PopulateMacroNames()
        {
            if (DalamudApi.Configuration.UsePluginMacros)
            {
                foreach (var mac in DalamudApi.Configuration.PluginMacros)
                {
                    macroNames.Add(mac.Name);
                    newMacroNames.Add(mac.Name);
                }
            }
            else
            {
                foreach (var mac in DalamudApi.Configuration.Macros)
                {
                    macroNames.Add(mac.Name);
                    newMacroNames.Add(mac.Name);
                }
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
