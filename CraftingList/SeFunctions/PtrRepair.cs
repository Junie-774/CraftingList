using CraftingList.Utility;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

[Addon("Repair")]
[StructLayout(LayoutKind.Explicit, Size = 0xF7E8)]
unsafe struct AddonRepair65
{
    [FieldOffset(616)] public AtkComponentButton* RepairAllButton;
}

namespace CraftingList.SeFunctions
{
    public unsafe struct PtrRepair
    {
        private const int repairAllButtonId = 25;
        AddonRepair65* AddonPointer;

        public static implicit operator PtrRepair(IntPtr pointer)
            => new() { AddonPointer = Module.Cast<AddonRepair65>(pointer) };

        public static implicit operator bool(PtrRepair addonRepair)
        {
            return addonRepair.AddonPointer != null;
        }


        public void ClickRepairAll()
        {
            if (AddonPointer == null) return;
            Module.ClickAddon(AddonPointer, AddonPointer->RepairAllButton->AtkComponentBase.OwnerNode, EventType.Change, repairAllButtonId);
        }
    }
}
