// Spelplanens storlek
int bredd = 30;
int hojd = 10;

// Karaktärens startplats
int x = 5;
int y = 5;

Console.CursorVisible = false;

while (true)
{
    // 1. RITA
    Console.Clear();
    Console.WriteLine("+" + new string('-', bredd) + "+");

    for (int rad = 0; rad < hojd; rad++)
    {
        Console.Write("|");
        for (int kolumn = 0; kolumn < bredd; kolumn++)
        {
            if (kolumn == x && rad == y)
                Console.Write("@");
            else
                Console.Write(" ");
        }
        Console.WriteLine("|");
    }

    Console.WriteLine("+" + new string('-', bredd) + "+");
    Console.WriteLine("Piltangenter = gå, Esc = avsluta");

    // 2. LYSSNA
    var knapp = Console.ReadKey(true).Key;

    // 3. UPPDATERA
    if (knapp == ConsoleKey.Escape) break;
    if (knapp == ConsoleKey.UpArrow && y > 0) y--;
    if (knapp == ConsoleKey.DownArrow && y < hojd - 1) y++;
    if (knapp == ConsoleKey.LeftArrow && x > 0) x--;
    if (knapp == ConsoleKey.RightArrow && x < bredd - 1) x++;
}