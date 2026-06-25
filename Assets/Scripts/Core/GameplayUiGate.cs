namespace RaftProto.Core
{
    /// <summary>
    /// Tracks open gameplay menus for the local player. Unlocks the cursor and blocks
    /// camera look while any panel is open.
    /// </summary>
    public static class GameplayUiGate
    {
        private static int _openPanelCount;

        public static bool BlocksCameraLook => _openPanelCount > 0;

        public static void SetPanelOpen(bool open)
        {
            _openPanelCount += open ? 1 : -1;
            if (_openPanelCount < 0)
            {
                _openPanelCount = 0;
            }

            ApplyCursor();
        }

        private static void ApplyCursor()
        {
            bool showCursor = _openPanelCount > 0;
            UnityEngine.Cursor.lockState = showCursor
                ? UnityEngine.CursorLockMode.None
                : UnityEngine.CursorLockMode.Locked;
            UnityEngine.Cursor.visible = showCursor;
        }
    }
}
