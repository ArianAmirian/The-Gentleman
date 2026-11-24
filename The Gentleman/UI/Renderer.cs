using TheGentleman.Core;
using TheGentleman.Models;

namespace TheGentleman.UI;

/// <summary>
/// Allt som skrivs ut på skärmen samlas här, så att spellogiken
/// (LevelSession, Game) inte behöver känna till Console-färger och layout.
/// </summary>
public class Renderer
{
    // Hur bred varje rad fylls ut, så att gammal text skrivs över utan att skärmen flimrar.
    private const int LineWidth = 80;

    // ---------- Banan ----------

    public void DrawLevel(LevelSession session, int levelNumber, int levelCount, int score, int lives)
    {
        // I stället för Console.Clear() (som flimrar) skriver vi över allt från övre hörnet.
        Console.SetCursorPosition(0, 0);

        var level = session.Level;
        var player = session.Player;
        var watched = level.WatchedTiles();

        WriteLine($"  THE GENTLEMAN  ·  Bana {levelNumber}/{levelCount}: {level.Name}", ConsoleColor.White);
        WriteLine("");

        for (int y = 0; y < level.Height; y++)
        {
            Console.Write("  ");
            for (int x = 0; x < level.Width; x++)
            {
                var (symbol, color) = GetCell(new Position(x, y), level, player, watched);
                Console.ForegroundColor = color;
                Console.Write(symbol);
            }
            Console.ResetColor();
            Console.WriteLine(new string(' ', Math.Max(0, LineWidth - level.Width - 2)));
        }

        WriteLine("");
        string hearts = new string('♥', lives) + new string('·', Math.Max(0, Game.StartLives - lives));
        string diamond = player.HasDiamond ? "JA" : "nej";
        WriteLine($"  Liv: {hearts}   Poäng: {score}   Byte: {player.Loot}   Nycklar: {player.Keys}   Diamant: {diamond}", ConsoleColor.White);
        WriteLine($"  Drag: {session.Moves} (par {session.Data.Par})", ConsoleColor.Gray);
        WriteLine("  " + session.Message, ConsoleColor.Yellow);
        WriteLine("  Piltangenter/WASD = gå   Mellanslag = vänta   R = börja om   Esc = avbryt", ConsoleColor.DarkGray);
        WriteLine("");
    }

    private static (char Symbol, ConsoleColor Color) GetCell(
        Position p, Level level, Player player, HashSet<Position> watched)
    {
        if (player.Position == p)
            return (player.Symbol, player.Color);

        var guard = level.Guards.FirstOrDefault(g => g.Position == p);
        if (guard is not null)
            return (guard.Symbol, guard.Color);

        var item = level.ItemAt(p);
        if (item is not null)
            return (item.Symbol, watched.Contains(p) ? ConsoleColor.Red : item.Color);

        return level.GetTile(p) switch
        {
            Tile.Wall => ('█', ConsoleColor.DarkGray),
            Tile.Door => ('D', ConsoleColor.DarkYellow),
            Tile.Exit => ('E', player.HasDiamond ? ConsoleColor.Green : ConsoleColor.DarkGreen),
            _ when watched.Contains(p) => ('·', ConsoleColor.DarkRed),
            _ => (' ', ConsoleColor.Gray)
        };
    }

    // ---------- Händelser under spelet ----------

    public void ShowCaught(int livesLeft)
    {
        WriteLine("  STOPP! En vakt fick syn på dig.", ConsoleColor.Red);
        WriteLine(livesLeft > 0
            ? $"  Du har {livesLeft} liv kvar. Banan börjar om."
            : "  Du har inga liv kvar...", ConsoleColor.Gray);
        WaitForKey();
    }

    public bool Confirm(string question)
    {
        WriteLine("  " + question, ConsoleColor.Yellow);
        while (true)
        {
            var key = Console.ReadKey(true).Key;
            if (key is ConsoleKey.J or ConsoleKey.Y) return true;
            if (key is ConsoleKey.N or ConsoleKey.Escape) return false;
        }
    }

    // ---------- Hela skärmar ----------

    public void ShowTitle()
    {
        Console.Clear();
        Console.WriteLine();
        WriteLine("              _______", ConsoleColor.DarkGray);
        WriteLine("             |       |", ConsoleColor.DarkGray);
        WriteLine("             |       |", ConsoleColor.DarkGray);
        WriteLine("          ___|_______|___", ConsoleColor.DarkGray);
        Console.WriteLine();
        WriteLine("    T  H  E     G  E  N  T  L  E  M  A  N", ConsoleColor.Yellow);
        WriteLine("         en smygkupp i fem akter", ConsoleColor.Gray);
        Console.WriteLine();
        WriteLine("    [1]  Starta kuppen", ConsoleColor.White);
        WriteLine("    [2]  Så spelar du", ConsoleColor.White);
        WriteLine("    [3]  Topplista", ConsoleColor.White);
        WriteLine("    [4]  Avsluta", ConsoleColor.White);
    }

    public void ShowInstructions()
    {
        Console.Clear();
        Console.WriteLine();
        WriteLine("  SÅ SPELAR DU", ConsoleColor.Yellow);
        Console.WriteLine();
        Console.WriteLine("  Du är The Gentleman (@), en mästertjuv som aldrig blir upptäckt.");
        Console.WriteLine("  På varje bana ska du ta diamanten ($) och sedan gå ut genom utgången (E).");
        Console.WriteLine();
        Console.WriteLine("  Spelet är turbaserat: vakterna rör sig bara när du gör ett drag.");
        Console.WriteLine("  Vakter (> < ^ v) går fram och tillbaka och ser rakt fram i 6 rutor.");
        Console.WriteLine("  Det de ser visas med röda prickar. Står du där åker du fast.");
        Console.WriteLine("  Väggar och stängda dörrar skymmer sikten.");
        Console.WriteLine();
        Console.WriteLine("  k  nyckel, öppnar en låst dörr (D). Varje nyckel används en gång.");
        Console.WriteLine("  *  guldmynt, +100 poäng.");
        Console.WriteLine("  $  diamanten, +1000 poäng.");
        Console.WriteLine();
        Console.WriteLine($"  Klarad bana: +{LevelSession.LevelClearBonus} poäng, plus {LevelSession.BonusPerSavedMove} poäng för varje drag under par.");
        Console.WriteLine($"  Du har {Game.StartLives} liv. Klarar du alla banor får du {Game.LifeBonus} poäng per liv kvar.");
        Console.WriteLine();
        Console.WriteLine("  Piltangenter/WASD = gå   Mellanslag = vänta   R = börja om banan   Esc = avbryt");
        WaitForKey();
    }

    public void ShowHighscores(IReadOnlyList<HighscoreEntry> entries)
    {
        Console.Clear();
        Console.WriteLine();
        WriteLine("  TOPPLISTA", ConsoleColor.Yellow);
        Console.WriteLine();

        if (entries.Count == 0)
            Console.WriteLine("  Inga resultat än. Bli den första!");

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            var color = i == 0 ? ConsoleColor.Yellow : ConsoleColor.White;
            WriteLine($"  {i + 1,2}. {e.Name,-15} {e.Score,7} p   {e.LevelsCleared} banor   {e.Date:yyyy-MM-dd}", color);
        }
        WaitForKey();
    }

    public void ShowBriefing(int levelNumber, int levelCount, LevelData data)
    {
        Console.Clear();
        Console.WriteLine();
        WriteLine($"  BANA {levelNumber} AV {levelCount}: {data.Name.ToUpper()}", ConsoleColor.Yellow);
        Console.WriteLine();
        foreach (var line in data.Briefing.Split('\n'))
            Console.WriteLine("  " + line);
        Console.WriteLine();
        WriteLine($"  Par: {data.Par} drag", ConsoleColor.Gray);
        WaitForKey("Tryck på valfri tangent för att börja...");
    }

    public void ShowLevelComplete(LevelSession session, int levelScore, int totalScore)
    {
        Console.Clear();
        Console.WriteLine();
        WriteLine($"  KLART! Du kom undan från {session.Level.Name}.", ConsoleColor.Green);
        Console.WriteLine();
        int moveBonus = Math.Max(0, session.Data.Par - session.Moves) * LevelSession.BonusPerSavedMove;
        Console.WriteLine($"  Byte:            {session.Player.Loot,6}");
        Console.WriteLine($"  Klarad bana:     {LevelSession.LevelClearBonus,6}");
        Console.WriteLine($"  Dragbonus:       {moveBonus,6}   ({session.Moves} drag, par {session.Data.Par})");
        Console.WriteLine("  ----------------------");
        WriteLine($"  Banan gav:       {levelScore,6}", ConsoleColor.White);
        WriteLine($"  Totalt:          {totalScore,6}", ConsoleColor.Yellow);
        WaitForKey();
    }

    public void ShowGameOver(GameResult result, Game game, int levelCount)
    {
        Console.Clear();
        Console.WriteLine();
        switch (result)
        {
            case GameResult.Won:
                WriteLine("  KUPPEN LYCKADES!", ConsoleColor.Green);
                Console.WriteLine("  Alla fem diamanter är dina. Ingen såg något. Ingen vet något.");
                Console.WriteLine($"  Bonus för {game.Lives} liv kvar: +{game.Lives * Game.LifeBonus}");
                break;
            case GameResult.Lost:
                WriteLine("  GAME OVER", ConsoleColor.Red);
                Console.WriteLine("  Handbojorna klickar. Även en gentleman har dåliga dagar.");
                break;
            default:
                WriteLine("  KUPPEN AVBRUTEN", ConsoleColor.DarkYellow);
                Console.WriteLine("  Du smet iväg innan någon märkte något.");
                break;
        }
        Console.WriteLine();
        WriteLine($"  Banor klarade: {game.LevelsCleared}/{levelCount}", ConsoleColor.White);
        WriteLine($"  Slutpoäng:     {game.Score}", ConsoleColor.Yellow);
        WaitForKey();
    }

    public string AskName()
    {
        Console.WriteLine();
        WriteLine("  Nytt rekord på topplistan! Skriv ditt namn:", ConsoleColor.Yellow);
        Console.Write("  > ");
        Console.CursorVisible = true;
        string name = (Console.ReadLine() ?? "").Trim();
        Console.CursorVisible = false;

        if (name.Length == 0) name = "Anonym";
        return name.Length > 15 ? name[..15] : name;
    }

    // ---------- Hjälpmetoder ----------

    private static void WaitForKey(string text = "Tryck på valfri tangent...")
    {
        Console.WriteLine();
        WriteLine("  " + text, ConsoleColor.DarkGray);
        Console.ReadKey(true);
    }

    private static void WriteLine(string text, ConsoleColor color = ConsoleColor.Gray)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text.PadRight(LineWidth));
        Console.ResetColor();
    }
}
