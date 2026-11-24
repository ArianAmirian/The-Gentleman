using TheGentleman.UI;

namespace TheGentleman.Core;

public enum GameResult
{
    Won,
    Lost,
    Quit
}

/// <summary>
/// En hel spelomgång: går igenom banorna i ordning och håller koll på liv och poäng.
/// </summary>
public class Game
{
    public const int StartLives = 3;
    public const int LifeBonus = 250;

    private readonly Renderer renderer;
    private readonly IReadOnlyList<LevelData> levels;

    public Game(Renderer renderer, IReadOnlyList<LevelData> levels)
    {
        this.renderer = renderer;
        this.levels = levels;
    }

    public int Score { get; private set; }
    public int Lives { get; private set; } = StartLives;
    public int LevelsCleared { get; private set; }

    private enum LevelResult
    {
        Escaped,
        Caught,
        Restart,
        Quit
    }

    public GameResult Run()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            renderer.ShowBriefing(i + 1, levels.Count, levels[i]);

            bool cleared = false;
            while (!cleared)
            {
                // Ny session = banan återställs (dörrar, föremål, vakter).
                var session = new LevelSession(levels[i]);

                switch (PlayLevel(session, i + 1))
                {
                    case LevelResult.Escaped:
                        int levelScore = session.CalculateScore();
                        Score += levelScore;
                        LevelsCleared++;
                        renderer.ShowLevelComplete(session, levelScore, Score);
                        cleared = true;
                        break;

                    case LevelResult.Caught:
                        Lives--;
                        renderer.ShowCaught(Lives);
                        if (Lives == 0)
                            return GameResult.Lost;
                        break;

                    case LevelResult.Restart:
                        break;

                    case LevelResult.Quit:
                        return GameResult.Quit;
                }
            }
        }

        Score += Lives * LifeBonus;
        return GameResult.Won;
    }

    private LevelResult PlayLevel(LevelSession session, int levelNumber)
    {
        Console.Clear();

        while (true)
        {
            renderer.DrawLevel(session, levelNumber, levels.Count, Score, Lives);

            var key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.Escape)
            {
                if (renderer.Confirm("Vill du avbryta kuppen? (J/N)"))
                    return LevelResult.Quit;
                Console.Clear();
                continue;
            }

            if (key == ConsoleKey.R)
                return LevelResult.Restart;

            PlayerAction? action = key switch
            {
                ConsoleKey.UpArrow or ConsoleKey.W => PlayerAction.Up,
                ConsoleKey.DownArrow or ConsoleKey.S => PlayerAction.Down,
                ConsoleKey.LeftArrow or ConsoleKey.A => PlayerAction.Left,
                ConsoleKey.RightArrow or ConsoleKey.D => PlayerAction.Right,
                ConsoleKey.Spacebar => PlayerAction.Wait,
                _ => null
            };

            if (action is null)
                continue;

            switch (session.Step(action.Value))
            {
                case TurnOutcome.Escaped:
                    return LevelResult.Escaped;

                case TurnOutcome.Caught:
                    // Rita en sista gång så att man ser vilken vakt som såg en.
                    renderer.DrawLevel(session, levelNumber, levels.Count, Score, Lives);
                    return LevelResult.Caught;
            }
        }
    }
}
