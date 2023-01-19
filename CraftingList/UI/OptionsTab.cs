using CraftingList.Crafting.Macro;
using CraftingList.Utility;
using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.UI
{
    internal class OptionsTab : ITab, IDisposable
    {
        string ITab.Name => "Options";

        private readonly CraftingList plugin;
        void IDisposable.Dispose()
        {
        }

        public OptionsTab(CraftingList plugin)
        {
            this.plugin = plugin;
        }
        void ITab.Draw()
        {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(1, 350);
            ImGui.SetColumnWidth(2, 350);
            float availWidth = ImGui.GetColumnWidth();

            ImGui.SetNextItemWidth(300 - 0 - ImGui.CalcTextSize("Repair threshold: ").X);
            ImGui.SliderInt("Repair Threshold##RepairThreshold", ref DalamudApi.Configuration.RepairThresholdPercent, 0, 99);

            ImGui.Checkbox("Only repair if durability for all items is below 99?", ref DalamudApi.Configuration.OnlyRepairIfBelow99);
            ImGui.NewLine();

            // auxillary variables to allow for error checking
            int completeSoundEffect = DalamudApi.Configuration.SoundEffectListComplete;
            int cancelSoundEffect = DalamudApi.Configuration.SoundEffectListCancel;


            ImGui.Checkbox("Play Sound effect when crafting terminates?", ref DalamudApi.Configuration.AlertOnTerminate);
            if (DalamudApi.Configuration.AlertOnTerminate)
            {
                ImGui.PushItemWidth(ImGui.CalcTextSize("00000").X);
                ImGui.Dummy(new Vector2(50f, 0));
                ImGui.SameLine();
                if (ImGui.InputInt("List Complete Sound Effect", ref completeSoundEffect, 0))
                {
                    if (completeSoundEffect >= 1 && completeSoundEffect <= 16) DalamudApi.Configuration.SoundEffectListComplete = completeSoundEffect;
                }

                ImGui.Dummy(new Vector2(50f, 0));
                ImGui.SameLine();
                if (ImGui.InputInt("List Cancelled Sound Effect", ref cancelSoundEffect, 0))
                {
                    if (cancelSoundEffect >= 1 && cancelSoundEffect <= 16) DalamudApi.Configuration.SoundEffectListCancel = cancelSoundEffect;
                }
                ImGui.PopItemWidth();
            }
            ImGui.NewLine();

            int extraTimeout = DalamudApi.Configuration.MacroExtraTimeoutMs;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize("0000000").X);

            if (ImGui.InputInt("Extra Timeout on Macros (ms)", ref extraTimeout, 0))
            {
                if (extraTimeout > 0) DalamudApi.Configuration.MacroExtraTimeoutMs = extraTimeout;
            }

            int addonTimeout = DalamudApi.Configuration.AddonTimeout;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize("0000000").X);
            if (ImGui.InputInt("Timeout on Waiting for Menus (ms)", ref addonTimeout, 0))
            {
                if (addonTimeout > 0) DalamudApi.Configuration.AddonTimeout = addonTimeout;
            }


            ImGui.NextColumn();

            // auxillary variables to allow for error checking
            float clickSynthesizeDelayMin = DalamudApi.Configuration.ClickSynthesizeDelayMinSeconds;
            float clickSynthesizeDelayMax = DalamudApi.Configuration.ClickSynthesizeDelayMaxSeconds;

            ImGui.PushItemWidth(ImGui.CalcTextSize("00000").X);

            ImGui.Text("Delay after clicking synthesize");
            ImGui.Dummy(new Vector2(18f, 0));
            ImGui.SameLine();
            if (ImGui.InputFloat("Min. (s)##ClickSynthesizDelayMin", ref clickSynthesizeDelayMin, 0, 0, "%.1f"))
            {
                if (clickSynthesizeDelayMin > 0 && clickSynthesizeDelayMin <= clickSynthesizeDelayMax)
                    DalamudApi.Configuration.ClickSynthesizeDelayMinSeconds = clickSynthesizeDelayMin;
            }

            ImGui.Dummy(new Vector2(18f, 0));
            ImGui.SameLine();
            if (ImGui.InputFloat("Max. (s)##ClickSynthesizeDelayMax", ref clickSynthesizeDelayMax, 0, 0, "%.1f"))
            {
                if (clickSynthesizeDelayMax > 0 && clickSynthesizeDelayMin <= clickSynthesizeDelayMax)
                    DalamudApi.Configuration.ClickSynthesizeDelayMaxSeconds = clickSynthesizeDelayMax;
            }

            ImGui.NewLine();

            // auxillary variables to allow for error checking
            float executeMacroDelayMin = DalamudApi.Configuration.ExecuteMacroDelayMinSeconds;
            float executeMacroDelayMax = DalamudApi.Configuration.ExecuteMacroDelayMaxSeconds;

            ImGui.Text("Delay after executing macro");

            ImGui.Dummy(new Vector2(18f, 0));
            ImGui.SameLine();
            if (ImGui.InputFloat("Min. (s).##ExecuteMacroDelayMin", ref executeMacroDelayMin, 0, 0, "%.1f"))
            {
                if (executeMacroDelayMin > 0 && executeMacroDelayMin <= executeMacroDelayMax)
                    DalamudApi.Configuration.ExecuteMacroDelayMinSeconds = executeMacroDelayMin;
            }

            ImGui.Dummy(new Vector2(18f, 0));
            ImGui.SameLine();
            if (ImGui.InputFloat("Max. (s)##ExecuteMacroDelayMax", ref executeMacroDelayMax, 0, 0, "%.1f"))
            {
                if (executeMacroDelayMax > 0 && executeMacroDelayMin <= executeMacroDelayMax)
                    DalamudApi.Configuration.ExecuteMacroDelayMaxSeconds = executeMacroDelayMax;
            }
            ImGui.PopItemWidth();

            ImGui.Checkbox("Ignore <wait.x> and wait intelligently?", ref DalamudApi.Configuration.SmartWait);

            ImGui.NewLine();
            /*
            if (ImGui.Button("Import Old Macros"))
            {
                foreach (var macro in DalamudApi.Configuration.IngameMacros)
                {
                    var names = DalamudApi.Configuration.PluginMacros.Select(x => x.Name).ToArray();
                    if (!names.Contains(macro.Name))
                    {
                        var newName = MacroManager.GenerateUniqueName(MacroManager.MacroNames, macro.Name);
                        var newMacro = PluginMacro.FromTimedIngameMacro(macro);
                        newMacro.Name = newName;
                        DalamudApi.Configuration.PluginMacros.Add(newMacro);
                    }
                }
            }*/
            if (ImGui.Button("Show Macro Change Message"))
            {
                DalamudApi.Configuration.AcknowledgedMacroChange = false;
            }

            ImGui.Columns(1); 
        }

    }
}
