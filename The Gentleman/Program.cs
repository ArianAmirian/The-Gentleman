using System.Text;
using TheGentleman.Core;
using TheGentleman.UI;

// The Gentleman: ett turbaserat smygspel i terminalen.
// Programmet startar bara menyn. All logik finns i mapparna Models, Core, UI och Data.

if (Console.IsInputRedirected)
{
    Console.WriteLine("The Gentleman måste köras i en riktig terminal (t.ex. 'dotnet run').");
    return;
}

Console.OutputEncoding = Encoding.UTF8;
Console.CursorVisible = false;

try
{
    var menu = new Menu(new Renderer(), new HighscoreManager());
    menu.Run();
}
finally
{
    // Återställ terminalen även om något går fel.
    Console.ResetColor();
    Console.CursorVisible = true;
    Console.Clear();
}
