using CraftingList.Crafting.Macro;
using CraftingList.UI.CraftingListTab;
using CraftingList.Utility;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel.Sheets;
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

        readonly private IEnumerable<Item> craftingFoods;
        readonly private List<string> foodNames;
        readonly private IEnumerable<Item> craftingMedicine;
        readonly private List<string> medicineNames;


        private CraftingMacro? draggedMacro = null;
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
                foodNames.Add(item!.Name.ToString());
            }
            foreach (var item in craftingMedicine)
            {
                medicineNames.Add(item!.Name.ToString());
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
        public void DrawMacroPage()
        {
            ImGuiAddons.BeginGroupPanel("Macros", new Vector2(-1, 0));
            DrawMacros();
            ImGuiAddons.EndGroupPanel();

            if (toDelete != "")
            {
                EntryListTable.RemoveMacroName(toDelete);
                MacroManager.RemoveMacro(toDelete);
                toDelete = "";
            }
        }

        public void Dispose()
        {

        }

        public void DrawMacros()
        {
            for (int i = 0; i < Service.Configuration.CraftingMacros.Count ; i++)
            {
                if (i == MacroManager.CraftingMacros.Count)
                {
                    var mac = Service.Configuration.IngameMacros[i];
                }
                var macro = Service.Configuration.CraftingMacros[i];
                var expanded = ImGui.TreeNodeEx($"##CraftingMacro-{i}", ImGuiTreeNodeFlags.NoAutoOpenOnLog, macro.Name);
                DrawCraftingMacroPopup(macro);
                bool dragTarget = CraftingMacroDragDrop(macro);
                if (expanded)
                {
                    DrawCraftingMacro(macro, i);
                    ImGui.TreePop();
                }
            }
            if (ImGuiAddons.IconButton(FontAwesomeIcon.Plus, "Add a macro."))
            {
                MacroManager.AddEmptyCraftingMacro("New Macro");
            }

        }

        public void DrawCraftingMacro(CraftingMacro macro, int id)
        {
            if (!macro.UseIngameMacro)
                ImGuiAddons.UnderlinedText("In-Plugin macro");
            else
                ImGui.Text("In-Plugin macro");

            ImGui.SameLine();

            ImGuiAddons.ToggleSwitch("Toooogle", ref macro.UseIngameMacro);
            ImGui.SameLine();

            if (macro.UseIngameMacro)
                ImGuiAddons.UnderlinedText("In-Game macro");
            else
                ImGui.Text("In-Game macro");

            var name = macro.Name;
            if (ImGui.InputText($"##RenameCraftingMacro-{id}", ref name, 100)
                && name != macro.Name)
            {
                EntryListTable.UpdateMacroNameInEntries(macro.Name, name);
                MacroManager.RenameMacro(macro.Name, name);

                Service.Configuration.Save();
            }
            FoodSelectionBox(macro);
            MedicineSelectionBox(macro);

            

            if (macro.UseIngameMacro)
            {
                ImGui.PushItemWidth(50 * ImGuiHelpers.GlobalScale);
                ImGui.InputInt($"1st Macro Number##{macro.Name}-Num1", ref macro.Macro1Num, 0);
                ImGui.InputInt($"2nd Macro Number##{macro.Name}-Num2", ref macro.Macro2Num, 0);
                ImGui.PopItemWidth();
            }
            else
            {
                var contents = macro.Text;
                if (ImGui.InputTextMultiline($"##{macro.Name}-editor", ref contents, 100_000,
                    ImGuiHelpers.ScaledVector2(350, 14 * ImGui.CalcTextSize("Z").Y)))
                {
                    macro.Text = contents;
                    Service.Configuration.Save();
                }
            }



        }

        public void DrawCraftingMacroPopup(CraftingMacro macro)
        {
            if (ImGui.BeginPopupContextItem($"##{macro.Name}-popup"))
            {
                DrawMacroPopupContent(macro);
                ImGui.SameLine();

                ImGui.EndPopup();
            }
        }


        public void DrawIngameMacro(IngameMacro macro, int id)
        {
            var name = macro.Name;
            if (ImGui.InputText($"##RenameIngameMacro-{id}", ref name, 100)
                && name != macro.Name)
            {
                EntryListTable.UpdateMacroNameInEntries(macro.Name, name);
                MacroManager.RenameMacro(macro.Name, name);

                Service.Configuration.Save();
            }

            FoodSelectionBox(macro);

            MedicineSelectionBox(macro);

            ImGui.PushItemWidth(50 * ImGuiHelpers.GlobalScale);
            ImGui.InputInt($"1st Macro Number##{macro.Name}-Num1", ref macro.Macro1Num, 0);
            ImGui.InputInt($"2nd Macro Number##{macro.Name}-Num2", ref macro.Macro2Num, 0);
            ImGui.PopItemWidth();
        }


        private void DrawMacroPopupContent(CraftingMacro macro)
        {
            var name = macro.Name;
            if (ImGui.InputText($"##rename", ref name, 100, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue)
                && name != macro.Name)
            {
                EntryListTable.UpdateMacroNameInEntries(macro.Name, name);
                MacroManager.RenameMacro(macro.Name, name);
                
                Service.Configuration.Save();
            }
            if (ImGuiAddons.IconButton(FontAwesomeIcon.TrashAlt, "Delete"))
            {
                toDelete = macro.Name;
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
            int selectedFood1 = macro.FoodID != 0 ? foodNames.IndexOf(Service.Items[(int) GetBaseFoodID(macro.FoodID)].Name.ToString()) : 0;
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
            int selectedMeds1 = macro.MedicineID != 0 ? medicineNames.IndexOf(Service.Items[(int) GetBaseFoodID(macro.MedicineID)].Name.ToString()) : 0;
            if (IsItemHQ(macro.MedicineID))
                selectedMeds1--;

            var newMeds = DrawComboBox("Medicine", $"${macro.Name}-medicine", ref selectedMeds1, medicineNames.ToArray(), 325, macro.MedicineID);
            if (newMeds != macro.MedicineID)
            {
                macro.MedicineID = newMeds;
                Service.Configuration.Save();
            }
        }

        public bool CraftingMacroDragDrop(CraftingMacro macro)
        {
            if (ImGui.BeginDragDropSource())
            {
                this.draggedMacro = macro;
                ImGui.Text(macro.Name);
                ImGui.SetDragDropPayload("CraftingMacroPayload", IntPtr.Zero, 0);
                ImGui.EndDragDropSource();
            }

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("CraftingMacroPayload");

                bool nullPtr;
                unsafe
                {
                    nullPtr = payload.NativePtr == null;
                }

                if (!nullPtr && payload.IsDelivery() && draggedMacro != null)
                {
                    var index = Service.Configuration.CraftingMacros.IndexOf(macro);
                    MacroManager.MoveCraftingMacro(draggedMacro, index);
                    Service.Configuration.Save();
                }
                
                ImGui.EndDragDropTarget();
                return true;
            }

            return false;
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
