using CraftingList.Utility;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureMacroModule;
using static System.Net.Mime.MediaTypeNames;

namespace CraftingList.Crafting.Macro
{
    internal class MacroManager
    {
        protected static readonly Random randomDelay = new(DateTime.Now.Millisecond);

        public static List<PluginMacro> PluginMacros
            => Service.Configuration.PluginMacros;

        public static List<IngameMacro> IngameMacros
            => Service.Configuration.IngameMacros;

        public static List<CraftingMacro> CraftingMacros
            => Service.Configuration.CraftingMacros;

        public static List<string> MacroNames { get; set; } = new() { };// "<Quick Synth>" };

        public static void InitializeMacros()
        {
            foreach (var macro in CraftingMacros)
            {
                MacroNames.Add(macro.Name);
            }

        }
        public static CraftingMacro? GetMacro(string macroName)
        {
            foreach (var macro in CraftingMacros)
            {
                if (macro.Name == macroName)
                    return macro;
            }

            return null;
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

        public static void AddCraftingMacro(CraftingMacro newMacro)
        {
            newMacro.Name = GenerateUniqueName(MacroNames, newMacro.Name);
            CraftingMacros.Add(newMacro);
            RegenerateMacroNames();
        }

        public static void AddEmptyCraftingMacro(string newName)
        {
            var newMacro = new CraftingMacro(GenerateUniqueName(MacroNames, newName), 0, 0, false, "", -1, -1);
            AddCraftingMacro(newMacro);
        }


        public static void MoveCraftingMacro(CraftingMacro macro, int index)
        {
            if(index < 0 || index > CraftingMacros.Count)
                return;

            CraftingMacro copy = CraftingMacros[CraftingMacros.IndexOf(macro)];
            if (CraftingMacros.Remove(macro))
            {
                CraftingMacros.Insert(index, copy);
            }
        }




        public static int RemoveMacro(string macroName)
        {
            MacroNames.RemoveAll(m => m == macroName);

            return CraftingMacros.RemoveAll(m => m.Name == macroName);
        }

        public static void RenameMacro(string currName, string newName)
        {
            var macro = GetMacro(currName);
            if (macro == null || macro.Name == newName)
                return;
            
            macro.Name = GenerateUniqueName(MacroNames, newName);

            int index = MacroNames.IndexOf(currName);
            if (index == -1)
            {
                Service.PluginLog.Error($"Name '{currName}' exists in Macro list, but not in MacroNames. Adding to MacroNames, but this should never happen.");
                MacroNames.Add(newName);
                return;
            }

            MacroNames[index] = newName;
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

        // Regenerate them to make sure they always keep a consistent order
        private static void RegenerateMacroNames()
        {
            MacroNames.Clear();
            //MacroNames.Add("<Quick Synth>");

            foreach (var macro in CraftingMacros)
            {
                MacroNames.Add(macro.Name);
            }

        }

        public static async Task<bool> ExecuteMacroCommands(IEnumerable<MacroCommand> commands)
        {
            foreach (var command in commands)
            {
                for (int retry = 0; retry <= Service.Configuration.MaxMacroCommandTimeoutRetries; retry++)
                {
                    if (await command.Execute())
                    {
                        Service.PluginLog.Verbose("Success!");
                        break;
                    }
                    else
                    {
                        if (retry == Service.Configuration.MaxMacroCommandTimeoutRetries)
                        {
                            return false;
                        }
                    }
                }

            }

            var recipeNote = SeInterface.WaitForAddon("RecipeNote", true,
                Service.Configuration.MacroExtraTimeoutMs);

            try { recipeNote.Wait(); }
            catch {
                Service.PluginLog.Error("RecipeNote wait timed out.");
                return false;
            }

            await Task.Delay(randomDelay.Next((int)Service.Configuration.ExecuteMacroDelayMinSeconds * 1000,
                                              (int)Service.Configuration.ExecuteMacroDelayMaxSeconds * 1000)
            );
            await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);
            return true;
        }
    }
}
