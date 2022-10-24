namespace CraftingList.Crafting
{
    public unsafe class CListEntry
    {
        public bool Complete { get; set; } = false;

        public string Name;
        public uint ItemId;
        public string NumCrafts;
        
        public bool running = false;
        public int[] HQSelection = new int[6];

        //Dummy variables to store stuff for the UI
        public int MacroIndex;

        public CListEntry(string name, uint itemId, string numCrafts, int macroIndex)
        {
            this.Name = name;
            this.ItemId = itemId;
            this.NumCrafts = numCrafts == "max" || int.TryParse(numCrafts, out _) ? numCrafts : "0";
            

            this.MacroIndex = macroIndex;
        }

        public void Decrement()
        {
            if (NumCrafts.ToLower() == "max") return;
            NumCrafts = (int.Parse(NumCrafts) - 1).ToString();
            if (NumCrafts == "0") Complete = true;
        }

        public static CListEntry Clone(CListEntry other)
        {
            return new CListEntry(other.Name, other.ItemId, other.NumCrafts, other.MacroIndex);
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
