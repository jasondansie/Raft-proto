namespace RaftProto.Core
{
    /// <summary>
    /// Reports whether the most recent Interact press collected a floating resource.
    /// Lets building defer placement when pickup wins on the same frame.
    /// </summary>
    public interface IResourceInteractPickup
    {
        bool LastInteractCollected { get; }
    }
}
