using Dalamud.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.SeFunctions
{
    public delegate void OpenContextMenuForAddonDelegate(IntPtr ContextAgent, uint ownerAddonId, bool bindToOwner);
    internal class OpenContextMenuForAddon : SeFunction<OpenContextMenuForAddonDelegate>
    {
        public OpenContextMenuForAddon(SigScanner sig)
            :base(sig, "41 0F B6 C0 89 91")
        {

        }
    }
}
