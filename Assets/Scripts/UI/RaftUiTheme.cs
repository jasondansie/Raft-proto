using UnityEngine;

namespace RaftProto.UI
{
    /// <summary>
    /// Procedural Raft-like UI colors until dedicated art assets exist.
    /// </summary>
    internal static class RaftUiTheme
    {
        public static readonly Color PanelFill = new Color(0.58f, 0.42f, 0.28f, 0.96f);
        public static readonly Color PanelBorder = new Color(0.32f, 0.2f, 0.11f, 1f);
        public static readonly Color TitleText = new Color(0.93f, 0.84f, 0.68f, 1f);
        public static readonly Color BodyText = new Color(0.88f, 0.78f, 0.62f, 1f);
        public static readonly Color SlotFill = new Color(0.42f, 0.28f, 0.16f, 1f);
        public static readonly Color SlotBorder = new Color(0.24f, 0.14f, 0.08f, 1f);
        public static readonly Color SlotSelected = new Color(0.95f, 0.78f, 0.35f, 0.85f);
        public static readonly Color Divider = new Color(0.28f, 0.17f, 0.09f, 0.9f);
    }
}
