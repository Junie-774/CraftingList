﻿using CraftingList.Crafting;
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
        readonly private IEnumerable<Recipe?> craftableItems;
        readonly private List<string> craftableNames;


        // Two separate lists because we want to present an empty option for a new list entry, but not present an empty option for an existing entry.
        public string Name => "CraftingList";

        private readonly CraftingList plugin;


        public CraftingListTab(CraftingList plugin)
        {
            this.plugin = plugin;
            EntryListTable = new(plugin.Crafter);
            craftableNames = new List<string>
            {
                ""
            };

            craftableItems = Service.DataManager.GetExcelSheet<Recipe>()!;

            foreach (var item in craftableItems)
            {
                craftableNames.Add(item!.ItemResult.Value!.Name);
            }
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

            ImGui.Text($"Estimated time to complete list:\n~{EntryListTable.EstimatedTime.Hours}h, {EntryListTable.EstimatedTime.Minutes}m, {EntryListTable.EstimatedTime.Seconds}s.");

            ImGui.NewLine();
            if (ImGui.Button("Craft!"))
                plugin.Crafter.CraftAllItems();
            
            ImGui.SameLine();

            if (ImGui.Button("Cancel"))
                plugin.Crafter.Cancel("Cancelling craft...", false);
            ImGuiAddons.EndGroupPanel();


            ImGui.NextColumn();

            if (!plugin.Crafter.IsRunning())
            {
                ImGuiAddons.BeginGroupPanel("Ingredient Summary", new Vector2(-1, -1));
                EntryListTable.IngredientSummary.DisplayListings();
                ImGuiAddons.EndGroupPanel();
            }
            ImGui.Columns(1);
            
        }

   

        public void Dispose()
        {

        }
    }
}