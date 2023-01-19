using CraftingList.Utility;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.Crafting.Macro
{
    internal class MacroManager
    {
        public static List<PluginMacro> PluginMacros
            => DalamudApi.Configuration.PluginMacros;

        public static List<IngameMacro> IngameMacros
            => DalamudApi.Configuration.IngameMacros;

        public static List<string> MacroNames { get; set; } = new();

        public static void InitializeMacros()
        {
            foreach (var macro in PluginMacros)
            {
                MacroNames.Add(macro.Name);
            }
            foreach (var macro in IngameMacros)
            {
                MacroNames.Add(macro.Name);
            }
        }
        public static CraftingMacro? GetMacro(string macroName)
        {
            return GetMacroFromList(PluginMacros, macroName) ?? GetMacroFromList(IngameMacros, macroName) ?? null;
        }

        public static CraftingMacro? GetMacroFromList(IEnumerable<CraftingMacro> macros, string macroName)
        {
            foreach (var macro in macros)
            {
                if (macro.Name == macroName)
                    return macro;
            }

            return null;
        }

        public static bool ExistsMacro(string macroName)
        {
            return ExistsMacroInList(PluginMacros, macroName) || ExistsMacroInList(IngameMacros, macroName);
        }

        public static bool ExistsMacroInList(IEnumerable<CraftingMacro> macroList, string macroName)
        {
            foreach (var macro in macroList)
            {
                if (macro.Name == macroName)
                    return true;
            }
            return false;
        }

        public static string GenerateUniqueName(IEnumerable<string> names, string baseNewName)
        {
            string modifier = "";
            int i = 1;
            while (names.Contains(baseNewName + modifier))
            {
                modifier = $" ({i++})";
            }

            return baseNewName + modifier;
        }

        public static void AddEmptyPluginMacro(string newName)
        {
            var newMacro = new PluginMacro(GenerateUniqueName(MacroNames, newName), 0, 0, "");
            
            PluginMacros.Add(newMacro);
            MacroNames.Add(newMacro.Name);
        }

        public static void AddEmptyIngameMacro(string newName)
        {
            var newMacro = new IngameMacro(newName, -1, -1);

            IngameMacros.Add(newMacro);
            MacroNames.Add(newMacro.Name);
        }

        public static void RemoveMacro(string macroName)
        {
            if (PluginMacros.RemoveAll(m => m.Name == macroName) > 0)
                MacroNames.Remove(macroName);

            else if (IngameMacros.RemoveAll(m => m.Name == macroName) > 0)
                MacroNames.Remove(macroName);
        }

        public static void RenameMacro(string currName, string newName)
        {
            var macro = GetMacro(currName);
            if (macro == null)
                return;
            
            macro.Name = GenerateUniqueName(MacroNames, newName);

            int index = MacroNames.IndexOf(currName);
            if (index == -1)
            {
                PluginLog.Error($"Name '{currName}' exists in Macro list, but not in MacroNames. Adding to MacroNames, but this should never happen.");
                MacroNames.Add(newName);
                return;
            }

            MacroNames[index] = newName;
            
            
        }
    }
}
