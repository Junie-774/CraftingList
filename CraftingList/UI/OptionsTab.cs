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

            ImGui.SetNextItemWidth(availWidth - 10 - ImGui.CalcTextSize("Repair threshold ").X);
            ImGui.SliderInt("Repair Threshold ", ref plugin.Configuration.RepairThresholdPercent, 0, 99);

            ImGui.Checkbox("Only repair if durability is below 99?", ref plugin.Configuration.OnlyRepairIfBelow99);
            ImGui.NewLine();

            // auxillary variables to allow for error checking
            int completeSoundEffect = plugin.Configuration.SoundEffectListComplete;
            int cancelSoundEffect = plugin.Configuration.SoundEffectListCancel;

            ImGui.Checkbox("Play Sound effect when crafting terminates?", ref plugin.Configuration.AlertOnTerminate);
            if (plugin.Configuration.AlertOnTerminate)
            {
                ImGui.PushItemWidth(60);
                ImGui.Dummy(new Vector2(50f, 0));
                ImGui.SameLine();
                if (ImGui.InputInt("List Complete Sound Effect", ref completeSoundEffect, 0))
                {
                    if (completeSoundEffect >= 1 && completeSoundEffect <= 16) plugin.Configuration.SoundEffectListComplete = completeSoundEffect;
                }

                ImGui.Dummy(new Vector2(50f, 0));
                ImGui.SameLine();
                if (ImGui.InputInt("List Cancelled Sound Effect", ref cancelSoundEffect, 0))
                {
                    if (cancelSoundEffect >= 1 && cancelSoundEffect <= 16) plugin.Configuration.SoundEffectListCancel = cancelSoundEffect;
                }
                ImGui.PopItemWidth();
            }
            ImGui.NewLine();

            int extraTimeout = plugin.Configuration.MacroExtraTimeoutMs;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize("0000000").X);
            if (ImGui.InputInt("Extra Timeout on Macros (ms)", ref extraTimeout, 0))
            {
                if (extraTimeout > 0) plugin.Configuration.MacroExtraTimeoutMs = extraTimeout;
            }

            int addonTimeout = plugin.Configuration.AddonTimeout;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize("0000000").X);
            if (ImGui.InputInt("Timeout on Waiting for Menus (ms)", ref addonTimeout, 0))
            {
                if (addonTimeout > 0) plugin.Configuration.AddonTimeout = addonTimeout;
            }
            ImGui.NextColumn();
            ImGui.Columns(1); 
        }

        void ITab.OnConfigChange()
        {
            
        }
    }
}
