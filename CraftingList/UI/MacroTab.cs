using CraftingList.Crafting.Macro;
using CraftingList.Utility;
using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureMacroModule;

namespace CraftingList.UI
{
    internal class MacroTab : ITab, IDisposable
    {
        public string Name => "Macros";

        readonly private IEnumerable<Item?> craftingFoods;
        readonly private List<string> foodNames;
        private int selectedFood = 0;
        readonly private IEnumerable<Item?> craftingMedicine;
        readonly private List<string> medicineNames;
        private int selectedMedicine = 0;

        private const string newMacroEntryString = "New Macro...";

        private string selectedMacroName = "";

        private TimedIngameMacro dummyMacro = new("", -1, 0, -1, 0);

        List<string> macroNames;

        private readonly CraftingList plugin;

        public MacroTab(CraftingList plugin)
        {
            this.plugin = plugin;
            macroNames = new List<string>
            {
                ""
            };
            foodNames = new List<string>();
            medicineNames = new List<string>();

            foreach (var macro in DalamudApi.Configuration.Macros)
            {
                macroNames.Add(macro.Name);
            }

            craftingFoods = DalamudApi.CraftingConsumables.Where(meal => meal.ItemAction.Value!.Type == 844 || meal.ItemAction.Value!.Type == 845);
            craftingMedicine = DalamudApi.CraftingConsumables.Where(meal => meal.ItemAction.Value!.Type == 846);


            foreach (var item in craftingFoods)
            {
                foodNames.Add(item!.Name);
            }
            foreach (var item in craftingMedicine)
            {
                medicineNames.Add(item!.Name);
            }
            foodNames.Add("None");
            foodNames.Reverse();
            medicineNames.Add("None");
            medicineNames.Reverse();

            for (int i = 1; i < foodNames.Count; i++)
            {
                string hqFood = "(HQ) " + foodNames[i];
                foodNames.Insert(i, hqFood);
                i++;
            }
            for (int i = 1; i < medicineNames.Count; i++)
            {
                string hqFood = "(HQ) " + medicineNames[i];
                medicineNames.Insert(i, hqFood);
                i++;
            }
        }

        public void Dispose()
        {

        }

        public void DrawPluginMacros()
        {
            foreach (var macro in DalamudApi.Configuration.PluginMacros.ToArray())
            {
                var expanded = ImGui.TreeNode($"{macro.Name}##tree");
                DrawMacroPopup(macro);
                if (expanded)
                {
                    DrawPluginMacro(macro);
                    ImGui.TreePop();
                }
            }  
        }

        public void DrawPluginMacro(PluginMacro macro)
        {
            
            float availSize = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;

            ImGui.Text("Food: ");
            ImGui.SameLine();
            int selectedFood1 = macro.FoodID != 0 ? foodNames.IndexOf(DalamudApi.GetRowFromId(GetBaseFoodID(macro.FoodID))!.Name) : 0;
            if (IsItemHQ(macro.FoodID)) selectedFood1--;

            float macroLineLength = ImGui.CalcTextSize("Macro 1 Number: Macro 1 Duration (s) : ").X;
            ImGui.SetNextItemWidth(macroLineLength + (availSize - macroLineLength) * 0.17f - ImGui.CalcTextSize("Food: ").X);

            var newFood = DrawComboBox($"##{macro.Name}-food", ref selectedFood1, foodNames.ToArray(), macro.FoodID);
            if (newFood != macro.FoodID)
            {
                macro.FoodID = newFood;
                DalamudApi.Configuration.Save();
                plugin.PluginUi.OnConfigChange();
            }


            ImGui.Text("Medicine: ");
            ImGui.SameLine();

            int selectedMeds1 = macro.MedicineID != 0 ? medicineNames.IndexOf(DalamudApi.GetRowFromId(GetBaseFoodID(macro.MedicineID))!.Name) : 0;
            if (IsItemHQ(macro.MedicineID)) selectedMeds1--;

            ImGui.SetNextItemWidth(macroLineLength + (availSize - macroLineLength) * 0.17f - ImGui.CalcTextSize("Medicine: ").X);

            var newMeds = DrawComboBox($"$${macro.Name}-medicine", ref selectedMeds1, medicineNames.ToArray(), macro.MedicineID);
            if (newMeds != macro.MedicineID)
            {
                macro.MedicineID = newMeds;
                DalamudApi.Configuration.Save();
                plugin.PluginUi.OnConfigChange();
            }
            var contents = macro.Text;
            
            if (ImGui.InputTextMultiline($"##{macro.Name}-editor", ref contents, 100_000, new Vector2(macroLineLength + (availSize - macroLineLength) * 0.17f,
                14 * ImGui.CalcTextSize("Z").Y)))
            {
                macro.Text = contents;
                DalamudApi.Configuration.Save();
                plugin.PluginUi.OnConfigChange();
            }

        }

        private void DrawMacroPopup(PluginMacro macro)
        {
            if (ImGui.BeginPopupContextItem($"##{macro.Name}-popup"))
            {
                var name = macro.Name;
                if (ImGui.InputText($"##rename", ref name, 100, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    var names = DalamudApi.Configuration.PluginMacros.Select(x => x.Name).ToArray();
                    if (!names.Contains(macro.Name))
                    {
                        macro.Name = name;
                        DalamudApi.Configuration.Save();
                        plugin.PluginUi.OnConfigChange();
                    }
                    
                }

                if (ImGuiAddons.IconButton(FontAwesomeIcon.TrashAlt, "Delete"))
                {
                    DalamudApi.Configuration.PluginMacros.RemoveAll(m => m.Name == name);
                    plugin.PluginUi.OnConfigChange();
                }
                ImGui.EndPopup();
                return;
            }
            return;
        }
        
        public void DrawPluginMacroPage()
        {
            ImGui.Text("Macros:");
            if (ImGuiAddons.IconButton(FontAwesomeIcon.Plus, "Add a macro."))
            {
                //Generate a unique name to avoid duplicates
                string baseName = "New Macro";
                string modifier = "";
                var names = DalamudApi.Configuration.PluginMacros.Select(x => x.Name).ToArray();
                int i = 1;
                while (names.Contains(baseName + modifier))
                {
                    modifier = " (" + i.ToString() + ")";
                    i++;
                }
                DalamudApi.Configuration.PluginMacros.Add(new PluginMacro(baseName + modifier, 0, 0, ""));
                plugin.PluginUi.OnConfigChange();
            }
            DrawPluginMacros();

            ImGui.NewLine();
            if (ImGui.Button("Import From InGame macros"))
            {
                foreach (var macro in DalamudApi.Configuration.Macros)
                {
                    var names = DalamudApi.Configuration.PluginMacros.Select(x => x.Name).ToArray();
                    if (!names.Contains(macro.Name))
                    {
                        DalamudApi.Configuration.PluginMacros.Add(PluginMacro.FromTimedIngameMacro(macro));
                        plugin.PluginUi.OnConfigChange();
                    }
                }
            }
        }
        public void Draw()
        {
            if (plugin.Crafter.IsRunning())
            {
                ImGui.Text("Cannot edit macros while a crafting job is running.");
                ImGui.SetWindowFontScale(1f);
                return;
            }

            if (ImGui.Checkbox("Use Fancy Plugin Macros?", ref DalamudApi.Configuration.UsePluginMacros))
            {
                plugin.PluginUi.OnConfigChange();
            }
            

            if (DalamudApi.Configuration.UsePluginMacros)
            {
                DrawPluginMacroPage();
                return;
            }
            else
            {
                DrawIngameMacroSelector();

                if (selectedMacroName != "")
                {
                    selectedMacroName = DrawSelectedIngameMacro(selectedMacroName);
                }
            }           
        }

        public void DrawIngameMacroSelector()
        {
            ImGui.Text("Select Macro");

            float availSize = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;

            ImGui.SetNextItemWidth(availSize * 0.75f);
            if (ImGui.BeginCombo("##Current Macro", selectedMacroName))
            {

                if (ImGui.Selectable(""))
                {
                    selectedMacroName = "";
                }
                if (ImGui.Selectable(newMacroEntryString))
                {
                    selectedMacroName = newMacroEntryString;
                    dummyMacro.Name = "";
                    dummyMacro.Macro1Num = -1;
                    dummyMacro.Macro1DurationSeconds = 0;
                    dummyMacro.Macro2Num = -1;
                    dummyMacro.Macro2DurationSeconds = 0;
                    dummyMacro.FoodID = 0;
                    dummyMacro.MedicineID = 0;
                }
                foreach (var macro in macroNames)
                {
                    //We want "" to be a selectable macro name for the purposes of the crafting list, but we also want it to come first in this dropdown,
                    //So we put "" as a selectable manually and skip it in this loop
                    if (macro == "") continue;

                    bool isSelected = (selectedMacroName == macro);
                    if (ImGui.Selectable(macro, isSelected))
                    {
                        selectedMacroName = macro;
                        if (selectedMacroName != "" && selectedMacroName != newMacroEntryString)
                        {
                            var mac = plugin.Configuration.Macros.Where(m => m.Name == macro).First();
                            dummyMacro.Name = mac.Name;
                            dummyMacro.Macro1Num = mac.Macro1Num;
                            dummyMacro.Macro1DurationSeconds = mac.Macro1DurationSeconds;
                            dummyMacro.Macro2Num = mac.Macro2Num;
                            dummyMacro.Macro2DurationSeconds = mac.Macro2DurationSeconds;
                            dummyMacro.FoodID = mac.FoodID;
                            dummyMacro.MedicineID = mac.MedicineID;

                            var baseFoodId = dummyMacro.FoodID > 1000000 ? dummyMacro.FoodID - 1000000 : dummyMacro.FoodID;

                            selectedFood = baseFoodId == 0 ? 0 :
                                foodNames.IndexOf(craftingFoods.Where(m => m!.RowId == baseFoodId).First()!.Name);
                            if (dummyMacro.FoodID > 1000000) selectedFood -= 1;

                            var baseTincId = dummyMacro.MedicineID > 1000000 ? dummyMacro.MedicineID - 1000000 : dummyMacro.MedicineID;

                            selectedMedicine = baseTincId == 0 ? 0 :
                                medicineNames.IndexOf(craftingMedicine.Where(m => m!.RowId == baseTincId).First()!.Name);
                            if (dummyMacro.MedicineID > 1000000) selectedMedicine -= 1;
                        }
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }
        }

        public string DrawSelectedIngameMacro(string selectedMacroName)
        {
            float availSize = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;

            var currMacro = plugin.Configuration.Macros.Where(m => m.Name == selectedMacroName);
            ImGui.Text("Name:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth((availSize * 0.75f) - ImGui.CalcTextSize("Name:").X);
            ImGui.InputText("##CurrMacroName", ref dummyMacro.Name, 50);


            ImGui.PushItemWidth(availSize * 0.085f);

            ImGui.Text("Macro 1 Number: ");
            ImGui.SameLine();
            ImGui.InputInt("##CurrMacroNum1", ref dummyMacro.Macro1Num, 0);
            ImGui.SameLine();

            ImGui.Text("Macro 1 Duration (s): ");
            ImGui.SameLine();
            ImGui.InputInt("##CurrMacroDur1", ref dummyMacro.Macro1DurationSeconds, 0);

            ImGui.Text("Macro 2 Number: ");
            ImGui.SameLine();
            ImGui.InputInt("##CurrMacroNum2", ref dummyMacro.Macro2Num, 0);
            ImGui.SameLine();

            ImGui.Text("Macro 2 Duration (s): ");
            ImGui.SameLine();
            ImGui.InputInt("##CurrMacroDur2", ref dummyMacro.Macro2DurationSeconds, 0);

            ImGui.PopItemWidth();

            ImGui.Text("Food: ");
            ImGui.SameLine();
            float macroLineLength = ImGui.CalcTextSize("Macro 1 Number: Macro 1 Duration (s) : ").X;
            ImGui.SetNextItemWidth(macroLineLength + (availSize - macroLineLength) * 0.17f - ImGui.CalcTextSize("Food: ").X);

            if (ImGui.Combo("##MacroFood", ref selectedFood, foodNames.ToArray(), foodNames.Count))
            {

                var foodName = foodNames[selectedFood];
                bool isFoodHQ = false;

                if (foodName[..4] == "(HQ)")
                {
                    isFoodHQ = true;
                    foodName = foodName[5..];
                }

                uint foodID = selectedFood == 0 ? 0 : craftingFoods.Where(x => x!.Name == foodName).First()!.RowId;

                if (isFoodHQ) foodID += 1000000;

                dummyMacro.FoodID = foodID;

            }
            ImGui.Text("Medicine: ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(macroLineLength + (availSize - macroLineLength) * 0.17f - ImGui.CalcTextSize("Medicine: ").X);
            if (ImGui.Combo("##MacroMedicine", ref selectedMedicine, medicineNames.ToArray(), medicineNames.Count))
            {
                var medicineName = medicineNames[selectedMedicine];
                bool isMedicineHQ = false;

                if (medicineName[..4] == "(HQ)")
                {
                    isMedicineHQ = true;
                    medicineName = medicineName[5..];
                }

                uint medicineID = selectedMedicine == 0 ? 0 : craftingMedicine.Where(x => x!.Name == medicineName).First()!.RowId;

                if (isMedicineHQ) medicineID += 1000000;

                dummyMacro.MedicineID = medicineID;
            }
            if (selectedMacroName == newMacroEntryString)
            {
                if (ImGui.Button("+", new Vector2(25, 25)) &&
                    !macroNames.Contains(dummyMacro.Name) &&
                    dummyMacro.Name != newMacroEntryString &&
                    TimedIngameMacro.isValidMacro(dummyMacro))
                {
                    plugin.Configuration.Macros.Add(new TimedIngameMacro(dummyMacro.Name, dummyMacro.Macro1Num, dummyMacro.Macro1DurationSeconds, dummyMacro.Macro2Num, dummyMacro.Macro2DurationSeconds));
                    macroNames.Add(dummyMacro.Name);

                    selectedMacroName = "";
                    plugin.PluginUi.OnConfigChange();
                }
            }
            else
            {
                if (ImGui.Button("Save", new Vector2(60, 25)))
                {
                    if (dummyMacro.Name != "" &&
                        dummyMacro.Name != newMacroEntryString &&
                        (dummyMacro.Name == selectedMacroName || !macroNames.Contains(dummyMacro.Name)) &&
                        TimedIngameMacro.isValidMacro(dummyMacro))
                    {
                        currMacro.First().Name = dummyMacro.Name;
                        macroNames[macroNames.FindIndex(m => m == selectedMacroName)] = dummyMacro.Name;
                        selectedMacroName = dummyMacro.Name;
                        currMacro.First().Macro1Num = dummyMacro.Macro1Num;
                        currMacro.First().Macro2Num = dummyMacro.Macro2Num;
                        currMacro.First().Macro1DurationSeconds = dummyMacro.Macro1DurationSeconds;
                        currMacro.First().Macro2DurationSeconds = dummyMacro.Macro2DurationSeconds;
                        currMacro.First().FoodID = dummyMacro.FoodID;
                        currMacro.First().MedicineID = dummyMacro.MedicineID;
                        selectedMacroName = "";
                        plugin.PluginUi.OnConfigChange();
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(60f, 25f)))
                {
                    selectedMacroName = "";
                }
                ImGui.SameLine();
                if (ImGui.Button("Delete...", new Vector2(60, 25)))
                {

                    plugin.Configuration.Macros.RemoveAll(m => m.Name == selectedMacroName);
                    macroNames.Remove(selectedMacroName);
                    selectedMacroName = "";
                    plugin.PluginUi.OnConfigChange();
                }
                ImGui.SameLine();
            }
            return selectedMacroName;
        }

        public uint DrawComboBox(string imguiID, ref int selection, string[] names, uint oldResult)
        {        

            if (ImGui.Combo($"##{imguiID}", ref selection, names, names.Length))
            {
                var consumableName = names[selection];
                bool isConsumableHQ = false;

                if (consumableName[..4] == "(HQ)")
                {
                    isConsumableHQ = true;
                    consumableName = consumableName[5..];
                }
                uint consumableID = selection == 0 ? 0 : DalamudApi.CraftingConsumables.Where(x => x!.Name == consumableName).First()!.RowId;
                if (isConsumableHQ) consumableID += 1000000;

                return consumableID;
            }
            
            // Searching through the consumables every frame is very slow, so we pass the old result to send through
            // if the combo box doesn't change, instead of re-searching.
            return oldResult;
        }

        public uint GetBaseFoodID(uint id)
        {
            if (IsItemHQ(id)) id -= 1000000;
            return id;
        }

        public bool IsItemHQ(uint id)
        {
            return id > 1000000;
        }
        public void OnConfigChange()
        {
        }
    }
}
