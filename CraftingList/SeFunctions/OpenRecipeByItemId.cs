using Dalamud.Game;
using System;

namespace CraftingList.SeFunctions
{
    public delegate IntPtr OpenRecipeByItemIdDelegate(IntPtr recipeNote, int id);
    internal class OpenRecipeByItemId : SeFunction<OpenRecipeByItemIdDelegate>
    {
        public OpenRecipeByItemId(SigScanner sigScanner)
            : base(sigScanner, "E8 ?? ?? ?? ?? EB 7A 48 83 F8 06")
        {
        }
    }
}
