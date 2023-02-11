using CraftingList.Crafting.Macro;
using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace CraftingList.Crafting
{
    public class Crafter
    {
        public ManualResetEvent CraftUpdateEvent = new(false);

        private bool m_running = false;
        uint lastUsedFood = 0;
        uint lastUsedMedicine = 0;

        public Crafter()
        {}

        public Task<bool> CraftAllItems()
        {
            if (m_running)
            {
                Service.ChatManager.PrintError("A craft is already running!");
                return Task.FromResult(false);
            }
            if (!IsListValid())
            {
                Service.ChatManager.PrintError("An error occured validating the list. Please make sure all of your amounts are correct, and that all of your macros are selected.");
                return Task.FromResult(false);
            }

            m_running = true;

            return Task.Run(async () =>
            {
                Service.ChatManager.PrintMessage("Starting crafting!");

                var craftStart = DateTime.Now;

                //Enforces a clean
                if (!await CraftHelper.ExitCrafting())
                {
                    PluginLog.Debug($"Failed to exit crafting stance, stopping craft...");
                    Cancel("A problem occurred trying to close the crafting log, cancelling...", true);
                    
                }

                lastUsedFood = 0;
                lastUsedMedicine = 0;

                PluginLog.Debug($"Last food: {lastUsedFood}");
                foreach (var entry in Service.Configuration.EntryList.ToList())
                {
                    if (!m_running) break;
                    if (Service.Configuration.HasCraftTimeout
                        && Service.Configuration.CraftTimeoutMinutes > 0
                        && (DateTime.Now - craftStart).TotalMinutes >= Service.Configuration.CraftTimeoutMinutes)
                        break;

                    var job = Service.Recipes[entry.RecipeId].CraftType.Value!.RowId;             

                    await CraftHelper.ChangeJobs((int) job);

                    entry.running = true;

                    if (!m_running) break;
                    if (entry.MacroName == "<Quick Synth>")
                    {
                        await QuickSynthesizeEntry(entry);
                    }
                    else
                    {
                        await CraftEntryWithMacro(entry);
                    }
                    if (!m_running)
                    {
                        break;
                    }

                    // Leave crafting stance after each entry in case we need to switch jobs. Could hypothetically stay in crafting stance if next entry uses the same job,
                    // but that's a pretty minor gain and this simplifies code flow.
                    if (!await CraftHelper.ExitCrafting())
                    {
                        PluginLog.Debug($"Failed to exit crafting stance, stopping craft...");
                        Cancel("A problem occurred trying to close the crafting log, cancelling...", true);
                        break;
                    }
                }
                Service.Configuration.EntryList.RemoveAll(x => x.Complete);
                Service.Configuration.Save();
                await Task.Delay(500);
                TerminationAlert();
                CraftUpdateEvent.Set();
                m_running = false;
                return true;
            });
        }

        public async Task<bool> CraftEntryWithMacro(CListEntry entry)
        {
            CraftingMacro? macro = MacroManager.GetMacro(entry.MacroName);
            if (macro == null)
            {
                PluginLog.Error($"Entry {entry}'s macro did not match any in the active macro list.");
                Cancel("An internal error occurred, stopping craft. Check `/xllog` for more details.", true);
                return false;
            }
            else
            {
                if (macro is IngameMacro && !IngameMacro.IsValidMacro((IngameMacro)macro))
                {
                    Cancel($"Macro \"{macro.Name}\" was invalid. This is likely an internal error x.x", true);
                    return false;
                }
            }

            PluginLog.Debug($"Crafting {entry.NumCrafts} {entry.Name}. Macro: {macro.Name}. FoodId: {macro.FoodID}");

            while (entry.running && !entry.Complete)
            {
                if (!m_running) return false;

                var repairAndConsumablesResult = await RepairAndApplyConsumables(entry, macro, lastUsedFood, lastUsedMedicine);
                if (!repairAndConsumablesResult.Item1)
                    break;

                lastUsedFood = repairAndConsumablesResult.Item2;
                lastUsedMedicine = repairAndConsumablesResult.Item3;

                if (!m_running || !entry.running) break; // Cancel button can be pressed while repairing/eating food, or repairing/eating food can fail.

                if (!await CraftOneItem(entry, macro))
                    break;

            }

            return true;
        }

        // Takes lastUsedFood and lastUsedMedicine as input/output parameters,
        // because the function needs to return a bool to check for success and trying to mimic union types in C# seems stinky
        public async Task<(bool, uint, uint)> RepairAndApplyConsumables(CListEntry entry, CraftingMacro macro, uint lastUsedFood, uint lastUsedMedicine)
        {
            bool needToChangeFood = CraftHelper.NeedToChangeConsumable(lastUsedFood, macro.FoodID, false);
            bool needToChangeMedicine = CraftHelper.NeedToChangeConsumable(lastUsedMedicine, macro.MedicineID, true);
            bool needToRepair = CraftHelper.NeedsRepair();

            PluginLog.Debug($"Last food: {lastUsedFood}, Curr food: {macro.FoodID}");
            PluginLog.Debug($"Need change food: {needToChangeFood}");
            PluginLog.Debug($"Last medicine: {lastUsedMedicine}, Curr medicine: {macro.MedicineID}");
            PluginLog.Debug($"Need change medicine: {needToChangeMedicine}");
            PluginLog.Debug($"Need repair: {needToRepair}");

            if (needToChangeFood || needToChangeMedicine || needToRepair)
            {
                if (!await CraftHelper.ExitCrafting())
                {
                    Cancel($"A problem occured exiting the crafting stance for entry '{entry.Name}'. Cancelling craft.", true);
                }
            }
                    

            if (needToChangeFood)
            {
                if (!await CraftHelper.ChangeFood(macro.FoodID, false))
                {
                    CraftHelper.CancelEntry(entry, $"A problem occurred consuming food for entry '{entry.Name}'. Moving to next entry.", true);
                    return (false, 0, 0);
                }
                lastUsedFood = macro.FoodID;
            }


            if (needToChangeMedicine)
            {
                if (!await CraftHelper.ChangeFood(macro.MedicineID, true))
                {
                    CraftHelper.CancelEntry(entry, $"A problem occurred consuming medication for entry '{entry.Name}'. Moving to next entry.", true);
                    return (false, 0, 0);
                }
                lastUsedMedicine = macro.MedicineID;
            }

            if (needToRepair)
            {
                if (!await CraftHelper.Repair())
                {
                    Cancel($"A problem occured while trying to repair, during entry '{entry.Name}'. Cancelling craft...", true);
                    return (false, 0, 0);
                }
            }

            
            return (true, lastUsedFood, lastUsedMedicine);
        }

        public async Task<bool> CraftOneItem(CListEntry entry, CraftingMacro macro)
        {
            bool isCollectible = Service.Recipes[entry.RecipeId].ItemResult.Value!.IsCollectable;

            //if (!await CraftHelper.OpenRecipeByItem((int)Service.Recipes[entry.RecipeId].ItemResult.Value!.RowId))
            if (!await CraftHelper.OpenRecipeByRecipe(entry.RecipeId))
            {
                Cancel("A problem occurred while trying to open crafting log, cancelling craft...", true);
                return false;
            }


            if (!await CraftHelper.FillHQMats(entry.HQSelection))
            {
                Cancel("A problem occured while trying to fill HQ mats, cancelling craft...", true);
                return false;
            }
            await Task.Delay(500);


            if (!await CraftHelper.ClickSynthesize())
            {
                if (entry.NumCrafts == "max")
                {
                    entry.Complete = true;
                    entry.running = false;
                }
                else
                {
                    CraftHelper.CancelEntry(entry,
                        $"A problem occured starting craft for entry '{entry.Name}'.\n" +
                        $"Make sure you\n" +
                        $"- Have all of the requisite materials in your inventory,\n" +
                        $"- Have selected the correct profile of HQ materials to use,\n" +
                        $"- Meet the minimum requirements to start the craft,\n" +
                        $"If all of these conditions are met and you weren't screwing around with the crafting log when this error happened, this is likely an internal error.",
                        true);
                    return false;
                }

                return false;
            }

            if (!await macro.Execute(isCollectible))
            {
                Cancel($"Craft did not complete after executing macro '{macro.Name}'. Cancelling craft job.", true);
                return false;
            }
            entry.Decrement();
            CraftUpdateEvent.Set();
            return true;
        }

        public async Task<bool> QuickSynthesizeEntry(CListEntry entry)
        {
            if (entry.MacroName != "<Quick Synth>")
                return false;

            entry.running = true;
            while (entry.running && !entry.Complete)
            {
                if (CraftHelper.NeedsRepair())
                {
                    if (!await CraftHelper.ExitCrafting())
                    {
                        Cancel($"A problem occured exiting the crafting stance for entry '{entry.Name}'. Cancelling craft.", true);
                        return false;
                    }
                    if (!await CraftHelper.Repair())
                    {
                        Cancel($"A problem occured while trying to repair, during entry '{entry.Name}'. Cancelling craft...", true);
                        return false;
                    }

                }
                if (!await PerformOneQuickSynth(entry))
                {
                    entry.running = false;
                }
                CraftUpdateEvent.Set();
            }
            return true;
        }

        public async Task<bool> PerformOneQuickSynth(CListEntry entry)
        {
            int numToQuickSynth;

            if (entry.NumCrafts.ToLower() == "max")
                numToQuickSynth = 99;
            else
                numToQuickSynth = Math.Min(int.Parse(entry.NumCrafts), 99);

            if (!await CraftHelper.OpenRecipeByRecipe(entry.RecipeId))
            {
                CraftHelper.CancelEntry(entry, $"A problem occured while opening the crafting log to '{entry.Name}'. Moving to next entry...", true);
                entry.running = false;
                return false;
            }

            if (!await CraftHelper.ClickQuickSynthesize())
            {
                CraftHelper.CancelEntry(entry, $"A problem occured while trying to click Quick Synthesize for '{entry.Name}'. Moving to next entry...", true);
                entry.running = false;
                return false;
            }

            if (!await CraftHelper.EnterQuickSynthAmount(numToQuickSynth))
            {
                CraftHelper.CancelEntry(entry, $"A problem occured while trying to enter quick synth amount for '{entry.Name}'. Moving to next entry...", true);
                entry.running = false;
                return false;
            }

            if (!await CraftHelper.StartQuickSynth())
            {
                CraftHelper.CancelEntry(entry, $"A problem occured while trying to start quick synth for '{entry.Name}'. Moving to next entry...", true);
                entry.running = false;
                return false;
            }

            return await WaitForQuickSynthToFinish(entry, numToQuickSynth);
        }

        public async Task<bool> WaitForQuickSynthToFinish(CListEntry entry, int numToQuickSynth)
        {
            var simpleSynthDialog = SeInterface.GetUiObject("SynthesisSimple");

            int lastNumCompleted = 0; // Idk how to name this well, but its an auxillary variable that lags behind numCompleted so we can check if numCompleted has changed.
            var lastCompletionTime = DateTime.Now;
            int numCompleted;

            PluginLog.Debug("[WaitForQuickSynthToFinish] Starting loop waiting for quicksynth to finish.");
            for (numCompleted = 0; numCompleted < numToQuickSynth; numCompleted = ((PtrSynthesisSimple)simpleSynthDialog).GetCurrCrafts())
            {

                if (lastNumCompleted != numCompleted) // if numCompleted changed since last iteration
                {
                    lastCompletionTime = DateTime.Now;
                    lastNumCompleted = numCompleted;
                    entry.Decrement();
                    CraftUpdateEvent.Set();
                }

                if (DateTime.Now.CompareTo(lastCompletionTime.AddSeconds(5)) > 0)
                {
                    if (!await CraftHelper.CloseQuickSynthWindow())
                    {
                        CraftHelper.CancelEntry(entry, $"A problem occured while trying to exit the Quick Synth window for entry {entry.Name}. You may fail quick synths due to gear breaking.", true);
                        continue;
                    }
                    await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);

                    CraftHelper.CancelEntry(entry, $"Quick Synth finished early, skipping rest of entry {entry.Name}.", true);
                    return false;
                }


                if (!SeInterface.IsAddonAvailable(simpleSynthDialog, true))
                {
                    Cancel("The Quick Synthesis Window closed unexpectdly. Stopping craft.", true);

                    if (!await CraftHelper.ExitCrafting())
                    {
                        Cancel($"A problem occured trying to exit the crafting stance after quick synthing {entry.Name}.", true);
                        return false;
                    }

                    return false;
                }
                bool needsRepair = CraftHelper.NeedsRepair();

                //Quit this synth early if gear breaks or cancelled
                if (needsRepair || !m_running)
                {
                    ((PtrSynthesisSimple)simpleSynthDialog).ClickQuit();

                    if (!await CraftHelper.CloseQuickSynthWindow())
                    {
                        CraftHelper.CancelEntry(entry, $"A problem occured while trying to exit the Quick Synth window for entry {entry.Name}. You may fail quick synths due to gear breaking.", true);
                        continue;
                    }
                    await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);


                    // We've closed the dialog now and have no way to get the number, so we decrement one 
                    // after clicking quit because the current craft is finished before the dialog closes.

                    entry.Decrement();

                    await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);

                    if (!await CraftHelper.ExitCrafting())
                    {
                        Cancel($"A problem occured trying to exit the crafting stance after quick synthing {entry.Name}.", true);
                        return false;
                    }

                    if (needsRepair)
                    {
                        if (!await CraftHelper.Repair())
                            return false;
                    }

                    return true;
                }
                await Task.Delay(Service.Configuration.WaitDurations.WaitForConditionLoop);

            }

            entry.Decrement();
            ((PtrSynthesisSimple)simpleSynthDialog).ClickQuit();
            if (!await CraftHelper.WaitForCloseAddon("SynthesisSimple", true, Service.Configuration.AddonTimeout))
            {
                Cancel("Error exiting quick synth window", true);
                return false;
            }

            return true;
        }

        public static void TerminationAlert()
        {
            if (Service.Configuration.EntryList.Count == 0)
            {
                SendAlert("List complete!", Service.Configuration.SoundEffectListComplete);
            }
            else
            {
                SendAlert("Crafting stopped.", Service.Configuration.SoundEffectListCancel);
            }
        }

        private static void SendAlert(string message, int soundEffect)
        {
            Service.ChatManager.SendMessage("/echo " + message + " <se." + soundEffect + ">");
        }


        public void Cancel(string cancelMessage, bool error)
        {
            if (m_running)
            {
                if (error) Service.ChatManager.PrintError(cancelMessage);
                else Service.ChatManager.PrintMessage(cancelMessage);
            }
            foreach (var entry in Service.Configuration.EntryList)
            {
                entry.running = false;
            }
            m_running = false;
        }

        public static bool IsEntryValid(CListEntry entry)
        {
            if (entry.NumCrafts.ToLower() != "max" && (!int.TryParse(entry.NumCrafts, out _) || int.Parse(entry.NumCrafts) <= 0)) {
                PluginLog.Debug("bad amount");
                return false;
            }
            return true;
        }

        public static bool IsListValid()
        {
            foreach (var entry in Service.Configuration.EntryList)
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
