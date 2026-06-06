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
    private readonly TileManager tileManager;
    private readonly TileStyles tileStyles;

    private OverlayMode OverlayMode { get; set; } = OverlayMode.LockedTiles;

    public TileOverlayRenderer(TileManager tileManager, TileStyles tileStyles)
    {
      this.tileManager = tileManager;
      this.tileStyles = tileStyles;
      tileStyles.ApplyConfigColors();
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
      if (
        PerSaveConfig.GetBool(PerSaveConfig.Key.ONLY_LOCK_TILES_PLAYER_CAN_REACH) &&
        tileManager.ReachableTiles.Count == 0
      )
      {
        tileManager.UpdateReachableTiles(Game1.currentLocation);
      }

      if (OverlayMode != OverlayMode.Off)
        DrawTileOverlay(e, lastHoveredTile);
    }

    private void DrawTileOverlay(RenderedWorldEventArgs e, Vector2? lastHoveredTile)
    {
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
          
          if (PerSaveConfig.GetBool(PerSaveConfig.Key.ONLY_LOCK_TILES_PLAYER_CAN_REACH) && !tileManager.ReachableTiles.Contains(tile))
            continue;
            
          bool hoveredWithUnveilingGlass = lastHoveredTile == tile && Game1.player.ActiveItem?.ItemId == "UnveilingGlass";

          if (OverlayMode == OverlayMode.LockedTiles && !tileManager.IsTileUnlocked(Game1.currentLocation, tile))
          {
            tileStyles.LockedTile.Draw(e.SpriteBatch, tile, hoveredWithUnveilingGlass);
          }
          if (OverlayMode == OverlayMode.UnlockedTiles && tileManager.IsTileUnlocked(Game1.currentLocation, tile))
          {
            tileStyles.UnlockedTile.Draw(e.SpriteBatch, tile, hoveredWithUnveilingGlass);
          }
        }
      }
    }
  }
  internal class TileStyle
  {
      public GraphicLayer Graphic { get; set; } = new();
      public FillLayer Fill { get; set; } = new();
      public BorderLayer Border { get; set; } = new();

      private static Texture2D? whitePixelBacking;
      private static Texture2D WhitePixel
      {
          get
          {
              if (whitePixelBacking == null)
              {
                  whitePixelBacking = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
                  whitePixelBacking.SetData(new[] { Color.White });
              }
              return whitePixelBacking;
          }
      }

      public void Draw(SpriteBatch b, Rectangle bounds, bool hovered = false)
      {
          DrawFill(b, bounds, hovered);
          DrawBorder(b, bounds, hovered);
          DrawGraphic(b, bounds, hovered);
      }

      public void Draw(SpriteBatch b, Vector2 tile, bool hovered = false)
      {
          Draw(b, GetTileBounds(tile), hovered);
      }

      private void DrawFill(SpriteBatch spriteBatch, Rectangle tileBounds, bool hovered)
      {
          Rectangle fillBounds = tileBounds;
          fillBounds.Inflate(-Fill.Padding, -Fill.Padding);

          spriteBatch.Draw(
              WhitePixel,
              fillBounds,
              Fill.Color * (hovered ? Fill.HoveredOpacity : Fill.Opacity)
          );
      }

      private void DrawBorder(SpriteBatch spriteBatch, Rectangle tileBounds, bool hovered)
      {
            // Draw an outlined box
            Rectangle borderBounds = tileBounds;

            //Padding
            borderBounds.Inflate(-Border.Padding, -Border.Padding);

            // Top
            spriteBatch.Draw(
              WhitePixel,
              new Rectangle(borderBounds.X, borderBounds.Y, borderBounds.Width, Border.Thickness),
              Border.Color * (hovered ? Border.HoveredOpacity : Border.Opacity)
            );
            // Left
            spriteBatch.Draw(
              WhitePixel,
              new Rectangle(borderBounds.X, borderBounds.Y + Border.Thickness, Border.Thickness, borderBounds.Height - 2 * Border.Thickness),
              Border.Color * (hovered ? Border.HoveredOpacity : Border.Opacity)
            );
            // Right
            spriteBatch.Draw(
              WhitePixel,
              new Rectangle(borderBounds.Right - Border.Thickness, borderBounds.Y + Border.Thickness, Border.Thickness, borderBounds.Height - 2 * Border.Thickness),
              Border.Color * (hovered ? Border.HoveredOpacity : Border.Opacity)
            );
            // Bottom
            spriteBatch.Draw(
              WhitePixel,
              new Rectangle(borderBounds.X, borderBounds.Bottom - Border.Thickness, borderBounds.Width, Border.Thickness),
              Border.Color * (hovered ? Border.HoveredOpacity : Border.Opacity)
            );
      }

      private void DrawGraphic(SpriteBatch spriteBatch, Rectangle tileBounds, bool hovered)
      {
          // TODO
      }

      private Rectangle GetTileBounds(Vector2 tile)
      {
          return new Rectangle(
              (int)tile.X * Game1.tileSize - Game1.viewport.X,
              (int)tile.Y * Game1.tileSize - Game1.viewport.Y,
              Game1.tileSize,
              Game1.tileSize
          );
      }

      public void CopyFrom(TileStyle other)
      {
          Fill = other.Fill.Clone();
          Border = other.Border.Clone();
          Graphic = other.Graphic.Clone();
      }
      
  }

  internal class GraphicLayer
  {
      public string? AssetName { get; set; }

      public Color Tint { get; set; } = Color.White;

      public float Opacity { get; set; } = 1f;
      
      public float HoveredOpacity { get; set; } = 1f;

      public GraphicLayer Clone()
      {
          return new GraphicLayer
          {
              AssetName = AssetName,
              Tint = Tint,
              Opacity = Opacity,
              HoveredOpacity = HoveredOpacity
          };
      }
  }

  internal class FillLayer
  {
      public Color Color { get; set; } = Color.Red;

      public int Padding { get; set; } = 4;

      public float Opacity { get; set; } = 0.2f;

      public float HoveredOpacity { get; set; } = 1f;

      public FillLayer Clone()
      {
          return new FillLayer
          {
              Color = Color,
              Padding = Padding,
              Opacity = Opacity,
              HoveredOpacity = HoveredOpacity
          };
      }
  }

  internal class BorderLayer
  {
      public Color Color { get; set; } = Color.White;

      public int Padding { get; set; } = -1;

      public float Opacity { get; set; } = 1f;

      public float HoveredOpacity { get; set; } = 1f;
      
      public int Thickness { get; set; } = 2;

      public BorderLayer Clone()
      {
          return new BorderLayer
          {
              Color = Color,
              Padding = Padding,
              Opacity = Opacity,
              HoveredOpacity = HoveredOpacity,
              Thickness = Thickness
          };
      }
  }

}
