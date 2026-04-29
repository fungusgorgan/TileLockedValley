using Microsoft.Xna.Framework;

namespace TileLocked.Multiplayer
{
  internal sealed class PeerConnectionMessage
  {
    public const string TYPE = "PeerConnection";
    public Dictionary<string, HashSet<Vector2>> unlockedTiles = new();
    public int numBonusTiles;
    public int numUnlockedTiles;
    public int numPurchasedTiles;
    public Dictionary<string, string> perSaveConfig = new();
  }
}