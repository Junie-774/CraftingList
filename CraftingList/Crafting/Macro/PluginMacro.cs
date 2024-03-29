﻿using System.Threading.Tasks;

namespace CraftingList.Crafting.Macro
{
    public class PluginMacro : CraftingMacro
    {
        public PluginMacro(string name, uint foodID, uint medicineID, string text)
            : base(name, foodID, medicineID, text)
        { }

        public static PluginMacro FromIngameMacro(IngameMacro timedIngameMacro)
        {
            var name = timedIngameMacro.Name;
            var foodId = timedIngameMacro.FoodID;
            var medID = timedIngameMacro.MedicineID;

            var text = "";
            if (timedIngameMacro.Macro1Num >= 0 && timedIngameMacro.Macro1Num <= 99)
                text += IngameMacro.GetMacroTextFromNum(timedIngameMacro.Macro1Num);
            if (timedIngameMacro.Macro2Num >= 0 && timedIngameMacro.Macro2Num <= 99)
                text += IngameMacro.GetMacroTextFromNum(timedIngameMacro.Macro2Num);

            return new PluginMacro(name, foodId, medID, text);
        }
    }


}
