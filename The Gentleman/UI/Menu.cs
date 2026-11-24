using TheGentleman.Core;
using TheGentleman.Data;

namespace TheGentleman.UI;

/// <summary>Huvudmenyn: starta spel, läs instruktioner, se topplistan eller avsluta.</summary>
public class Menu
{
    private readonly Renderer renderer;
    private readonly HighscoreManager highscores;

    public Menu(Renderer renderer, HighscoreManager highscores)
    {
        this.renderer = renderer;
        this.highscores = highscores;
    }

    public void Run()
    {
        while (true)
        {
            renderer.ShowTitle();

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1 or ConsoleKey.NumPad1 or ConsoleKey.Enter:
                    PlayGame();
                    break;
                case ConsoleKey.D2 or ConsoleKey.NumPad2:
                    renderer.ShowInstructions();
                    break;
                case ConsoleKey.D3 or ConsoleKey.NumPad3:
                    renderer.ShowHighscores(highscores.Entries);
                    break;
                case ConsoleKey.D4 or ConsoleKey.NumPad4 or ConsoleKey.Escape or ConsoleKey.Q:
                    return;
            }
        }
    }

    private void PlayGame()
    {
        var game = new Game(renderer, LevelLibrary.All);
        var result = game.Run();
        renderer.ShowGameOver(result, game, LevelLibrary.All.Count);

        if (highscores.Qualifies(game.Score))
        {
            string name = renderer.AskName();
            highscores.Add(new HighscoreEntry(name, game.Score, game.LevelsCleared, DateTime.Now));
            renderer.ShowHighscores(highscores.Entries);
        }
    }
}
