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
            ImGui.SliderInt("Repair Threshold##RepairThreshold", ref Service.Configuration.RepairThresholdPercent, 0, 99);

            ImGui.Checkbox("Only repair if durability for all items is below 99?", ref Service.Configuration.OnlyRepairIfBelow99);
            ImGui.NewLine();

            // auxillary variables to allow for error checking
            int completeSoundEffect = Service.Configuration.SoundEffectListComplete;
            int cancelSoundEffect = Service.Configuration.SoundEffectListCancel;


            ImGui.Checkbox("Play Sound effect when crafting terminates?", ref Service.Configuration.AlertOnTerminate);
            if (Service.Configuration.AlertOnTerminate)
            {
                ImGui.PushItemWidth(ImGui.CalcTextSize("00000").X);
                ImGui.Dummy(new Vector2(50f, 0));
                ImGui.SameLine();
                if (ImGui.InputInt("List Complete Sound Effect", ref completeSoundEffect, 0))
                {
                    if (completeSoundEffect >= 1 && completeSoundEffect <= 16) Service.Configuration.SoundEffectListComplete = completeSoundEffect;
                }

                ImGui.Dummy(new Vector2(50f, 0));
                ImGui.SameLine();
                if (ImGui.InputInt("List Cancelled Sound Effect", ref cancelSoundEffect, 0))
                {
                    if (cancelSoundEffect >= 1 && cancelSoundEffect <= 16) Service.Configuration.SoundEffectListCancel = cancelSoundEffect;
                }
                ImGui.PopItemWidth();
            }
            ImGui.NewLine();

            int extraTimeout = Service.Configuration.MacroExtraTimeoutMs;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize("0000000").X);

            if (ImGui.InputInt("Extra Timeout on Macros (ms)", ref extraTimeout, 0))
            {
                if (extraTimeout > 0) Service.Configuration.MacroExtraTimeoutMs = extraTimeout;
            }

            int addonTimeout = Service.Configuration.AddonTimeout;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize("0000000").X);
            if (ImGui.InputInt("Timeout on Waiting for Menus (ms)", ref addonTimeout, 0))
            {
                if (addonTimeout > 0) Service.Configuration.AddonTimeout = addonTimeout;
            }


            ImGui.NextColumn();

            ImGui.NewLine();

            if (ImGui.TreeNode("Macro options"))
            {

                MacroOptionsSection();
            }
            ImGui.NewLine();

            ImGui.Columns(1);
        }

        public void MacroOptionsSection()
        {
            ImGui.Checkbox("Ignore <wait.x> and wait intelligently?", ref Service.Configuration.SmartWait);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Uses the next action as soon as possible.\nMacros can't complete the craft early if this option is enabled.");
            }

            ImGui.InputInt("Number of retries allowed for macro actions before giving up?", ref Service.Configuration.MaxMacroCommandTimeoutRetries);

            AfterClickSynthesisOption();

            AfterExecuteMacroOption();
        }

        public void AfterExecuteMacroOption()
        {
            // auxillary variables to allow for error checking
            float executeMacroDelayMin = Service.Configuration.ExecuteMacroDelayMinSeconds;
            float executeMacroDelayMax = Service.Configuration.ExecuteMacroDelayMaxSeconds;

            ImGui.Checkbox("Wait after executing macro?", ref Service.Configuration.ExecuteMacroDelay);
            if (Service.Configuration.ExecuteMacroDelay)
            {

                ImGui.Dummy(new Vector2(25, 0));
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.CalcTextSize(executeMacroDelayMin.ToString()).X + 20);
                if (ImGui.InputFloat("s  to ##ExecuteMacroDelayMin", ref executeMacroDelayMin, 0, 0, "%.1f"))
                {
                    if (executeMacroDelayMin > 0 && executeMacroDelayMin <= executeMacroDelayMax)
                    {
                        Service.Configuration.ExecuteMacroDelayMinSeconds = executeMacroDelayMin;
                        Service.Configuration.Save();
                    }
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.CalcTextSize(executeMacroDelayMax.ToString()).X + 20);
                if (ImGui.InputFloat("s##ExecuteMacroDelayMax", ref executeMacroDelayMax, 0, 0, "%.1f"))
                {
                    if (executeMacroDelayMax > 0 && executeMacroDelayMin <= executeMacroDelayMax)
                    {
                        Service.Configuration.ExecuteMacroDelayMaxSeconds = executeMacroDelayMax;
                        Service.Configuration.Save();
                    }
                }
            }
        }

        public void AfterClickSynthesisOption()
        {
            // auxillary variables to allow for error checking
            float clickSynthesizeDelayMin = Service.Configuration.ClickSynthesizeDelayMinSeconds;
            float clickSynthesizeDelayMax = Service.Configuration.ClickSynthesizeDelayMaxSeconds;


            ImGui.Checkbox("Wait after clicking synthesize?", ref Service.Configuration.ClicksynthesizeDelay);

            if (Service.Configuration.ClicksynthesizeDelay)
            {
                ImGui.Dummy(new Vector2(25, 0));
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.CalcTextSize(clickSynthesizeDelayMin.ToString()).X + 20);
                if (ImGui.InputFloat("Min. (s)##ClickSynthesizDelayMin", ref clickSynthesizeDelayMin, 0, 0, "%.1f"))
                {
                    if (clickSynthesizeDelayMin > 0 && clickSynthesizeDelayMin <= clickSynthesizeDelayMax)
                    {
                        Service.Configuration.ClickSynthesizeDelayMinSeconds = clickSynthesizeDelayMin;
                        Service.Configuration.Save();
                    }
                }
                ImGui.SetNextItemWidth(ImGui.CalcTextSize(clickSynthesizeDelayMax.ToString()).X + 20);
                ImGui.SameLine();
                if (ImGui.InputFloat("Max. (s)##ClickSynthesizeDelayMax", ref clickSynthesizeDelayMax, 0, 0, "%.1f"))
                {
                    if (clickSynthesizeDelayMax > 0 && clickSynthesizeDelayMin <= clickSynthesizeDelayMax)
                    {
                        Service.Configuration.ClickSynthesizeDelayMaxSeconds = clickSynthesizeDelayMax;
                        Service.Configuration.Save();
                    }
                }
            }
        }

    }
}
