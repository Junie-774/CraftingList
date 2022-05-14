using Dalamud.Game;
using System;

namespace CraftingList.SeFunctions
{
    public delegate void AddonContextReceiveEventDelegate(IntPtr atkUnit, ushort eventType, int which, IntPtr source, IntPtr unused);
    internal class AddonContextMenuReceiveEvent : SeFunction<AddonContextReceiveEventDelegate>
    {
        public AddonContextMenuReceiveEvent(SigScanner sig)
            : base(sig, "40 53 48 83 EC 30 0F B7 C2 4D 8B D1")
        { }
    }
}
