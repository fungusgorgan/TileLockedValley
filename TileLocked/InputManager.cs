using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using TileLocked.Config;
using TileLocked.Rendering;

namespace TileLocked
{
  internal sealed class InputManager
  {
    private readonly ModConfig config;
    private readonly TileManager tileManager;
    private readonly TileOverlayRenderer tileOverlayRenderer;
    public Vector2? LastHoveredTile { get; private set; }

    public InputManager(ModConfig config, TileManager tileManager, TileOverlayRenderer tileOverlayRenderer)
    {
      this.config = config;
      this.tileManager = tileManager;
      this.tileOverlayRenderer = tileOverlayRenderer;
    }

    public void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
      // Ignore if player is not free to act (e.g. in a menu, cutscene, etc.)
      if (!Context.IsPlayerFree)
      {
        return;
      }

      Item? activeItem = Game1.player.ActiveItem;
      if (e.Button == config.UnlockTileKeybind
          && activeItem != null
          && activeItem.ItemId.Equals(TileLockedConstants.UNVEILING_GLASS_ITEM_NAME))
      {
        OnUseUnveilingGlass(e);
      }
      else if (e.Button == config.TileOverlayToggleKeybind)
      {
        tileOverlayRenderer.ToggleOverlayMode();
      }
    }

    public void OnCursorMoved(object? sender, CursorMovedEventArgs e)
    {
      LastHoveredTile = e.NewPosition.Tile;
    }

    private void OnUseUnveilingGlass(ButtonPressedEventArgs e)
    {
      Vector2 tile = e.Cursor.Tile;
      GameLocation location = Game1.currentLocation;

      if (!tileManager.IsTileUnlocked(location, tile))
      {
        tileManager.TryPurchaseTile(location, tile);
      }
    }
  }
}