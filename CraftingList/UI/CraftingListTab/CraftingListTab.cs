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
        public EntryListTable EntryListTable = new();
        readonly private IEnumerable<Recipe?> craftableItems;
        readonly private List<string> craftableNames;


        // Two separate lists because we want to present an empty option for a new list entry, but not present an empty option for an existing entry.
        public string Name => "CraftingList";

        private readonly CraftingList plugin;


        public CraftingListTab(CraftingList plugin)
        {
            this.plugin = plugin;
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
            EntryListTable.DrawEntryTable();
            EntryListTable.DrawNewListEntry();
            //DrawHQMatSelection();
            ImGui.NewLine();
            ImGui.Columns(2);
            if (ImGui.Button("Craft!"))
            {
                plugin.Crafter.CraftAllItems();
            }
            ImGui.SameLine();

            if (ImGui.Button("Cancel"))
            {
                plugin.Crafter.Cancel("Cancelling craft...", false);
            }
            ImGui.NewLine();
            ImGui.Checkbox("##HasCraftTimeout", ref plugin.Configuration.HasCraftTimeout);
            ImGui.SameLine();
            ImGui.Text(" Stop after ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(25);
            ImGui.InputInt("##CraftTimeout", ref plugin.Configuration.CraftTimeoutMinutes, 0, 0);
            ImGui.SameLine();
            ImGui.Text(" Minutes");

            ImGui.NextColumn();

            EntryListTable.IngredientSummary.DisplayListings();

            ImGui.Columns(1);
        }

   

        public void Dispose()
        {

        }
    }
}
