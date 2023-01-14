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

        private CraftingMacro dummyMacro = new("", -1, 0, -1, 0);

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

            foreach (var macro in plugin.Configuration.Macros)
            {
                macroNames.Add(macro.Name);
            }
            var MealIndexes = DalamudApi.DataManager.GetExcelSheet<ItemFood>()!
                .Select(m => m.RowId);

            var craftingConsumables = DalamudApi.DataManager.GetExcelSheet<Item>()!
                .Where(item => item.ItemAction.Value!.DataHQ[1] != 0 && MealIndexes.Contains(item.ItemAction.Value.DataHQ[1]))
                .Where(meal =>
                {
                    int param = DalamudApi.DataManager.GetExcelSheet<ItemFood>()!
                        .GetRow(meal.ItemAction.Value!.DataHQ[1])!.UnkData1[0].BaseParam;
                    return param == 11 || param == 70 || param == 71;
                });

            craftingFoods = craftingConsumables.Where(meal => meal.ItemAction.Value!.Type == 844 || meal.ItemAction.Value!.Type == 845);
            craftingMedicine = craftingConsumables.Where(meal => meal.ItemAction.Value!.Type == 846);


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

        public void Draw()
        {
            ImGui.SetWindowFontScale(1.1f);
            if (plugin.Crafter.IsRunning())
            {
                ImGui.Text("Cannot edit macros while a crafting job is running.");
                ImGui.SetWindowFontScale(1f);
                return;
            }
            
            ImGui.Text("Select Macro");
            ImGui.SetWindowFontScale(1f);

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

            if (selectedMacroName != "")
            {
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

                if (ImGui.Combo("##MacroFood", ref selectedFood, foodNames.ToArray(), foodNames.Count)) {

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
                        CraftingMacro.isValidMacro(dummyMacro))
                    {
                        plugin.Configuration.Macros.Add(new CraftingMacro(dummyMacro.Name, dummyMacro.Macro1Num, dummyMacro.Macro1DurationSeconds, dummyMacro.Macro2Num, dummyMacro.Macro2DurationSeconds));
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
                            CraftingMacro.isValidMacro(dummyMacro))
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
            }
        }

        public void OnConfigChange()
        {
        }
    }
}
