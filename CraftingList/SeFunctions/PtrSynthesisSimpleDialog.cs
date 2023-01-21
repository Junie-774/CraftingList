using CraftingList.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.SeFunctions
{
    public unsafe struct PtrSynthesisSimpleDialog
    {
        public IntPtr Pointer;
        public static implicit operator PtrSynthesisSimpleDialog(IntPtr ptr)
            => new() { Pointer = ptr };
        public void ClickButton(int which)
        {
            Module.ClickAddon((void*)Pointer, null, EventType.Change, which);
        }

        public void StartSynthesis()
        {
            ClickButton(0);
        }

        public void SetAmount(int amount)
        {
            if (amount < 1 || amount > 99)
                return;
            AtkComponentNumericInput* node = (AtkComponentNumericInput*)((AtkUnitBase*)Pointer)->GetNodeById(6)->GetAsAtkComponentNode()->Component;
            node->SetValue(amount);
        }
    }
}
