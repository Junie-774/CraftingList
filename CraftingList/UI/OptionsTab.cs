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
            ImGuiAddons.BeginGroupPanel("Options", new Vector2(-1, 0));
            if (ImGui.TreeNodeEx("Crafting Options", ImGuiTreeNodeFlags.FramePadding))
            {
                CraftingOptionsSection();
                ImGui.TreePop();
            }

            if (ImGui.TreeNodeEx("Macro options", ImGuiTreeNodeFlags.FramePadding))
            {

                MacroOptionsSection();
                ImGui.TreePop();
            }
            ImGuiAddons.EndGroupPanel();
        }

        public void CraftingOptionsSection()
        {
            ImGui.SetNextItemWidth(300 - ImGui.CalcTextSize("Repair threshold: ").X);
            if (ImGui.SliderInt("Repair Threshold##RepairThreshold", ref Service.Configuration.RepairThresholdPercent, 0, 99))
                Service.Configuration.Save();


            if (ImGui.Checkbox("Only repair if durability for all items is below 99?", ref Service.Configuration.OnlyRepairIfBelow99))
                Service.Configuration.Save();


            SoundEffectOption();


            if (ImGuiAddons.BoundedAutoWidthInputInt("Timeout on Waiting for Menus (ms)", ref Service.Configuration.AddonTimeout, 500, 50000))
                Service.Configuration.Save();
        }

        public void MacroOptionsSection()
        {
            ImGui.Checkbox("Ignore <wait.x> and use actions as soon as available?", ref Service.Configuration.SmartWait);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Uses the next action as soon as possible.\nMacros can't complete the craft early if this option is enabled.");
            }

            int maxRetries = Service.Configuration.MaxMacroCommandTimeoutRetries;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize(maxRetries.ToString()).X + 16);
            if (ImGui.InputInt("Number of retries allowed for macro actions before giving up?", ref maxRetries, 0, 0))
            {
                if (maxRetries > 0)
                    Service.Configuration.MaxMacroCommandTimeoutRetries = maxRetries;
            }

            AfterClickSynthesisOption();

            AfterExecuteMacroOption();
        }

        public void SoundEffectOption()
        {
            // auxillary variables to allow for error checking
            int completeSoundEffect = Service.Configuration.SoundEffectListComplete;
            int cancelSoundEffect = Service.Configuration.SoundEffectListCancel;


            ImGui.Checkbox("Play Sound effect when crafting terminates?", ref Service.Configuration.AlertOnTerminate);
            if (Service.Configuration.AlertOnTerminate)
            {
                ImGui.Dummy(new Vector2(25, 0));
                ImGui.SameLine();

                if (ImGuiAddons.BoundedAutoWidthInputInt("List Complete Sound Effect", ref Service.Configuration.SoundEffectListComplete, 1, 16))
                    Service.Configuration.Save();

                ImGui.Dummy(new Vector2(25, 0));
                ImGui.SameLine();

                if (ImGuiAddons.BoundedAutoWidthInputInt("List Cancel Sound Effect", ref Service.Configuration.SoundEffectListCancel, 1, 16))
                    Service.Configuration.Save();
            }
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
