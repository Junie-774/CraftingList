using Dalamud.Game;
using System;

namespace CraftingList.SeFunctions
{
    public delegate void AddonRepairReceiveEventDelegate(IntPtr atkUnit, ushort eventType, int which, IntPtr source, IntPtr unused);

    internal class AddonRepairReceiveEvent : SeFunction<AddonRepairReceiveEventDelegate>
    {
        public AddonRepairReceiveEvent(SigScanner sig)
            : base(sig, "48 89 5C 24 ?? 55 56 41 56 48 8B EC 48 83 EC 70 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 F0 4C 8B 75 40")
        {

        }
    }
}
