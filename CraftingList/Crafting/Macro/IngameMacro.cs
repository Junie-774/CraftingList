using CraftingList.Utility;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.IO;
using System.Linq;
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

        public override async Task<bool> Execute(bool _)
        {
            return await ExecuteWithMacroCommands();
        }
        public async Task<bool> ExecuteByCallingIngameMacro(bool collectible)
        {
           
            int macro1Duration = GetWaitTimeFromMacroText(Macro1Num);
            PluginLog.Debug($"Executing Ingame Macro {Macro1Num}");
            int completionAnimationTime = collectible ? Service.Configuration.WaitDurations.AfterCompleteMacroCollectible :
                                                        Service.Configuration.WaitDurations.AfterCompleteMacroHQ;

            SeInterface.ExecuteFFXivInternalMacroByNumber(Macro1Num);

            await Task.Delay(macro1Duration + 1500);

            // No particular reason for a random delay here, why do you ask?
            await Task.Delay(randomDelay.Next((int) Service.Configuration.ExecuteMacroDelayMinSeconds * 1000,
                                              (int) Service.Configuration.ExecuteMacroDelayMaxSeconds * 1000)
            );

            if (Macro2Num != -1)
            {
                int macro2Duration = GetWaitTimeFromMacroText(Macro2Num);
                PluginLog.Debug($"Executing Ingame Macro {Macro2Num}");
                SeInterface.ExecuteFFXivInternalMacroByNumber(Macro2Num);
                await Task.Delay(macro2Duration + completionAnimationTime);
            }


            var recipeNote = SeInterface.WaitForAddon("RecipeNote", true,
                Service.Configuration.MacroExtraTimeoutMs);

            try { recipeNote.Wait(); }
            catch { return false; }

            await Task.Delay(randomDelay.Next((int) Service.Configuration.ExecuteMacroDelayMinSeconds * 1000,
                                              (int) Service.Configuration.ExecuteMacroDelayMaxSeconds * 1000)
            );
            await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);

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

        public async Task<bool> ExecuteWithMacroCommands()
        {
            var text1 = GetMacroText(Macro1Num);
            var text2 = GetMacroText(Macro2Num);

            var commands = MacroManager.Parse(text1).Concat(MacroManager.Parse(text2));

            return await MacroManager.ExecuteMacroCommands(commands);
        }
        public static unsafe string GetMacroText(int macroNum)
        {
            if (!IsMacroNumInBounds(macroNum))
                return "";

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
