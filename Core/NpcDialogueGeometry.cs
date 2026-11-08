namespace UntranslatedStardew.Core;

/// <summary>Screen bounds for a dialogue frame; independent of source parsing and input actions.</summary>
internal static class NpcDialogueGeometry
{
    internal const int VanillaHeight = 384;
    internal const int BottomPadding = 48;
    internal static int AvailableBody(int viewportHeight) => viewportHeight < VanillaHeight + 144
        ? 0 : viewportHeight - 144 - BottomPadding;
    internal static int FrameHeight(int contentHeight) => Math.Max(VanillaHeight, contentHeight + BottomPadding);
    internal static int PortraitTopOffset(int frameHeight) => (frameHeight - VanillaHeight) / 2;
}
