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
        public IngameMacro(string name, uint foodID, uint medicineID, int m1, int m2)
            : base(name, foodID, medicineID, m1, m2)
        {
            UseIngameMacro = true;
        }
    }


}
