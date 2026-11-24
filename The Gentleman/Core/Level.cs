using TheGentleman.Models;

namespace TheGentleman.Core;

/// <summary>Vad en ruta på kartan består av.</summary>
public enum Tile
{
    Floor,
    Wall,
    Door,
    Exit
}

/// <summary>
/// Beskrivningen av en bana som text. Se Data/LevelLibrary.cs för teckenförklaring.
/// </summary>
public record LevelData(string Name, string Briefing, int Par, string[] Map);

/// <summary>
/// En bana under spel: kartan, vakterna och föremålen som ligger kvar.
/// Banan ändras när man spelar (dörrar öppnas, föremål plockas),
/// därför skapas en ny Level från LevelData varje gång banan startas om.
/// </summary>
public class Level
{
    private readonly Tile[,] tiles;

    private Level(string name, int width, int height)
    {
        Name = name;
        Width = width;
        Height = height;
        tiles = new Tile[width, height];
    }

    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    public Position PlayerStart { get; private set; }
    public List<Guard> Guards { get; } = new();
    public List<Item> Items { get; } = new();

    /// <summary>Bygger en bana från textkartan.</summary>
    public static Level Parse(LevelData data)
    {
        var level = new Level(data.Name, data.Map.Max(row => row.Length), data.Map.Length);
        int players = 0, diamonds = 0, exits = 0;

        for (int y = 0; y < level.Height; y++)
        {
            string row = data.Map[y];
            for (int x = 0; x < level.Width; x++)
            {
                char c = x < row.Length ? row[x] : ' ';
                var p = new Position(x, y);
                level.tiles[x, y] = Tile.Floor;

                switch (c)
                {
                    case '#': level.tiles[x, y] = Tile.Wall; break;
                    case 'D': level.tiles[x, y] = Tile.Door; break;
                    case 'E': level.tiles[x, y] = Tile.Exit; exits++; break;
                    case '@': level.PlayerStart = p; players++; break;
                    case '*': level.Items.Add(new Coin(p)); break;
                    case 'k': level.Items.Add(new Key(p)); break;
                    case '$': level.Items.Add(new Diamond(p)); diamonds++; break;
                    case '^': level.Guards.Add(new Guard(p, Direction.Up)); break;
                    case 'v': level.Guards.Add(new Guard(p, Direction.Down)); break;
                    case '<': level.Guards.Add(new Guard(p, Direction.Left)); break;
                    case '>': level.Guards.Add(new Guard(p, Direction.Right)); break;
                }
            }
        }

        if (players != 1 || diamonds != 1 || exits < 1)
            throw new InvalidOperationException(
                $"Banan '{data.Name}' måste ha exakt en @, exakt en $ och minst en E.");

        return level;
    }

    public Tile GetTile(Position p) =>
        p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height ? tiles[p.X, p.Y] : Tile.Wall;

    public void SetTile(Position p, Tile tile) => tiles[p.X, p.Y] = tile;

    /// <summary>Väggar och stängda dörrar skymmer vakternas sikt.</summary>
    public bool BlocksSight(Position p) => GetTile(p) is Tile.Wall or Tile.Door;

    /// <summary>Vakter går bara på golv och aldrig in i en annan vakt.</summary>
    public bool IsWalkableForGuard(Position p) =>
        GetTile(p) == Tile.Floor && Guards.All(g => g.Position != p);

    public Item? ItemAt(Position p) => Items.FirstOrDefault(i => i.Position == p);

    /// <summary>Ser någon vakt den här rutan?</summary>
    public bool IsWatched(Position p) => Guards.Any(g => g.CanSee(p, this));

    /// <summary>Alla rutor som någon vakt ser, används för att rita synfälten.</summary>
    public HashSet<Position> WatchedTiles() =>
        Guards.SelectMany(g => g.VisibleTiles(this)).ToHashSet();
}
