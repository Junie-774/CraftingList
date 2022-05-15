using Dalamud.Game;
using System;

namespace CraftingList.SeFunctions
{
    public delegate void AddonRecipeNoteReceiveEventDelegate(IntPtr atkUnit, ushort eventType, int which, IntPtr source, IntPtr unused);
    internal class AddonRecipeNoteReceiveEvent : SeFunction<AddonRecipeNoteReceiveEventDelegate>
    {
        public AddonRecipeNoteReceiveEvent(SigScanner sig)
            : base(sig, "40 53 48 83 EC 30 0F B7 C2 4D 8B D1")
        { }
    }
}
