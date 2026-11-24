namespace TheGentleman.Models;

/// <summary>De fyra riktningar man kan gå eller titta åt.</summary>
public enum Direction
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>En ruta på kartan. "record struct" ger oss jämförelse (==) gratis.</summary>
public readonly record struct Position(int X, int Y)
{
    /// <summary>Returnerar rutan bredvid, i den angivna riktningen.</summary>
    public Position Move(Direction direction) => direction switch
    {
        Direction.Up => this with { Y = Y - 1 },
        Direction.Down => this with { Y = Y + 1 },
        Direction.Left => this with { X = X - 1 },
        Direction.Right => this with { X = X + 1 },
        _ => this
    };
}

public static class DirectionExtensions
{
    /// <summary>Motsatt riktning, används när en vakt vänder.</summary>
    public static Direction Opposite(this Direction direction) => direction switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => direction
    };
}
