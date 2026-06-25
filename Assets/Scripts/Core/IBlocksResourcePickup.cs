namespace RaftProto.Core
{
    /// <summary>
    /// When true, the resource hook is ignored (e.g. while build mode is active).
    /// Interact pickup is not blocked — see <see cref="IResourceInteractPickup"/>.
    /// </summary>
    public interface IBlocksResourcePickup
    {
        bool BlocksResourcePickup { get; }
    }
}
