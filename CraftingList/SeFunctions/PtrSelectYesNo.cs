using CraftingList.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;

namespace CraftingList.SeFunctions
{
    public unsafe struct PtrSelectYesNo
    {
        private const int yesButtonId = 0;
        private const int noButtonId = 1;

        AddonSelectYesno* AddonPointer;

        public static implicit operator PtrSelectYesNo(IntPtr addonPointer)
            => new() { AddonPointer = Module.Cast<AddonSelectYesno>(addonPointer) };

        public static implicit operator bool(PtrSelectYesNo selectYesNo)
            => selectYesNo.AddonPointer != null;

        public void ClickYes()
        {
            Module.ClickAddon(AddonPointer, AddonPointer->YesButton, EventType.Change, yesButtonId);
        }

        public void ClickNo()
        {
            Module.ClickAddon(AddonPointer, AddonPointer->NoButton, EventType.Change, noButtonId);
        }
    }
}
