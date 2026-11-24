using System.Numerics;
using Raylib_cs;

namespace Gentleman2D.UI;

/// <summary>
/// Spelets gemensamma utseende: färger, typsnitt och hjälpmetoder för att rita paneler och text.
/// Allt gränssnitt går via den här klassen så att stilen blir enhetlig.
/// </summary>
public static class Theme
{
    // Färgpalett
    public static readonly Color Panel = new(18, 20, 26, 225);
    public static readonly Color PanelLight = new(34, 38, 48, 235);
    public static readonly Color PanelBorder = new(255, 255, 255, 28);
    public static readonly Color Text = new(236, 238, 242, 255);
    public static readonly Color TextDim = new(150, 156, 170, 255);
    public static readonly Color Gold = new(232, 176, 75, 255);
    public static readonly Color Green = new(110, 210, 130, 255);
    public static readonly Color Red = new(235, 87, 87, 255);
    public static readonly Color Blue = new(90, 160, 255, 255);
    public static readonly Color Cyan = new(120, 220, 235, 255);

    public static Font Font { get; private set; }
    private static bool customFont;

    /// <summary>Laddar ett mjukt, runt typsnitt från macOS. Finns det inte används Raylibs standardtypsnitt.</summary>
    public static void Load()
    {
        string[] candidates =
        {
            "/System/Library/Fonts/Supplemental/Arial Rounded Bold.ttf",
            "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
            "C:/Windows/Fonts/arialbd.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
        };

        // Tecken 32–255 räcker för svenska (å, ä, ö) plus några symboler vi använder.
        var codepoints = Enumerable.Range(32, 224).Concat(new[] { 0x2605, 0x2192, 0x2713, 0x2022 }).ToArray();

        foreach (var path in candidates.Where(File.Exists))
        {
            Font = Raylib.LoadFontEx(path, 64, codepoints, codepoints.Length);
            Raylib.SetTextureFilter(Font.Texture, TextureFilter.Bilinear);
            customFont = true;
            return;
        }
        Font = Raylib.GetFontDefault();
    }

    public static void Unload()
    {
        if (customFont) Raylib.UnloadFont(Font);
    }

    public static Vector2 Measure(string text, float size) => Raylib.MeasureTextEx(Font, text, size, 1);

    public static void DrawText(string text, float x, float y, float size, Color color, bool shadow = false)
    {
        if (shadow)
            Raylib.DrawTextEx(Font, text, new Vector2(x + 2, y + 2), size, 1, new Color(0, 0, 0, 150));
        Raylib.DrawTextEx(Font, text, new Vector2(x, y), size, 1, color);
    }

    public static void DrawTextCentered(string text, float centerX, float y, float size, Color color, bool shadow = false)
    {
        var m = Measure(text, size);
        DrawText(text, centerX - m.X / 2, y, size, color, shadow);
    }

    public static void DrawTextRight(string text, float rightX, float y, float size, Color color)
    {
        var m = Measure(text, size);
        DrawText(text, rightX - m.X, y, size, color);
    }

    /// <summary>Rundad panel med tunn kant, används för allt gränssnitt.</summary>
    public static void DrawPanel(Rectangle r, Color? fill = null, float roundness = 0.18f)
    {
        var shadow = new Rectangle(r.X + 3, r.Y + 5, r.Width, r.Height);
        Raylib.DrawRectangleRounded(shadow, roundness, 10, new Color(0, 0, 0, 70));
        Raylib.DrawRectangleRounded(r, roundness, 10, fill ?? Panel);
        Raylib.DrawRectangleRoundedLinesEx(r, roundness, 10, 1.5f, PanelBorder);
    }

    /// <summary>Liten "pill" med text, t.ex. för klockan.</summary>
    public static void DrawPill(float x, float y, string text, float size, Color fill, Color textColor)
    {
        var m = Measure(text, size);
        var r = new Rectangle(x, y, m.X + 24, m.Y + 10);
        Raylib.DrawRectangleRounded(r, 1f, 12, fill);
        DrawText(text, x + 12, y + 5, size, textColor);
    }

    public static void DrawBar(Rectangle r, float value, Color fill, Color? back = null)
    {
        Raylib.DrawRectangleRounded(r, 1f, 8, back ?? new Color(255, 255, 255, 30));
        if (value <= 0) return;
        var f = new Rectangle(r.X, r.Y, Math.Max(r.Height, r.Width * Math.Clamp(value, 0, 1)), r.Height);
        Raylib.DrawRectangleRounded(f, 1f, 8, fill);
    }

    /// <summary>En stjärna (för efterlysningsnivån) ritad med trianglar.</summary>
    public static void DrawStar(Vector2 c, float radius, Color color)
    {
        var pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float r = i % 2 == 0 ? radius : radius * 0.45f;
            float a = -MathF.PI / 2 + i * MathF.PI / 5;
            pts[i] = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
        }
        for (int i = 0; i < 10; i++)
            Raylib.DrawTriangle(c, pts[(i + 1) % 10], pts[i], color);
    }

    public static Color WithAlpha(Color c, float alpha) => Raylib.ColorAlpha(c, Math.Clamp(alpha, 0, 1));

    public static Color Lerp(Color a, Color b, float t) => Raylib.ColorLerp(a, b, Math.Clamp(t, 0, 1));
}
