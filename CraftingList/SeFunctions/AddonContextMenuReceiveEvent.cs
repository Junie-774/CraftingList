using Dalamud.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.SeFunctions
{
    public delegate void AddonContextReceiveEventDelegate(IntPtr atkUnit, ushort eventType, int which, IntPtr source, IntPtr unused);
    internal class AddonContextMenuReceiveEvent : SeFunction<AddonContextReceiveEventDelegate>
    {
        public AddonContextMenuReceiveEvent(SigScanner sig)
            :base(sig, "40 53 55 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 0F B7 C2")
        { }
    }
}
