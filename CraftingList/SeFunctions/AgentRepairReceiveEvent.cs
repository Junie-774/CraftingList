using Dalamud.Game;
using System;

namespace CraftingList.SeFunctions
{
    public delegate long AgentRepairReceiveEventDelegate(IntPtr a1, long a2, long a3, long a4, long a5);
    internal class AgentRepairReceiveEvent : SeFunction<AgentRepairReceiveEventDelegate>
    {

        public AgentRepairReceiveEvent(SigScanner sig)
            : base(sig, "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 20 48 8B 44 24 ?? 49 8B F8 4C 8B F2 48 8B D9")
        {

        }
    }
}
