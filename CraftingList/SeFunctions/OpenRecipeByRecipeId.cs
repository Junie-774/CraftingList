using Dalamud.Game;
using System;

namespace CraftingList.SeFunctions
{
    public delegate IntPtr OpenRecipeByRecipeIdDelegate(IntPtr recipeNote, int recipeId);
    public sealed class OpenRecipebyRecipeId : SeFunction<OpenRecipeByRecipeIdDelegate>
    {
        public OpenRecipebyRecipeId(SigScanner sigScanner)
            : base(sigScanner, "48 89 5C 24 ?? 57 48 83 EC 20 83 B9 ?? ?? ?? ?? ?? 8B FA 48 8B D9 0F 85")
        {
        }
    }
}
