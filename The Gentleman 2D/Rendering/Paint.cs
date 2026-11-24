using System.Numerics;
using Raylib_cs;

namespace Gentleman2D.Rendering;

public enum HeadWear
{
    TopHat,     // The Gentleman
    PoliceCap,
    Hair,
    Cap
}

/// <summary>
/// Hjälpmetoder för att rita figurer och föremål med enkla former.
/// Spelet använder inga bildfiler, allt ritas med kod.
/// </summary>
public static class Paint
{
    public static void Shadow(Vector2 p, float w, float h, float alpha = 0.28f) =>
        Raylib.DrawEllipse((int)p.X, (int)(p.Y + h * 0.6f), w, h, new Color(0, 0, 0, (int)(255 * alpha)));

    /// <summary>En person sedd ovanifrån: axlar, armar, huvud och huvudbonad.</summary>
    public static void Person(Vector2 p, Vector2 facing, Color body, Color skin, HeadWear headWear, Color hatColor,
        float walkPhase, float alpha = 1f)
    {
        if (facing == Vector2.Zero) facing = new Vector2(0, 1);
        var side = new Vector2(-facing.Y, facing.X);
        float swing = MathF.Sin(walkPhase) * 5f;

        Color A(Color c) => Raylib.ColorAlpha(c, alpha * (c.A / 255f));

        Shadow(p + new Vector2(0, 4), 15, 7, 0.3f * alpha);

        // Fötter som rör sig när man går
        var dark = new Color(30, 30, 36, 255);
        Raylib.DrawCircleV(p + side * 6 + facing * swing, 4.5f, A(dark));
        Raylib.DrawCircleV(p - side * 6 - facing * swing, 4.5f, A(dark));

        // Kropp (axlar) och armar
        Raylib.DrawCircleV(p + side * 11 - facing * swing * 0.6f, 5f, A(Darken(body, 0.85f)));
        Raylib.DrawCircleV(p - side * 11 + facing * swing * 0.6f, 5f, A(Darken(body, 0.85f)));
        Raylib.DrawCircleV(p, 12.5f, A(body));
        Raylib.DrawCircleV(p - facing * 2, 10f, A(Lighten(body, 1.12f)));

        // Huvud
        var head = p + facing * 3;
        Raylib.DrawCircleV(head, 7.5f, A(skin));

        switch (headWear)
        {
            case HeadWear.TopHat:
                Raylib.DrawCircleV(head, 10.5f, A(new Color(22, 22, 26, 255)));   // brättet
                Raylib.DrawCircleV(head, 7f, A(new Color(40, 40, 46, 255)));      // kullen
                Raylib.DrawRing(head, 6.2f, 7.6f, 0, 360, 24, A(hatColor));        // bandet
                Raylib.DrawCircleV(head - new Vector2(2, 2), 2.2f, A(new Color(255, 255, 255, 40)));
                break;
            case HeadWear.PoliceCap:
                Raylib.DrawCircleV(head + facing * 4, 6f, A(new Color(20, 24, 40, 255))); // skärm
                Raylib.DrawCircleV(head, 8f, A(hatColor));
                Raylib.DrawCircleV(head, 3f, A(new Color(240, 200, 80, 255)));            // märke
                break;
            case HeadWear.Cap:
                Raylib.DrawCircleV(head + facing * 4, 5f, A(Darken(hatColor, 0.7f)));
                Raylib.DrawCircleV(head, 7.5f, A(hatColor));
                break;
            default:
                Raylib.DrawCircleV(head - facing * 1.5f, 7f, A(hatColor));
                break;
        }
    }

    public static Color Darken(Color c, float f) =>
        new((int)(c.R * f), (int)(c.G * f), (int)(c.B * f), (int)c.A);

    public static Color Lighten(Color c, float f) =>
        new(Math.Min(255, (int)(c.R * f)), Math.Min(255, (int)(c.G * f)), Math.Min(255, (int)(c.B * f)), (int)c.A);

    /// <summary>En liten ikon för ett föremål (ädelsten, guld...).</summary>
    public static void ItemIcon(Core.ItemType type, Vector2 c, float size)
    {
        var info = Core.ItemCatalog.Get(type);
        switch (type)
        {
            case Core.ItemType.RawGem:
                Raylib.DrawPoly(c, 5, size, 10, Darken(info.Color, 0.7f));
                Raylib.DrawPoly(c - new Vector2(1, 1), 5, size * 0.8f, 30, info.Color);
                break;
            case Core.ItemType.CutDiamond:
                var top = c + new Vector2(0, -size);
                var left = c + new Vector2(-size, -size * 0.2f);
                var right = c + new Vector2(size, -size * 0.2f);
                var bottom = c + new Vector2(0, size);
                Raylib.DrawTriangle(top, left, right, Lighten(info.Color, 1.1f));
                Raylib.DrawTriangle(left, bottom, right, Darken(info.Color, 0.8f));
                Raylib.DrawLineEx(left, right, 1.5f, new Color(255, 255, 255, 180));
                break;
            case Core.ItemType.GoldScrap:
                Raylib.DrawCircleV(c + new Vector2(-size * 0.4f, size * 0.2f), size * 0.5f, Darken(info.Color, 0.8f));
                Raylib.DrawCircleV(c + new Vector2(size * 0.35f, size * 0.3f), size * 0.45f, info.Color);
                Raylib.DrawCircleV(c + new Vector2(0, -size * 0.3f), size * 0.5f, Lighten(info.Color, 1.1f));
                break;
            default:
                var bar = new Rectangle(c.X - size, c.Y - size * 0.5f, size * 2, size);
                Raylib.DrawRectangleRounded(bar, 0.3f, 6, Darken(info.Color, 0.8f));
                Raylib.DrawRectangleRounded(new Rectangle(bar.X + size * 0.25f, bar.Y + 2, bar.Width - size * 0.5f, bar.Height * 0.55f), 0.3f, 6, Lighten(info.Color, 1.08f));
                break;
        }
    }
}
