namespace CraftingList.Crafting
{
    public unsafe class CListEntry
    {
        public bool Complete { get; set; } = false;

        public string Name;
        public uint ItemId;
        public string NumCrafts;
        
        public CraftingMacro Macro;
        public uint FoodId = 0;
        public bool running = false;
        public bool HQMats = false;
        public int[] HQSelection = new int[6];

        //Dummy variables to store stuff for the UI
        public int FoodIndex;
        public int MacroIndex;

        public CListEntry(string name, uint itemId, string numCrafts, CraftingMacro macro, uint food, bool hqmats, int macroIndex, int foodIndex)
        {
            this.Name = name;
            this.ItemId = itemId;
            this.NumCrafts = numCrafts == "max" || int.TryParse(numCrafts, out _) ? numCrafts : "0";
            
            this.Macro = macro;
            this.FoodId = food;
            this.HQMats = hqmats;

            this.MacroIndex = macroIndex;
            this.FoodIndex = foodIndex;
        }
        /*
        public CListEntry(CListEntry other)
        {
            this.Name = other.Name;
            this.ItemId = other.ItemId;
            this.NumCrafts = other.NumCrafts;
            this.Macro = other.Macro;
            this.FoodId = other.FoodId;
            this.HQMats = other.HQMats;
            this.HQSelection = other.HQSelection;
        }*/
    }
}
