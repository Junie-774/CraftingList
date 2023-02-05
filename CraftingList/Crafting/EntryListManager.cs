using CraftingList.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.Crafting
{
    internal class EntryListManager
    {
        public static List<CListEntry> Entries
            => Service.Configuration.EntryList;


        public static void AddEntry(CListEntry entry)
        {
            entry.EntryId = Entries.Count;
            Entries.Add(entry);
        }

        public static void ReassignIds()
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                Entries[i].EntryId = i; 
            }
        }
    }
}
