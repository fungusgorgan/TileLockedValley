using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Triggers;
using TileLocked.Config;
using TileLocked.Multiplayer;
using TileLocked.Rendering;

namespace TileLocked
{
  public class ModEntry : Mod
  {
    private const string GSQ_BONUS_TILES = "Fungus.TileLockedValley_BONUS_TILES";
    private const string GSQ_UNLOCKED_TILES = "Fungus.TileLockedValley_UNLOCKED_TILES";
    private const string GSQ_PURCHASED_TILES = "Fungus.TileLockedValley_PURCHASED_TILES";
    private const string GSQ_TILE_UNLOCKED = "Fungus.TileLockedValley_TILE_UNLOCKED";
    
    private const string TA_GIVE_BONUS_TILES = "Fungus.TileLockedValley_GiveBonusTiles";
    private const string TA_UNLOCK_TILE = "Fungus.TileLockedValley_UnlockTile";
    
    private ModConfig config = new();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private ConfigMenuRegistrar configMenuRegistrar;
    private RewardsManager rewardsManager;
    private TileManager tileManager;
    private PlayerManager playerManager;
    private InputManager inputManager;
    private MultiplayerManager multiplayerManager;
    private TileOverlayRenderer tileOverlayRenderer;
    private TileInfoRenderer tileInfoRenderer;
    private UnveilingGlassTooltipRenderer unveilingGlassTooltipRenderer;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public override void Entry(IModHelper helper)
    {
      tileManager = new(helper);
      rewardsManager = new(tileManager);
      config = helper.ReadConfig<ModConfig>();

      configMenuRegistrar = new ConfigMenuRegistrar(helper, ModManifest, config);
      tileOverlayRenderer = new(config, tileManager);
      inputManager = new(config, tileManager, tileOverlayRenderer);
      playerManager = new(tileManager);
      multiplayerManager = new(helper, ModManifest, tileManager);
      tileInfoRenderer = new(tileManager);
      unveilingGlassTooltipRenderer = new(tileManager);

      helper.Events.GameLoop.GameLaunched += OnGameLaunched;
      helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
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
      
      GameStateQuery.Register(GSQ_BONUS_TILES, BonusTilesGameStateQuery);
      GameStateQuery.Register(GSQ_UNLOCKED_TILES, UnlockedTilesGameStateQuery);
      GameStateQuery.Register(GSQ_PURCHASED_TILES, PurchasedTilesGameStateQuery);
      GameStateQuery.Register(GSQ_TILE_UNLOCKED, TileUnlockedGameStateQuery);
      
      TriggerActionManager.RegisterAction(TA_GIVE_BONUS_TILES, GiveBonusTilesTriggerAction);
      TriggerActionManager.RegisterAction(TA_UNLOCK_TILE, UnlockTileTriggerAction);
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
      unveilingGlassTooltipRenderer.OnRenderedHud(inputManager.LastHoveredTile);
    }

    private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
    {
      tileInfoRenderer.OnRenderingHud();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
      configMenuRegistrar.InitializeConfigMenu();
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
        rewardsManager.Initialize();
      }

      configMenuRegistrar.InitializeConfigMenu();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
      tileManager.SaveData();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
      playerManager.OnDayStarted(sender, e);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      if (Context.IsMainPlayer)
      {
        rewardsManager.CheckForChanges();
      }

      playerManager.OnUpdateTicked(sender, e);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      inputManager.OnButtonPressed(sender, e);
    }

    private void OnCursorMoved(object? sender, CursorMovedEventArgs e)
    {
      if (!Context.IsWorldReady) return;

      inputManager.OnCursorMoved(sender, e);
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
      tileOverlayRenderer.OnRenderedWorld(e, inputManager.LastHoveredTile);
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
      multiplayerManager.OnModMessageReceived(sender, e);
    }

    private void OnPeerContextReceived(object? sender, PeerContextReceivedEventArgs e)
    {
      multiplayerManager.OnPeerContextReceived(sender, e);
    }

    private bool BonusTilesGameStateQuery(string[] query, GameStateQueryContext context)
    {
      if(!ArgUtility.TryGetInt(query, 1, out int min, out string error, "int minCount"))
        return GameStateQuery.Helpers.ErrorResult(query, error);
      
      if (!ArgUtility.TryGetInt(query, 2, out int max, out string _, "int maxCount"))
        max = int.MaxValue;

      int count = tileManager.GetNumBonusTiles();

      return count > min && count < max;
    }
    
    private bool UnlockedTilesGameStateQuery(string[] query, GameStateQueryContext context)
    {
      if(!ArgUtility.TryGetInt(query, 1, out int min, out string error, "int minCount"))
        return GameStateQuery.Helpers.ErrorResult(query, error);
      
      if (!ArgUtility.TryGetInt(query, 2, out int max, out string _, "int maxCount"))
        max = int.MaxValue;

      int count = tileManager.GetNumUnlockedTiles();

      return count > min && count < max;
    }
    
    private bool PurchasedTilesGameStateQuery(string[] query, GameStateQueryContext context)
    {
      if(!ArgUtility.TryGetInt(query, 1, out int min, out string error, "int minCount"))
        return GameStateQuery.Helpers.ErrorResult(query, error);
      
      if (!ArgUtility.TryGetInt(query, 2, out int max, out string _, "int maxCount"))
        max = int.MaxValue;

      int count = tileManager.GetNumPurchasedTiles();

      return count > min && count < max;
    }

    private bool TileUnlockedGameStateQuery(string[] query, GameStateQueryContext context)
    {
      GameLocation location = context.Location;
      if (!GameStateQuery.Helpers.TryGetLocationArg(query, 1, ref location, out string error))
        return GameStateQuery.Helpers.ErrorResult(query, error);
      
      if(!ArgUtility.TryGetInt(query, 2, out int x, out error, "int xCoordinate"))
        return GameStateQuery.Helpers.ErrorResult(query, error);
      
      if(!ArgUtility.TryGetInt(query, 3, out int y, out error, "int yCoordinate"))
        return GameStateQuery.Helpers.ErrorResult(query, error);

      return tileManager.IsTileUnlocked(location, new Vector2(x, y));
    }

    private bool GiveBonusTilesTriggerAction(string[] args, TriggerActionContext context, out string error)
    {
      if(!ArgUtility.TryGetInt(args, 1, out int count, out error))
        return false;

      tileManager.AddBankedTiles(count);
      return true;
    }

    private bool UnlockTileTriggerAction(string[] args, TriggerActionContext context, out string error)
    {
      GameLocation location = Game1.currentLocation;
      if (!GameStateQuery.Helpers.TryGetLocationArg(args, 1, ref location, out error))
        return false;
      
      if(!ArgUtility.TryGetInt(args, 2, out int x, out error, "int xCoordinate"))
        return false;
      
      if(!ArgUtility.TryGetInt(args, 3, out int y, out error, "int yCoordinate"))
        return false;
      
      return tileManager.TryPurchaseTile(location, new Vector2(x, y));
    }
    
  }
}
