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

        public string MacroName;

        public CListEntry(string name, uint itemId, string numCrafts, string macroName)
        {
            this.Name = name;
            this.ItemId = itemId;
            this.NumCrafts = numCrafts == "max" || int.TryParse(numCrafts, out _) ? numCrafts : "0";
            

            this.MacroName = macroName;
        }

        public void Decrement()
        {
            if (!int.TryParse(NumCrafts, out int numCrafts))
                return;
            NumCrafts = (numCrafts - 1).ToString();
            if (NumCrafts == "0") Complete = true;
        }

        public void Decrement(int numCompleted)
        {
            if (!int.TryParse(NumCrafts, out int numCrafts))
                return;

            NumCrafts = (numCrafts - numCompleted).ToString();
            if (NumCrafts == "0") Complete = true;
        }

        public override string ToString()
        {
            return $"[Name: \"{Name}\", ItemId: {ItemId}, NumCrafts: {NumCrafts}, Macro name: \"{MacroName}\"]";
        }

        public static CListEntry Clone(CListEntry other)
        {
            return new CListEntry(other.Name, other.ItemId, other.NumCrafts, other.Name);
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
