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
using System.Threading;

namespace CraftingList.Crafting.Macro
{
    public class MacroCommand
    {
        private static ManualResetEvent DataWaiter
        => Service.GameEventManager.DataAvailableWaiter;

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

        public async Task<bool> Execute()
        {
            PluginLog.Debug($"Executing '{Text}'");
            PluginLog.Debug($"Will wait {WaitMS} ms");

            DataWaiter.Reset();
            try
            {
                Service.ChatManager.SendMessage(Text);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex.Message);
                return false;
            }

            if (Service.Configuration.SmartWait && WaitMS != 0)
            {
                if (!DataWaiter.WaitOne(Service.Configuration.WaitDurations.CraftingActionMaxDelay))
                {
                    PluginLog.Error("[MacroCommand.Execute()] Didn't receive a response after using action.");
                    return false;
                }

                if (!await Service.WaitForCondition(ConditionFlag.Crafting40, false, Service.Configuration.WaitDurations.CraftingActionMaxDelay))
                {
                    PluginLog.Error("[MacroCommand.Execute()] (?????? This shouldn't happen) Waiting for action to finish took too long");
                }
                return true;
            }
            else
            {
                await Task.Delay(WaitMS);
            }
            return true;
        }

        
    }
}