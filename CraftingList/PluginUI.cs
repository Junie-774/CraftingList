using CraftingList.Utility;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace CraftingList
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    unsafe class PluginUI : IDisposable
    {
        private Configuration configuration;

        private IEnumerable<Item?> craftableItems;
        private List<string> craftableNames;
        private IEnumerable<Item?> craftingFoods;
        private List<string> foodNames;

        private string newMacroName = "";
        private int newMacroNum = 0;
        private int newMacroDur = 0;

        private string currMacroName = "";
        private int currMacroNum = 0;
        private int currMacroDur = 0;


        private List<string> newEntryItemNameSearchResults;
        int newEntryItemNameSelection = 0;
        string newEntryItemName = "";
        bool newEntryShowItemNameList = false;


        int newEntryFoodNameSelection = 0;

        int newEntrySelectedMacro = 0;
        int newEntryCraftAmount = 0;

        int selectedMacro = 0;
        List<String> macroNames;

        int repairThreshold;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        // passing in the image here just for simplicity
        public PluginUI(Configuration configuration)
        {
            this.selectedMacro = 0;
            this.configuration = configuration;
            this.repairThreshold = configuration.RepairThresholdPercent;
            macroNames = new List<String>
            {
                ""
            };
            craftableNames = new List<string>
            {
                ""
            };
            foodNames = new List<string>();

            foreach (Crafting.CraftingMacro mac in configuration.Macros)
            {
                macroNames.Add(mac.Name);
            }

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
                .Select(r => r.ItemResult.Value).Where(r => r != null && r.Name != "");
            foreach (var item in craftableItems)
            {
                craftableNames.Add(item!.Name);
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
            newEntryItemNameSearchResults = craftableNames.Where(x => x.Contains(newEntryItemName)).ToList();

            configuration.Crafter.RepairThresholdPercent = configuration.RepairThresholdPercent;
            configuration.Crafter.OnlyRepairIfBelow99 = configuration.OnlyRepairIfBelow99;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            DrawMainWindow();
            DrawSettingsWindow();
        }

        public void DrawEntryTable()
        {
            float tableSize = ImGui.GetWindowContentRegionWidth() - 25;


            ImGui.Text("Crafting list:");
            ImGui.SetWindowFontScale(1.1f);
            ImGui.BeginTable("meow", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable);
            ImGui.TableNextColumn();

            ImGui.Text("Item Name");
            ImGui.TableNextColumn();

            ImGui.Text("Amount");
            ImGui.TableNextColumn();

            ImGui.Text("Macro");
            ImGui.TableNextColumn();

            ImGui.Text("Food");
            ImGui.TableNextColumn();
            ImGui.TableNextRow();
            ImGui.SetWindowFontScale(1f);
            try
            {
                foreach (var item in configuration.Crafter.EntryList)
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(tableSize);
                    ImGui.Text(item.Name);

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(tableSize * 0.3f);
                    ImGui.Text(item.MaxCrafts.ToString());

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(tableSize * 0.4f);
                    ImGui.Text(item.Macro.Name);

                    ImGui.TableNextColumn();

                    bool HQ = item.FoodId > 1000000;
                    ImGui.Text((HQ ? "(HQ) " : "")
                        + DalamudApi.DataManager.GetExcelSheet<Item>()!
                            .Where(x => x.RowId == (HQ ? item.FoodId - 1000000 : item.FoodId)).First().Name
                    );

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(50);
                    if (ImGui.Button("Remove"))
                    {
                        item.Complete = true;
                    }

                    ImGui.TableNextRow();
                }
                configuration.Crafter.EntryList.RemoveAll(x => x.Complete);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex.Message);
            }
            ImGui.EndTable();
        }

        private void DrawNewListEntry()
        {
            ImGui.Text("Add item to list:");
            float dynamicSize = ImGui.GetWindowContentRegionWidth() - 25 - ImGui.CalcTextSize("Item  ...  Amount  Macro  Food  + ").X;

            ImGui.SetNextItemWidth(dynamicSize * 0.3f);
            if (ImGui.InputText("Item ", ref newEntryItemName, 25))
            {
                newEntryItemNameSearchResults = craftableNames.Where(x => newEntryItemName == "" || x.ToLower().Contains(newEntryItemName.ToLower())).ToList();
                newEntryShowItemNameList = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("..."))
            {
                ImGui.SetKeyboardFocusHere(-1);
                newEntryShowItemNameList = !newEntryShowItemNameList;
            }
            ImGui.SameLine();

            ImGui.SetNextItemWidth(dynamicSize * 0.1f);
            ImGui.InputInt("Amount  ", ref newEntryCraftAmount, 0);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(dynamicSize * 0.2f);
            ImGui.Combo("Macro  ", ref newEntrySelectedMacro, macroNames.ToArray(), macroNames.Count);
            ImGui.SameLine();

            ImGui.SetNextItemWidth(dynamicSize * 0.3f);
            ImGui.Combo("Food  ", ref newEntryFoodNameSelection, foodNames.ToArray(), foodNames.Count);
            ImGui.SameLine();


            if (ImGui.Button("+", new Vector2(25f, 25f)))
            {
                var items = craftableItems.Where(item => item != null && item.Name == newEntryItemName);
                var macro = configuration.Macros.Where(x => x.Name == macroNames[newEntrySelectedMacro]);
                var foodName = foodNames[newEntryFoodNameSelection];
                bool HQ = false;
                if (foodName.Substring(0, 4) == "(HQ)")
                {
                    HQ = true;
                    foodName = foodName.Substring(5);
                }
                PluginLog.Information(foodName);

                if (items.Count() > 0 && newEntryCraftAmount > 0 && macro.Count() > 0)
                {


                    uint foodID = newEntryFoodNameSelection == 0 ? 0 : DalamudApi.DataManager.GetExcelSheet<Item>()!
                        .Where(x => x!.Name == foodName).First().RowId;
                    if (HQ) foodID += 1000000;
                    uint itemID = items.First()!.RowId;
                    configuration.Crafter.EntryList.Add(new Crafting.CListEntry(newEntryItemName, itemID, newEntryCraftAmount, macro.First(), foodID));
                }
            }

            ImGui.SetNextItemWidth(dynamicSize * 0.45f);
            if (newEntryShowItemNameList)
            {
                if (ImGui.ListBox("",
                    ref newEntryItemNameSelection,
                    newEntryItemNameSearchResults.ToArray(),
                    newEntryItemNameSearchResults.Count,
                    20)
                || ImGui.IsKeyDown((int)ImGuiKey.Enter))
                {
                    newEntryItemName = newEntryItemNameSearchResults[newEntryItemNameSelection];
                    newEntryShowItemNameList = false;
                }
            }
        }
        public void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("Crafting List", ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {

                DrawEntryTable();
                ImGui.NewLine();

                DrawNewListEntry();
                ImGui.NewLine();
                if (ImGui.Button("Craft!"))
                {
                    configuration.Crafter.CraftAllItems();
                }
                ImGui.End();
            }
        }

        public void DrawSelectedMacro()
        {
            ImGui.Text("Current Macro");
            if (ImGui.Combo("", ref selectedMacro, macroNames.ToArray(), macroNames.Count))
            {
                currMacroName = macroNames[selectedMacro];
                currMacroNum = configuration.Macros[selectedMacro - 1].MacroNum;
                currMacroDur = configuration.Macros[selectedMacro - 1].DurationSeconds;
            }

            if (macroNames[selectedMacro] != "")
            {

                ImGui.SetNextItemWidth((ImGui.GetWindowContentRegionWidth() - 25 - ImGui.CalcTextSize("Number Duration(s)").X) * 0.45f);
                if (ImGui.InputText("Name", ref this.currMacroName, 20)) configuration.Macros[selectedMacro - 1].Name = currMacroName;

                ImGui.PushItemWidth((ImGui.GetWindowContentRegionWidth() - 25) * 0.15f);
                if (ImGui.InputInt("Number", ref currMacroNum, 0)) configuration.Macros[selectedMacro - 1].MacroNum = currMacroNum;

                if (ImGui.InputInt("Duration (s)", ref currMacroDur, 0)) configuration.Macros[selectedMacro - 1].DurationSeconds = currMacroDur;

                ImGui.PopItemWidth();

                if (ImGui.Button("Delete...", new Vector2(60, 25)))
                {
                    int toDelete = selectedMacro;
                    selectedMacro = 0;
                    configuration.Macros.RemoveAt(toDelete - 1);
                    macroNames.RemoveAt(toDelete);
                }
            }
        }

        public void DrawNewMacro()
        {
            ImGui.Text("New Macro:");
            float availSize = ImGui.GetWindowContentRegionWidth() - 25 - ImGui.CalcTextSize("Macro Name  Macro Number  Macro Duration(s) ").X;
            ImGui.SetNextItemWidth(availSize * 0.45f);
            ImGui.InputTextWithHint("Macro Name ", "New Macro Name", ref newMacroName, 10);
            ImGui.SameLine();
            ImGui.PushItemWidth(availSize * 0.15f);
            ImGui.InputInt("Macro Number ", ref newMacroNum, 0);
            ImGui.SameLine();
            ImGui.InputInt("Macro Duration (s) ", ref newMacroDur, 0);
            ImGui.SameLine();
            ImGui.PopItemWidth();

            if (ImGui.Button("+", new Vector2(25, 25)) && newMacroName != "" && !macroNames.Contains(newMacroName))
            {
                configuration.Macros.Add(new Crafting.CraftingMacro(newMacroName, newMacroNum, newMacroDur));
                macroNames.Add(newMacroName);
                selectedMacro = macroNames.Count - 1;
                currMacroName = macroNames[selectedMacro];
                currMacroNum = configuration.Macros[selectedMacro - 1].MacroNum;
                currMacroDur = configuration.Macros[selectedMacro - 1].DurationSeconds;
                newMacroName = "";
                newMacroNum = 0;
                newMacroDur = 0;
            }
        }

        public void DrawOptionsTab()
        {
            ImGui.Text("Options");
            ImGui.NewLine();
            float availWidth = ImGui.GetWindowContentRegionWidth() - ImGui.CalcTextSize("Repair  ").X;

            ImGui.SetNextItemWidth(availWidth * 0.3f);
            if (ImGui.SliderInt("Repair Threshold ", ref configuration.RepairThresholdPercent, 0, 99))
            {
                configuration.Crafter.RepairThresholdPercent = configuration.RepairThresholdPercent;
            }

            ImGui.Checkbox("Only repair if durability is below 99?", ref configuration.OnlyRepairIfBelow99);
        }
        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            if (ImGui.Begin("A Wonderful Configuration Window", ref this.settingsVisible,
                 ImGuiWindowFlags.None))
            {
                ImGui.BeginTabBar("##ConfigTab");
                if (ImGui.BeginTabItem("Macros"))
                {
                    DrawSelectedMacro();

                    ImGui.NewLine();
                    ImGui.NewLine();

                    DrawNewMacro();

                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Other"))
                {
                    DrawOptionsTab();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();




            }
            configuration.Save();
            ImGui.End();
        }

    }
}
