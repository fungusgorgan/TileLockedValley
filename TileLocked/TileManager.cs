using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using TileLocked.Config;
using TileLocked.Multiplayer;

namespace TileLocked
{
  internal sealed class TileManager
  {
    private const string SAVE_KEY = "Fungus.TileLockedValley_TileData";
    private const string SAVE_LOCATION = "TileData";

    private readonly IModHelper helper;

    private Dictionary<string, HashSet<Vector2>> unlockedTiles = new();
    private int numBonusTiles = 0;
    private int numUnlockedTiles = 0;
    private int numPurchasedTiles = 0;

    private readonly Dictionary<string, HashSet<Vector2>> reachableTiles = new();
    public HashSet<Vector2> ReachableTiles
    {
        get
        {
            string key = GetLocationKey(Game1.currentLocation);

            if (!reachableTiles.TryGetValue(key, out var tiles))
            {
                tiles = new HashSet<Vector2>();
                reachableTiles[key] = tiles;
            }

            return tiles;
        }
    }

    public TileManager(IModHelper helper)
    {
      this.helper = helper;
    }

    public static string GetLocationKey(GameLocation location)
    {
      if (location is AnimalHouse || location.Name.Contains("Shed"))
      {
        return location.NameOrUniqueName;
      }

      string mapPath = location.mapPath.Value;      
      if (mapPath.Contains("Beach")) {
        return "Beach";
      }
      if (mapPath.Contains("Forest")) {
        return "Forest";
      }
      if (mapPath.Contains("Town")) {
        return "Town";
      }
      if (mapPath.Contains("Desert"))
      {
        return "Desert";
      }

      if (location is MineShaft mineShaft)
      {
        // Skull cavern levels are 121+
        if (mineShaft.mineLevel > 120)
        {
          return "SkullCavern_" + mapPath;
        }
        return location.Name + "_" + mapPath;
      }
      if (location is VolcanoDungeon volcanoDungeon)
      {
        return "VolcanoDungeon_" + volcanoDungeon.layoutIndex;
      }
      return location.Name;
    }

    public void Reset()
    {
      unlockedTiles = new();
      numBonusTiles = 0;
      numUnlockedTiles = 0;
      numPurchasedTiles = 0;
    }

    public bool IsTileUnlocked(GameLocation location, Vector2 tile)
    {
      string key = GetLocationKey(location);
      if (!unlockedTiles.ContainsKey(key))
        return false;
      Vector2 realTile = GetRealTile(
        tile,
        location is FarmHouse,
        location is FarmHouse farmhouse ? farmhouse.upgradeLevel : 0,
        location is Shed,
        location is Shed shed && shed.mapPath.Contains("Shed2"));
      return unlockedTiles[key].Contains(realTile);
    }

    public bool IsNeverWalkable(GameLocation location, Vector2 tile)
    {

        if (!location.isTileOnMap(tile))
            return true;

        //Volcano Dungeon Lava    
        if (location is VolcanoDungeon)
        {
          if (location.isWaterTile((int)tile.X, (int)tile.Y) &&
            !location.CanRefillWateringCanOnTile((int)tile.X,(int)tile.Y))
            return false;
        }

        string tileaction = location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Action","Buildings");

        if (tileaction != null && ( tileaction.Contains("Door") || tileaction.Contains("Bridge") ))
        {
            // This is a door or bridge tile.
            //Game1.addHUDMessage(new HUDMessage("Action Found: " + tileaction , HUDMessage.achievement_type));
            return false;
        }

        if (!location.isTilePassable(tile))
            return true;

        return false;
    }

    public void TileUnlocked(TileUnlockedMessage message)
    {
      string locationKey = message.locationKey;
      if (!unlockedTiles.ContainsKey(locationKey))
        unlockedTiles[locationKey] = new HashSet<Vector2>();

      Vector2 realTile = GetRealTile(message.tile, message.isFarmhouse, message.farmhouseUpgradeLevel, message.isShed, message.isUpgradedShed);
      unlockedTiles[locationKey].Add(realTile);
      numUnlockedTiles++;

      if (message.purchased)
        numPurchasedTiles++;
    }

    public bool TryPurchaseTile(GameLocation location, Vector2 tile)
    {
      if (numBonusTiles > 0)
      {
        BankedTileUsed();
        UnlockTile(location, tile, false);
        helper.Multiplayer.SendMessage(
          new BankedTileUsedMessage(),
          BankedTileUsedMessage.TYPE,
          new[] { helper.ModRegistry.ModID }
        );
        return true;
      }

      int cost = GetTilePurchaseCost();
      if (Game1.player.Money < cost)
        return false;

      Game1.player.Money -= cost;
      UnlockTile(location, tile, true);
      return true;
    }

    public void AddBankedTiles(int quantity)
    {
      BankedTilesAdded(quantity);

      helper.Multiplayer.SendMessage(
        new BankedTilesAddedMessage() { quantity = quantity },
        BankedTilesAddedMessage.TYPE,
        new[] { helper.ModRegistry.ModID }
      );
    }

    public void BankedTilesAdded(int quantity)
    {
      numBonusTiles += quantity;
      Game1.addHUDMessage(new HUDMessage("+" + quantity + " bonus tiles!", HUDMessage.achievement_type));
    }

    public void BankedTileUsed()
    {
      if (numBonusTiles > 0)
        --numBonusTiles;
    }

    public void SaveData()
    {
      UnlockedTilesModel model = new()
      {
        Unlocked = unlockedTiles,
        NumBonus = numBonusTiles,
        NumUnlocked = numUnlockedTiles,
        NumPurchased = numPurchasedTiles,
      };
      helper.Data.WriteSaveData(SAVE_KEY, model);
    }

    public void LoadData()
    {
      var model = helper.Data.ReadSaveData<UnlockedTilesModel>(SAVE_KEY);

      if (model == null)
        model = helper.Data.ReadJsonFile<UnlockedTilesModel>(GetSaveLocation());
      
      if (model != null)
      {
        unlockedTiles = model.Unlocked ?? new();
        numBonusTiles = model.NumBonus;
        numUnlockedTiles = model.NumUnlocked;
        numPurchasedTiles = model.NumPurchased;
      }
    }

    public PeerConnectionMessage GetPeerConnectionMessage()
    {
      return new PeerConnectionMessage()
      {
        unlockedTiles = unlockedTiles,
        numBonusTiles = numBonusTiles,
        numUnlockedTiles = numUnlockedTiles,
        numPurchasedTiles = numPurchasedTiles,
        perSaveConfig = PerSaveConfig.GetConfig(),
      };
    }

    public void LoadFromPeerConnectionMessage(PeerConnectionMessage message)
    {
      unlockedTiles = message.unlockedTiles;
      numBonusTiles = message.numBonusTiles;
      numUnlockedTiles = message.numUnlockedTiles;
      numPurchasedTiles = message.numPurchasedTiles;
      PerSaveConfig.SetAll(message.perSaveConfig);
    }

    public int GetTilePurchaseCost()
    {
      if (numPurchasedTiles < PerSaveConfig.GetInt(PerSaveConfig.Key.NUM_TILES_AT_10_GOLD))
        return 10;
      if (numPurchasedTiles < PerSaveConfig.GetInt(PerSaveConfig.Key.NUM_TILES_AT_100_GOLD))
        return 100;
      if (numPurchasedTiles < PerSaveConfig.GetInt(PerSaveConfig.Key.NUM_TILES_AT_1000_GOLD))
        return 1000;
      return 10000;
    }

    public int GetNumBonusTiles()
    {
      return numBonusTiles;
    }

    public int GetNumUnlockedTiles()
    {
      return numUnlockedTiles;
    }

    public int GetNumPurchasedTiles()
    {
      return numPurchasedTiles;
    }

    public HashSet<Vector2> GetReachableTiles(
        GameLocation location,
        HashSet<Vector2>? visited = null,
        Vector2? discoveredTile = null)
    {
        visited ??= new HashSet<Vector2>();

        Queue<Vector2> queue = new();

        if (discoveredTile is Vector2 seedTile && visited.Add(seedTile))
        {
            queue.Enqueue(seedTile);
        }

        foreach (Vector2 seed in GetEntranceTiles(location))
        {
            if (visited.Add(seed))
                queue.Enqueue(seed);
        }


        while (queue.Count > 0)
        {
            Vector2 tile = queue.Dequeue();

            foreach (Vector2 neighbor in GetNeighbors(tile))
            {
                if (visited.Contains(neighbor))
                    continue;

                if (IsNeverWalkable(location, neighbor))
                    continue;

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return visited;
    }
    public void UpdateReachableTiles(
        GameLocation location,
        HashSet<Vector2>? visited = null)
    {
        reachableTiles[GetLocationKey(location)] = GetReachableTiles(location, visited);
    }
    
    public void ExpandReachableTiles(
        GameLocation location,
        Vector2? start = null)
    {
        reachableTiles[GetLocationKey(location)] = GetReachableTiles(location, reachableTiles[GetLocationKey(location)], start);
    }

    public void ClearStaleCachedTiles()
    {
      var occupiedLocations = Game1.getAllFarmers()
        .Select(f => GetLocationKey(f.currentLocation))
        .ToHashSet();

      foreach (string key in reachableTiles.Keys.ToList())
        {
            if (!occupiedLocations.Contains(key))
                reachableTiles.Remove(key);
        }
    }

    public Dictionary<string, int> DebugReachableTileKeys()
    {
        return reachableTiles.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Count);
    }
    
    private static IEnumerable<Vector2> GetNeighbors(Vector2 tile)
    {
        yield return new Vector2(tile.X - 1, tile.Y);
        yield return new Vector2(tile.X + 1, tile.Y);
        yield return new Vector2(tile.X, tile.Y - 1);
        yield return new Vector2(tile.X, tile.Y + 1);
    }

    private static IEnumerable<Vector2> GetEntranceTiles(GameLocation location)
    {
        foreach (Warp warp in location.warps)
        {
            yield return new Vector2(warp.X, warp.Y);
        }
        // include player position as fallback seed
        yield return Game1.player.Tile;
    }

    private void UnlockTile(GameLocation location, Vector2 tile, bool purchased)
    {
      TileUnlockedMessage message = BuildTileUnlockedMessage(location, tile, purchased);
      TileUnlocked(message);
      
      Game1.playSound("purchaseClick");

      helper.Multiplayer.SendMessage(
        message,
        TileUnlockedMessage.TYPE,
        new[] { helper.ModRegistry.ModID }
      );
    }

    private static Vector2 GetRealTile(Vector2 tile, bool isFarmhouse = false, int farmhouseUpgradeLevel = 0, bool isShed = false, bool isUpgradedShed = false)
    {
      if (isFarmhouse)
      {
        return GetFarmhouseTile(tile, farmhouseUpgradeLevel);
      }
      if (isShed)
      {
        return GetShedTile(tile, isUpgradedShed);
      }
      return tile;
    }

    private static Vector2 GetFarmhouseTile(Vector2 tile, int upgradeLevel)
    {
      return tile - GetFarmhouseUpgradeOffset(upgradeLevel);
    }

    private static Vector2 GetShedTile(Vector2 tile, bool isUpgraded)
    {
      if (isUpgraded)
        return tile - new Vector2(3, 3);
      return tile;
    }

    private static TileUnlockedMessage BuildTileUnlockedMessage(GameLocation location, Vector2 tile, bool purchased)
    {
      TileUnlockedMessage message = new() {
        locationKey = GetLocationKey(location), 
        tile = tile,
        purchased = purchased};
      if (location is FarmHouse farmhouse)
      {
        message.isFarmhouse = true;
        message.farmhouseUpgradeLevel = farmhouse.upgradeLevel;
      }
      else if (location is Shed shed)
      {
        message.isShed = true;
        message.isUpgradedShed = shed.mapPath.Contains("Shed2");
      }
      return message;
    }

    private static Vector2 GetFarmhouseUpgradeOffset(int upgradeLevel)
    {
      return upgradeLevel switch
      {
        1 => new Vector2(6, 0),
        2 => new Vector2(24, 19),
        3 => new Vector2(24, 19),
        _ => new Vector2(0, 0),
      };
    }

    private static string GetSaveLocation()
    {
      return $"data/${Constants.SaveFolderName}_{SAVE_LOCATION}.json";
    }
  }

  public class UnlockedTilesModel
  {
    public Dictionary<string, HashSet<Vector2>> Unlocked { get; set; } = new();
    public int NumBonus;
    public int NumUnlocked;
    public int NumPurchased;
  }
}