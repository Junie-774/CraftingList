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
        bool newEntryHQmats = false;


        int newEntryFoodNameSelection = 0;

        int newEntrySelectedMacro = 0;
        string newEntryCraftAmount = "";

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

            float tableSize = (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X);


            ImGui.Text("Crafting list:");
            ImGui.Columns(6);
            float dynamicAvailWidth = (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) - (18 + 18 + 12) - (ImGui.CalcTextSize("Amount").X + ImGui.CalcTextSize("HQ Mats?").X + ImGui.CalcTextSize("Remove").X);
            ImGui.SetColumnWidth(0, dynamicAvailWidth * 0.5f);
            ImGui.SetColumnWidth(1, 18 + ImGui.CalcTextSize("Amount").X);
            ImGui.SetColumnWidth(2, dynamicAvailWidth * 0.25f);
            ImGui.SetColumnWidth(3, dynamicAvailWidth * 0.25f);
            ImGui.SetColumnWidth(4, 18 + ImGui.CalcTextSize("HQ Mats?").X);
            
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

            ImGui.Text("HQ Mats?");

            ImGui.SetWindowFontScale(1f);


            ImGui.Separator();
            ImGui.NextColumn();

            ImGui.NextColumn();

            foreach (var item in configuration.EntryList)
            {
                ImGui.Text(item.Name);
                ImGui.NextColumn();

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 12);
                ImGui.Text(item.NumCrafts.ToString());
                ImGui.NextColumn();

                ImGui.Text(item.Macro.Name);
                ImGui.NextColumn();

                bool HQ = item.FoodId > 1000000;
                ImGui.Text((HQ ? "(HQ) " : "")
                    + DalamudApi.DataManager.GetExcelSheet<Item>()!
                        .Where(x => x.RowId == (HQ ? item.FoodId - 1000000 : item.FoodId)).First().Name
                );
                ImGui.NextColumn();

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 20);
                ImGui.Checkbox("##HQ" + item.Name, ref item.HQMats);
                ImGui.NextColumn();

                if (ImGui.Button("Remove##" + configuration.EntryList.IndexOf(item)))
                {
                    item.Complete = true;
                }
                ImGui.NextColumn();

                ImGui.Separator();
            }
            configuration.EntryList.RemoveAll(x => x.Complete);
            configuration.Save();

        }

        private void DrawNewListEntry()
        {
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##Item", ref newEntryItemName, 65535, ImGuiInputTextFlags.AllowTabInput))
            {
                newEntryItemNameSearchResults = craftableNames.Where(x => newEntryItemName == "" || x.ToLower().Contains(newEntryItemName.ToLower())).ToList();
                newEntryShowItemNameList = true;
            }
            ImGui.NextColumn();


            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##Amount", ref newEntryCraftAmount, 65535);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Number of crafts. Enter \"max\" to craft until you run out of materials or inventory space.");
            }
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            ImGui.Combo("##Macro", ref newEntrySelectedMacro, macroNames.ToArray(), macroNames.Count);
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            ImGui.Combo("##Food", ref newEntryFoodNameSelection, foodNames.ToArray(), foodNames.Count);
            ImGui.NextColumn();

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 20);
            ImGui.Checkbox("##HQNewItem", ref newEntryHQmats);
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 18);

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

                if (items.Count() > 0 && (newEntryCraftAmount.ToLower() == "max" || int.TryParse(newEntryCraftAmount, out _)) && macro.Count() > 0)
                {

                    uint foodID = newEntryFoodNameSelection == 0 ? 0 : DalamudApi.DataManager.GetExcelSheet<Item>()!
                        .Where(x => x!.Name == foodName).First().RowId;

                    if (HQ) foodID += 1000000;

                    uint itemID = items.First()!.RowId;
                    configuration.EntryList.Add(new CListEntry(newEntryItemName, itemID, newEntryCraftAmount.ToLower(), macro.First(), foodID, newEntryHQmats));
                    newEntryItemName = "";
                    newEntryCraftAmount = "";
                    newEntrySelectedMacro = 0;
                    newEntryFoodNameSelection = 0;
                    newEntryHQmats = false;
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
                DrawNewListEntry();
                ImGui.NewLine();
                if (ImGui.Button("Craft!"))
                {
                    crafter.CraftAllItems();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    crafter.Cancel("Cancelling craft...", false);
                }
                if (crafter.waitingForHQSelection)
                {
                    ImGui.Text("Waiting for you to select the HQ mats for your craft, please press the button below when finished.");
                    if (ImGui.Button("I've Selected My HQ Mats"))
                    {
                        crafter.SignalHQMatsSelected();
                    }
                }
                ImGui.End();
            }
        }

        public void DrawSelectedMacro()
        {
            ImGui.SetNextItemWidth((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) * 0.35f);
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
                var currMacroDummyName = currMacroName;
                ImGui.SetNextItemWidth(((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) - 25 - ImGui.CalcTextSize("Number Duration(s)").X) * 0.45f);
                if (ImGui.InputText("Name", ref currMacroDummyName, 20))
                {
                    currMacro.First().Name = currMacroDummyName;
                    macroNames[macroNames.FindIndex(m => m == currMacroName)] = currMacroDummyName;
                    currMacroName = currMacroDummyName;
                }

                ImGui.PushItemWidth(((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) - 25) * 0.15f);
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
            ImGui.SetWindowFontScale(1.1f);
            ImGui.Text("New Macro");
            ImGui.SetWindowFontScale(1f);

            float availSize = (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) - 25 - ImGui.CalcTextSize("Macro Name: Macro Number: Macro Duration(s): + ").X;

            ImGui.Text("Macro Name:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(availSize * 0.6f);
            ImGui.InputTextWithHint("##Macro Name", "New Macro Name", ref newMacroName, 20);
            ImGui.SameLine();

            ImGui.Text(" Macro Number:");
            ImGui.SameLine();
            ImGui.PushItemWidth(availSize * 0.125f);
            ImGui.InputInt("##Macro Number", ref newMacroNum, 0);
            ImGui.SameLine();

            ImGui.Text(" Macro Duration (s):");
            ImGui.SameLine();
            ImGui.InputInt("##Macro Duration", ref newMacroDur, 0);
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
            ImGui.SetWindowFontScale(1.15f);
            ImGui.Text("Options");
            ImGui.SetWindowFontScale(1f);
            ImGui.Columns(2);
            float availWidth = (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) - ImGui.CalcTextSize("Repair  ").X;

            ImGui.SetNextItemWidth(availWidth * 0.3f);
            ImGui.SliderInt("Repair Threshold ", ref configuration.RepairThresholdPercent, 0, 99);
            ImGui.Checkbox("Only repair if durability is below 99?", ref configuration.OnlyRepairIfBelow99);
            ImGui.NewLine();

            // auxillary variables to allow for error checking
            int completeSoundEffect = configuration.SoundEffectListComplete;
            int cancelSoundEffect = configuration.SoundEffectListCancel;
            ImGui.Checkbox("Play Sound effect when crafting terminates?", ref configuration.AlertOnTerminate);
            if (configuration.AlertOnTerminate)
            {
                float width = (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) - ImGui.CalcTextSize("List Cancelled Sound Effect").X;
                ImGui.PushItemWidth(width * 0.1f);
                ImGui.Dummy(new Vector2((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) * 0.05f, 0));
                ImGui.SameLine();
                if (ImGui.InputInt("List Complete Sound Effect", ref completeSoundEffect, 0))
                {
                    if (completeSoundEffect >= 1 && completeSoundEffect <= 16) configuration.SoundEffectListComplete = completeSoundEffect;
                }

                ImGui.Dummy(new Vector2((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) * 0.05f, 0));
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
            ImGui.NextColumn();
            ImGui.Checkbox("Flash window when prompting to select HQ Materials?", ref configuration.FlashWindowOnHQPrompt);
            ImGui.Columns(1);
        }

        public void DrawExperimentalTab()
        {
            ImGui.Text("Wait durations (ms)");
            ImGui.PushItemWidth(ImGui.CalcTextSize("0000000").X);

            object box = configuration.WaitDurations;
            foreach (var field in typeof(WaitDurationHelper).GetFields())
            {
                int toref = (int)(field.GetValue(box) ?? 2000);
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
