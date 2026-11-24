namespace TheGentleman.Models;

/// <summary>The Gentleman själv.</summary>
public class Player : Entity
{
    public Player(Position start) : base(start)
    {
    }

    public override char Symbol => '@';
    public override ConsoleColor Color => ConsoleColor.Yellow;

    /// <summary>Antal nycklar spelaren bär på just nu.</summary>
    public int Keys { get; set; }

    /// <summary>Banan kan bara lämnas när diamanten är tagen.</summary>
    public bool HasDiamond { get; set; }

    /// <summary>Värdet på allt byte spelaren plockat upp på den här banan.</summary>
    public int Loot { get; set; }
}
