using CraftingList.Utility;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureMacroModule;
using static System.Net.Mime.MediaTypeNames;


namespace CraftingList.Crafting.Macro
{
    public class IngameMacro : CraftingMacro
    {
        private static readonly Regex WaitRegex = new(@"(?<modifier><wait\.(?<wait>\d+(?:\.\d+)?)>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public int Macro1Num = -1;

        public int Macro2Num = -1;


        public IngameMacro(string name, int macro1Num, int macro2Num)
            : base(name, 0, 0)
        {
            Macro1Num = macro1Num;
            Macro2Num = macro2Num;
        }

        public override async Task<bool> Execute(bool collectible)
        {
           
            int macro1Duration = GetWaitTimeFromMacroText(Macro1Num);
            PluginLog.Debug($"Executing Ingame Macro {Macro1Num}");
            int completionAnimationTime = collectible ? DalamudApi.Configuration.WaitDurations.AfterCompleteMacroCollectible :
                                                        DalamudApi.Configuration.WaitDurations.AfterCompleteMacroHQ;

            SeInterface.ExecuteMacroByNumber(Macro1Num);

            await Task.Delay(macro1Duration + 1500);

            // No particular reason for a random delay here, why do you ask?
            await Task.Delay(randomDelay.Next((int) DalamudApi.Configuration.ExecuteMacroDelayMinSeconds * 1000,
                                              (int) DalamudApi.Configuration.ExecuteMacroDelayMaxSeconds * 1000)
            );

            if (Macro2Num != -1)
            {
                int macro2Duration = GetWaitTimeFromMacroText(Macro2Num);
                PluginLog.Debug($"Executing Ingame Macro {Macro2Num}");
                SeInterface.ExecuteMacroByNumber(Macro2Num);
                await Task.Delay(macro2Duration + completionAnimationTime);
            }


            var recipeNote = SeInterface.WaitForAddon("RecipeNote", true,
                DalamudApi.Configuration.MacroExtraTimeoutMs);

            try { recipeNote.Wait(); }
            catch { return false; }

            await Task.Delay(randomDelay.Next((int) DalamudApi.Configuration.ExecuteMacroDelayMinSeconds * 1000,
                                              (int) DalamudApi.Configuration.ExecuteMacroDelayMaxSeconds * 1000)
            );
            await Task.Delay(DalamudApi.Configuration.WaitDurations.AfterOpenCloseMenu);

            return true;
        }

        public static bool IsValidMacro(IngameMacro macro)
        {
            if (macro.Name == null || macro.Name == "")
                return false;
            if (!IsMacroNumInBounds(macro.Macro1Num))
                return false;
            if (GetWaitTimeFromMacroText(macro.Macro1Num) <= 0)
                return false;
     
            return true;
        }

        public static int GetWaitTimeFromMacroText(int macroNum)
        {
            var macroText = GetMacroText(macroNum);
            var line = "";
            using var reader = new StringReader(macroText);
            int waitMS = 0;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                var waitMatch = WaitRegex.Match(line);
                if (!waitMatch.Success)
                {
                    continue;
                }
                var waitValue = waitMatch.Groups["wait"].Value;
                waitMS += (int)(float.Parse(waitValue) * 1000);
            }
            return waitMS;
        }

        public static bool IsMacroNumInBounds(int macroNum)
        {
            return macroNum > 0 && macroNum <= 99;
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
    }


}
