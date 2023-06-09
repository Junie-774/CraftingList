using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Game.ClientState.Conditions;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.Crafting
{
    internal class CraftHelper
    {
        private readonly static Random randomDelay = new(DateTime.Now.Millisecond);

        public static async Task<int> ChangeJobs(int job)
        {
            PluginLog.Debug($"[CraftHelper.ChangeJobs()] Changing jobs to {SeInterface.DoHJobs[job]}");
            SeInterface.SwapToDOHJob(job);
            await Task.Delay(Service.Configuration.WaitDurations.AfterChangeJobs);
            return 0;
        }

        public static async Task<bool> OpenRecipeByRecipe(int recipeId)
        {
            if (SeInterface.RecipeNote().IsVisible())
            {
                await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);
                return true;
            }

            //We close the recipe note when the job starts, so if it's open, it's open because
            // we opened it to the right item.
            PluginLog.Debug($"[CraftHelper.OpenRecipeByRecipe()] Opening crafting log to recipe {recipeId}");
            SeInterface.RecipeNote().OpenRecipeByRecipeId((int) Service.Recipes[recipeId].RowId);

            if (!await WaitForAddon("RecipeNote", true, Service.Configuration.AddonTimeout))
                return false;

            await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);

            return true;
        }
        public static async Task<bool> OpenRecipeByItem(int itemId)
        {

            //We close the recipe note when the job starts, so if it's open, it's open because
            // we opened it to the right item.
            if (SeInterface.RecipeNote().IsVisible())
            {
                await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);
                return true;
            }

            PluginLog.Debug($"[CraftHelper.OpenRecipeByItem()] Opening crafting log to item {itemId}");
            SeInterface.RecipeNote().OpenRecipeByItemId(itemId);

            if (!await WaitForAddon("RecipeNote", true, Service.Configuration.AddonTimeout))
                return false;

            await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);

            return true;
        }

        public static async Task<bool> ClickSynthesize()
        {
            PluginLog.Debug($"[CraftHelper.ClickSynthesize()] Clicking Synthesize...");

            SeInterface.RecipeNote().Synthesize();

            if (!await WaitForAddon("Synthesis", true, Service.Configuration.AddonTimeout))
                return false;

            await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);
            if (!SeInterface.IsAddonAvailable((IntPtr) SeInterface.Synthesis(), true))
                return false;

            await Task.Delay(randomDelay.Next((int)Service.Configuration.ClickSynthesizeDelayMinSeconds * 1000,
                                              (int)Service.Configuration.ClickSynthesizeDelayMaxSeconds * 1000)
            );
            return true;
        }

        public static async Task<bool> ClickQuickSynthesize()
        {
            PluginLog.Debug($"[CraftHelper.ClickQuickSynthesize()] Clicking Quick Synthesize...");

            SeInterface.RecipeNote().QuickSynthesize();

            if (!await WaitForAddon("SynthesisSimpleDialog", true, Service.Configuration.AddonTimeout))
                return false;

            await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);

            await Task.Delay(randomDelay.Next((int)Service.Configuration.ClickSynthesizeDelayMinSeconds * 1000,
                                              (int)Service.Configuration.ClickSynthesizeDelayMaxSeconds * 1000));

            return true;
        }

        public static async Task<bool> EnterQuickSynthAmount(int amount)
        {
            PluginLog.Debug("[CraftHelper.EnterQuickSynthAmount()] Entering Quick Synth amount.");

            var quickSynthDialog = SeInterface.SynthesisSimpleDialog();

            if (!SeInterface.IsAddonAvailable((IntPtr) quickSynthDialog, true))
            {
                PluginLog.Error("[CraftHelper.EnterQuickSynthAmount()] Called when SynthesisSimpleDialog was not available.");
                return false;
            }

            quickSynthDialog.SetAmount(amount);

            await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);

            return true;
        }

        public static async Task<bool> CheckQuickSynthHQMatBox()
        {
            PluginLog.Debug("[CraftHelper.CheckQuicksynthHQMatBox()] Checking the 'Use HQ materials' box.");

            var quickSynthDialog = SeInterface.SynthesisSimpleDialog();
            if (!SeInterface.IsAddonAvailable((IntPtr) quickSynthDialog, true))
            {
                PluginLog.Error("[CraftHelper.CheckQuicksynthHQMatBox()] Called when SynthesisSimpleDialog was not available.");
                return false;
            }

            quickSynthDialog.SetHQMats(true);

            await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);

            return true;
        }
        public static async Task<bool> StartQuickSynth()
        {
            PluginLog.Debug("[CraftHelper.StartQuickSynth()] Starting Quick synth from dialog.");

            var quickSynthDialog = SeInterface.GetUiObject("SynthesisSimpleDialog");

            if (!SeInterface.IsAddonAvailable(quickSynthDialog, true))
            {
                PluginLog.Error("[CraftHelper.StartQuickSynth()] Called when SynthesisSimpleDialog was not available.");
                return false;
            }

            ((PtrSynthesisSimpleDialog)quickSynthDialog).StartSynthesis();

            if (!await WaitForAddon("SynthesisSimple", true, Service.Configuration.AddonTimeout))
                return false;

            await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);
            return true;
        }

        public static bool NeedToChangeConsumable(uint lastFood, uint currEntryFoodId, bool medicine)
        {
            PluginLog.Debug($"last: {lastFood} | curr {currEntryFoodId}");
            bool hasFood = medicine ? SeInterface.HasStatusID(49) : SeInterface.HasStatusID(48);

            // If we need to refresh
            if (lastFood == currEntryFoodId && currEntryFoodId != 0)
            {
                if (hasFood)
                {
                    PluginLog.Debug("  has food");
                    return false;
                }
                return true;
            }
            // Need to have food, AND lastFood isn't the food we need
            if (currEntryFoodId != 0)
            {
                return true;
            }
            //CurrFood == 0.
            var foodEntry = Service.DataManager.GetExcelSheet<ItemFood>()!
                        .Where(x => x.RowId == Service.DataManager.GetExcelSheet<Item>()!.Where(x => x.RowId == lastFood || x.RowId == lastFood - 1000000).First().ItemAction.Value!.DataHQ[1])
                        .First();

            var stat1 = foodEntry.UnkData1[0].BaseParam;
            var stat2 = foodEntry.UnkData1[1].BaseParam;
            return stat1 == 70 || stat2 == 70;
        }

        public static async Task<bool> ExitCrafting()
        {
            PluginLog.Debug($"[CraftHelper.ExitCrafting()] Closing Recipe Note...");

            SeInterface.ExecuteFFXIVInternalMacro(SeInterface.Instance.CloseNoteMacro);
            if (!await WaitForCloseAddon("RecipeNote", true, Service.Configuration.AddonTimeout))
                return false;

            if (!await Service.WaitForCondition(Dalamud.Game.ClientState.Conditions.ConditionFlag.Crafting, false, 5000))
            {
                PluginLog.Error("[CraftHelper.ExitCrafting()] Took too long to exit crafting stance");
                return false;
            }
            return true;
        }

        public static async Task<bool> ChangeFood(uint newFoodId, bool medication)
        {
            PluginLog.Debug($"[CraftHelper.ChangeFood()] Changing food/medication to {newFoodId}");
            string relevantStatus = medication ? Localization.GetMedicatedString() : Localization.GetWellFedStatusString();

            SeInterface.Statusoff(relevantStatus);

            await Task.Delay(Service.Configuration.WaitDurations.AfterClickOffFood);
            if (newFoodId != 0)
            {
                PluginLog.Debug($"[CraftHelper.ChangeFood()] Consuming food/medication {newFoodId}...");

                SeInterface.UseItem(newFoodId);

                await Task.Delay(Service.Configuration.WaitDurations.AfterEatFood);

                if (medication) return SeInterface.HasStatusID(49);
                else return SeInterface.HasStatusID(48);
            }
            else
            {

                if (medication) return !SeInterface.HasStatusID(49);
                else return !SeInterface.HasStatusID(48);
            }
        }
        public static async Task<bool> FillHQMats(CListEntry entry)
        {
            PluginLog.Debug("[CraftHelper.FillHQMats()] Selecting HQ Mats...");

            if (entry.PrioHQMats)
            {
                SeInterface.RecipeNote().FillHQ(true);
                await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);
                return true;
            }
            if (entry.HQSelection.Length != 6)
            {
                PluginLog.Error($"[CraftHelper.FillHQMats()] Bad hqSelection parameter passed to CraftHelper.FillHQMats. Should be length 6, was given length {entry.HQSelection.Length}");
                return false;
            }

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < entry.HQSelection[i]; j++)
                {
                    await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);
                    SeInterface.RecipeNote().ClickHQ(i);
                }
            }
            return true;
        }

        public static async Task<bool> Repair()
        {
            PluginLog.Debug($"[CraftHelper.Repair()] Repairing...");

            if (!SeInterface.IsAddonAvailable(SeInterface.GetUiObject("Repair"), true)) {
                SeInterface.ToggleRepairWindow();
            }

            if (!await WaitForAddon("Repair", true, Service.Configuration.AddonTimeout))
                return false;

            await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);

            PluginLog.Debug("[CraftHelper.Repair()] Clicking repair all...");
            SeInterface.Repair().ClickRepairAll();
            if (!await WaitForAddon("SelectYesno", true, Service.Configuration.AddonTimeout))
                return false;

            await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);


            //Occupied39, conveniently, signals that the player is busy repairing. Or at least it flags as on while the player repairs
            PluginLog.Debug("[CraftHelper.Repair()] Clicking confirm...");
            SeInterface.SelectYesNo().ClickYes();

            if (!await Service.WaitForCondition(ConditionFlag.Occupied39, false, 5000))
            {
                PluginLog.Error($"[CraftHelper.Repair()] Waiting for repair to finish timed out.");
            }

            PluginLog.Debug("[CraftHelper.Repair()] Closing repair window...");
            SeInterface.ToggleRepairWindow();
            if (!await WaitForCloseAddon("Repair", true, Service.Configuration.AddonTimeout))
                return false;

            PluginLog.Debug("[CraftHelper.Repair()] Repaired!");
            return true;
        }
        public static void CancelEntry(CListEntry entry, string cancelMessage, bool error)
        {
            if (entry.running)
            {
                if (error) Service.ChatManager.PrintError(cancelMessage);
                else Service.ChatManager.PrintError(cancelMessage);
            }
            entry.running = false;
        }

        public static Task<bool> WaitForAddon(string addonName, bool requiresVisible, int timeoutMS)
        {
            PluginLog.Debug($"[WaitForAddon] Waiting for addon '{addonName}'.");
            try { SeInterface.WaitForAddon(addonName, requiresVisible, timeoutMS).Wait(); }
            catch
            {
                PluginLog.Debug($"[WaitForAddon] Waiting for addon '{addonName}' to open timed out.");
                return Task.FromResult(false);
            }
            PluginLog.Debug("[WaitForAddon] Waiting done!");
            return Task.FromResult(true);
        }

        public static Task<bool> WaitForCloseAddon(string addonName, bool requiresVisible, int timeoutMS)
        {
            PluginLog.Debug($"[WaitForAddon] Waiting for addon '{addonName}' to close.");

            try { SeInterface.WaitForCloseAddon(addonName, requiresVisible, timeoutMS).Wait(); }
            catch
            {
                PluginLog.Debug($"[WaitForAddon] Waiting for addon '{{addonName}}' to open timed out.");
                return Task.FromResult(false);
            }
            PluginLog.Debug($"[WaitForAddon] Wait completed for '{addonName}'");
            return Task.FromResult(true);
        }

        public static async Task<bool> CloseQuickSynthWindow()
        {
            ((PtrSynthesisSimple)SeInterface.GetUiObject("SynthesisSimple")).ClickQuit();

            // Add 3500 to allow for a full quick synth to complete because the game waits for that synth to finish before closing.
            if (!await CraftHelper.WaitForCloseAddon("SynthesisSimple", true, Service.Configuration.AddonTimeout + 3500))
            {
                PluginLog.Debug($"[CraftHelper.CloseQuickSynthWindow()] A problem occured while trying to exit the Quick Synth window for entryk.", true);
                return false;
            }
            await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);

            return true;
        }

        public static unsafe bool NeedsRepair()
        {
            bool existsItemBelowThreshold = false;
            bool existsItemAbove100 = false;
            bool existsBrokenItem = false;
            for (int i = 0; i < 13; i++)
            {

                var condition = SeInterface.InventoryManager()->GetInventoryContainer(InventoryType.EquippedItems)->GetInventorySlot(i)->Condition;

                //30000 is the '100%' threshold for durability ingame.
                if (condition <= 30000 * Service.Configuration.RepairThresholdPercent / 100 || condition == 0)
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

            if (existsItemAbove100 && Service.Configuration.OnlyRepairIfBelow99) return false;

            return existsItemBelowThreshold;
        }

        public static bool IsCrafting()
            => Service.Condition[ConditionFlag.Crafting] && !Service.Condition[ConditionFlag.PreparingToCraft];
    }
}
