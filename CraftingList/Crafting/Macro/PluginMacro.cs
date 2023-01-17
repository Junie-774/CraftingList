using CraftingList.SeFunctions;
using CraftingList.Utility;
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
            foreach (var macro in Parse(Text))
            {
                DalamudApi.ChatGui.Print(macro.Text);
                await macro.Execute();
            }

            var recipeNote = SeInterface.WaitForAddon("RecipeNote", true,
                DalamudApi.Configuration.MacroExtraTimeoutMs);

            try { recipeNote.Wait(); }
            catch { return false; }

            await Task.Delay(randomDelay.Next(DalamudApi.Configuration.ExecuteMacroDelayMinSeconds * 1000,
                                              DalamudApi.Configuration.ExecuteMacroDelayMaxSeconds * 1000)
            );
            await Task.Delay(DalamudApi.Configuration.WaitDurations.AfterOpenCloseMenu);

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

        public static unsafe string GetMacroText(int macroNum)
        {
            RaptureMacroModule.Macro* macro = RaptureMacroModule.Instance->GetMacro(0, (uint)macroNum);
            var text = string.Empty;

            for (int i = 0; i <= 14; i++)
            {
                var line = macro->Line[i]->ToString();
                if (line.Length > 0)
                {
                    text += line;
                    text += "\n";
                }
            }

            return text;
        }

        public static PluginMacro FromTimedIngameMacro(TimedIngameMacro timedIngameMacro)
        {
            var name = timedIngameMacro.Name;
            var foodId = timedIngameMacro.FoodID;
            var medID = timedIngameMacro.MedicineID;

            var text = "";
            if (timedIngameMacro.Macro1Num >= 0 && timedIngameMacro.Macro1Num <= 99)
                text += GetMacroText(timedIngameMacro.Macro1Num);
            if (timedIngameMacro.Macro2Num >= 0 && timedIngameMacro.Macro2Num <= 99)
                text += GetMacroText(timedIngameMacro.Macro2Num);

            return new PluginMacro(name, foodId, medID, text);
        }
    }


}
