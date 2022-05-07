namespace CraftingList.Crafting
{

    public enum Status
    {
        Normal = 0,
        Poor = 1,
        Good = 2,
        Excellent = 3,
    }

    public static class StatusExtension
    {
        public static bool Improved(this Status status)
            => status == Status.Good || status == Status.Excellent;
    }
}