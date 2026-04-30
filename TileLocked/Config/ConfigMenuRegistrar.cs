using StardewModdingAPI;

namespace TileLocked.Config
{
  internal sealed class ConfigMenuRegistrar
  {
    private readonly IModHelper helper;
    private readonly IManifest modManifest;
    private ModConfig config;

    public ConfigMenuRegistrar(IModHelper helper, IManifest modManifest, ModConfig config)
    {
      this.helper = helper;
      this.modManifest = modManifest;
      this.config = config;
    }

    public void InitializeConfigMenu()
    {
      var configMenu = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
      if (configMenu == null)
      {
        return;
      }

      configMenu.Unregister(modManifest);
      configMenu.Register(modManifest, () => config = new ModConfig(), () => helper.WriteConfig(config));
      AddConfigOptionsForAllPlayers(configMenu);
      if (Context.IsWorldReady && Context.IsMainPlayer)
      {
        AddConfigOptionsForMainPlayer(configMenu);
      }
    }

    private void AddConfigOptionsForAllPlayers(IGenericModConfigMenuApi configMenu)
    {
      configMenu.AddSectionTitle(modManifest, () => "Save Creation");
      configMenu.AddBoolOption(
        mod: modManifest,
        name: () => "Enable mod for new saves",
        tooltip: () => "Enable or disable mod when creating a new save. Saves will always retain their Tile-Locked status.",
        getValue: () => config.ModEnabledForNewSaves,
        setValue: value => config.ModEnabledForNewSaves = value
      );
      configMenu.AddNumberOption(
        mod: modManifest,
        name: () => "Bonus tiles for new farmers",
        tooltip: () => "The number of bonus tiles you receive when starting a new game. For multiplayer games, this is per farmer.",
        getValue: () => config.NumBonusTilesForNewFarmers,
        setValue: value => config.NumBonusTilesForNewFarmers = value,
        min: 0
      );
      configMenu.AddSectionTitle(modManifest, () => "Tile Overlay");
      configMenu.AddKeybind(
        mod: modManifest,
        name: () => "Tile overlay toggle",
        tooltip: () => "Press this key to toggle the tile overlay mode between highlighting locked tiles, highlighting unlocked tiles, or turning the overlay off.",
        getValue: () => config.TileOverlayToggleKeybind,
        setValue: value => config.TileOverlayToggleKeybind = value
      );
      configMenu.AddTextOption(
        mod: modManifest,
        name: () => "Locked tile overlay color",
        tooltip: () => "The color used to highlight locked tiles, in hexadecimal RGB format (e.g. FF0000 for red).",
        getValue: () => config.LockedTileOverlayColor,
        setValue: value => config.LockedTileOverlayColor = value
      );
      configMenu.AddTextOption(
        mod: modManifest,
        name: () => "Unlocked tile overlay color",
        tooltip: () => "The color used to highlight unlocked tiles, in hexadecimal RGB format (e.g. 00FF00 for green).",
        getValue: () => config.UnlockedTileOverlayColor,
        setValue: value => config.UnlockedTileOverlayColor = value
      );
    }

    private void AddConfigOptionsForMainPlayer(IGenericModConfigMenuApi configMenu)
    {
      configMenu.AddSectionTitle(modManifest, () => "Tile Cost");
      configMenu.AddNumberOption(
        mod: modManifest,
        name: () => "# of tiles at 10g",
        tooltip: () => "The number of tiles that can be purchased for 10g before the price increases.",
        getValue: () => PerSaveConfig.GetInt(PerSaveConfig.Key.NUM_TILES_AT_10_GOLD),
        setValue: value => PerSaveConfig.Set(PerSaveConfig.Key.NUM_TILES_AT_10_GOLD, value.ToString(), helper),
        min: 0
      );
      configMenu.AddNumberOption(
        mod: modManifest,
        name: () => "# of tiles at 100g",
        tooltip: () => "The number of tiles that can be purchased for 100g before the price increases.",
        getValue: () => PerSaveConfig.GetInt(PerSaveConfig.Key.NUM_TILES_AT_100_GOLD),
        setValue: value => PerSaveConfig.Set(PerSaveConfig.Key.NUM_TILES_AT_100_GOLD, value.ToString(), helper),
        min: 0
      );
      configMenu.AddNumberOption(
        mod: modManifest,
        name: () => "# of tiles at 1000g",
        tooltip: () => "The number of tiles that can be purchased for 1000g before the price increases.",
        getValue: () => PerSaveConfig.GetInt(PerSaveConfig.Key.NUM_TILES_AT_1000_GOLD),
        setValue: value => PerSaveConfig.Set(PerSaveConfig.Key.NUM_TILES_AT_1000_GOLD, value.ToString(), helper),
        min: 0
      );
      configMenu.AddSectionTitle(modManifest, () => "Bonus Tiles");
      configMenu.AddNumberOption(
        mod: modManifest,
        name: () => "Bonus tiles for CC items",
        tooltip: () => "The number of bonus tiles received when donating an item to the community center.",
        getValue: () => PerSaveConfig.GetInt(PerSaveConfig.Key.NUM_BONUS_TILES_FOR_CC_ITEMS),
        setValue: value => PerSaveConfig.Set(PerSaveConfig.Key.NUM_BONUS_TILES_FOR_CC_ITEMS, value.ToString(), helper),
        min: 0
      );
      configMenu.AddNumberOption(
        mod: modManifest,
        name: () => "Bonus tiles for CC bundles",
        tooltip: () => "The number of additional bonus tiles received when completing a community center bundle.",
        getValue: () => PerSaveConfig.GetInt(PerSaveConfig.Key.NUM_BONUS_TILES_FOR_CC_BUNDLES),
        setValue: value => PerSaveConfig.Set(PerSaveConfig.Key.NUM_BONUS_TILES_FOR_CC_BUNDLES, value.ToString(), helper),
        min: 0
      );
      configMenu.AddNumberOption(
        mod: modManifest,
        name: () => "Bonus tiles for museum donations",
        tooltip: () => "The number of additional bonus tiles received when donating a new item to the museum.",
        getValue: () => PerSaveConfig.GetInt(PerSaveConfig.Key.NUM_BONUS_TILES_FOR_MUSEUM_ITEMS),
        setValue: value => PerSaveConfig.Set(PerSaveConfig.Key.NUM_BONUS_TILES_FOR_MUSEUM_ITEMS, value.ToString(), helper),
        min: 0
      );
      configMenu.AddSectionTitle(modManifest, () => "Other");
      configMenu.AddBoolOption(
        mod: modManifest,
        name: () => "Knock out on failed unlock attempt",
        tooltip: () => "If enabled, failing to unlock a tile you are forced to unlock (e.g. by warping to a locked tile without any bonus tiles or cash) will cause your character to be knocked out. If disabled, you will simply unlock the tile for free.",
        getValue: () => PerSaveConfig.GetBool(PerSaveConfig.Key.KNOCK_OUT_ON_FAILED_UNLOCK_ATTEMPT),
        setValue: value => PerSaveConfig.Set(PerSaveConfig.Key.KNOCK_OUT_ON_FAILED_UNLOCK_ATTEMPT, value.ToString(), helper)
      );
    }
  }
}