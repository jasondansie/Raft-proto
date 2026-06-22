namespace RaftProto.Core
{
    /// <summary>
    /// When true, resource pickup/hook input is ignored (e.g. while build mode is active).
    /// Implemented by <c>BuildingSystem</c> without the Resources assembly referencing Building.
    /// </summary>
    public interface IBlocksResourcePickup
    {
        bool BlocksResourcePickup { get; }
    }
}
