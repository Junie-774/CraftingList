using CraftingList.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.SeFunctions
{
    internal unsafe class PtrSynthesisSimpleDialog
    {
        public IntPtr Pointer;
        public static implicit operator PtrSynthesisSimpleDialog(IntPtr ptr)
            => new() { Pointer = ptr };
        public void ClickButton(int which)
        {
            Module.ClickAddon((void*) Pointer, null, EventType.Change, which);
        }
    }
}
