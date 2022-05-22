namespace CraftingList.Crafting
{
    public unsafe class CListEntry
    {
        public string Name;
        public uint ItemId;
        public string NumCrafts;
        public bool Complete = false;
        public CraftingMacro Macro;
        public uint FoodId = 0;
        public bool running = false;

        public CListEntry(string name, uint itemId, string numCrafts, CraftingMacro macro, uint food)
        {
            this.Name = name;
            this.ItemId = itemId;
            this.NumCrafts = numCrafts == "max" || int.TryParse(numCrafts, out _) ? numCrafts : "0";
            this.Macro = macro;
            this.FoodId = food;

        }
    }
}
