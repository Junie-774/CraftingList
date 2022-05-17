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
        public int RepairThresholdPercent = 99;
        public bool OnlyRepairIfBelow99 = true;
        public WaitDurationHelper WaitDurationHelper = new WaitDurationHelper();

        private bool m_running = false;
        public List<CListEntry> EntryList { get; set; }

        private SeInterface m_seInterface;

        public Crafter(SeInterface seInterface)
        {
            this.m_seInterface = seInterface;
            EntryList = new List<CListEntry>();
            this.RepairThresholdPercent = 99;
        }

        public Task<bool> CraftAllItems()
        {
            m_running = true;
            return Task.Run(async () =>
            {
                uint lastUsedFood = 0;
                foreach (var entry in EntryList.ToList())
                {
                    PluginLog.Debug($"Crafting {entry.MaxCrafts} {entry.Name}. Macro: {entry.Macro.Name}. FoodId: {entry.FoodId}");

                    if (!m_running) break;
                    var job = DalamudApi.DataManager.GetExcelSheet<Recipe>()!
                        .Where(recipe => recipe.ItemResult.Value!.RowId == entry.ItemId)
                        .First().CraftType.Value!.RowId;

                    bool isCollectible = DalamudApi.DataManager.GetExcelSheet<Item>()!
                        .Where(item => item.RowId == entry.ItemId)
                        .First().IsCollectable;

                    await ChangeJobs((DoHJob) job);

                    if (entry.Macro.Name == "(Quick Synth)")
                    {
                        //await OpenRecipeByItem((int) entry.ItemId);
                    }
                    else
                    {
                        while (entry.MaxCrafts > 0)
                        {
                            if (!m_running) break;

                            bool needToChangeFood = NeedToChangeFood(lastUsedFood, entry.FoodId).Result;
                            bool needToRepair = NeedsRepair();

                            PluginLog.Debug($"Last food: {lastUsedFood}, Curr food: {entry.FoodId}");
                            PluginLog.Debug($"Need change food: {needToChangeFood}");
                            PluginLog.Debug($"Need repair: {needToRepair}");

                            if (needToChangeFood || needToRepair)
                            {
                                await ExitCrafting();
                                if (needToChangeFood)
                                {
                                    await ChangeFood(entry.FoodId);
                                    lastUsedFood = entry.FoodId;
                                }
                                if (needToRepair)
                                {
                                    await Repair();
                                }
                            }

                            await OpenRecipeByItem((int)entry.ItemId);

                            await ClickSynthesize();

                            await ExecuteMacro(entry.Macro, isCollectible);
                            entry.MaxCrafts--;
                        }
                    }
                    if (!m_running)
                    {
                        PluginLog.Information("Stopping execution...");
                        break;
                    }
                    entry.Complete = true;
                    await ExitCrafting();
                }
                EntryList.RemoveAll(x => x.Complete || x.MaxCrafts == 0);
                PluginLog.Information("Crafting Complete!");
                return true;
            });
        }

        public async Task<int> ChangeJobs(DoHJob job)
        {
            m_seInterface.SwapToDOHJob(job);
            await Task.Delay(WaitDurationHelper.AfterChangeJobs);
            return 0;
        }

        public async Task<int> OpenRecipeByItem(int itemId)
        {
            PluginLog.Debug($"Opening crafting log to item {itemId}");
            m_seInterface.RecipeNote().OpenRecipeByItemId(itemId);
            await Task.Delay(WaitDurationHelper.AfterOpenCloseMenu);
            return 0;
        }

        public async Task<int> ExitCrafting()
        {
            PluginLog.Debug($"Closing Recipe Note...");
            m_seInterface.ExecuteMacro(m_seInterface.CloseNoteMacro);
            PluginLog.Debug($"Closed Recipe Note.");
            await Task.Delay(WaitDurationHelper.AfterExitCrafting);
            return 0;
        }

        public async Task<int> ChangeFood(uint newFoodId)
        {
            PluginLog.Debug($"Changing food to {newFoodId}");
            if (newFoodId != 0)
            {
                PluginLog.Debug($"Eating food {newFoodId}...");
                m_seInterface.UseItem(newFoodId);
                PluginLog.Debug($"Ate Food.");
                await Task.Delay(WaitDurationHelper.AfterEatFood);
                
            }
            else
            {
                PluginLog.Debug($"Removing food...");
                m_seInterface.RemoveFood();
                PluginLog.Debug($"Removed food.");
                await Task.Delay(WaitDurationHelper.AfterClickOffFood);
            }
            return 0;
        }

        public async Task<int> ClickSynthesize()
        {
            PluginLog.Debug($"Clicking Synthesize...");
            m_seInterface.RecipeNote().Synthesize();
            await Task.Delay(WaitDurationHelper.AfterClickSynthesize);
            return 0;
        }

        public async Task<int> ExecuteMacro(CraftingMacro macro, bool collectible)
        {
            PluginLog.Debug($"Executing Macro {macro.MacroNum}");
            m_seInterface.ExecuteMacroByNumber(macro.MacroNum);
            if (collectible)
            {
                await Task.Delay(macro.DurationSeconds * 1000 + WaitDurationHelper.AfterCompleteMacroCollectible);
            }
            else
            {
                await Task.Delay(macro.DurationSeconds * 1000 + WaitDurationHelper.AfterCompleteMacroHQ);
            }
            return 0;
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

        public unsafe bool NeedsRepair()
        {
            bool existsItemBelowThreshold = false;
            bool existsItemAbove100 = false;
            bool existsBrokenItem = false;
            for (int i = 0; i < 13; i++)
            {
                var condition = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems)->GetInventorySlot(i)->Condition;
                if (condition <= (ushort)30000 * RepairThresholdPercent / 100 || condition == 0)
                {
                    existsItemBelowThreshold = true;
                }
                if (condition > 30000)
                {
                    existsItemAbove100 = true;
                }
                if (condition == 0)
                {
                    existsBrokenItem = true;
                }
            }

            if (existsBrokenItem) return true;
            if (existsItemAbove100 && OnlyRepairIfBelow99) return false;
            return existsItemBelowThreshold;
        }

        public async Task<bool> Repair()
        {
            PluginLog.Debug($"Repairing...");
            PluginLog.Debug("Opening repair...");
            m_seInterface.ToggleRepairWindow();
            await Task.Delay(WaitDurationHelper.AfterOpenCloseMenu);

            PluginLog.Debug("Clicking repair all...");
            m_seInterface.Repair().ClickRepairAll();
            await Task.Delay(WaitDurationHelper.AfterOpenCloseMenu);

            PluginLog.Debug("Clicking confirm...");
            m_seInterface.SelectYesNo().ClickYes();
            await Task.Delay(WaitDurationHelper.AfterRepairConfirm);

            PluginLog.Debug("Closing repair window...");
            m_seInterface.ToggleRepairWindow();
            await Task.Delay(WaitDurationHelper.AfterOpenCloseMenu);

            PluginLog.Debug("Repaired!");
            return true;
        }

        public void Cancel()
        {
            m_running = false;
        }
    }
}
