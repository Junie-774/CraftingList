using FFXIVClientStructs.FFXIV.Client.System.String;
using System;
using System.Runtime.InteropServices;


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
        {
            this.IconId = iconId;
            this.Unk = unk;
            this.Name = new Utf8String();
            this.Line = new Lines();
            Line[0] = Utf8String.FromString(line);
            for (int i = 1; i <= 14; i++)
            {
                Line[i] = Utf8String.FromString("");
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
    public unsafe class CraftingMacro
    {
        public string Name { get; set; }
        public int MacroNum { get; set; }

        public int DurationSeconds { get; set; } = 0;

        public CraftingMacro(string name, int macroNum, int durationSeconds)
        {
            Name = name;
            DurationSeconds = durationSeconds;
            MacroNum = macroNum;
        }

    }


}
