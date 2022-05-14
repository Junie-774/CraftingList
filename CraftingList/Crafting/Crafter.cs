using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CraftingList.SeFunctions.SeInterface;

namespace CraftingList.Crafting
{
    public class Crafter
    {
        private bool m_running = false;
        public List<CListEntry> EntryList { get; set; }

        private SeInterface m_seInterface;

        public Crafter(SeInterface seInterface)
        {
            this.m_seInterface = seInterface;
            EntryList = new List<CListEntry>();
        }

        public Task<bool> CraftAllItems()
        {
            m_running = true;
            return Task.Run(async () =>
            {
                uint lastUsedFood = 0;
                foreach (var entry in EntryList)
                {
                    if (!m_running) break;
                    var job = DalamudApi.DataManager.GetExcelSheet<Recipe>()!
                        .Where(recipe => recipe.ItemResult.Value!.RowId == entry.ItemId)
                        .First().CraftType.Value!.RowId;
                    m_seInterface.SwapToDOHJob((DoHJob)job);
                    await Task.Delay(1500);
                    m_seInterface.RecipeNote().OpenRecipeByItemId((int)entry.ItemId);
                    await Task.Delay(1500);


                    for (int i = 0; i < entry.MaxCrafts; i++)
                    {
                        if (!m_running) break;
                        bool needToChangeFood = NeedToChangeFood(lastUsedFood, entry.FoodId).Result;
                        bool needToRepair = NeedsRepair();
                        PluginLog.Information($"Change food: {needToChangeFood}");
                        PluginLog.Information($"Repair: {needToRepair}");
                        if (needToChangeFood || needToRepair)
                        {
                            m_seInterface.ExecuteMacro(m_seInterface.CloseNoteMacro);
                            await Task.Delay(4000);
                            if (needToChangeFood)
                            {
                                if (entry.FoodId != 0)
                                {
                                    m_seInterface.UseItem(entry.FoodId);
                                    lastUsedFood = entry.FoodId;
                                    await Task.Delay(3000);
                                }
                                else
                                {
                                    m_seInterface.RemoveFood();
                                    lastUsedFood = 0;
                                    await Task.Delay(1500);
                                }
                            }
                            if (needToRepair)
                            {
                                await Repair();
                            }

                            m_seInterface.RecipeNote().OpenRecipeByItemId((int)entry.ItemId);
                            await Task.Delay(1500);

                        }


                        m_seInterface.RecipeNote().Synthesize();
                        await Task.Delay(2000);

                        PluginLog.Information("EXECUTING MACRO");
                        m_seInterface.ExecuteMacroByNumber(entry.Macro.MacroNum);
                        await Task.Delay(entry.Macro.DurationSeconds * 1000 + 4000);

                        await Task.Delay(1000);


                    }

                    entry.Complete = true;
                }
                EntryList.RemoveAll(x => x.Complete);
                PluginLog.Information("CRAFTING DONE");
                return true;
            });
        }

        public static async Task<bool> NeedToChangeFood(uint lastFood, uint currEntryFoodId)
        {
            bool hasFood = false;
            while (DalamudApi.ClientState.LocalPlayer == null)
            {
                await Task.Delay(20);
            }
            foreach (var status in DalamudApi.ClientState.LocalPlayer.StatusList)
            {
                if (status == null) continue;

                if (status.StatusId == 48)
                {
                    hasFood = true;
                }
            }
            // If we need to refresh
            if (lastFood == currEntryFoodId && currEntryFoodId != 0)
            {
                if (hasFood) return false;
                return true;
            }
            // Need to have food, AND lastFood isn't the food we need
            if (currEntryFoodId != 0)
            {
                return true;
            }
            //CurrFood == 0.
            var foodEntry = DalamudApi.DataManager.GetExcelSheet<ItemFood>()!
                        .Where(x => x.RowId == DalamudApi.DataManager.GetExcelSheet<Item>()!.Where(x => x.RowId == lastFood).First().ItemAction.Value!.DataHQ[1])
                        .First();

            var stat1 = foodEntry.UnkData1[0].BaseParam;
            var stat2 = foodEntry.UnkData1[1].BaseParam;
            return stat1 == 70 || stat2 == 70;
        }

        public static unsafe bool NeedsRepair()
        {
            for (int i = 0; i < 13; i++)
            {
                if (InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems)->GetInventorySlot(i)->Condition < 29700)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> Repair()
        {
            PluginLog.Debug("Opening repair...");
            m_seInterface.OpenRepair();
            await Task.Delay(2000);
            PluginLog.Debug("Clicking repair all...");
            m_seInterface.Repair().ClickRepairAll();
            await Task.Delay(2000);
            m_seInterface.SelectYesNo().ClickYes();
            await Task.Delay(4000);
            m_seInterface.OpenRepair();
            await Task.Delay(2000);
            return true;
        }

        public void Cancel()
        {
            m_running = false;
        }
    }
}
