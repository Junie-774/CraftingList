using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Linq;
using System.Threading.Tasks;
using static CraftingList.SeFunctions.SeInterface;

namespace CraftingList.Crafting
{
    public class Crafter
    {
        private Random randomDelay = new Random(DateTime.Now.Millisecond);

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
            if (m_running)
            {
                DalamudApi.ChatGui.PrintError("[CraftingList] A craft is already running!");
                return Task.FromResult(false);
            }
            if (!IsListValid())
            {
                DalamudApi.ChatGui.PrintError("[CraftingList] An error occured validating the list. Please make sure all of your amounts are correct, and that all of your macros are selected.");
                return Task.FromResult(false);
            }

            m_running = true;
            return Task.Run(async () =>
            {
                DalamudApi.ChatGui.Print("[CraftingList] Starting crafting!");

                var craftStart = DateTime.Now;

                await ExitCrafting();
                await seInterface.WaitForCloseAddon("RecipeNote", true, configuration.AddonTimeout);

                uint lastUsedFood = 0;
                uint lastUsedMedicine = 0;

                PluginLog.Debug($"Last food: {lastUsedFood}");
                foreach (var entry in configuration.EntryList.ToList())
                {
                    if (!m_running) break;
                    if (configuration.HasCraftTimeout && configuration.CraftTimeoutMinutes > 0 && (DateTime.Now - craftStart).TotalMinutes >= configuration.CraftTimeoutMinutes) break;
                    entry.running = true;

                    var macro = configuration.Macros[entry.MacroIndex];
                    if (!TimedIngameMacro.isValidMacro(macro))
                    {
                        Cancel($"Error: Macro \"{macro.Name}\" was invalid. This is likely an internal error x.x", true);
                    }
                    await Task.Delay(1000);                 
                                        
                    PluginLog.Debug($"Crafting {entry.NumCrafts} {entry.Name}. Macro: {macro.Name}. FoodId: {macro.FoodID}");

                    var job = DalamudApi.DataManager.GetExcelSheet<Recipe>()!
                        .Where(recipe => recipe.ItemResult.Value!.RowId == entry.ItemId)
                        .First().CraftType.Value!.RowId;             

                    await ChangeJobs((DoHJob)job);

                    if (macro.Name == "(Quick Synth)")
                    {
                        //await OpenRecipeByItem((int) entry.ItemId);
                    }
                    else
                    {
                        while (entry.NumCrafts == "max" || int.Parse(entry.NumCrafts) > 0)
                        {
                            if (!m_running || !entry.running) break;

                            var repairAndConsumablesResult = await RepairAndApplyConsumables(macro, lastUsedFood, lastUsedMedicine);
                            if (!repairAndConsumablesResult.Item1)
                            {
                                break;
                            }
                            lastUsedFood = repairAndConsumablesResult.Item2;
                            lastUsedMedicine = repairAndConsumablesResult.Item3;

                            if (!await CraftOneItem(entry))
                            {
                                break;
                            }
                            
                        }
                    }
                    if (!m_running)
                    {
                        break;
                    }

                    // Leave crafting stance after each entry in case we need to switch jobs. Could hypothetically stay in crafting stance if next entry uses the same job,
                    // but that's a pretty minor gain and this simplifies code flow.yea
                    if (!await ExitCrafting())
                    {
                        PluginLog.Debug($"Failed to exit crafting stance, stopping craft...");
                        Cancel("[CraftingList] A problem occurred trying to close the crafting log, cancelling...", true);
                        break;
                    }
                }
                configuration.EntryList.RemoveAll(x => x.Complete);
                await Task.Delay(500);
                TerminationAlert();
                m_running = false;
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

        // Takes lastUsedFood and lastUsedMedicine as input/output parameters,
        // because the function needs to return a bool to check for success and trying to mimic union types in C# seems stinky
        public async Task<(bool, uint, uint)> RepairAndApplyConsumables(TimedIngameMacro macro, uint lastUsedFood, uint lastUsedMedicine)
        {
            bool needToChangeFood = NeedToChangeConsumable(lastUsedFood, macro.FoodID, false).Result;
            bool needToChangeMedicine = NeedToChangeConsumable(lastUsedMedicine, macro.MedicineID, true).Result;
            bool needToRepair = NeedsRepair();

            PluginLog.Debug($"Last food: {lastUsedFood}, Curr food: {macro.FoodID}");
            PluginLog.Debug($"Need change food: {needToChangeFood}");
            PluginLog.Debug($"Last medicine: {lastUsedMedicine}, Curr medicine: {macro.MedicineID}");
            PluginLog.Debug($"Need change medicine: {needToChangeMedicine}");
            PluginLog.Debug($"Need repair: {needToRepair}");

            if (needToChangeFood || needToChangeMedicine || needToRepair)
            {
                await ExitCrafting();
                if (needToChangeFood)
                {
                    if (!await ChangeFood(macro.FoodID, false))
                    {
                        PluginLog.Debug($"Consuming food failed, stopping craft...");
                        Cancel("[CraftingList] A problem occurred while trying to consume food, cancelling craft...", true);
                        return (false, 0, 0);
                    }
                    lastUsedFood = macro.FoodID;
                }
                if (needToChangeMedicine)
                {

                    if (!await ChangeFood(macro.MedicineID, true))
                    {
                        PluginLog.Debug($"Consuming medication failed, stopping craft...");
                        Cancel("[CraftingList] A problem occurred while trying to consume medication, cancelling craft...", true);
                        return (false, 0, 0);
                    }
                    lastUsedMedicine = macro.MedicineID;
                }

                if (needToRepair)
                {
                    if (!await Repair())
                    {
                        PluginLog.Debug($"Repairing fialed, stopping craft...");
                        Cancel("[CraftingList] A problem occured while trying to repair, cancelling craft...", true);
                        return (false, 0, 0);
                    }
                }

            }
            return (true, lastUsedFood, lastUsedMedicine);
        }
        public async Task<bool> CraftOneItem(CListEntry entry)
        {
            var macro = configuration.Macros[entry.MacroIndex];
            bool isCollectible = DalamudApi.DataManager.GetExcelSheet<Item>()!
                        .Where(item => item.RowId == entry.ItemId)
                        .First().IsCollectable;

            if (!await OpenRecipeByItem((int)entry.ItemId))
            {
                PluginLog.Debug($"Open Recipe Note failed, stopping craft...");
                Cancel("[CraftingList] A problem occurred while trying to open crafting log, cancelling craft...", true);
                return false;
            }


            if (!FillHQMats(entry.HQSelection))
            {
                PluginLog.Debug($"Filling HQ Mats failed, stopping craft...");
                Cancel("[CraftingList] A problem occured while trying to fill HQ mats, cancelling craft...", true);
                return false;
            }
            await Task.Delay(500);

            if (!await ClickSynthesize())
            {
                if (entry.NumCrafts == "max")
                {
                    entry.Complete = true;
                }
                else
                {
                    PluginLog.Debug($"Click Synthesize failed, stopping current entry...");
                    Cancel("[CraftingList] A problem occured starting craft, cancelling...", true);
                    return false;
                }

                return false;
            }

            if (!await ExecuteMacro(macro, isCollectible))
            {
                PluginLog.Debug($"Executing macro timed out, stopping craft...");
                Cancel($"[CraftingList] Macro {{{macro.Name}, {macro.Macro1Num}, {macro.Macro1DurationSeconds}s}} timed out before completing the craft, cancelling...", true);
                return false;
            }
            entry.Decrement();
            return true;
        }

        public async Task<bool> OpenRecipeByItem(int itemId)
        {
            if (seInterface.RecipeNote().IsVisible())
            {
                await Task.Delay(configuration.WaitDurations.AfterOpenCloseMenu); return true;
            }
            PluginLog.Debug($"Opening crafting log to item {itemId}");
            seInterface.RecipeNote().OpenRecipeByItemId(itemId);

            var task = seInterface.WaitForAddon("RecipeNote", true, configuration.AddonTimeout);
            try { task.Wait(); }
            catch { return false; }
            await Task.Delay(configuration.WaitDurations.AfterOpenCloseMenu);
            return true;
        }

        public async Task<bool> ExitCrafting()
        {
            PluginLog.Debug($"Closing Recipe Note...");
            seInterface.ExecuteMacro(seInterface.CloseNoteMacro);
            var recipeNoteClosed = seInterface.WaitForCloseAddon("RecipeNote", true, configuration.AddonTimeout);
            try { recipeNoteClosed.Wait(); }
            catch { return false; }

            PluginLog.Debug($"Closed Recipe Note.");

            await Task.Delay(configuration.WaitDurations.AfterExitCrafting);
            return true;
        }

        public async Task<bool> ChangeFood(uint newFoodId, bool medication)
        {
            PluginLog.Debug($"Changing food/medication to {newFoodId}");
            if (newFoodId != 0)
            {

                if (medication) seInterface.RemoveMedicated();
                else seInterface.RemoveFood();

                await Task.Delay(configuration.WaitDurations.AfterClickOffFood);

                PluginLog.Debug($"Consuming food/medication {newFoodId}...");

                seInterface.UseItem(newFoodId);

                await Task.Delay(configuration.WaitDurations.AfterEatFood);

                if (medication) return await HasMedication();
                else return await HasFood();
            }
            else
            {
                PluginLog.Debug($"Removing food/medication...");

                if (medication) seInterface.RemoveMedicated();
                else seInterface.RemoveFood();

                PluginLog.Debug($"Removed food/medication.");

                await Task.Delay(configuration.WaitDurations.AfterClickOffFood);

                if (medication) return await HasMedication();
                else return await HasFood();
            }
        }

        public async Task<bool> ClickSynthesize()
        {
            PluginLog.Debug($"Clicking Synthesize...");

            

            seInterface.RecipeNote().Synthesize();
            var waitForSynthWindoResult = seInterface.WaitForAddon("Synthesis", true, configuration.AddonTimeout);
            try { waitForSynthWindoResult.Wait(); }
            catch { return false; }

            await Task.Delay(configuration.WaitDurations.AfterOpenCloseMenu);

            await Task.Delay(randomDelay.Next(configuration.ClickSynthesizeDelayMinSeconds * 1000,
                                              configuration.ClickSynthesizeDelayMaxSeconds * 1000)
             );
            return true;
        }

        public async Task<bool> ExecuteMacro(TimedIngameMacro macro, bool collectible)
        {
            PluginLog.Debug($"Executing Macro {macro.Macro1Num}");

            seInterface.ExecuteMacroByNumber(macro.Macro1Num);

            await Task.Delay(macro.Macro1DurationSeconds * 1000 + 1500);

            // No particular reason for a random delay here, why do you ask?
            await Task.Delay(randomDelay.Next(configuration.ExecuteMacroDelayMinSeconds * 1000,
                                              configuration.ExecuteMacroDelayMaxSeconds * 1000)
            );
            if (macro.Macro2Num != -1)
            {
                PluginLog.Debug($"Executing Macro {macro.Macro2Num}");
                seInterface.ExecuteMacroByNumber(macro.Macro2Num);
            }

            int completionAnimationTime = collectible ? configuration.WaitDurations.AfterCompleteMacroCollectible : configuration.WaitDurations.AfterCompleteMacroHQ;
            var recipeNote = seInterface.WaitForAddon("RecipeNote", true, macro.Macro2DurationSeconds * 1000 + completionAnimationTime + configuration.MacroExtraTimeoutMs);

            try { recipeNote.Wait(); }
            catch { return false; }

            await Task.Delay(randomDelay.Next(configuration.ExecuteMacroDelayMinSeconds * 1000,
                                              configuration.ExecuteMacroDelayMaxSeconds * 1000)
            );
            await Task.Delay(configuration.WaitDurations.AfterOpenCloseMenu);
            return true;

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

        private void SendAlert(string message, int soundEffect)
        {
            var mac = new Macro(0, 0, "Alert", new string[] { "/echo [CraftingList] " + message + " <se." + soundEffect + ">" });
            seInterface.ExecuteMacro(mac);
        }

        public static async Task<bool> NeedToChangeConsumable(uint lastFood, uint currEntryFoodId, bool medicine)
        {
            bool hasFood = medicine ? await HasMedication() : await HasFood();
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
                        .Where(x => x.RowId == DalamudApi.DataManager.GetExcelSheet<Item>()!.Where(x => x.RowId == lastFood || x.RowId == lastFood - 1000000).First().ItemAction.Value!.DataHQ[1])
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

        public bool FillHQMats(int[] hqSelection)
        {
            
            if (hqSelection.Length != 6)
            {
                PluginLog.Debug("[CraftingList] Internal error selecting HQ mats x.x");
                return false;
            }
            
            PluginLog.Debug("Selecting HQ Mats...");
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < hqSelection[i]; j++)
                {
                    //await Task.Delay(500);
                    seInterface.RecipeNote().ClickHQ(i);
                }
            }
            return true;
        }

        public static async Task<bool> HasFood()
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
            return hasFood;
        }

        public static async Task<bool> HasMedication()
        {
            bool hasMeds = false;
            while (DalamudApi.ClientState.LocalPlayer == null)
            {
                await Task.Delay(20);
            }
            foreach (var status in DalamudApi.ClientState.LocalPlayer.StatusList)
            {
                if (status == null) continue;

                if (status.StatusId == 49)
                {
                    hasMeds = true;
                }
     
            }
            return hasMeds;
        }
        public async Task<bool> Repair()
        {
            PluginLog.Debug($"Repairing...");
            PluginLog.Debug("Opening repair...");
            seInterface.ToggleRepairWindow();
            var repair = seInterface.WaitForAddon("Repair", true, 5000);
            try { repair.Wait(); }
            catch
            {
                PluginLog.Debug($"Failed to open repair window.");
                return false;
            }
            await Task.Delay(configuration.WaitDurations.AfterOpenCloseMenu);

            PluginLog.Debug("Clicking repair all...");
            seInterface.Repair().ClickRepairAll();
            var selectYesno = seInterface.WaitForAddon("SelectYesno", true, configuration.AddonTimeout);
            try { selectYesno.Wait(); }
            catch
            {
                PluginLog.Debug($"Failed to open YesNo Dialog.");
                return false;
            }
            await Task.Delay(configuration.WaitDurations.AfterOpenCloseMenu);

            //Can't wait for any addons here because the yesno dialog closes immediately on animation start and the repair window doesn't close
            PluginLog.Debug("Clicking confirm...");
            seInterface.SelectYesNo().ClickYes();
            var waitForRepairAnim = Task.Delay(configuration.WaitDurations.AfterRepairConfirm);

            PluginLog.Debug("Closing repair window...");
            seInterface.ToggleRepairWindow();
            var closeRepairtask = seInterface.WaitForCloseAddon("Repair", true, configuration.AddonTimeout);
            try { closeRepairtask.Wait(); }
            catch
            {
                PluginLog.Debug("Failed to close Repair window.");
            }
            await waitForRepairAnim;
            PluginLog.Debug("Repaired!");
            return true;
        }

        public void Cancel(string cancelMessage, bool error)
        {
            if (m_running)
            {
                if (error) DalamudApi.ChatGui.PrintError(cancelMessage);
                else DalamudApi.ChatGui.Print(cancelMessage);
            }
            m_running = false;
        }


        public bool IsEntryValid(CListEntry entry)
        {
            if (entry.NumCrafts.ToLower() != "max" && (!int.TryParse(entry.NumCrafts, out _) || int.Parse(entry.NumCrafts) <= 0)) {
                PluginLog.Debug("bad amount");
                return false;
            }

            if (entry.MacroIndex < 0 || entry.MacroIndex > configuration.Macros.Count) { PluginLog.Debug("Bad macro"); return false; }

            return true;
        }

        public bool IsListValid()
        {
            foreach (var entry in configuration.EntryList)
            {
                if (!IsEntryValid(entry)) return false;
            }
            return true;
        }

        public bool IsRunning()
        {
            return m_running;
        }
    }
}
