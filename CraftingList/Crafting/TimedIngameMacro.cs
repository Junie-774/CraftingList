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
    public unsafe class TimedIngameMacro
    {
        public string Name = "";
        public int Macro1Num = -1;
        public int Macro1DurationSeconds = 0;

        public int Macro2Num = -1;
        public int Macro2DurationSeconds  = 0;

        public uint FoodID = 0;
        public uint MedicineID = 0;

        public TimedIngameMacro(string name, int macro1Num, int macro1DurationSeconds, int macro2Num, int macro2DurationSeconds)
        {
            Name = name;
            Macro1DurationSeconds = macro1DurationSeconds;
            Macro1Num = macro1Num;
            Macro2Num = macro2Num;
            Macro2DurationSeconds = macro2DurationSeconds;
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
