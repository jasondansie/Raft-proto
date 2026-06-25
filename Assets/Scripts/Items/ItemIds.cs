namespace RaftProto.Items
{
    /// <summary>
    /// Stable string IDs synced over the network later — never sync ScriptableObject references.
    /// </summary>
    public static class ItemIds
    {
        public const string Wood = "wood";
        public const string Plastic = "plastic";
        public const string Scrap = "scrap";
        public const string Plank = "plank";
    }
}
