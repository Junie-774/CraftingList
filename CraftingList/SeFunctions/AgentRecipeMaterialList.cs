using Dalamud.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.SeFunctions
{
    public delegate long AgentRecipeMaterialListReceiveEventDelegate(IntPtr a1, long a2, long a3, long a4, long a5);
    
    public class AgentRecipeMaterialListReceiveEvent : SeFunction<AgentRecipeMaterialListReceiveEventDelegate>
    {
        public AgentRecipeMaterialListReceiveEvent(SigScanner sigScanner)
            : base(sigScanner, "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 48 8B D9 48 89 55 40")
        {
        }
    }
}
