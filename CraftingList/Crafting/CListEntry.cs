namespace CraftingList.Crafting
{
    public unsafe class CListEntry
    {
        public string Name;
        public uint ItemId;
        public int MaxCrafts;
        public int CurrentCrafts;
        public bool Complete = false;
        public CraftingMacro Macro;
        public uint FoodId = 0;
        public bool running = false;

        public CListEntry(string name, uint itemId, int maxCrafts, CraftingMacro macro, uint food)
        {
            this.Name = name;
            this.ItemId = itemId;
            this.MaxCrafts = maxCrafts;
            this.CurrentCrafts = 0;
            this.Macro = macro;
            this.FoodId = food;

        }
    }
}
