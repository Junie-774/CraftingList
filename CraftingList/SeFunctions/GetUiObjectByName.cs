using Dalamud.Game;
using System;

namespace CraftingList.SeFunctions
{
    public delegate IntPtr GetUiObjectByNameDelegate(IntPtr baseUiObj, string name, int index);

    public sealed class GetUiObjectByName : SeFunction<GetUiObjectByNameDelegate>
    {
        public GetUiObjectByName(SigScanner sigScanner)
            : base(sigScanner, "E8 ?? ?? ?? ?? 48 8B CF 48 89 87 ?? ?? 00 00 E8 ?? ?? ?? ?? 41 B8 01 00 00 00")
        {

        }
    }
}
