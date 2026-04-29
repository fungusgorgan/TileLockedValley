using StardewValley;

namespace TileLocked.Rendering
{
  internal static class RenderingUtils
  {
    public static bool ShouldRenderUi()
    {
      return Game1.displayHUD && !Game1.eventUp;
    }
  }
}