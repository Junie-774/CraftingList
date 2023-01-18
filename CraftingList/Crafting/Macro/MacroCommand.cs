using CraftingList.Utility;
using Dalamud.Game.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dalamud.Game.ClientState.Conditions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace CraftingList.Crafting.Macro
{
    public class MacroCommand
    {
        private static readonly Regex WaitRegex = new(@"(?<modifier><wait\.(?<wait>\d+(?:\.\d+)?)>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ActionNameRegex = new(@"^/(?:ac|action)\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Text { get; }

        public int WaitMS { get; }

        //Currently unused.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Will be used later.")]
        private string actionName = "";

        public MacroCommand(string text, int wait, string actionName)
        {
            Text = text;
            WaitMS = wait;
            this.actionName = actionName.ToLowerInvariant();

        }

        public static MacroCommand Parse(string text)
        {
            var wait = 0;
            var waitMatch = WaitRegex.Match(text);
            if (waitMatch.Success)
            {
                var waitValue = waitMatch.Groups["wait"].Value;
                wait = (int)(float.Parse(waitValue) * 1000);
            }
            text = text.Remove(waitMatch.Groups["modifier"].Index, waitMatch.Groups["modifier"].Length);

            string actionName = "";
            var nameMatch = ActionNameRegex.Match(text);
            if (nameMatch.Success)
            {
                actionName = ExtractAndUnquote(nameMatch, "name");
            }

            return new MacroCommand(text, wait, actionName);
        }

        protected static string ExtractAndUnquote(Match match, string groupName)
        {
            var group = match.Groups[groupName];
            var groupValue = group.Value;

            if (groupValue.StartsWith('"') && groupValue.EndsWith('"'))
                groupValue = groupValue.Trim('"');

            return groupValue;
        }

        public async Task Execute()
        {
            PluginLog.Debug($"Executing '{Text}'");

            try
            {
                SeInterface.SendChatMessage(Text);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex.Message);
            }
            if (DalamudApi.Configuration.SmartWait)
            {
                await Task.Delay(100);

                while (DalamudApi.Condition[ConditionFlag.Crafting40])
                    await Task.Delay(250);
                await Task.Delay(100);
            }
            else
            {
                await Task.Delay(WaitMS);
            }
            return;
        }
    }
}