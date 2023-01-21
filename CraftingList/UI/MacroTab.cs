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
        readonly private IEnumerable<Item?> craftingMedicine;
        readonly private List<string> medicineNames;

        private readonly CraftingList plugin;

        private string toDelete = "";

        public MacroTab(CraftingList plugin)
        {
            this.plugin = plugin;

            foodNames = new List<string>();
            medicineNames = new List<string>();

            craftingFoods = Service.CraftingConsumables.Where(meal => meal.ItemAction.Value!.Type == 844 || meal.ItemAction.Value!.Type == 845);
            craftingMedicine = Service.CraftingConsumables.Where(meal => meal.ItemAction.Value!.Type == 846);


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

        public void DrawIngameMacros()
        {
            if (ImGui.TreeNode($"In-Game Macros"))
            {
                if (ImGuiAddons.IconButton(FontAwesomeIcon.Plus, "Add a macro."))
                {
                    MacroManager.AddEmptyIngameMacro("New Macro");
                }
                foreach (var macro in MacroManager.IngameMacros)
                {
                    var expanded = ImGui.TreeNode($"{macro.Name}##tree");
                    DrawIngameMacroPopup(macro);
                    if (expanded)
                    {
                        DrawIngameMacro(macro);
                        ImGui.TreePop();
                    }

                }
                ImGui.TreePop();
            }
        }

        public void DrawPluginMacros()
        {
            if (ImGui.TreeNode($"In-Plugin Macros"))
            {
                if (ImGuiAddons.IconButton(FontAwesomeIcon.Plus, "Add a macro."))
                {
                    MacroManager.AddEmptyPluginMacro("New Macro");
                }
                foreach (var macro in MacroManager.PluginMacros)
                {
                    var expanded = ImGui.TreeNode($"{macro.Name}##tree");
                    DrawPluginMacroPopup(macro);
                    if (expanded)
                    {
                        DrawPluginMacro(macro);
                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }
        }

        public void DrawIngameMacro(IngameMacro macro)
        {
            FoodSelectionBox(macro);

            MedicineSelectionBox(macro);

            ImGui.PushItemWidth(50 * ImGuiHelpers.GlobalScale);
            ImGui.InputInt($"1st Macro Number##{macro.Name}-Num1", ref macro.Macro1Num, 0);
            ImGui.InputInt($"2nd Macro Number##{macro.Name}-Num2", ref macro.Macro2Num, 0);
            ImGui.PopItemWidth();
        }


        public void DrawPluginMacro(PluginMacro macro)
        {
            FoodSelectionBox(macro);

            MedicineSelectionBox(macro);

            var contents = macro.Text;
            if (ImGui.InputTextMultiline($"##{macro.Name}-editor", ref contents, 100_000,
                ImGuiHelpers.ScaledVector2(350, 14 * ImGui.CalcTextSize("Z").Y)))
            {
                PluginLog.Debug(contents);
                macro.Text = contents;
                Service.Configuration.Save();
            }

        }
        private void DrawPluginMacroPopup(PluginMacro macro)
        {
            if (ImGui.BeginPopupContextItem($"##{macro.Name}-popup"))
            {
                DrawMacroPopupContent(macro);
                ImGui.SameLine();

                ImGui.EndPopup();
            }
        }
        private void DrawIngameMacroPopup(IngameMacro macro)
        {
            if (ImGui.BeginPopupContextItem($"##{macro.Name}-popup"))
            {
                DrawMacroPopupContent(macro);
                ImGui.SameLine();

                if (ImGuiAddons.IconButton(FontAwesomeIcon.FileImport, "Import to In-Plugin Macros"))
                {
                    MacroManager.AddPluginMacro(PluginMacro.FromIngameMacro(macro));
                    Service.Configuration.Save();
                }
                ImGui.EndPopup();

            }
        }
        private void DrawMacroPopupContent(CraftingMacro macro)
        {
            var name = macro.Name;
            if (ImGui.InputText($"##rename", ref name, 100, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
            {
                CraftingListTab.UpdateMacroNameInEntries(macro.Name, name);
                MacroManager.RenameMacro(macro.Name, name);
                
                Service.Configuration.Save();
            }
            if (ImGuiAddons.IconButton(FontAwesomeIcon.TrashAlt, "Delete"))
            {
                toDelete = macro.Name;
            }

        }
        
        public void DrawMacroPage()
        {
            ImGui.Text("Macros:");

            DrawPluginMacros();
            DrawIngameMacros();

            if (toDelete != "")
            {
                CraftingListTab.RemoveMacroName(toDelete);
                MacroManager.RemoveMacro(toDelete);
                toDelete = "";
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


            DrawMacroPage();

        }

   

        public uint DrawComboBox(string label, string imguiID, ref int selection, string[] names, float width, uint oldResult)
        {

            ImGui.SetNextItemWidth((width - ImGui.CalcTextSize(label).X) * ImGuiHelpers.GlobalScale);
            if (ImGui.Combo($"{label}##{imguiID}", ref selection, names, names.Length))
            {
                var consumableName = names[selection];
                bool isConsumableHQ = false;

                if (consumableName[..4] == "(HQ)")
                {
                    isConsumableHQ = true;
                    consumableName = consumableName[5..];
                }
                uint consumableID = selection == 0 ? 0 : Service.CraftingConsumables.Where(x => x!.Name == consumableName).First()!.RowId;
                if (isConsumableHQ) consumableID += 1000000;

                return consumableID;
            }
            
            // Searching through the consumables every frame is very slow, so we pass the old result to send through
            // if the combo box doesn't change, instead of re-searching.
            return oldResult;
        }
        public void FoodSelectionBox(CraftingMacro macro)
        {
            int selectedFood1 = macro.FoodID != 0 ? foodNames.IndexOf(Service.GetRowFromId(GetBaseFoodID(macro.FoodID))!.Name) : 0;
            if (IsItemHQ(macro.FoodID))
                selectedFood1--;

            var newFood = DrawComboBox("Food", $"##{macro.Name}-food", ref selectedFood1, foodNames.ToArray(), 325, macro.FoodID);
            if (newFood != macro.FoodID)
            {
                macro.FoodID = newFood;
                Service.Configuration.Save();
            }
        }

        public void MedicineSelectionBox(CraftingMacro macro)
        {
            int selectedMeds1 = macro.MedicineID != 0 ? medicineNames.IndexOf(Service.GetRowFromId(GetBaseFoodID(macro.MedicineID))!.Name) : 0;
            if (IsItemHQ(macro.MedicineID))
                selectedMeds1--;

            var newMeds = DrawComboBox("Medicine", $"${macro.Name}-medicine", ref selectedMeds1, medicineNames.ToArray(), 325, macro.MedicineID);
            if (newMeds != macro.MedicineID)
            {
                macro.MedicineID = newMeds;
                Service.Configuration.Save();
            }
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
