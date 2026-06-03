using StardewValley;

namespace TileLocked.Rendering
{
  internal static class RenderingUtils
  {
    public static bool ShouldRenderUi()
    {
      if (Game1.displayHUD && !Game1.eventUp)
        return true;

      return Game1.CurrentEvent != null
          && Game1.CurrentEvent.isFestival;
    }
  }
}