namespace TheGentleman.Models;

/// <summary>
/// Något som kan plockas upp. Varje sorts föremål bestämmer själv
/// vad som händer när det plockas upp (polymorfism).
/// </summary>
public abstract class Item : Entity
{
    protected Item(Position position) : base(position)
    {
    }

    /// <summary>Texten som visas när föremålet plockas upp.</summary>
    public abstract string PickupMessage { get; }

    public abstract void PickUp(Player player);
}

/// <summary>Guldmynt, ger extrapoäng.</summary>
public class Coin : Item
{
    public const int Value = 100;

    public Coin(Position position) : base(position)
    {
    }

    public override char Symbol => '*';
    public override ConsoleColor Color => ConsoleColor.DarkYellow;
    public override string PickupMessage => $"Guldmynt! +{Value} poäng.";

    public override void PickUp(Player player) => player.Loot += Value;
}

/// <summary>Nyckel, öppnar en låst dörr (D).</summary>
public class Key : Item
{
    public Key(Position position) : base(position)
    {
    }

    public override char Symbol => 'k';
    public override ConsoleColor Color => ConsoleColor.Cyan;
    public override string PickupMessage => "Du hittade en nyckel. Den öppnar en låst dörr.";

    public override void PickUp(Player player) => player.Keys++;
}

/// <summary>Diamanten, målet med varje bana.</summary>
public class Diamond : Item
{
    public const int Value = 1000;

    public Diamond(Position position) : base(position)
    {
    }

    public override char Symbol => '$';
    public override ConsoleColor Color => ConsoleColor.Magenta;
    public override string PickupMessage => $"DIAMANTEN är din! +{Value} poäng. Ta dig till utgången (E)!";

    public override void PickUp(Player player)
    {
        player.HasDiamond = true;
        player.Loot += Value;
    }
}
