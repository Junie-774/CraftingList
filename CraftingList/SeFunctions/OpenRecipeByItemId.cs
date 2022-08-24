using Dalamud.Game;
using System;

namespace CraftingList.SeFunctions
{
    public delegate IntPtr OpenRecipeByItemIdDelegate(IntPtr recipeNote, int id);
    internal class OpenRecipeByItemId : SeFunction<OpenRecipeByItemIdDelegate>
    {
        public OpenRecipeByItemId(SigScanner sigScanner)
            : base(sigScanner, "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 83 F8 06")
        {
        }
    }
}
