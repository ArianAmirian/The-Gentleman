using System.Numerics;
using Raylib_cs;
using Gentleman2D.World;

namespace Gentleman2D.Rendering;

/// <summary>
/// Ritar marken (gräs, vägar, golv, väggar...) och "dekor" som ska ligga ovanpå figurerna
/// (trädkronor och buskar). Allt ritas med enkla former och lite slumpmässig variation.
/// </summary>
public static class WorldRenderer
{
    private const int T = TileMap.TileSize;

    // Fast "slump" per ruta, så att gräset ser likadant ut varje bildruta.
    private static float Hash(int x, int y, int salt = 0)
    {
        float v = MathF.Sin(x * 127.1f + y * 311.7f + salt * 74.7f) * 43758.5453f;
        return v - MathF.Floor(v);
    }

    private static Color Col(int r, int g, int b, int a = 255) => new(r, g, b, a);

    public static void DrawGround(GameWorld world, Rectangle view)
    {
        var map = world.Map;
        int x0 = Math.Max(0, (int)(view.X / T) - 1), y0 = Math.Max(0, (int)(view.Y / T) - 1);
        int x1 = Math.Min(map.Width - 1, (int)((view.X + view.Width) / T) + 1);
        int y1 = Math.Min(map.Height - 1, (int)((view.Y + view.Height) / T) + 1);

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                DrawTile(world, map, x, y);

        // Skuggor från väggar och tak faller nedåt på marken
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (IsTall(map[x, y]) && !IsTall(map[x, y + 1]))
                    Raylib.DrawRectangleGradientV(x * T, (y + 1) * T, T, 16, Col(0, 0, 0, 80), Col(0, 0, 0, 0));
    }

    private static bool IsTall(Tile t) => t is Tile.Wall or Tile.Roof or Tile.RoofSlate;

    private static void DrawTile(GameWorld world, TileMap map, int x, int y)
    {
        int px = x * T, py = y * T;
        var tile = map[x, y];
        float h = Hash(x, y);

        switch (tile)
        {
            case Tile.Grass:
            case Tile.Tree:
            case Tile.Bush:
                Grass(px, py, x, y, h);
                break;

            case Tile.Road:
                Raylib.DrawRectangle(px, py, T, T, Raylib.ColorLerp(Col(62, 66, 74), Col(58, 62, 70), h));
                // Mittlinje: streckad gul linje mellan körfälten
                bool hTop = map[x, y - 1] != Tile.Road && map[x, y + 1] == Tile.Road && map[x, y + 2] != Tile.Road;
                bool vLeft = map[x - 1, y] != Tile.Road && map[x + 1, y] == Tile.Road && map[x + 2, y] != Tile.Road;
                if (hTop && x % 2 == 0) Raylib.DrawRectangle(px + 10, py + T - 2, 28, 4, Col(232, 196, 90));
                if (vLeft && y % 2 == 0) Raylib.DrawRectangle(px + T - 2, py + 10, 4, 28, Col(232, 196, 90));
                if (h > 0.97f) Raylib.DrawCircle(px + 30, py + 20, 6, Col(54, 58, 66));
                break;

            case Tile.Sidewalk:
                Raylib.DrawRectangle(px, py, T, T, Col(160, 164, 170));
                Raylib.DrawRectangleLines(px, py, T, T, Col(146, 150, 156));
                Raylib.DrawLine(px + T / 2, py, px + T / 2, py + T, Col(150, 154, 160));
                // Trottoarkant mot vägen
                if (map[x, y + 1] == Tile.Road) Raylib.DrawRectangle(px, py + T - 5, T, 5, Col(120, 124, 130));
                if (map[x, y - 1] == Tile.Road) Raylib.DrawRectangle(px, py, T, 5, Col(186, 190, 196));
                if (map[x + 1, y] == Tile.Road) Raylib.DrawRectangle(px + T - 5, py, 5, T, Col(120, 124, 130));
                if (map[x - 1, y] == Tile.Road) Raylib.DrawRectangle(px, py, 5, T, Col(120, 124, 130));
                break;

            case Tile.Path:
                Raylib.DrawRectangle(px, py, T, T, Col(192, 172, 132));
                for (int i = 0; i < 4; i++)
                    Raylib.DrawCircle(px + (int)(Hash(x, y, i) * 40) + 4, py + (int)(Hash(x, y, i + 9) * 40) + 4, 2, Col(170, 150, 112));
                break;

            case Tile.Plaza:
                Raylib.DrawRectangle(px, py, T, T, (x + y) % 2 == 0 ? Col(198, 190, 176) : Col(188, 180, 166));
                Raylib.DrawRectangleLines(px, py, T, T, Col(172, 164, 150));
                break;

            case Tile.WoodFloor:
                Raylib.DrawRectangle(px, py, T, T, Col(150, 106, 70));
                for (int i = 1; i < 4; i++) Raylib.DrawLine(px, py + i * 12, px + T, py + i * 12, Col(124, 86, 56));
                Raylib.DrawLine(px + (int)(h * 40), py, px + (int)(h * 40), py + 12, Col(124, 86, 56));
                Raylib.DrawLine(px + (int)(Hash(x, y, 3) * 40), py + 24, px + (int)(Hash(x, y, 3) * 40), py + 36, Col(124, 86, 56));
                break;

            case Tile.StoreFloor:
                Raylib.DrawRectangle(px, py, T, T, (x + y) % 2 == 0 ? Col(226, 222, 212) : Col(206, 200, 190));
                break;

            case Tile.VaultFloor:
                Raylib.DrawRectangle(px, py, T, T, Col(108, 114, 128));
                Raylib.DrawRectangleLines(px + 1, py + 1, T - 2, T - 2, Col(92, 98, 112));
                foreach (var (dx, dy) in new[] { (5, 5), (T - 6, 5), (5, T - 6), (T - 6, T - 6) })
                    Raylib.DrawCircle(px + dx, py + dy, 1.8f, Col(150, 156, 170));
                break;

            case Tile.OfficeFloor:
                Raylib.DrawRectangle(px, py, T, T, Col(178, 188, 200));
                Raylib.DrawRectangleLines(px, py, T, T, Col(164, 174, 186));
                break;

            case Tile.Door:
                Raylib.DrawRectangle(px, py, T, T, Col(124, 88, 58));
                Raylib.DrawRectangle(px + 4, py + 4, T - 8, T - 8, Col(150, 108, 72));
                Raylib.DrawRectangleLines(px + 4, py + 4, T - 8, T - 8, Col(98, 68, 44));
                break;

            case Tile.Wall:
                bool below = IsTall(map[x, y + 1]);
                Raylib.DrawRectangle(px, py, T, T, Col(66, 70, 88));
                Raylib.DrawRectangle(px, py, T, 5, Col(96, 102, 124));                   // överkant
                if (!below) Raylib.DrawRectangle(px, py + T - 14, T, 14, Col(44, 46, 60)); // framsida
                if (!IsTall(map[x - 1, y])) Raylib.DrawRectangle(px, py, 3, T, Col(84, 90, 110));
                break;

            case Tile.Roof:
            case Tile.RoofSlate:
                var baseCol = tile == Tile.Roof ? Col(160, 78, 62) : Col(84, 98, 118);
                var dark = Paint.Darken(baseCol, 0.82f);
                Raylib.DrawRectangle(px, py, T, T, baseCol);
                for (int i = 0; i < 4; i++)
                {
                    int off = i % 2 == 0 ? 0 : 12;
                    Raylib.DrawLine(px, py + i * 12 + 11, px + T, py + i * 12 + 11, dark);
                    Raylib.DrawLine(px + off + 12, py + i * 12, px + off + 12, py + i * 12 + 11, dark);
                    Raylib.DrawLine(px + off + 36, py + i * 12, px + off + 36, py + i * 12 + 11, dark);
                }
                if (map[x, y - 1] != tile) Raylib.DrawRectangle(px, py, T, 4, Paint.Lighten(baseCol, 1.15f));
                if (map[x, y + 1] != tile) Raylib.DrawRectangle(px, py + T - 12, T, 12, Paint.Darken(baseCol, 0.6f));
                if (map[x - 1, y] != tile) Raylib.DrawRectangle(px, py, 4, T, Paint.Darken(baseCol, 0.7f));
                if (map[x + 1, y] != tile) Raylib.DrawRectangle(px + T - 4, py, 4, T, Paint.Darken(baseCol, 0.7f));
                break;

            case Tile.Water:
                Raylib.DrawRectangle(px, py, T, T, Col(52, 106, 150));
                float t = world.Time;
                for (int i = 0; i < 2; i++)
                {
                    float wy = py + 14 + i * 20 + MathF.Sin(t * 1.5f + x * 0.8f + i) * 3;
                    Raylib.DrawLineEx(new Vector2(px + 6 + h * 10, wy), new Vector2(px + 22 + h * 10, wy), 2, Col(110, 170, 210, 120));
                }
                if (map[x, y - 1] != Tile.Water) Raylib.DrawRectangle(px, py, T, 4, Col(200, 230, 245, 160));
                break;

            case Tile.Pier:
                Raylib.DrawRectangle(px, py, T, T, Col(140, 104, 68));
                for (int i = 1; i < 4; i++) Raylib.DrawLine(px + i * 12, py, px + i * 12, py + T, Col(108, 78, 50));
                if (map[x, y + 1] == Tile.Water) Raylib.DrawRectangle(px, py + T - 6, T, 6, Col(90, 64, 40));
                break;
        }
    }

    private static void Grass(int px, int py, int x, int y, float h)
    {
        Raylib.DrawRectangle(px, py, T, T, Raylib.ColorLerp(Col(98, 148, 82), Col(90, 140, 76), h));
        for (int i = 0; i < 5; i++)
        {
            int gx = px + (int)(Hash(x, y, i) * 42) + 3;
            int gy = py + (int)(Hash(x, y, i + 20) * 42) + 3;
            Raylib.DrawLine(gx, gy, gx + 2, gy - 5, i % 2 == 0 ? Col(76, 124, 64) : Col(122, 170, 96));
        }
        if (Hash(x, y, 77) > 0.9f)
            Raylib.DrawCircle(px + 30, py + 14, 2.5f, Hash(x, y, 5) > 0.5f ? Col(240, 220, 120) : Col(240, 160, 190));
    }

    /// <summary>Trädkronor och buskar ritas efter figurerna så att man kan gå "under" dem.</summary>
    public static void DrawDecor(GameWorld world, Rectangle view)
    {
        var map = world.Map;
        int x0 = Math.Max(0, (int)(view.X / T) - 2), y0 = Math.Max(0, (int)(view.Y / T) - 2);
        int x1 = Math.Min(map.Width - 1, (int)((view.X + view.Width) / T) + 2);
        int y1 = Math.Min(map.Height - 1, (int)((view.Y + view.Height) / T) + 2);
        var playerTile = world.Player.Tile;

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var c = TileMap.TileCenter(x, y);
                float h = Hash(x, y, 4);
                float sway = MathF.Sin(world.Time * 1.2f + x + y) * 1.2f;

                if (map[x, y] == Tile.Tree)
                {
                    Raylib.DrawCircleV(c + new Vector2(8, 12), 30, Col(0, 0, 0, 60));
                    Raylib.DrawCircleV(c + new Vector2(sway, 0), 30, Col(48, 104, 60));
                    Raylib.DrawCircleV(c + new Vector2(-5 + sway, -6), 24, h > 0.5f ? Col(66, 132, 72) : Col(74, 138, 66));
                    Raylib.DrawCircleV(c + new Vector2(-10 + sway, -12), 12, Col(104, 170, 96));
                }
                else if (map[x, y] == Tile.Bush)
                {
                    bool inside = playerTile == (x, y);
                    float a = inside ? 0.72f : 1f;
                    Raylib.DrawCircleV(c + new Vector2(4, 8), 22, Col(0, 0, 0, (int)(50 * a)));
                    Raylib.DrawCircleV(c + new Vector2(-9, 3), 15, Raylib.ColorAlpha(Col(56, 116, 64), a));
                    Raylib.DrawCircleV(c + new Vector2(9, 4), 15, Raylib.ColorAlpha(Col(52, 110, 60), a));
                    Raylib.DrawCircleV(c + new Vector2(0, -6), 17, Raylib.ColorAlpha(Col(70, 136, 74), a));
                    Raylib.DrawCircleV(c + new Vector2(-4, -11), 7, Raylib.ColorAlpha(Col(110, 174, 100), a));
                }
            }
    }
}
