namespace RaftProto.Core
{
    /// <summary>
    /// Exposes whether the player is currently swimming. Lets Building and other systems
    /// react without referencing the Player assembly.
    /// </summary>
    public interface ISwimStateProvider
    {
        bool IsSwimming { get; }
    }
}
