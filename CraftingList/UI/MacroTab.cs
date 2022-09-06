using CraftingList.Crafting;
using ImGuiNET;
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

        private const string newMacroEntryString = "New Macro...";

        private string selectedMacroName = "";
        private string currMacroName = "";
        private int currMacroNum1 = 0;
        private int currMacroDur1 = 0;
        private int currMacroNum2 = 0;
        private int currMacroDur2 = 0;

        List<string> macroNames;

        private readonly CraftingList plugin;

        public MacroTab(CraftingList plugin)
        {
            this.plugin = plugin;
            macroNames = new List<string>
            {
                ""
            };
            foreach (var macro in plugin.Configuration.Macros)
            {
                macroNames.Add(macro.Name);
            }
        }

        public void Dispose()
        {

        }

        public void Draw()
        {
            ImGui.SetWindowFontScale(1.1f);
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
                    currMacroName = "";
                    currMacroNum1 = -1;
                    currMacroDur1 = 0;
                    currMacroNum2 = -1;
                    currMacroDur2 = 0;
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
                            currMacroName = mac.Name;
                            currMacroNum1 = mac.Macro1Num;
                            currMacroDur1 = mac.Macro1DurationSeconds;
                            currMacroNum2 = mac.Macro2Num;
                            currMacroDur2 = mac.Macro2DurationSeconds;
                        }
                        if (selectedMacroName == "")
                        {

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
                ImGui.InputText("##CurrMacroName", ref currMacroName, 50);


                ImGui.PushItemWidth(availSize * 0.085f);

                ImGui.Text("Macro 1 Number: ");
                ImGui.SameLine();
                ImGui.InputInt("##CurrMacroNum1", ref currMacroNum1, 0);
                ImGui.SameLine();

                ImGui.Text("Macro 1 Duration (s): ");
                ImGui.SameLine();
                ImGui.InputInt("##CurrMacroDur1", ref currMacroDur1, 0);

                ImGui.Text("Macro 2 Number: ");
                ImGui.SameLine();
                ImGui.InputInt("##CurrMacroNum2", ref currMacroNum2, 0);
                ImGui.SameLine();

                ImGui.Text("Macro 2 Duration (s): ");
                ImGui.SameLine();
                ImGui.InputInt("##CurrMacroDur2", ref currMacroDur2, 0);

                ImGui.PopItemWidth();

                if (selectedMacroName == newMacroEntryString)
                {
                    if (ImGui.Button("+", new Vector2(25, 25)) &&
                        !macroNames.Contains(currMacroName) &&
                        currMacroName != newMacroEntryString &&
                        CraftingMacro.isValidMacro(new CraftingMacro(currMacroName, currMacroNum1, currMacroDur1, currMacroNum2, currMacroDur2)))
                    {
                        plugin.Configuration.Macros.Add(new CraftingMacro(currMacroName, currMacroNum1, currMacroDur1, currMacroNum2, currMacroDur2));
                        macroNames.Add(currMacroName);

                        selectedMacroName = "";
                        plugin.PluginUi.OnConfigChange();
                    }
                }
                else
                {
                    if (ImGui.Button("Save", new Vector2(60, 25)))
                    {
                        if (currMacroName != "" &&
                            currMacroName != newMacroEntryString &&
                            (currMacroName == selectedMacroName || !macroNames.Contains(currMacroName)) &&
                            CraftingMacro.isValidMacro(new CraftingMacro(currMacroName, currMacroNum1, currMacroDur1, currMacroNum2, currMacroDur2)))
                        {
                            currMacro.First().Name = currMacroName;
                            macroNames[macroNames.FindIndex(m => m == selectedMacroName)] = currMacroName;
                            selectedMacroName = currMacroName;
                            currMacro.First().Macro1Num = currMacroNum1;
                            currMacro.First().Macro2Num = currMacroNum2;
                            currMacro.First().Macro1DurationSeconds = currMacroDur1;
                            currMacro.First().Macro2DurationSeconds = currMacroDur2;
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
                }
            }
        }

        public void OnConfigChange()
        {
        }
    }
}
