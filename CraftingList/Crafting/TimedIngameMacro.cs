using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.String;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureMacroModule;


namespace CraftingList.Crafting
{
    [StructLayout(LayoutKind.Sequential, Size = 0x688)]
    public unsafe struct Macro
    {
        public uint IconId;
        public uint Unk;
        public Utf8String Name;
        public Lines Line;

        public Macro(uint iconId, uint unk, string name, string line)
            : this(iconId, unk, name, new string[] { line })
        {

        }

        public Macro(uint iconId, uint unk, string name, string[] lines)
        {
            IconId = iconId;
            Unk = unk;
            Name = new Utf8String();
            Line = new Lines();
            for (int i = 0; i < 14; i++)
            {
                if (i >= lines.Length)
                {
                    Line[i] = Utf8String.FromString("");
                }
                else
                {
                    Line[i] = Utf8String.FromString(lines[i]);
                }
            }
        }
        [StructLayout(LayoutKind.Sequential, Size = 0x618)]
        public struct Lines
        {
            public fixed byte data[0x618];

            public Utf8String* this[int i]
            {
                get
                {
                    if (i < 0 || i > 14) return null;
                    fixed (byte* p = data)
                    {
                        return (Utf8String*)(p + sizeof(Utf8String) * i);
                    }
                }

                set
                {
                    if (i < 0 || i > 14) return;
                    fixed (byte* p = data)
                    {
                        Buffer.MemoryCopy(value, p + sizeof(Utf8String) * i, 0x618 - (sizeof(Utf8String) * i), sizeof(Utf8String));
                    }
                }
            }
        }
    }
    public class TimedIngameMacro : CraftingMacro
    {
        public int Macro1Num = -1;
        public int Macro1DurationSeconds = 0;

        public int Macro2Num = -1;
        public int Macro2DurationSeconds  = 0;


        public TimedIngameMacro(string name, int macro1Num, int macro1DurationSeconds, int macro2Num, int macro2DurationSeconds)
            :base(name, 0, 0)
        {
            Macro1DurationSeconds = macro1DurationSeconds;
            Macro1Num = macro1Num;
            Macro2Num = macro2Num;
            Macro2DurationSeconds = macro2DurationSeconds;
        }

        public override async Task<bool> Execute(bool collectible)
        {
            PluginLog.Debug($"Executing Macro {Macro1Num}");
            int completionAnimationTime = collectible ? DalamudApi.Configuration.WaitDurations.AfterCompleteMacroCollectible :
                                                        DalamudApi.Configuration.WaitDurations.AfterCompleteMacroHQ;

            SeInterface.ExecuteMacroByNumber(Macro1Num);

            await Task.Delay(Macro1DurationSeconds * 1000 + 1500);

            // No particular reason for a random delay here, why do you ask?
            await Task.Delay(randomDelay.Next(DalamudApi.Configuration.ExecuteMacroDelayMinSeconds * 1000,
                                              DalamudApi.Configuration.ExecuteMacroDelayMaxSeconds * 1000)
            );

            if (Macro2Num != -1)
            {
                PluginLog.Debug($"Executing Macro {Macro2Num}");
                SeInterface.ExecuteMacroByNumber(Macro2Num);
                await Task.Delay(Macro2DurationSeconds + completionAnimationTime);
            }

            
            var recipeNote = SeInterface.WaitForAddon("RecipeNote", true,
                DalamudApi.Configuration.MacroExtraTimeoutMs);

            try { recipeNote.Wait(); }
            catch { return false; }

            await Task.Delay(randomDelay.Next(DalamudApi.Configuration.ExecuteMacroDelayMinSeconds * 1000,
                                              DalamudApi.Configuration.ExecuteMacroDelayMaxSeconds * 1000)
            );
            await Task.Delay(DalamudApi.Configuration.WaitDurations.AfterOpenCloseMenu);

            return true;
        }

        public static bool isValidMacro(TimedIngameMacro macro)
        {
            if (macro.Name == null || macro.Name == "") return false;
            if (macro.Macro1Num < 0 || macro.Macro1Num > 99) return false;
            if (macro.Macro1DurationSeconds <= 0) return false;
            if (macro.Macro2Num == -1)
            {
                if (macro.Macro2DurationSeconds != 0) return false;
            }
            else
            {
                if (macro.Macro2DurationSeconds <= 0) return false;
            }

            return true;
        }
    }


}
