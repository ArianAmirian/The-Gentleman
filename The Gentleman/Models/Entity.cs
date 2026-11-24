namespace TheGentleman.Models;

/// <summary>
/// Basklass för allt som står på kartan: spelaren, vakter och föremål.
/// Abstrakt eftersom man aldrig skapar en "Entity" direkt, bara subklasser.
/// </summary>
public abstract class Entity
{
    protected Entity(Position position)
    {
        Position = position;
    }

    public Position Position { get; set; }

    /// <summary>Tecknet som ritas på skärmen.</summary>
    public abstract char Symbol { get; }

    /// <summary>Färgen tecknet ritas med.</summary>
    public abstract ConsoleColor Color { get; }
}
