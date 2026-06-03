using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace TileLocked.Rendering
{

  internal sealed class TileInfoRenderer
  {
    private readonly TileManager tileManager;
    private readonly PerScreen<ClickableTextureComponent> icon = new(
    () => new ClickableTextureComponent(
      "",
      new Rectangle(GetWidthInPlayArea() - 134, 270, 14 * Game1.pixelZoom, 15 * Game1.pixelZoom),
      "",
      "",
      Game1.mouseCursors,
      new Rectangle(208, 321, 14, 15),
      Game1.pixelZoom
    ));

    public TileInfoRenderer(TileManager tileManager)
    {
      this.tileManager = tileManager;
    }

    public void OnRenderingHud()
    {
      if (RenderingUtils.ShouldRenderUi())
      {
        RenderTileInfoIcon();
      }
    }

    public void OnRenderedHud()
    {
      if (RenderingUtils.ShouldRenderUi() && icon.Value.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
      {
        RenderTileInfoHoverText();
      }
    }

    private void RenderTileInfoIcon()
    {
      Point iconPosition = GetIconPosition();
      ClickableTextureComponent placedIcon = icon.Value;
      placedIcon.bounds.X = iconPosition.X;
      placedIcon.bounds.Y = iconPosition.Y;
      icon.Value = placedIcon;
      icon.Value.draw(Game1.spriteBatch, Color.White, 1f);
    }

    private void RenderTileInfoHoverText()
    {
      int numUnlocked = tileManager.GetNumUnlockedTiles();
      int numPurchased = tileManager.GetNumPurchasedTiles();
      int numFromBonus = numUnlocked - numPurchased;
      int numBonus = tileManager.GetNumBonusTiles();
      string hoverText = "Tiles Unlocked: " + numPurchased + " (+" + numFromBonus + ")\nTotal Unlocked Tiles: " + numUnlocked + "\nBonus Tiles: " + numBonus;
      IClickableMenu.drawHoverText(Game1.spriteBatch, hoverText, Game1.dialogueFont);
    }

    private static Point GetIconPosition()
    {
      int yPos = 320;
      int xPosition = GetWidthInPlayArea() - 100;

      return new Point(xPosition, yPos);
    }

    private static int GetWidthInPlayArea()
    {
      if (Game1.isOutdoorMapSmallerThanViewport())
      {
        int right = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right;
        int totalWidth = Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize;
        int someOtherWidth = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right - totalWidth;

        return right - someOtherWidth / 2;
      }

      return Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right;
    }
  }
}
