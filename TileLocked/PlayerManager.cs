using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using TileLocked.Config;

namespace TileLocked
{
  internal sealed class PlayerManager
  {
    private readonly TileManager tileManager;
    private string? lastPlayerLocation = null;
    private Vector2? lastPlayerPosition = null;

    public PlayerManager(TileManager tileManager)
    {
      this.tileManager = tileManager;
    }

    public void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
      lastPlayerLocation = TileManager.GetLocationKey(Game1.currentLocation);
      lastPlayerPosition = Game1.player.Position;

      if (IsPlayerBoxInLockedTile(Game1.player.GetBoundingBox()))
      {
        PurchaseCurrentTilesOrWarpHome();
      }
    }

    public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
      if (lastPlayerLocation == null
          || lastPlayerPosition == null
          || Game1.player.Position == lastPlayerPosition)
        return;

      Rectangle playerBox = Game1.player.GetBoundingBox();
      string location = TileManager.GetLocationKey(Game1.currentLocation);
      if (location != lastPlayerLocation)
      {
        if (IsPlayerBoxInLockedTile(playerBox))
        {
          PurchaseCurrentTilesOrWarpHome();
          lastPlayerLocation = location;
          lastPlayerPosition = Game1.player.Position;
          return;
        }
      }

      // If locked tile is more than 1 away from previous tile, assume this was a warp
      if (IsPlayerBoxInLockedTile(playerBox)
          && lastPlayerPosition != null
          && Vector2.Distance(Game1.player.position.Get(), lastPlayerPosition.Value) > 64)
      {
        PurchaseCurrentTilesOrWarpHome();
        lastPlayerLocation = location;
        lastPlayerPosition = Game1.player.Position;
        return;
      }

      // Prevent player from walking into locked tiles
      if (IsPlayerBoxInLockedTile(playerBox) && lastPlayerPosition != null)
      {
        int deltaX = Convert.ToInt32(Game1.player.position.X - lastPlayerPosition.Value.X);
        int deltaY = Convert.ToInt32(Game1.player.position.Y - lastPlayerPosition.Value.Y);
        Rectangle xOnlyPlayerBox = new(playerBox.X, playerBox.Y - deltaY, playerBox.Width, playerBox.Height);
        Rectangle yOnlyPlayerBox = new(playerBox.X - deltaX, playerBox.Y, playerBox.Width, playerBox.Height);
        if (!IsPlayerBoxInLockedTile(xOnlyPlayerBox))
        {
            Game1.player.Position -= new Vector2(0, deltaY);
        }
        else if (!IsPlayerBoxInLockedTile(yOnlyPlayerBox))
        {
            Game1.player.Position -= new Vector2(deltaX, 0);
        }
        else
        {
            Game1.player.Position = (Vector2)lastPlayerPosition;
        }
      }

      lastPlayerLocation = location;
      lastPlayerPosition = Game1.player.Position;
    }

    private void PurchaseCurrentTilesOrWarpHome()
    {
      Rectangle playerBox = Game1.player.GetBoundingBox();
      List<Vector2> currentTiles = new()
      {
        new Vector2(playerBox.Left / Game1.tileSize, playerBox.Top / Game1.tileSize),
        new Vector2((playerBox.Right - 1) / Game1.tileSize, playerBox.Top / Game1.tileSize),
        new Vector2(playerBox.Left / Game1.tileSize, (playerBox.Bottom - 1) / Game1.tileSize),
        new Vector2((playerBox.Right - 1) / Game1.tileSize, (playerBox.Bottom - 1) / Game1.tileSize)
      };

      foreach (Vector2 tile in currentTiles)
      {
        if (tileManager.IsTileUnlocked(Game1.currentLocation, tile))
          continue;

        if (tileManager.TryPurchaseTile(Game1.currentLocation, tile))
        {
          continue;
        }
        else if (PerSaveConfig.GetBool(PerSaveConfig.Key.KNOCK_OUT_ON_FAILED_UNLOCK_ATTEMPT))
        {
          Game1.addHUDMessage(new HUDMessage("Tried to visit a tile you can't afford. Good night...", HUDMessage.error_type));
          Game1.player.stamina = -15;
          return;
        }
        else
        {
          Game1.addHUDMessage(new HUDMessage("Tried to visit a tile you can't afford. A bonus tile was given to use instead.", HUDMessage.error_type));
          tileManager.AddBankedTiles(1);
          tileManager.TryPurchaseTile(Game1.currentLocation, tile);
        }
      }
    }

    private bool IsPlayerBoxInLockedTile(Rectangle playerBox)
    {
      var topLeft = new Vector2(playerBox.Left / Game1.tileSize, playerBox.Top / Game1.tileSize);
      var topRight = new Vector2((playerBox.Right - 1) / Game1.tileSize, playerBox.Top / Game1.tileSize);
      var bottomLeft = new Vector2(playerBox.Left / Game1.tileSize, (playerBox.Bottom - 1) / Game1.tileSize);
      var bottomRight = new Vector2((playerBox.Right - 1) / Game1.tileSize, (playerBox.Bottom - 1) / Game1.tileSize);

      return !tileManager.IsTileUnlocked(Game1.currentLocation, topLeft)
          || !tileManager.IsTileUnlocked(Game1.currentLocation, topRight)
          || !tileManager.IsTileUnlocked(Game1.currentLocation, bottomLeft)
          || !tileManager.IsTileUnlocked(Game1.currentLocation, bottomRight);
    }
  }
}