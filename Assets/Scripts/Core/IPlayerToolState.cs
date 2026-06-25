namespace RaftProto.Core
{
    /// <summary>
    /// Read-only view of the locally selected tool. Building, hook, and UI query this
    /// instead of reading inventory directly.
    /// </summary>
    public interface IPlayerToolState
    {
        EquippedToolType ActiveTool { get; }
        bool IsHammerActive { get; }
        bool IsAxeActive { get; }
        int SelectedHotbarSlotIndex { get; }
    }
}
