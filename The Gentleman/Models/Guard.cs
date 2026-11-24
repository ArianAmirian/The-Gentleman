using TheGentleman.Core;

namespace TheGentleman.Models;

/// <summary>
/// En vakt som patrullerar fram och tillbaka i en rak linje.
/// Vakten ser rakt fram tills en vägg eller dörr skymmer sikten.
/// </summary>
public class Guard : Entity
{
    /// <summary>Hur många rutor fram en vakt ser.</summary>
    public const int SightRange = 6;

    public Guard(Position start, Direction facing) : base(start)
    {
        Facing = facing;
    }

    public Direction Facing { get; private set; }

    // Vakten ritas som en pil så att man ser åt vilket håll den tittar.
    public override char Symbol => Facing switch
    {
        Direction.Up => '^',
        Direction.Down => 'v',
        Direction.Left => '<',
        _ => '>'
    };

    public override ConsoleColor Color => ConsoleColor.Red;

    /// <summary>
    /// Ett steg framåt. Om vägen är blockerad vänder vakten i stället
    /// (och står still den turen), vilket ger spelaren en chans att smita förbi.
    /// </summary>
    public void Patrol(Level level)
    {
        var next = Position.Move(Facing);
        if (level.IsWalkableForGuard(next))
            Position = next;
        else
            Facing = Facing.Opposite();
    }

    /// <summary>Alla rutor vakten kan se just nu.</summary>
    public IEnumerable<Position> VisibleTiles(Level level)
    {
        var current = Position;
        for (int step = 0; step < SightRange; step++)
        {
            current = current.Move(Facing);
            if (level.BlocksSight(current))
                yield break;
            yield return current;
        }
    }

    public bool CanSee(Position target, Level level) =>
        target == Position || VisibleTiles(level).Contains(target);
}
