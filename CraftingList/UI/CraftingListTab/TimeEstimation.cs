using CraftingList.Crafting;
using CraftingList.Crafting.Macro;
using CraftingList.Utility;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.UI.CraftingListTab
{
    public class TimeEstimation
    {

        public static int EstimateMacroDurationMS(CraftingMacro macro)
        {
            if (macro == null)
                return 0;

            if (macro.UseIngameMacro)
            {
                //PluginLog.Debug("Meep?");
                int total = 0;
                foreach (var command in MacroManager.Parse(IngameMacro.GetMacroTextFromNum(macro.Macro1Num)))
                {
                    //PluginLog.Debug($"{command.Text}: {command.WaitMS} ms");
                    total += command.WaitMS;
                }

                foreach (var command in MacroManager.Parse(IngameMacro.GetMacroTextFromNum(macro.Macro2Num)))
                    total += command.WaitMS;

                return total;
            }
            else
            {
                

                int total = 0;
                foreach (var command in MacroManager.Parse(macro.Text))
                {
                    total += command.WaitMS;
                }

                return total;
            }
        }


        public static int EstimateEntryDurationMS(CListEntry entry, EntryIngredientSummary summary)
        {
            int avgExecMacroDelay = (int) (Service.Configuration.ExecuteMacroDelayMinSeconds + Service.Configuration.ExecuteMacroDelayMaxSeconds) / 2;
            int avgClickSynthDelay = (int)(Service.Configuration.ClickSynthesizeDelayMinSeconds + Service.Configuration.ClickSynthesizeDelayMaxSeconds) / 2;
            int setupTime = Service.Configuration.WaitDurations.AfterExitCrafting // exit crafting
                + Service.Configuration.WaitDurations.AfterEatFood // eat food
                + Service.Configuration.WaitDurations.AfterOpenCloseMenu * 2 // Open recipenote & click quick synth
                + avgClickSynthDelay;

            int timePerCraft;
            int numRestarts;
            int numCrafts = summary.NumCrafts;
            //PluginLog.Debug($"Numcrafts: {numCrafts}");
            int betweenCrafts = avgExecMacroDelay
                + avgClickSynthDelay
                + Service.Configuration.WaitDurations.AfterOpenCloseMenu;

            if (entry.MacroName == "<Quick Synth>")
            {
                timePerCraft = 3000;
                numRestarts = numCrafts / 99;
            }
            else
            {
                numRestarts = numCrafts;
                timePerCraft = EstimateMacroDurationMS(MacroManager.GetMacro(entry.MacroName)!);
                //PluginLog.Debug($"Time per craft: {timePerCraft}");
            }

            return (numRestarts * betweenCrafts) + (timePerCraft * numCrafts) + setupTime;

        }
    }
}
