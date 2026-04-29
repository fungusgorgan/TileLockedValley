using TileLocked.Config;

namespace TileLocked.Multiplayer
{
  internal sealed class PerSaveConfigUpdateMessage
  {
    public const string TYPE = "PerSaveConfigUpdate";

    public PerSaveConfig.Key key;
    public string value = "";
  }
}