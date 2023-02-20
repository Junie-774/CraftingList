using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.Crafting.Macro
{
    public class CraftingMacro
    {
        protected static readonly Random randomDelay = new(DateTime.Now.Millisecond);

        public string Name = "";
        public uint FoodID = 0;
        public uint MedicineID = 0;

        public bool HasMinStats = false;
        public bool UseIngameMacro = false;
        public CraftingStats? MinStats = null;

        public string Text = string.Empty;

        public int Macro1Num = -1;
        public int Macro2Num = -1;

        [JsonConstructor]
        public CraftingMacro(string name, uint foodID, uint medicineID, bool useIngameMacro, string text, int macro1Num, int macro2Num) : this(name, foodID, medicineID)
        {
            UseIngameMacro = useIngameMacro;
            Text = text;
            Macro1Num = macro1Num;
            Macro2Num = macro2Num;
        }

        private CraftingMacro(string name, uint foodID, uint medicineID)
        {
            Name = name;
            FoodID = foodID;
            MedicineID = medicineID;
        }

        public CraftingMacro(string name, uint foodID, uint medicineID, string inPluginText)
            :this(name, foodID, medicineID)
        {
            Text = inPluginText;
        }

        public CraftingMacro(string name, uint foodID, uint medicineID, int m1, int m2)
            :this(name, foodID, medicineID)
        {
            if (IsMacroNumInBounds(m1))
                Macro1Num = m1;

            if (IsMacroNumInBounds(m2))
                Macro2Num = m2;
        }

        public async Task<bool> Execute(bool collectible)
        {
            return UseIngameMacro ? await ExecuteWithMacroCommands() : await MacroManager.ExecuteMacroCommands(MacroManager.Parse(Text));

        }

        public async Task<bool> ExecuteWithMacroCommands()
        {
            var text1 = GetMacroTextFromNum(Macro1Num);
            var text2 = GetMacroTextFromNum(Macro2Num);

            var commands = MacroManager.Parse(text1).Concat(MacroManager.Parse(text2));

            return await MacroManager.ExecuteMacroCommands(commands);
        }

        public static unsafe string GetMacroTextFromNum(int macroNum)
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

        public static bool IsMacroNumInBounds(int macroNum)
        {
            return macroNum > 0 && macroNum <= 99;
        }
    }
}
