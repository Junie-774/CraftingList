using Dalamud.Game;
using System;

namespace CraftingList.SeFunctions
{
    public delegate byte UseActionDelegate(IntPtr AM, uint actionType, uint actionID, long targetID, uint a4, uint a5, int a6, IntPtr a7);

    internal class UseAction : SeFunction<UseActionDelegate>
    {
        public UseAction(SigScanner sigScanner)
            : base(sigScanner, "E8 ?? ?? ?? ?? EB 64 B1 01")
        {

        }
    }
}
