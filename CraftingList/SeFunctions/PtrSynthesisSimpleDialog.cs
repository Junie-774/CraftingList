using CraftingList.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.SeFunctions
{
    public unsafe struct PtrSynthesisSimpleDialog
    {
        public IntPtr Pointer;
        public static implicit operator PtrSynthesisSimpleDialog(IntPtr ptr)
            => new() { Pointer = ptr };

        public static explicit operator nint(PtrSynthesisSimpleDialog v)
            => v.Pointer;

        public void ClickButton(int which)
        {
            Utility.Module.ClickAddon((void*)Pointer, null, EventType.Change, which);
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

        public void SetHQMats(bool useHQ)
        {
            AtkComponentCheckBox* node = (AtkComponentCheckBox*)((AtkUnitBase*)Pointer)->GetNodeById(5)->GetAsAtkComponentNode()->Component;
            if (useHQ)
            {
                node->AtkComponentButton.Flags |= (1U << 18);
            }
            else
            {
                node->AtkComponentButton.Flags &= ~(1U << 18);
            }
        }
    }
}
