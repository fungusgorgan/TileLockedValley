using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using TileLocked.Config;
using xTile;

namespace TileLocked.Rendering
{
  public enum OverlayMode
  {
    LockedTiles,
    UnlockedTiles,
    Off,
  }

  internal sealed class TileOverlayRenderer
  {
    private readonly Texture2D whitePixel;
    private readonly ModConfig config;
    private readonly TileManager tileManager;

    private OverlayMode OverlayMode { get; set; } = OverlayMode.LockedTiles;

    public TileOverlayRenderer(ModConfig config, TileManager tileManager)
    {
      this.config = config;
      this.tileManager = tileManager;
      whitePixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
      whitePixel.SetData(new[] { Color.White });
    }

    public void ToggleOverlayMode()
    {
      switch (OverlayMode)
      {
        case OverlayMode.LockedTiles:
          OverlayMode = OverlayMode.UnlockedTiles;
          break;
        case OverlayMode.UnlockedTiles:
          OverlayMode = OverlayMode.Off;
          break;
        case OverlayMode.Off:
          OverlayMode = OverlayMode.LockedTiles;
          break;
      }
    }

    public void OnRenderedWorld(RenderedWorldEventArgs e, Vector2? lastHoveredTile)
    {
      if (OverlayMode != OverlayMode.Off)
        DrawTileOverlay(e, lastHoveredTile);
    }

    private void DrawTileOverlay(RenderedWorldEventArgs e, Vector2? lastHoveredTile)
    {
      Color lockedColor = Color.Red;
      Color unlockedColor = Color.Green;
      if (TryParseHexColor(config.LockedTileOverlayColor, out Color parsedLockedColor))
      {
        lockedColor = parsedLockedColor;
      }
      if (TryParseHexColor(config.UnlockedTileOverlayColor, out Color parsedUnlockedColor))
      {
        unlockedColor = parsedUnlockedColor;
      }

      int left = Game1.viewport.X / Game1.tileSize;
      int top = Game1.viewport.Y / Game1.tileSize;
      int right = left + (Game1.viewport.Width / Game1.tileSize) + 2;
      int bottom = top + (Game1.viewport.Height / Game1.tileSize) + 2;

      Map map = Game1.currentLocation.map;
      for (int x = left; x < right; x++)
      {
        for (int y = top; y < bottom; y++)
        {
          if (x < 0 || x >= map.Layers[0].LayerWidth || y < 0 || y >= map.Layers[0].LayerHeight)
            continue;

          Vector2 tile = new(x, y);
          if (OverlayMode == OverlayMode.LockedTiles && !tileManager.IsTileUnlocked(Game1.currentLocation, tile))
          {
            Vector2 position = new(
                tile.X * Game1.tileSize - Game1.viewport.X,
                tile.Y * Game1.tileSize - Game1.viewport.Y
            );
            bool hoveredWithUnveilingGlass = lastHoveredTile == tile && Game1.player.ActiveItem?.ItemId == "UnveilingGlass";
            e.SpriteBatch.Draw(
              whitePixel,
              position + new Vector2(2, 2),
              new Rectangle(0, 0, Game1.tileSize - 4, Game1.tileSize - 4),
              lockedColor * (hoveredWithUnveilingGlass ? 0.1f : 0.2f)
            );
          }
          if (OverlayMode == OverlayMode.UnlockedTiles && tileManager.IsTileUnlocked(Game1.currentLocation, tile))
          {
            Vector2 position = new(
                tile.X * Game1.tileSize - Game1.viewport.X,
                tile.Y * Game1.tileSize - Game1.viewport.Y
            );
            // Draw an outlined box
            // Top
            e.SpriteBatch.Draw(
              whitePixel,
              position - new Vector2(1, 1),
              new Rectangle(0, 0, Game1.tileSize + 2, 2),
              unlockedColor
            );
            // Left
            e.SpriteBatch.Draw(
              whitePixel,
              position - new Vector2(1, 1),
              new Rectangle(0, 0, 2, Game1.tileSize + 2),
              unlockedColor
            );
            // Right
            e.SpriteBatch.Draw(
              whitePixel,
              position + new Vector2(Game1.tileSize - 1, -1),
              new Rectangle(0, 0, 2, Game1.tileSize + 2),
              unlockedColor
            );
            // Bottom
            e.SpriteBatch.Draw(
              whitePixel,
              position + new Vector2(-1, Game1.tileSize - 1),
              new Rectangle(0, 0, Game1.tileSize + 2, 2),
              unlockedColor
            );
          }
        }
      }
    }

    private static bool TryParseHexColor(string hex, out Color color)
    {
      color = Color.White;
      try
      {
        string cleanedHex = hex.StartsWith("#") ? hex[1..] : hex;
        byte r = 255, g = 255, b = 255, a = 255;

        if (cleanedHex.Length == 6)
        {
          r = Convert.ToByte(cleanedHex.Substring(0, 2), 16);
          g = Convert.ToByte(cleanedHex.Substring(2, 2), 16);
          b = Convert.ToByte(cleanedHex.Substring(4, 2), 16);
        }

        color = new Color(r, g, b, a);
        return true;
      }
      catch
      {
        return false;
      }
    }
  }
}
