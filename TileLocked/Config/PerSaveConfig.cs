using StardewModdingAPI;
using StardewValley;
using TileLocked.Multiplayer;

namespace TileLocked.Config
{
  internal static class PerSaveConfig
  {
    private const string PREFIX = "Fungus.TileLocked/";

    public enum Key
    {
      MOD_ENABLED,
      NUM_TILES_AT_10_GOLD,
      NUM_TILES_AT_100_GOLD,
      NUM_TILES_AT_1000_GOLD,
      NUM_BONUS_TILES_FOR_CC_ITEMS,
      NUM_BONUS_TILES_FOR_CC_BUNDLES,
      NUM_BONUS_TILES_FOR_MUSEUM_ITEMS,
      KNOCK_OUT_ON_FAILED_UNLOCK_ATTEMPT,
      ONLY_LOCK_TILES_PLAYER_CAN_REACH,
    }

    private static Dictionary<Key, string> Defaults { get; } = new()
    {
      { Key.MOD_ENABLED, "false" },
      { Key.NUM_TILES_AT_10_GOLD, "10" },
      { Key.NUM_TILES_AT_100_GOLD, "500" },
      { Key.NUM_TILES_AT_1000_GOLD, "5000" },
      { Key.NUM_BONUS_TILES_FOR_CC_ITEMS, "5" },
      { Key.NUM_BONUS_TILES_FOR_CC_BUNDLES, "25" },
      { Key.NUM_BONUS_TILES_FOR_MUSEUM_ITEMS, "0" },
      { Key.KNOCK_OUT_ON_FAILED_UNLOCK_ATTEMPT, "true" },
      { Key.ONLY_LOCK_TILES_PLAYER_CAN_REACH, "false" }
    };

    public static string Get(Key key)
    {
      if (Game1.CustomData.TryGetValue(GetKeyString(key), out string? value))
        return value;
      return Defaults[key];
    }

    public static bool GetBool(Key key)
    {
      if (bool.TryParse(Get(key), out bool value))
        return value;
      return bool.Parse(Defaults[key]);
    }

    public static int GetInt(Key key)
    {
      if (int.TryParse(Get(key), out int value))
        return value;
      return int.Parse(Defaults[key]);
    }

    public static void Set(Key key, string value, IModHelper? helper = null)
    {
      Game1.CustomData[GetKeyString(key)] = value;

      helper?.Multiplayer.SendMessage(
          new PerSaveConfigUpdateMessage() { key = key, value = value },
          PerSaveConfigUpdateMessage.TYPE,
          new[] { helper.ModRegistry.ModID }
        );
    }

    public static Dictionary<string, string> GetConfig()
    {
      return Game1.CustomData.Where(kv => kv.Key.StartsWith(PREFIX)).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public static void SetAll(Dictionary<string, string> config)
    {
      foreach (var kv in config)
        Game1.CustomData[kv.Key] = kv.Value;
    }

    private static string GetKeyString(Key key)
    {
      return PREFIX + key.ToString();
    }
  }
}