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
    private const string SAVE_LOCATION = "TileData";

    private readonly IModHelper helper;

    private Dictionary<string, HashSet<Vector2>> unlockedTiles = new();
    private int numBonusTiles = 0;
    private int numUnlockedTiles = 0;
    private int numPurchasedTiles = 0;

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
      if (location is FarmHouse farmHouse)
      {
        return IsOffsetTileUnlocked(location, tile - GetFarmhouseUpgradeOffset(farmHouse.upgradeLevel));
      }
      if (location is Shed shed)
      {
        return IsOffsetTileUnlocked(location, tile - GetShedUpgradeOffset(shed));
      }
      return IsOffsetTileUnlocked(location, tile);
    }

    private void UnlockTile(GameLocation location, Vector2 tile, bool purchased)
    {
      string key = GetLocationKey(location);
      TileUnlocked(key, tile, purchased);
      
      Game1.playSound("purchaseClick");

      helper.Multiplayer.SendMessage(
        new TileUnlockedMessage() { locationKey = key, tile = tile, purchased = purchased },
        TileUnlockedMessage.TYPE,
        new[] { helper.ModRegistry.ModID }
      );
    }

    public void TileUnlocked(string locationKey, Vector2 tile, bool purchased)
    {
      if (!unlockedTiles.ContainsKey(locationKey))
        unlockedTiles[locationKey] = new HashSet<Vector2>();

      unlockedTiles[locationKey].Add(tile);
      numUnlockedTiles++;

      if (purchased)
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
      helper.Data.WriteJsonFile(GetSaveLocation(), model);
    }

    public void LoadData()
    {
      var model = helper.Data.ReadJsonFile<UnlockedTilesModel>(GetSaveLocation());
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

    private bool IsOffsetTileUnlocked(GameLocation location, Vector2 tile)
    {
      string key = GetLocationKey(location);
      if (!unlockedTiles.ContainsKey(key))
        return false;
      return unlockedTiles[key].Contains(tile);
    }

    private static string GetSaveLocation()
    {
      return $"data/${Constants.SaveFolderName}_{SAVE_LOCATION}.json";
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

    private static Vector2 GetShedUpgradeOffset(Shed shed)
    {
      if (shed.mapPath.Contains("Shed2"))
        return new Vector2(3, 3);
      return new Vector2(0, 0);
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