using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using TileLocked.Config;

namespace TileLocked.Rendering
{
  internal sealed class UnveilingGlassTooltipRenderer
  {
    private readonly TileManager tileManager;

    public UnveilingGlassTooltipRenderer(TileManager tileManager)
    {
      this.tileManager = tileManager;
    }

    public void OnRenderedHud(Vector2? lastHoveredTile)
    {
      if (RenderingUtils.ShouldRenderUi()
          && Context.IsPlayerFree
          && Game1.player.ActiveItem?.ItemId == TileLockedConstants.UNVEILING_GLASS_ITEM_NAME
          && lastHoveredTile is Vector2 tile
          && !tileManager.IsTileUnlocked(Game1.player.currentLocation, lastHoveredTile.Value))
      {  
        if (PerSaveConfig.GetBool(PerSaveConfig.Key.ONLY_LOCK_TILES_PLAYER_CAN_REACH) && !tileManager.ReachableTiles.Contains(tile))
            return;
        RenderTooltip();
      }
    }

    private void RenderTooltip()
    {
      int cost = tileManager.GetTilePurchaseCost();
      string label = "Unlock Tile";
      if (tileManager.GetNumBonusTiles() > 0)
      {
        cost = 0;
        label += " (" + tileManager.GetNumBonusTiles() + ")";
      }
      IClickableMenu.drawHoverText(Game1.spriteBatch, label, Game1.dialogueFont, 0, -160, cost);
    }
  }
}