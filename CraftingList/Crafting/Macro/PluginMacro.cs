using CraftingList.Utility;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.Crafting.Macro
{
    public class PluginMacro : CraftingMacro
    {
        public string Text = string.Empty;

        public PluginMacro(string name, uint foodID, uint medicineID, string text)
            : base(name, foodID, medicineID)
        {

            Text = text;
        }

        public override async Task<bool> Execute(bool _)
        {
            try
            {
                foreach (var macro in Parse(Text))
                {
                    try
                    {
                        await macro.Execute();
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error(ex.Message);
                    }
                }

                var recipeNote = SeInterface.WaitForAddon("RecipeNote", true,
                    DalamudApi.Configuration.MacroExtraTimeoutMs);

                try { recipeNote.Wait(); }
                catch { return false; }

                await Task.Delay(randomDelay.Next((int) DalamudApi.Configuration.ExecuteMacroDelayMinSeconds * 1000,
                                                  (int) DalamudApi.Configuration.ExecuteMacroDelayMaxSeconds * 1000)
                );
                await Task.Delay(DalamudApi.Configuration.WaitDurations.AfterOpenCloseMenu);
            }
            catch(Exception ex)
            {
                PluginLog.Error(ex.Message);
                return false;
            }

            return true;
        }

        public static IEnumerable<MacroCommand> Parse(string macroText)
        {
            var line = string.Empty;
            using var reader = new StringReader(macroText);

            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                yield return MacroCommand.Parse(line);
            }
            yield break;
        }

        

        public static PluginMacro FromTimedIngameMacro(IngameMacro timedIngameMacro)
        {
            var name = timedIngameMacro.Name;
            var foodId = timedIngameMacro.FoodID;
            var medID = timedIngameMacro.MedicineID;

            var text = "";
            if (timedIngameMacro.Macro1Num >= 0 && timedIngameMacro.Macro1Num <= 99)
                text += IngameMacro.GetMacroText(timedIngameMacro.Macro1Num);
            if (timedIngameMacro.Macro2Num >= 0 && timedIngameMacro.Macro2Num <= 99)
                text += IngameMacro.GetMacroText(timedIngameMacro.Macro2Num);

            return new PluginMacro(name, foodId, medID, text);
        }
    }


}
