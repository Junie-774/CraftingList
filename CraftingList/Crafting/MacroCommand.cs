using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CraftingList.Crafting
{
    internal class MacroCommand
    {
        private static readonly Regex WaitRegex = new(@"(?<modifier><wait\.(?<wait>\d+(?:\.\d+)?)>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ActionNameRegex = new(@"^/(?:ac|action)\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Text { get; }

        public int Wait { get; }

        private string actionName = "";
        public MacroCommand(string text, int wait, string actionName)
        {
            Text = text;
            Wait = wait;
        }

 
    }
}
