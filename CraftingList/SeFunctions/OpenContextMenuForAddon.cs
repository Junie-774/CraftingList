using Dalamud.Game;
using System;

namespace CraftingList.SeFunctions
{
    public delegate void OpenContextMenuForAddonDelegate(IntPtr ContextAgent, uint ownerAddonId, bool bindToOwner);
    internal class OpenContextMenuForAddon : SeFunction<OpenContextMenuForAddonDelegate>
    {
        public OpenContextMenuForAddon(SigScanner sig)
            : base(sig, "41 0F B6 C0 89 91")
        {

        }
    }
}
