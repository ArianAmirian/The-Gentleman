using System.Numerics;

namespace Gentleman2D.World;

public enum Tile : byte
{
    Grass,
    Road,
    Sidewalk,
    Path,
    Plaza,
    WoodFloor,
    StoreFloor,
    VaultFloor,
    OfficeFloor,
    Door,
    Wall,
    Roof,
    RoofSlate,
    Tree,
    Bush,
    Water,
    Pier
}

/// <summary>
/// Rutnätet som världen består av. Vet vad som är solitt, vad som skymmer sikten
/// och kan hitta vägar (används av polisen).
/// </summary>
public class TileMap
{
    public const int TileSize = 48;

    private readonly Tile[,] tiles;
    private readonly bool[,] blocked; // möbler, maskiner, containrar osv.

    public TileMap(int width, int height)
    {
        Width = width;
        Height = height;
        tiles = new Tile[width, height];
        blocked = new bool[width, height];
    }

    public int Width { get; }
    public int Height { get; }
    public float PixelWidth => Width * TileSize;
    public float PixelHeight => Height * TileSize;

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public Tile this[int x, int y]
    {
        get => InBounds(x, y) ? tiles[x, y] : Tile.Wall;
        set { if (InBounds(x, y)) tiles[x, y] = value; }
    }

    public void SetBlocked(int x, int y, bool value = true)
    {
        if (InBounds(x, y)) blocked[x, y] = value;
    }

    public static bool IsSolidTile(Tile t) =>
        t is Tile.Wall or Tile.Roof or Tile.RoofSlate or Tile.Tree or Tile.Water;

    public bool IsSolid(int x, int y) => !InBounds(x, y) || IsSolidTile(tiles[x, y]) || blocked[x, y];

    public bool BlocksSight(int x, int y) => !InBounds(x, y) || tiles[x, y] is Tile.Wall or Tile.Roof or Tile.RoofSlate or Tile.Tree;

    public bool IsHidingSpot(int x, int y) => this[x, y] == Tile.Bush;

    public static (int X, int Y) ToTile(Vector2 p) => ((int)MathF.Floor(p.X / TileSize), (int)MathF.Floor(p.Y / TileSize));

    public static Vector2 TileCenter(int x, int y) => new((x + 0.5f) * TileSize, (y + 0.5f) * TileSize);

    /// <summary>Krockar en kvadrat med halva sidan "radius" runt punkten med något solitt?</summary>
    public bool CollidesAt(Vector2 p, float radius)
    {
        var (x0, y0) = ToTile(p - new Vector2(radius));
        var (x1, y1) = ToTile(p + new Vector2(radius - 0.01f));
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                if (IsSolid(x, y)) return true;
        return false;
    }

    /// <summary>Fri sikt mellan två punkter? Vi går längs linjen i små steg och kollar varje ruta.</summary>
    public bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        float dist = Vector2.Distance(from, to);
        int steps = (int)(dist / 8) + 1;
        for (int i = 1; i < steps; i++)
        {
            var p = Vector2.Lerp(from, to, i / (float)steps);
            var (x, y) = ToTile(p);
            if (BlocksSight(x, y)) return false;
        }
        return true;
    }

    /// <summary>Kortaste vägen mellan två rutor (bredden-först-sökning). Tom lista om ingen väg finns.</summary>
    public List<(int X, int Y)> FindPath((int X, int Y) start, (int X, int Y) goal, int maxNodes = 4000)
    {
        var result = new List<(int, int)>();
        if (IsSolid(goal.X, goal.Y) || start == goal) return result;

        var cameFrom = new Dictionary<(int, int), (int, int)> { [start] = start };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);
        (int, int)[] dirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        while (queue.Count > 0 && cameFrom.Count < maxNodes)
        {
            var cur = queue.Dequeue();
            if (cur == goal)
            {
                for (var n = goal; n != start; n = cameFrom[n]) result.Add(n);
                result.Reverse();
                return result;
            }
            foreach (var (dx, dy) in dirs)
            {
                var next = (cur.X + dx, cur.Y + dy);
                if (IsSolid(next.Item1, next.Item2) || cameFrom.ContainsKey(next)) continue;
                cameFrom[next] = cur;
                queue.Enqueue(next);
            }
        }
        return result;
    }
}
