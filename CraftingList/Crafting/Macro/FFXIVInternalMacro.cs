using FFXIVClientStructs.FFXIV.Client.System.String;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.Crafting.Macro
{
    [StructLayout(LayoutKind.Sequential, Size = 0x688)]
    public unsafe struct FFXIVInternalMacro
    {
        public uint IconId;
        public uint Unk;
        public Utf8String Name;
        public Lines Line;

        public FFXIVInternalMacro(uint iconId, uint unk, string name, string line)
            : this(iconId, unk, name, new string[] { line })
        {

        }

        public FFXIVInternalMacro(uint iconId, uint unk, string name, string[] lines)
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
                        Buffer.MemoryCopy(value, p + sizeof(Utf8String) * i, 0x618 - sizeof(Utf8String) * i, sizeof(Utf8String));
                    }
                }
            }
        }
    }
}
