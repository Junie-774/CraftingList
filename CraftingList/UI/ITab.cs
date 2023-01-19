using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.UI
{
    internal interface ITab : IDisposable
    {
        public string Name { get; }

        void Draw();

    }
}
