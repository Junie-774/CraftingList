using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static CraftingList.SeFunctions.SeInterface;

namespace CraftingList.Crafting
{
    public class Crafter
    {

        private bool m_running = false;
        public bool waitingForHQSelection = false;


        private SeInterface seInterface;
        private Configuration configuration;
        private bool HQUnselected = true;
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

            m_running = true;
            return Task.Run(async () =>
            {

                var tokenSource = new CancellationTokenSource();
                var token = tokenSource.Token;

                uint lastUsedFood = 0;
                foreach (var entry in configuration.EntryList.ToList())
                {
                    entry.running = true;
                    HQUnselected = true;
                    await Task.Delay(1000);
                    PluginLog.Debug($"Crafting {entry.NumCrafts} {entry.Name}. Macro: {entry.Macro.Name}. FoodId: {entry.FoodId}");

                    if (!m_running) break;
                    if (!CraftingMacro.isValidMacro(entry.Macro))
                    {
                        DalamudApi.ChatGui.PrintError("[CraftingList] Entry " + entry.Name + ": Macro is invalid. Try reselecting it. Skipping to next craft.");
                        continue;
                    }
                    var job = DalamudApi.DataManager.GetExcelSheet<Recipe>()!
                        .Where(recipe => recipe.ItemResult.Value!.RowId == entry.ItemId)
                        .First().CraftType.Value!.RowId;

                    bool isCollectible = DalamudApi.DataManager.GetExcelSheet<Item>()!
                        .Where(item => item.RowId == entry.ItemId)
                        .First().IsCollectable;

                    await ChangeJobs((DoHJob)job);


                    if (entry.Macro.Name == "(Quick Synth)")
                    {
                        //await OpenRecipeByItem((int) entry.ItemId);
                    }
                    else
                    {
                        while (entry.NumCrafts == "max" || int.Parse(entry.NumCrafts) > 0)
                        {
                            if (!m_running || !entry.running) break;

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
                                    HQUnselected = true;
                                }
                                if (needToRepair)
                                {
                                    HQUnselected = true;
                                    await Repair();
                                }
                            }
                            if (!m_running) break;

                            if (!await OpenRecipeByItem((int)entry.ItemId))
                            {
                                PluginLog.Debug($"Open Recipe Note failed, stopping craft...");
                                Cancel("[CraftingList] A problem occurred while trying to open crafting log, cancelling...", true);
                                break;
                            }
                            if (!m_running) break;

                            if (HQUnselected)
                            {
                                //await PromptForHqMats((int)entry.ItemId);
                                FillHQMats(entry.HQSelection);
                                await Task.Delay(500);
                            }
                            if (!await ClickSynthesize())
                            {
                                if (entry.NumCrafts == "max")
                                {
                                    entry.Complete = true;
                                }
                                else
                                {
                                    PluginLog.Debug($"Click Synthesize failed, stopping current entry...");
                                    DalamudApi.ChatGui.PrintError("[CraftingList] A problem occured starting craft, moving on to next item in the list...");
                                }

                                break;
                            }

                            if (!await ExecuteMacro(entry.Macro, isCollectible))
                            {
                                PluginLog.Debug($"Executing macro timed out, stopping craft...");
                                Cancel($"[CraftingList] Macro {{{entry.Macro.Name}, {entry.Macro.Macro1Num}, {entry.Macro.Macro1DurationSeconds}s}} timed out before completing the craft, cancelling...", true);
                                break;
                            }
                            if (entry.NumCrafts != "max") entry.NumCrafts = (int.Parse(entry.NumCrafts) - 1).ToString();
                        }
                    }
                    if (!m_running)
                    {
                        break;
                    }
                    if (entry.NumCrafts == "0") entry.Complete = true;
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
            var res = seInterface.WaitForAddon("Synthesis", true, configuration.AddonTimeout);
            try { res.Wait(); }
            catch { return false; }
            await Task.Delay(configuration.WaitDurations.AfterOpenCloseMenu);
            return true;
        }

        public async Task<bool> ExecuteMacro(CraftingMacro macro, bool collectible)
        {
            PluginLog.Debug($"Executing Macro {macro.Macro1Num}");

            seInterface.ExecuteMacroByNumber(macro.Macro1Num);
            await Task.Delay(macro.Macro1DurationSeconds * 1000 + 1500);
            if (macro.Macro2Num != -1)
            {
                PluginLog.Debug($"Executing Macro {macro.Macro2Num}");
                seInterface.ExecuteMacroByNumber(macro.Macro2Num);
            }

            int completionAnimationTime = collectible ? configuration.WaitDurations.AfterCompleteMacroCollectible : configuration.WaitDurations.AfterCompleteMacroHQ;
            var recipeNote = seInterface.WaitForAddon("RecipeNote", true, macro.Macro2DurationSeconds * 1000 + completionAnimationTime + configuration.MacroExtraTimeoutMs);

            try { recipeNote.Wait(); }
            catch { return false; }

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
                    PluginLog.Debug("Clicking hq...");
                    //await Task.Delay(500);
                    seInterface.RecipeNote().ClickHQ(i);
                }
            }
            HQUnselected = false;
            return true;
        }
        public async Task<bool> PromptForHqMats(int itemId)
        {
            PluginLog.Debug("Waiting for HQ Material Selection...");
            if (!await OpenRecipeByItem(itemId))
            {
                PluginLog.Debug("Failed to open recipe note."); return false;
            }
            if (configuration.FlashWindowOnHQPrompt && !FlashWindow.ApplicationIsActivated())
            {
                var flashInfo = new FlashWindow.FLASHWINFO
                {
                    cbSize = (uint)Marshal.SizeOf<FlashWindow.FLASHWINFO>(),
                    uCount = uint.MaxValue,
                    dwTimeout = 0,
                    dwFlags = FlashWindow.FLASHW_ALL | FlashWindow.FLASHW_TIMERNOFG,
                    hwnd = Process.GetCurrentProcess().MainWindowHandle,

                };
                FlashWindow.FlashWindowEx(ref flashInfo);
            }
            waitingForHQSelection = true;
            while (waitingForHQSelection && m_running) {

                if (!waitingForHQSelection)
                {
                    break;
                }
            }
            return true;
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
            waitingForHQSelection = false;
        }

        public void SignalHQMatsSelected()
        {
            waitingForHQSelection = false;
        }

    }
}
