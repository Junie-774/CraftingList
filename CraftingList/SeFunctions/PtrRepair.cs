using CraftingList.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;

namespace CraftingList.SeFunctions
{
    public unsafe struct PtrRepair
    {
        private const int repairAllButtonId = 25;
        AddonRepair* AddonPointer;

        public static implicit operator PtrRepair(IntPtr pointer)
            => new() { AddonPointer = Module.Cast<AddonRepair>(pointer) };

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
