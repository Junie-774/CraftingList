using Dalamud.Game;
using System;

namespace CraftingList.SeFunctions
{
    public delegate void AgentRecipeNoteHideDelegate(IntPtr agentRecipeNote);
    internal class AgentRecipeNoteHide : SeFunction<AgentRecipeNoteHideDelegate>
    {
        public AgentRecipeNoteHide(SigScanner sig)
            : base(sig, "48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 4B 10")
        { }
    }
}
