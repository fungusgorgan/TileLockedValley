using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using TileLocked.Config;
using TileLocked.Delegate;
using TileLocked.Multiplayer;
using TileLocked.Rendering;

namespace TileLocked
{
  public class ModEntry : Mod
  {
    private ModConfig config = new();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private ConfigMenuRegistrar configMenuRegistrar;
    private DelegateRegistrar delegateRegistrar;
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
      delegateRegistrar = new DelegateRegistrar(tileManager);
      tileOverlayRenderer = new(config, tileManager);
      inputManager = new(config, tileManager, tileOverlayRenderer);
      playerManager = new(tileManager);
      multiplayerManager = new(helper, ModManifest, tileManager);
      tileInfoRenderer = new(config, tileManager);
      unveilingGlassTooltipRenderer = new(tileManager);

      helper.Events.GameLoop.GameLaunched += OnGameLaunched;
      helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
      helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
      helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
      helper.Events.Player.Warped += OnWarped;

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
      delegateRegistrar.InitializeDelegates();
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

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
      if (!e.IsLocalPlayer) return;

      if (PerSaveConfig.GetBool(PerSaveConfig.Key.ONLY_LOCK_TILES_PLAYER_CAN_REACH))
      {
        tileManager.UpdateReachableTiles(e.NewLocation);
        tileManager.ClearStaleCachedTiles();
      }
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
      multiplayerManager.OnModMessageReceived(sender, e);
    }

    private void OnPeerContextReceived(object? sender, PeerContextReceivedEventArgs e)
    {
      multiplayerManager.OnPeerContextReceived(sender, e);
    }
  }
}
