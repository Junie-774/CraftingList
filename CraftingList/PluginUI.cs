using CraftingList.Crafting;
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
        private Crafter crafter;
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

        List<string> macroNames;

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

        public PluginUI(Configuration configuration, Crafter crafter)
        {
            this.configuration = configuration;
            this.crafter = crafter;
            macroNames = new List<string>
            {
                "",
            };
            craftableNames = new List<string>
            {
                ""
            };
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
                .Select(r => r.ItemResult.Value).Where(r => r != null && r.Name != "");
            foreach (var item in craftableItems)
            {
                craftableNames.Add(item!.Name);
            }
            foreach (var item in craftingFoods)
            {
                foodNames.Add(item!.Name);
            }

            foreach (var macro in configuration.Macros)
            {
                macroNames.Add(macro.Name);
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
            try
            {
                PluginLog.Debug("Entering draw /clist...");
                float tableSize = ImGui.GetWindowContentRegionWidth() - 25;

                PluginLog.Debug("Drawing table first row...");
                ImGui.Text("Crafting list:");
                ImGui.SetWindowFontScale(1.1f);
                ImGui.BeginTable("meow", 5, ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY,
                    new Vector2(0.0f, ImGui.GetTextLineHeightWithSpacing() * 6f));
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
                    PluginLog.Debug("Drawing other entries...");
                    foreach (var item in configuration.EntryList)
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
                    PluginLog.Debug("Removing complete entries...");
                    configuration.EntryList.RemoveAll(x => x.Complete);
                    PluginLog.Debug("Saving...");
                    configuration.Save();
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex.Message);
                }
                ImGui.EndTable();
                PluginLog.Debug("Done drawing table.");
            }
            catch (Exception e)
            {
                PluginLog.Error(e.Message);
            }
        }

        private void DrawNewListEntry()
        {
            PluginLog.Debug("Drawing new list entry...");
            ImGui.Text("Add item to list:");
            float dynamicSize = ImGui.GetWindowContentRegionWidth() - 25 - ImGui.CalcTextSize("Item  ...  Amount  Macro  Food  + ").X;

            ImGui.SetNextItemWidth(dynamicSize * 0.3f);
            PluginLog.Debug("Drawing item name input");
            if (ImGui.InputText("Item ", ref newEntryItemName, 25, ImGuiInputTextFlags.AllowTabInput))
            {
                newEntryItemNameSearchResults = craftableNames.Where(x => newEntryItemName == "" || x.ToLower().Contains(newEntryItemName.ToLower())).ToList();
                newEntryShowItemNameList = true;
            }
            ImGui.SameLine();
            PluginLog.Debug("Drawing item name button...")
            if (ImGui.Button("..."))
            {
                ImGui.SetKeyboardFocusHere(-1);
                newEntryShowItemNameList = !newEntryShowItemNameList;
            }
            ImGui.SameLine();

            PluginLog.Debug("Drawing Amount input...");
            ImGui.SetNextItemWidth(dynamicSize * 0.1f);
            ImGui.InputInt("Amount  ", ref newEntryCraftAmount, 0);
            ImGui.SameLine();

            ImGui.SetNextItemWidth(dynamicSize * 0.2f);
            PluginLog.Debug("Drawing Macro selection...");
            ImGui.Combo("Macro  ", ref newEntrySelectedMacro, macroNames.ToArray(), macroNames.Count);
            ImGui.SameLine();

            PluginLog.Debug("Drawing food...");
            ImGui.SetNextItemWidth(dynamicSize * 0.3f);
            ImGui.Combo("Food  ", ref newEntryFoodNameSelection, foodNames.ToArray(), foodNames.Count);
            ImGui.SameLine();


            PluginLog.Debug("Drawing + button...");
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

                if (items.Count() > 0 && newEntryCraftAmount > 0 && macro.Count() > 0)
                {

                    uint foodID = newEntryFoodNameSelection == 0 ? 0 : DalamudApi.DataManager.GetExcelSheet<Item>()!
                        .Where(x => x!.Name == foodName).First().RowId;

                    if (HQ) foodID += 1000000;

                    uint itemID = items.First()!.RowId;
                    configuration.EntryList.Add(new Crafting.CListEntry(newEntryItemName, itemID, newEntryCraftAmount, macro.First(), foodID));
                }
            }

            ImGui.SetNextItemWidth(dynamicSize * 0.45f);
            if (newEntryShowItemNameList)
            {
                if (ImGui.ListBox("",
                    ref newEntryItemNameSelection,
                    newEntryItemNameSearchResults.ToArray(),
                    newEntryItemNameSearchResults.Count,
                    20))
                /*|| ImGui.IsKeyDown((int)ImGuiKey.Enter))*/
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
                    crafter.CraftAllItems();
                }
                ImGui.End();
            }
        }

        public void DrawSelectedMacro()
        {
            ImGui.Text("Current Macro");
            if (ImGui.BeginCombo("Current Macro", currMacroName))
            {
                foreach (var macro in macroNames)
                {
                    bool isSelected = (currMacroName == macro);
                    if (ImGui.Selectable(macro, isSelected))
                    {
                        currMacroName = macro;
                        if (currMacroName != "")
                        {
                            currMacroNum = configuration.Macros.Where(m => m.Name == macro).First().MacroNum;
                            currMacroDur = configuration.Macros.Where(m => m.Name == macro).First().DurationSeconds;
                        }
                    }
                    if (isSelected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            if (currMacroName != "")
            {

                var currMacro = configuration.Macros.Where(m => m.Name == currMacroName);
                ImGui.SetNextItemWidth((ImGui.GetWindowContentRegionWidth() - 25 - ImGui.CalcTextSize("Number Duration(s)").X) * 0.45f);
                if (ImGui.InputText("Name", ref this.currMacroName, 20)) currMacro.First().Name = currMacroName;

                ImGui.PushItemWidth((ImGui.GetWindowContentRegionWidth() - 25) * 0.15f);
                if (ImGui.InputInt("Number", ref currMacroNum, 0)) currMacro.First().MacroNum = currMacroNum;

                if (ImGui.InputInt("Duration (s)", ref currMacroDur, 0)) currMacro.First().DurationSeconds = currMacroDur;

                ImGui.PopItemWidth();

                if (ImGui.Button("Delete...", new Vector2(60, 25)))
                {
                    
                    configuration.Macros.RemoveAll(m => m.Name == currMacroName);
                    macroNames.Remove(currMacroName);
                    currMacroName = "";
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
                configuration.Macros.Add(new CraftingMacro(newMacroName, newMacroNum, newMacroDur));
                macroNames.Add(newMacroName);
                currMacroName = newMacroName;
                currMacroNum = newMacroNum;
                currMacroDur = newMacroDur;
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
            ImGui.SliderInt("Repair Threshold ", ref configuration.RepairThresholdPercent, 0, 99);
            ImGui.Checkbox("Only repair if durability is below 99?", ref configuration.OnlyRepairIfBelow99);
            ImGui.NewLine();

            // auxillary variables to allow for error checking
            int completeSoundEffect = configuration.SoundEffectListComplete;
            int cancelSoundEffect = configuration.SoundEffectListCancel;
            ImGui.Checkbox("Play Sound Effect on Craft Completion or Termination", ref configuration.AlertOnTerminate);
            if (configuration.AlertOnTerminate)
            {
                float width = ImGui.GetWindowContentRegionWidth() - ImGui.CalcTextSize("List Cancelled Sound Effect").X;
                ImGui.PushItemWidth(width * 0.1f);
                ImGui.Dummy(new Vector2(ImGui.GetWindowContentRegionWidth() * 0.05f, 0));
                ImGui.SameLine();
                if (ImGui.InputInt("List Complete Sound Effect", ref completeSoundEffect, 0))
                {
                    if (completeSoundEffect >= 1 && completeSoundEffect <= 16) configuration.SoundEffectListComplete = completeSoundEffect;
                }

                ImGui.Dummy(new Vector2(ImGui.GetWindowContentRegionWidth() * 0.05f, 0));
                ImGui.SameLine();
                if (ImGui.InputInt("List Cancelled Sound Effect", ref cancelSoundEffect, 0))
                {
                    if (cancelSoundEffect >= 1 && cancelSoundEffect <= 16) configuration.SoundEffectListCancel = cancelSoundEffect;
                }
                ImGui.PopItemWidth();
            }
            ImGui.NewLine();

            int extraTimeout = configuration.MacroExtraTimeoutMs;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize("0000000").X);
            if (ImGui.InputInt("Extra Timeout on Macros (ms)", ref extraTimeout, 0))
            {
                if (extraTimeout > 0) configuration.MacroExtraTimeoutMs = extraTimeout;
            }

            int addonTimeout = configuration.AddonTimeout;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize("0000000").X);
            if (ImGui.InputInt("Timeout on Waiting for Menus (ms)", ref addonTimeout, 0))
            {
                if (addonTimeout > 0) configuration.AddonTimeout = addonTimeout;
            }
        }

        public void DrawExperimentalTab()
        {
            ImGui.Text("Wait durations (ms)");
            ImGui.PushItemWidth(ImGui.CalcTextSize("0000000").X);
            
            object box = configuration.WaitDurations;
            foreach (var field in typeof(WaitDurationHelper).GetFields())
            {
                int toref = (int) (field.GetValue(box) ?? 2000);
                if (ImGui.InputInt(field.Name, ref toref, 0))
                {
                    field.SetValue(box, toref);
                    configuration.WaitDurations = (WaitDurationHelper)box;
                }
            }
            ImGui.PopItemWidth();
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
                if (ImGui.BeginTabItem("Experimental"))
                {
                    DrawExperimentalTab();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();




            }
            configuration.Save();
            ImGui.End();
        }

    }
}
