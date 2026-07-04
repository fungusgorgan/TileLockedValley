using Microsoft.Xna.Framework;
using TileLocked.Config;

namespace TileLocked.Rendering;

internal class TileStyles
{
    private ModConfig config;
    public TileStyle LockedTile { get; }
    public TileStyle UnlockedTile { get; }
    public TileStyle LockedTilePreview { get; }
    public TileStyle UnlockedTilePreview { get; }

    public TileStyles(ModConfig config)
    {
        this.config = config;
        LockedTile = new TileStyle();
        UnlockedTile = new TileStyle();
        LockedTilePreview = new TileStyle();
        UnlockedTilePreview = new TileStyle();
        InitializeDefaultStyles();
    }

    public static bool TryParseHexColor(string hex, out Color color)
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

    private void InitializeDefaultStyles()
    {
        LockedTile.Fill = new FillLayer
        {
            Color = Color.Red,
            Opacity = 0.2f,
            HoveredOpacity = 0.1f,
            Padding = 3
        };

        LockedTile.Border = new BorderLayer
        {
            Color = Color.Black,
            Opacity = 0.2f,
            HoveredOpacity = 0.1f,
            Padding = 2,
            Thickness = 4
        };

        UnlockedTile.Fill = new FillLayer
        {
            Opacity = 0f,
            HoveredOpacity = 0f
        };

        UnlockedTile.Border = new BorderLayer
        {
            Color = Color.Green,
            Padding = -1,
            Thickness = 2
        };

        ApplyConfigColors();
    }

    public void ApplyConfigColors()
    {
        if (TryParseHexColor(config.LockedTileOverlayColor, out Color parsedLockedColor))
            LockedTile.Fill.Color = parsedLockedColor;
      
        if (TryParseHexColor(config.UnlockedTileOverlayColor, out Color parsedUnlockedColor))
            UnlockedTile.Border.Color = parsedUnlockedColor;
    }
}