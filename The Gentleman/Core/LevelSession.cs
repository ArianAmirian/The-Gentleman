using TheGentleman.Models;

namespace TheGentleman.Core;

public enum PlayerAction
{
    Up,
    Down,
    Left,
    Right,
    Wait
}

public enum TurnOutcome
{
    /// <summary>Turen gick, spelet fortsätter.</summary>
    Continue,
    /// <summary>Draget gick inte att göra (vägg, låst dörr...). Ingen tur förflöt.</summary>
    Blocked,
    /// <summary>En vakt såg spelaren.</summary>
    Caught,
    /// <summary>Spelaren kom ut med diamanten.</summary>
    Escaped
}

/// <summary>
/// Spelreglerna för en bana, helt utan Console. Det gör att reglerna
/// kan testas automatiskt och att ritandet ligger separat i Renderer.
/// </summary>
public class LevelSession
{
    public LevelSession(LevelData data)
    {
        Data = data;
        Level = Level.Parse(data);
        Player = new Player(Level.PlayerStart);
    }

    public LevelData Data { get; }
    public Level Level { get; }
    public Player Player { get; }
    public int Moves { get; private set; }
    public string Message { get; private set; } = "";

    /// <summary>
    /// Kör en tur: först spelaren, sedan vakterna.
    /// Spelet är turbaserat, vakterna rör sig bara när du gör ett drag.
    /// </summary>
    public TurnOutcome Step(PlayerAction action)
    {
        Message = "";

        if (action != PlayerAction.Wait)
        {
            var direction = action switch
            {
                PlayerAction.Up => Direction.Up,
                PlayerAction.Down => Direction.Down,
                PlayerAction.Left => Direction.Left,
                _ => Direction.Right
            };
            var target = Player.Position.Move(direction);

            switch (Level.GetTile(target))
            {
                case Tile.Wall:
                    return TurnOutcome.Blocked;

                case Tile.Door:
                    if (Player.Keys == 0)
                    {
                        Message = "Dörren är låst. Du behöver en nyckel (k).";
                        return TurnOutcome.Blocked;
                    }
                    Player.Keys--;
                    Level.SetTile(target, Tile.Floor);
                    Message = "Klick! Du låste upp dörren.";
                    break; // Att låsa upp tar en tur men du står kvar.

                case Tile.Exit:
                    if (!Player.HasDiamond)
                    {
                        Message = "En gentleman går inte härifrån utan diamanten!";
                        return TurnOutcome.Blocked;
                    }
                    Player.Position = target;
                    Moves++;
                    return TurnOutcome.Escaped;

                default:
                    Player.Position = target;
                    PickUpItem();
                    break;
            }
        }

        Moves++;

        // Gick du rakt in i en vakts synfält?
        if (Level.IsWatched(Player.Position))
            return TurnOutcome.Caught;

        foreach (var guard in Level.Guards)
            guard.Patrol(Level);

        // Gick en vakt så att den nu ser dig?
        if (Level.IsWatched(Player.Position))
            return TurnOutcome.Caught;

        return TurnOutcome.Continue;
    }

    private void PickUpItem()
    {
        var item = Level.ItemAt(Player.Position);
        if (item is null)
            return;

        item.PickUp(Player);
        Level.Items.Remove(item);
        Message = item.PickupMessage;
    }

    /// <summary>Poängen för en klarad bana: byte + bonus för klarad bana + bonus för få drag.</summary>
    public int CalculateScore() =>
        Player.Loot + LevelClearBonus + Math.Max(0, Data.Par - Moves) * BonusPerSavedMove;

    public const int LevelClearBonus = 500;
    public const int BonusPerSavedMove = 20;
}
