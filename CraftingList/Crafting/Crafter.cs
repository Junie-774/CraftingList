using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Game.Text;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static CraftingList.SeFunctions.SeInterface;

namespace CraftingList.Crafting
{
    public class Crafter
    {
        private bool m_running = false;

        private SeInterface seInterface;
        private Configuration configuration;
        public Crafter(SeInterface seInterface, Configuration config)
        {
            this.seInterface = seInterface;
            configuration = config;
        }

        public Task<bool> CraftAllItems()
        {
           

            m_running = true;
            return Task.Run(async () =>
            {
                var tokenSource = new CancellationTokenSource();
                var token = tokenSource.Token;

                uint lastUsedFood = 0;
                foreach (var entry in configuration.EntryList.ToList())
                {
                    await Task.Delay(1000);
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

                            if (!await OpenRecipeByItem((int)entry.ItemId))
                            {
                                PluginLog.Debug($"Open Recipe Note failed, stopping craft...");
                                Cancel();
                                break;
                            }

                            if(!await ClickSynthesize())
                            {
                                PluginLog.Debug($"Click Synthesize failed, stopping craft...");
                                Cancel();
                                break;
                            }

                            var res = ExecuteMacro(entry.Macro, isCollectible);
                            res.Wait();
                            entry.MaxCrafts--;
                        }
                    }
                    if (!m_running)
                    {
                        PluginLog.Information("Stopping execution...");
                        break;
                    }
                    entry.Complete = true;
                    if (!await ExitCrafting())
                    {
                        PluginLog.Debug($"Failed to exit crafting stance, stopping craft...");
                        Cancel();
                        break;
                    }
                }
                configuration.EntryList.RemoveAll(x => x.Complete || x.MaxCrafts == 0);
                TerminationAlert();
                return true;
            });
        }

        public async Task<int> ChangeJobs(DoHJob job)
        {
            PluginLog.Debug($"Changing jobs to {job}");
            seInterface.SwapToDOHJob(job);
            await Task.Delay(configuration.WaitDurations.AfterChangeJobs);
            return 0;
        }

        public async Task<bool> OpenRecipeByItem(int itemId)
        {
            if (seInterface.RecipeNote().IsVisible())
            {
                await Task.Delay(configuration.WaitDurations.AfterOpenCloseMenu); return true;
            }
            PluginLog.Debug($"Opening crafting log to item {itemId}");
            seInterface.RecipeNote().OpenRecipeByItemId(itemId);

            var task = await seInterface.WaitForAddon("RecipeNote", true, 5000);
            if (task == IntPtr.Zero)
            {
                return false;
            }
            await Task.Delay(configuration.WaitDurations.AfterOpenCloseMenu);
            return true;
        }

        public async Task<bool> ExitCrafting()
        {
            PluginLog.Debug($"Closing Recipe Note...");
            seInterface.ExecuteMacro(seInterface.CloseNoteMacro);
            var recipeNote = seInterface.WaitForCloseAddon("RecipeNote", true, 5000);
            recipeNote.Wait();
            if (recipeNote.IsCanceled)
            {
                return false;
            }

            PluginLog.Debug($"Closed Recipe Note.");
            await Task.Delay(configuration.WaitDurations.AfterExitCrafting);
            return true;
        }

        public async Task<int> ChangeFood(uint newFoodId)
        {
            PluginLog.Debug($"Changing food to {newFoodId}");
            if (newFoodId != 0)
            {
                PluginLog.Debug($"Eating food {newFoodId}...");
                seInterface.UseItem(newFoodId);
                PluginLog.Debug($"Ate Food.");
                await Task.Delay(configuration.WaitDurations.AfterEatFood);
                
            }
            else
            {
                PluginLog.Debug($"Removing food...");
                seInterface.RemoveFood();
                PluginLog.Debug($"Removed food.");
                await Task.Delay(configuration.WaitDurations.AfterClickOffFood);
            }
            return 0;
        }

        public async Task<bool> ClickSynthesize()
        {
            PluginLog.Debug($"Clicking Synthesize...");
            seInterface.RecipeNote().Synthesize();
            var res = await seInterface.WaitForAddon("Synthesis", true, 5000);
            if (res == IntPtr.Zero)
            {
                return false;
            }
            await Task.Delay(configuration.WaitDurations.AfterClickSynthesize);
            return true;
        }

        public async Task<bool> ExecuteMacro(CraftingMacro macro, bool collectible)
        {
            PluginLog.Debug($"Executing Macro {macro.MacroNum}");
            seInterface.ExecuteMacroByNumber(macro.MacroNum);
            var recipeNote = await seInterface.WaitForAddon("RecipeNote", true, macro.DurationSeconds * 1000 + configuration.WaitDurations.AfterCompleteMacroHQ + 5000);
            if (recipeNote == IntPtr.Zero)
            {
                return false;
            }
            await Task.Delay(configuration.WaitDurations.AfterOpenCloseMenu);
            //await Task.Delay(collectible ? configuration.WaitDurations.AfterCompleteMacroCollectible : configuration.WaitDurations.AfterCompleteMacroHQ);
            return true;
            if (collectible)
            {
                await Task.Delay(macro.DurationSeconds * 1000 + configuration.WaitDurations.AfterCompleteMacroCollectible);
            }
            else
            {
                await Task.Delay(macro.DurationSeconds * 1000 + configuration.WaitDurations.AfterCompleteMacroHQ);
            }
            return true; ;
        }

        public void TerminationAlert()
        {
            if (configuration.EntryList.Count == 0)
            {
                SendAlert("List complete!", configuration.SoundEffectListComplete);
            }
            else
            {
                SendAlert("Crafting stopped.", configuration.SoundEffectListCancel);
            }
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

        private void SendAlert(string message, int soundEffect)
        {
            var mac = new Macro(0, 0, "Alert", new string[] { "/echo <se." + soundEffect + ">", "/echo " + message });
            seInterface.ExecuteMacro(mac);
        }

        public unsafe bool NeedsRepair()
        {
            bool existsItemBelowThreshold = false;
            bool existsItemAbove100 = false;
            bool existsBrokenItem = false;
            for (int i = 0; i < 13; i++)
            {
                var condition = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems)->GetInventorySlot(i)->Condition;
                if (condition <= (ushort)30000 * configuration.RepairThresholdPercent / 100 || condition == 0)
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
            if (existsItemAbove100 && configuration.OnlyRepairIfBelow99) return false;
            return existsItemBelowThreshold;
        }

        public async Task<bool> Repair()
        {
            PluginLog.Debug($"Repairing...");
            PluginLog.Debug("Opening repair...");
            seInterface.ToggleRepairWindow();
            await Task.Delay(configuration.WaitDurations.AfterOpenCloseMenu);

            PluginLog.Debug("Clicking repair all...");
            seInterface.Repair().ClickRepairAll();
            await Task.Delay(configuration.WaitDurations.AfterOpenCloseMenu);

            PluginLog.Debug("Clicking confirm...");
            seInterface.SelectYesNo().ClickYes();
            await Task.Delay(configuration.WaitDurations.AfterRepairConfirm);

            PluginLog.Debug("Closing repair window...");
            seInterface.ToggleRepairWindow();
            await Task.Delay(configuration.WaitDurations.AfterOpenCloseMenu);

            PluginLog.Debug("Repaired!");
            return true;
        }

        public void Cancel()
        {
            DalamudApi.ChatGui.Print("CraftingList: Cancelling...");
            m_running = false;
        }
    }
}
