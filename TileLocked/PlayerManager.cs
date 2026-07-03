using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using TileLocked.Config;

namespace TileLocked
{
  internal sealed class PlayerManager
  {
    private readonly TileManager tileManager;
    private readonly Dictionary<long, string?> _lastPlayerLocations = new();
    private readonly Dictionary<long, Vector2?> _lastPlayerPositions = new();

    private T? GetPlayerValue<T>(Dictionary<long, T?> dict)
    {
        return dict.TryGetValue(Game1.player.UniqueMultiplayerID, out var value)
            ? value
            : default;
    }

    private void SetPlayerValue<T>(Dictionary<long, T?> dict, T? value)
    {
        dict[Game1.player.UniqueMultiplayerID] = value;
    }

    private string? lastPlayerLocation
    {
        get => GetPlayerValue(_lastPlayerLocations);
        set => SetPlayerValue(_lastPlayerLocations, value);
    }

    private Vector2? lastPlayerPosition
    {
        get => GetPlayerValue(_lastPlayerPositions);
        set => SetPlayerValue(_lastPlayerPositions, value);
    }

    public PlayerManager(TileManager tileManager)
    {
      this.tileManager = tileManager;
    }

    public void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
      lastPlayerLocation = TileManager.GetLocationKey(Game1.player.currentLocation);
      lastPlayerPosition = Game1.player.Position;

      if (IsPlayerBoxInLockedTile(Game1.player.GetBoundingBox()))
      {
        PurchaseCurrentTilesOrWarpHome();
      }
    }

    private bool ShouldSkipUpdateTick()
    {
        if (lastPlayerLocation == null || lastPlayerPosition == null)
            return true;

        if (Game1.player.Position == lastPlayerPosition)
            return true;

        if (Game1.farmEvent != null)
            return true;

        if (PerSaveConfig.GetBool(PerSaveConfig.Key.DISABLE_LOCKED_TILES_DURING_CUTSCENES)
            && Game1.player.IsBusyDoingSomething() && !Game1.player.canMove )
            return true;

        return false;
    }

    public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
      if ( ShouldSkipUpdateTick() ) return;

      string location = TileManager.GetLocationKey(Game1.player.currentLocation);
      if (IsPlayerInBusTransit())
      {
        lastPlayerLocation = location;
        lastPlayerPosition = Game1.player.Position;
        return;
      }

      Rectangle playerBox = Game1.player.GetBoundingBox();
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
      
      // If player encounters a tile that is now reachable but was not when the map was loaded (Beach Bridge Unlock, for example)
      if (
        PerSaveConfig.GetBool(PerSaveConfig.Key.ONLY_LOCK_TILES_PLAYER_CAN_REACH) &&
        IsPlayerFacingAnUncachedReachableTile(out Vector2 tile)
        )
      {
          tileManager.ExpandReachableTiles(
              Game1.currentLocation,
              tile
          );
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
      // Create a rect from playerbox and shrink it to tune how close the player can get to the edge of neighboring tiles.
      Rectangle collisionPlayerbox = playerBox;
      int collisionPadding = -Game1.tileSize / 6;
      collisionPlayerbox.Inflate(collisionPadding, collisionPadding);

      if (IsPlayerBoxInLockedTile(collisionPlayerbox) && lastPlayerPosition != null)
      {
        Vector2 delta = Game1.player.nextPositionVector2() - collisionPlayerbox.Center.ToVector2();

        //Check if the player had only moved in the X axis if there would still be a collision.
        Rectangle xOnlyPlayerBox = collisionPlayerbox;
        xOnlyPlayerBox.Offset(0, -delta.Y);
        bool needToRevertXMovement = IsPlayerBoxInLockedTile(xOnlyPlayerBox);

        //Check if the player had only moved in the Y axis if there would still be a collision.
        Rectangle yOnlyPlayerBox = collisionPlayerbox;
        yOnlyPlayerBox.Offset(-delta.X, 0);
        bool needToRevertYMovement = IsPlayerBoxInLockedTile(yOnlyPlayerBox);

        //Revert X movement, Y movement, both, or none depending on collision results.
        Game1.player.Position = new Vector2(
            needToRevertXMovement ? lastPlayerPosition.Value.X : Game1.player.Position.X, 
            needToRevertYMovement ? lastPlayerPosition.Value.Y : Game1.player.Position.Y
        );
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
        if (tileManager.IsTileUnlocked(Game1.player.currentLocation, tile))
          continue;

        if (tileManager.TryPurchaseTile(Game1.player.currentLocation, tile))
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
          tileManager.TryPurchaseTile(Game1.player.currentLocation, tile);
        }
      }
    }

    private static bool IsPlayerInBusTransit()
    {
      return Game1.player.currentLocation switch
      {
        Desert desert => desert.drivingBack || desert.drivingOff,
        BusStop busStop => busStop.drivingBack || busStop.drivingOff,
        _ => false
      };
    }

    private bool IsPlayerBoxInLockedTile(Rectangle playerBox)
    {
      var topLeft = new Vector2(playerBox.Left / Game1.tileSize, playerBox.Top / Game1.tileSize);
      var topRight = new Vector2((playerBox.Right - 1) / Game1.tileSize, playerBox.Top / Game1.tileSize);
      var bottomLeft = new Vector2(playerBox.Left / Game1.tileSize, (playerBox.Bottom - 1) / Game1.tileSize);
      var bottomRight = new Vector2((playerBox.Right - 1) / Game1.tileSize, (playerBox.Bottom - 1) / Game1.tileSize);

      return !tileManager.IsTileUnlocked(Game1.player.currentLocation, topLeft)
          || !tileManager.IsTileUnlocked(Game1.player.currentLocation, topRight)
          || !tileManager.IsTileUnlocked(Game1.player.currentLocation, bottomLeft)
          || !tileManager.IsTileUnlocked(Game1.player.currentLocation, bottomRight);
    }

    private bool IsPlayerFacingAnUncachedReachableTile(out Vector2 tile)
    {
        tile = Vector2.Zero;

        GameLocation location = Game1.currentLocation;

        Vector2 facingTile = Game1.player.Tile;

        switch (Game1.player.FacingDirection)
        {
            case Game1.up:
                facingTile.Y -= 1;
                break;

            case Game1.right:
                facingTile.X += 1;
                break;

            case Game1.down:
                facingTile.Y += 1;
                break;

            case Game1.left:
                facingTile.X -= 1;
                break;
        }

        if (tileManager.ReachableTiles.Contains(facingTile))
            return false;

        if (tileManager.IsNeverWalkable(location, facingTile))
            return false;

        tile = facingTile;
        return true;
    }
  }
}