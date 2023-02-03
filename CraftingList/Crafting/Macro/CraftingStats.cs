using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.Crafting.Macro
{
    public class CraftingStats
    {
        public int Craftsmanship { get; set; } = 0;
        public int Control { get; set; } = 0;
        public int CP { get; set; } = 0;

        public CraftingStats(int cms, int ctrl, int cp)
        {
            Craftsmanship = cms;
            Control = ctrl;
            CP = cp;
        }
    }
}
