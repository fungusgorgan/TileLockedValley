using Microsoft.Xna.Framework;

namespace TileLocked.Multiplayer
{
  internal sealed class TileUnlockedMessage
  {
    public const string TYPE = "TileUnlocked";

    public string locationKey = "";
    public Vector2 tile;
    public bool purchased;
    public bool isFarmhouse;
    public int farmhouseUpgradeLevel;
    public bool isShed;
    public bool isUpgradedShed;
  }
}