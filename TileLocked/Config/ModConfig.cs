using StardewModdingAPI;

namespace TileLocked.Config
{
  internal sealed class ModConfig
  {
    public bool ModEnabledForNewSaves { get; set; } = true;
    public int NumBonusTilesForNewFarmers { get; set; } = 10;
    public SButton UnlockTileKeybind { get; set; } = SButton.MouseRight;
    public SButton TileOverlayToggleKeybind { get; set; } = SButton.L;
    public string LockedTileOverlayColor { get; set; } = "FF0000";
    public string UnlockedTileOverlayColor { get; set; } = "008000";
    public bool ShowTotalUnlockedTilesOnTooltip { get; set; } = false;
  }
}
