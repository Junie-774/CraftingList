using CraftingList.Crafting;
using CraftingList.Crafting.Macro;
using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.UI.CraftingListTab
{
    public class CraftingListTab : ITab, IDisposable
    {
        public EntryListTable EntryListTable;
        public string Name => "Crafting";

        private readonly CraftingList plugin;

        public CraftingListTab(CraftingList plugin)
        {
            this.plugin = plugin;
            EntryListTable = new(plugin.Crafter);
        }
        public void Draw()
        {
            EntryListTable.DrawEntries();
            EntryListTable.DrawNewEntry();

            ImGui.NewLine();

            ImGui.Columns(2, "Craft-IngedientSummary", false);
            ImGui.SetColumnWidth(0, (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) * 0.4f);
            ImGui.SetColumnWidth(1, (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) * 0.6f);

            ImGuiAddons.BeginGroupPanel("Craft", new Vector2(-1, -1));

            ImGui.Checkbox("##HasCraftTimeout", ref plugin.Configuration.HasCraftTimeout);
            ImGui.SameLine();
            ImGui.Text(" Stop after ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(25);
            ImGui.InputInt("##CraftTimeout", ref plugin.Configuration.CraftTimeoutMinutes, 0, 0);
            ImGui.SameLine();
            ImGui.Text(" Minutes");

            ImGui.Text($"Estimated time to complete list:\n{EntryListTable.FormatTime(EntryListTable.EstimatedTime)}");

            ImGui.NewLine();
            if (ImGui.Button("Craft!"))
                plugin.Crafter.CraftAllItems();
            
            ImGui.SameLine();

            if (ImGui.Button("Cancel"))
                plugin.Crafter.Cancel("Cancelling craft...", false);
            ImGuiAddons.EndGroupPanel();


            ImGui.NextColumn();

            ImGuiAddons.BeginGroupPanel("Ingredient Summary", new Vector2(-1, -1));
            EntryListTable.IngredientSummary.DisplaySummaries();
            ImGuiAddons.EndGroupPanel();
            ImGui.Columns(1);
            
        }

   

        public void Dispose()
        {
            EntryListTable.Dispose();
        }
    }
}
