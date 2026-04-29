using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using TileLocked.Config;
using TileLocked.Multiplayer;
using TileLocked.Rendering;

namespace TileLocked
{
  public class ModEntry : Mod
  {
    private const int HOME_TILE_X = 64;
    private const int HOME_TILE_Y = 15;
    private const bool ALLOW_HIDING_PLAYER = true;
    private readonly int[] VAULT_BUNDLE_KEYS = new int[] {23, 24, 25, 26};

    private ModConfig config = new();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private ConfigMenuRegistrar configMenuRegistrar;
    private TileManager tileManager;
    private TileOverlayRenderer tileOverlayRenderer;
    private TileInfoRenderer tileInfoRenderer;
    private UnveilingGlassTooltipRenderer unveilingGlassTooltipRenderer;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private string? lastPlayerLocation = null;
    private Vector2? lastPlayerPosition = null;
    private Vector2? lastHoveredTile = null;
    private CommunityCenter? communityCenter;
    private readonly Dictionary<int, bool[]> bundleState = new();

    public override void Entry(IModHelper helper)
    {
      tileManager = new(helper);
      config = helper.ReadConfig<ModConfig>();
      configMenuRegistrar = new ConfigMenuRegistrar(helper, ModManifest, config);
      tileOverlayRenderer = new(config, tileManager);
      tileInfoRenderer = new(tileManager);
      unveilingGlassTooltipRenderer = new(tileManager);

      helper.Events.GameLoop.GameLaunched += OnGameLaunched;
      helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
      helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;

      helper.Events.Display.RenderedHud += IfEnabled<RenderedHudEventArgs>(OnRenderedHud);
      helper.Events.Display.RenderingHud += IfEnabled<RenderingHudEventArgs>(OnRenderingHud);
      helper.Events.GameLoop.Saving += IfEnabled<SavingEventArgs>(OnSaving);
      helper.Events.GameLoop.DayStarted += IfEnabled<DayStartedEventArgs>(OnDayStarted);
      helper.Events.GameLoop.UpdateTicked += IfEnabled<UpdateTickedEventArgs>(OnUpdateTicked);
      helper.Events.Input.ButtonPressed += IfEnabled<ButtonPressedEventArgs>(OnButtonPressed);
      helper.Events.Input.CursorMoved += IfEnabled<CursorMovedEventArgs>(OnCursorMoved);
      helper.Events.Display.RenderedWorld += IfEnabled<RenderedWorldEventArgs>(OnRenderedWorld);
      helper.Events.Multiplayer.PeerContextReceived += IfEnabled<PeerContextReceivedEventArgs>(OnPeerContextReceived);
    }

    private static EventHandler<T> IfEnabled<T>(EventHandler<T> action)
    {
      return (sender, args) =>
      {
        if (PerSaveConfig.GetBool(PerSaveConfig.Key.MOD_ENABLED))
        {
          action(sender, args);
        }
      };
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
      configMenuRegistrar.InitializeConfigMenu();
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
      tileInfoRenderer.OnRenderedHud();
      unveilingGlassTooltipRenderer.OnRenderedHud(lastHoveredTile);
    }

    private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
    {
      tileInfoRenderer.OnRenderingHud();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
      if (Context.IsMainPlayer && Game1.stats.DaysPlayed <= 1 && config.ModEnabledForNewSaves)
      {
        PerSaveConfig.Set(PerSaveConfig.Key.MOD_ENABLED, "true");
      }
      if (!PerSaveConfig.GetBool(PerSaveConfig.Key.MOD_ENABLED))
      {
        return;
      }

      if (Game1.stats.DaysPlayed <= 1)
      {
        if (Context.IsMainPlayer)
          tileManager.Reset();
        tileManager.AddBankedTiles(config.NumBonusTilesForNewFarmers);
        Game1.player.addItemToInventory(ItemRegistry.Create("(O)" + TileLockedConstants.UNVEILING_GLASS_ITEM_NAME));
      }
      
      if (Context.IsMainPlayer)
      {
        tileManager.LoadData();
        communityCenter = Game1.getLocationFromName("CommunityCenter") as CommunityCenter;
        UpdateBundleState();
      }

      configMenuRegistrar.InitializeConfigMenu();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
      tileManager.SaveData();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
      LogInfo("OnDayStarted");

      lastPlayerLocation = TileManager.GetLocationKey(Game1.currentLocation);
      lastPlayerPosition = Game1.player.Position;

      if (!tileManager.IsTileUnlocked(Game1.currentLocation, Game1.player.Tile))
      {
        LogDebug("Starting tile is not purchased yet.");
        PurchaseCurrentTilesOrWarpHome();
      }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      if (Game1.activeClickableMenu is JunimoNoteMenu)
      {
        CheckForBundleChanges();
        UpdateBundleState();
      }

      if (lastPlayerLocation == null
          || lastPlayerPosition == null
          || Game1.player.Position == lastPlayerPosition)
        return;

      string location = TileManager.GetLocationKey(Game1.currentLocation);
      if (location != lastPlayerLocation)
      {
        LogDebug("Warped to new location.");
        if (!tileManager.IsTileUnlocked(Game1.currentLocation, Game1.player.Tile))
        {
          LogDebug("Warped to tile is not purchased yet.");
          PurchaseCurrentTilesOrWarpHome();
          lastPlayerLocation = location;
          lastPlayerPosition = Game1.player.Position;
          return;
        }
      }

      Rectangle playerBox = Game1.player.GetBoundingBox();

      // If locked tile is more than 1 away from previous tile, assume this was a warp
      if (IsPlayerBoxInLockedTile(playerBox)
          && lastPlayerPosition != null
          && Vector2.Distance(Game1.player.position.Get(), lastPlayerPosition.Value) > 64)
      {
        LogDebug("Player moved far enough to be considered a warp.");
        if (!tileManager.IsTileUnlocked(Game1.currentLocation, Game1.player.Tile))
        {
          LogDebug("Warped to tile is not purchased yet.");
          PurchaseCurrentTilesOrWarpHome();
          lastPlayerLocation = location;
          lastPlayerPosition = Game1.player.Position;
          return;
        }
      }

      // Prevent player from walking into locked tiles
      if (IsPlayerBoxInLockedTile(playerBox) && lastPlayerPosition != null)
      {
        LogDebug("Player moved to locked tile. Moving player back.");
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

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      LogInfo("OnButtonPressed: " + e.Button.ToString());

      Item? activeItem = Game1.player.ActiveItem;
      LogDebug("Active item: " + activeItem?.ItemId);
      if (e.Button.IsActionButton()
          && activeItem != null
          && activeItem.ItemId.Equals(TileLockedConstants.UNVEILING_GLASS_ITEM_NAME))
      {
        OnUseUnveilingGlass(sender, e);
      }
      else if (e.Button == config.TileOverlayToggleKeybind)
      {
        tileOverlayRenderer.ToggleOverlayMode();
      }
      else if (e.Button == SButton.P && ALLOW_HIDING_PLAYER)
      {
        Game1.displayFarmer = !Game1.displayFarmer;
      }
    }

    private void OnCursorMoved(object? sender, CursorMovedEventArgs e)
    {
      lastHoveredTile = e.NewPosition.Tile;
    }

    private void OnPeerContextReceived(object? sender, PeerContextReceivedEventArgs e)
    {
      if (Context.IsMainPlayer)
      {
        Helper.Multiplayer.SendMessage(
          tileManager.GetPeerConnectionMessage(),
          PeerConnectionMessage.TYPE,
          new[] { ModManifest.UniqueID },
          new[] { e.Peer.PlayerID }
        );
      }
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
      if (e.FromModID != ModManifest.UniqueID)
        return;
      
      switch (e.Type)
      {
        case PeerConnectionMessage.TYPE:
          PeerConnectionMessage peerConnectionMessage = e.ReadAs<PeerConnectionMessage>();
          tileManager.LoadFromPeerConnectionMessage(peerConnectionMessage);
          break;
        case TileUnlockedMessage.TYPE:
          TileUnlockedMessage tileUnlockedMessage = e.ReadAs<TileUnlockedMessage>();
          tileManager.TileUnlocked(tileUnlockedMessage.locationKey, tileUnlockedMessage.tile, tileUnlockedMessage.purchased);
          break;
        case BankedTilesAddedMessage.TYPE:
          BankedTilesAddedMessage bankedTilesAddedMessage = e.ReadAs<BankedTilesAddedMessage>();
          tileManager.BankedTilesAdded(bankedTilesAddedMessage.quantity);
          break;
        case BankedTileUsedMessage.TYPE:
          tileManager.BankedTileUsed();
          break;
        case PerSaveConfigUpdateMessage.TYPE:
          PerSaveConfigUpdateMessage perSaveConfigUpdateMessage = e.ReadAs<PerSaveConfigUpdateMessage>();
          PerSaveConfig.Set(perSaveConfigUpdateMessage.key, perSaveConfigUpdateMessage.value);
          break;
      }
    }

    private void OnUseUnveilingGlass(object? sender, ButtonPressedEventArgs e)
    {
      LogInfo("OnUseUnveilingGlass");

      Vector2 tile = e.Cursor.Tile;
      GameLocation location = Game1.currentLocation;
      bool isMineshaft = location is MineShaft;

      LogDebug("Current location: " + location.Name);
      LogDebug("Map path: " + location.mapPath.Value);
      LogDebug("Is Mineshaft: " + isMineshaft);
      LogDebug("Clicked tile: " + tile);

      if (tileManager.IsTileUnlocked(location, tile))
      {
        LogDebug("Tile already unlocked: " + tile);
        return;
      }

      if (tileManager.TryPurchaseTile(location, tile))
        LogDebug("Tile purchased: " + tile);
      else
        LogDebug("Failed to purchase tile.");
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
      tileOverlayRenderer.OnRenderedWorld(e, lastHoveredTile);
    }

    private void PurchaseCurrentTilesOrWarpHome()
    {
      Rectangle playerBox = Game1.player.GetBoundingBox();
      List<Vector2> currentTiles = new()
      {
        // Top left
        new Vector2(playerBox.Left / Game1.tileSize, playerBox.Top / Game1.tileSize),
        // Top right
        new Vector2((playerBox.Right - 1) / Game1.tileSize, playerBox.Top / Game1.tileSize),
        // Bottom left
        new Vector2(playerBox.Left / Game1.tileSize, (playerBox.Bottom - 1) / Game1.tileSize),
        // Bottom right
        new Vector2((playerBox.Right - 1) / Game1.tileSize, (playerBox.Bottom - 1) / Game1.tileSize)
      };

      foreach (Vector2 tile in currentTiles)
      {
        if (tileManager.IsTileUnlocked(Game1.currentLocation, tile))
          continue;
        
        LogDebug("Attempting to purchase tile: " + tile);
        if (tileManager.TryPurchaseTile(Game1.currentLocation, tile))
        {
          LogDebug("Tile purchased.");
        }
        else {
          LogDebug("Tile cannot be purchased. Warping home...");
          Game1.warpFarmer("Farm", HOME_TILE_X, HOME_TILE_Y, Farmer.down);
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

    private void CheckForBundleChanges()
    {
      if (communityCenter != null)
      {
        foreach (var kvp in communityCenter.bundles.Pairs)
        {
          if (!bundleState.ContainsKey(kvp.Key))
          {
            bundleState[kvp.Key] = (bool[])kvp.Value.Clone();
            continue;
          }

          for (int i = 0; i < bundleState[kvp.Key].Length; i++)
          {
            if (kvp.Value[i] && !bundleState[kvp.Key][i])
            {
              LogInfo("Found changed bundle item for key: " + kvp.Key);
              bool bundleComplete = false;
              if (communityCenter.bundles.TryGetValue(kvp.Key, out bool[] slots))
              {
                // Workaround for vault bundles
                if (slots.All(slot => slot) || VAULT_BUNDLE_KEYS.Contains(kvp.Key))
                {
                  LogDebug("All slots filled. Granting 25 bonus tiles.");
                  tileManager.AddBankedTiles(25);
                  UpdateBundleState();
                  bundleComplete = true;
                }
              }
              if (!bundleComplete)
              {
                LogDebug("Bundle not complete. Granting 5 bonus tiles.");
                tileManager.AddBankedTiles(5);
                UpdateBundleState();
              }
            }
          }
        }
      }
    }

    private void UpdateBundleState()
    {
      if (communityCenter != null)
      {
        bundleState.Clear();
        foreach (var kvp in communityCenter.bundles.Pairs)
        {
          bundleState[kvp.Key] = (bool[])kvp.Value.Clone();
        }
      }
    }

    private void LogDebug(string message)
    {
      Monitor.Log(message, LogLevel.Debug);
    }

    private void LogInfo(string message)
    {
      Monitor.Log(message, LogLevel.Info);
    }
  }
}
