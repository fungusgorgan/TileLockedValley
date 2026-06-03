using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Triggers;

namespace TileLocked.Extension
{
  internal sealed class DelegateRegistrar
  {
    private const string GSQ_BONUS_TILES = "Fungus.TileLockedValley_BONUS_TILES";
    private const string GSQ_UNLOCKED_TILES = "Fungus.TileLockedValley_UNLOCKED_TILES";
    private const string GSQ_PURCHASED_TILES = "Fungus.TileLockedValley_PURCHASED_TILES";
    private const string GSQ_TILE_UNLOCKED = "Fungus.TileLockedValley_TILE_UNLOCKED";

    private const string ACTION_GIVE_BONUS_TILES = "Fungus.TileLockedValley_GiveBonusTiles";
    private const string ACTION_PURCHASE_TILE = "Fungus.TileLockedValley_PurchaseTile";

    private readonly TileManager tileManager;

    public DelegateRegistrar(TileManager tileManager)
    {
      this.tileManager = tileManager;
    }

    public void InitializeDelegates()
    {
      GameStateQuery.Register(GSQ_BONUS_TILES, BonusTilesGameStateQuery);
      GameStateQuery.Register(GSQ_UNLOCKED_TILES, UnlockedTilesGameStateQuery);
      GameStateQuery.Register(GSQ_PURCHASED_TILES, PurchasedTilesGameStateQuery);
      GameStateQuery.Register(GSQ_TILE_UNLOCKED, TileUnlockedGameStateQuery);

      TriggerActionManager.RegisterAction(ACTION_GIVE_BONUS_TILES, GiveBonusTilesTriggerAction);
      TriggerActionManager.RegisterAction(ACTION_PURCHASE_TILE, UnlockTileTriggerAction);
    }

    private bool BonusTilesGameStateQuery(string[] query, GameStateQueryContext context)
    {
      if (!ArgUtility.TryGetInt(query, 1, out int min, out string error, "int minCount"))
        return GameStateQuery.Helpers.ErrorResult(query, error);

      if (!ArgUtility.TryGetInt(query, 2, out int max, out string _, "int maxCount"))
        max = int.MaxValue;

      int count = tileManager.GetNumBonusTiles();

      return count >= min && count <= max;
    }

    private bool UnlockedTilesGameStateQuery(string[] query, GameStateQueryContext context)
    {
      if (!ArgUtility.TryGetInt(query, 1, out int min, out string error, "int minCount"))
        return GameStateQuery.Helpers.ErrorResult(query, error);

      if (!ArgUtility.TryGetInt(query, 2, out int max, out string _, "int maxCount"))
        max = int.MaxValue;

      int count = tileManager.GetNumUnlockedTiles();

      return count >= min && count <= max;
    }

    private bool PurchasedTilesGameStateQuery(string[] query, GameStateQueryContext context)
    {
      if (!ArgUtility.TryGetInt(query, 1, out int min, out string error, "int minCount"))
        return GameStateQuery.Helpers.ErrorResult(query, error);

      if (!ArgUtility.TryGetInt(query, 2, out int max, out string _, "int maxCount"))
        max = int.MaxValue;

      int count = tileManager.GetNumPurchasedTiles();

      return count >= min && count <= max;
    }

    private bool TileUnlockedGameStateQuery(string[] query, GameStateQueryContext context)
    {
      GameLocation location = context.Location;
      if (!GameStateQuery.Helpers.TryGetLocationArg(query, 1, ref location, out string error))
        return GameStateQuery.Helpers.ErrorResult(query, error);

      if (!ArgUtility.TryGetInt(query, 2, out int x, out error, "int xCoordinate"))
        return GameStateQuery.Helpers.ErrorResult(query, error);

      if (!ArgUtility.TryGetInt(query, 3, out int y, out error, "int yCoordinate"))
        return GameStateQuery.Helpers.ErrorResult(query, error);

      return tileManager.IsTileUnlocked(location, new Vector2(x, y));
    }

    private bool GiveBonusTilesTriggerAction(string[] args, TriggerActionContext context, out string error)
    {
      if (!ArgUtility.TryGetInt(args, 1, out int count, out error))
        return false;

      tileManager.AddBankedTiles(count);
      return true;
    }

    private bool UnlockTileTriggerAction(string[] args, TriggerActionContext context, out string error)
    {
      GameLocation location = Game1.currentLocation;
      if (!GameStateQuery.Helpers.TryGetLocationArg(args, 1, ref location, out error))
        return false;

      if (!ArgUtility.TryGetInt(args, 2, out int x, out error, "int xCoordinate"))
        return false;

      if (!ArgUtility.TryGetInt(args, 3, out int y, out error, "int yCoordinate"))
        return false;

      return tileManager.TryPurchaseTile(location, new Vector2(x, y));
    }
  }
}
