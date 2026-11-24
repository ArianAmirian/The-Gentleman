using System.Numerics;
using Raylib_cs;
using Gentleman2D.Core;

namespace Gentleman2D.UI;

/// <summary>Butiken på laptopen: köp maskiner och prylar.</summary>
public class ShopUi
{
    private int selected;
    private float flash;
    private string feedback = "";

    public bool IsOpen { get; private set; }

    public void Open()
    {
        IsOpen = true;
        feedback = "";
    }

    public void Close() => IsOpen = false;

    public void Update(GameWorld world, float dt)
    {
        flash = Math.Max(0, flash - dt);
        if (!IsOpen) return;

        var input = world.Input;
        int count = Shop.Items.Count;
        if (input.Up) selected = (selected + count - 1) % count;
        if (input.Down) selected = (selected + 1) % count;
        if (input.Back) { IsOpen = false; return; }

        if (input.Confirm)
        {
            var item = Shop.Items[selected];
            if (world.State.Has(item.Id))
                feedback = "Du äger redan den här.";
            else if (world.State.Money < item.Price)
                feedback = $"Du behöver ${item.Price - world.State.Money} till.";
            else if (world.State.TryBuy(item))
            {
                feedback = $"{item.Name} köpt!";
                flash = 0.6f;
                world.Notify($"Köpt: {item.Name}", Theme.Green);
                if (item.Id is Upgrade.GemCutter or Upgrade.Smelter) world.Orders.RequestSoon();
            }
        }
    }

    public void Draw(GameWorld world, int screenW, int screenH)
    {
        if (!IsOpen) return;

        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 140));

        // Laptopens ram
        var frame = new Rectangle(screenW / 2f - 420, screenH / 2f - 270, 840, 520);
        Raylib.DrawRectangleRounded(new Rectangle(frame.X + 6, frame.Y + 12, frame.Width, frame.Height), 0.06f, 12, new Color(0, 0, 0, 100));
        Raylib.DrawRectangleRounded(frame, 0.06f, 12, new Color(36, 38, 46, 255));
        var screen = new Rectangle(frame.X + 16, frame.Y + 16, frame.Width - 32, frame.Height - 32);
        Raylib.DrawRectangleRounded(screen, 0.04f, 12, new Color(16, 18, 24, 255));

        Theme.DrawText("SVARTA MARKNADEN", screen.X + 28, screen.Y + 22, 26, Theme.Gold);
        Theme.DrawText("Utrustning för den diskreta gentlemannen", screen.X + 28, screen.Y + 54, 16, Theme.TextDim);
        Theme.DrawTextRight($"${world.State.Money}", screen.X + screen.Width - 28, screen.Y + 24, 28, Theme.Gold);

        // Lista till vänster
        float y = screen.Y + 94;
        for (int i = 0; i < Shop.Items.Count; i++)
        {
            var item = Shop.Items[i];
            bool owned = world.State.Has(item.Id);
            bool sel = i == selected;
            var row = new Rectangle(screen.X + 20, y, 380, 52);
            Raylib.DrawRectangleRounded(row, 0.3f, 10, sel ? new Color(48, 52, 64, 255) : new Color(26, 28, 36, 255));
            if (sel) Raylib.DrawRectangleRoundedLinesEx(row, 0.3f, 10, 2, Theme.Gold);

            Raylib.DrawCircleV(new Vector2(row.X + 28, row.Y + 26), 14, item.Color);
            Theme.DrawText(item.Name, row.X + 54, row.Y + 15, 20, owned ? Theme.TextDim : Theme.Text);

            string price = owned ? "Ägs" : $"${item.Price}";
            var priceColor = owned ? Theme.Green : world.State.Money >= item.Price ? Theme.Gold : Theme.Red;
            Theme.DrawTextRight(price, row.X + row.Width - 18, row.Y + 15, 20, priceColor);
            y += 60;
        }

        // Detaljer till höger
        var sel2 = Shop.Items[selected];
        var detail = new Rectangle(screen.X + 420, screen.Y + 94, screen.Width - 440, 352);
        Raylib.DrawRectangleRounded(detail, 0.06f, 10, new Color(26, 28, 36, 255));
        Raylib.DrawCircleV(new Vector2(detail.X + 50, detail.Y + 50), 28, sel2.Color);
        Theme.DrawText(sel2.Name, detail.X + 94, detail.Y + 36, 28, Theme.Text);
        DrawWrapped(sel2.Description, detail.X + 26, detail.Y + 100, detail.Width - 52, 18, Theme.TextDim);

        bool own = world.State.Has(sel2.Id);
        bool afford = world.State.Money >= sel2.Price;
        var btn = new Rectangle(detail.X + 26, detail.Y + detail.Height - 76, detail.Width - 52, 50);
        var btnColor = own ? new Color(40, 70, 50, 255) : afford ? Theme.Gold : new Color(70, 40, 44, 255);
        if (flash > 0) btnColor = Theme.Lerp(btnColor, Color.White, flash);
        Raylib.DrawRectangleRounded(btn, 0.4f, 10, btnColor);
        string btnText = own ? "Redan köpt" : afford ? $"Köp för ${sel2.Price}  (Enter)" : $"Du har inte råd (${sel2.Price})";
        Theme.DrawTextCentered(btnText, btn.X + btn.Width / 2, btn.Y + 14, 20, own || !afford ? Theme.Text : new Color(30, 24, 10, 255));

        if (feedback.Length > 0)
            Theme.DrawTextCentered(feedback, detail.X + detail.Width / 2, btn.Y - 34, 17, Theme.Text);

        Theme.DrawText("W/S välj   Enter köp   Esc stäng", screen.X + 28, screen.Y + screen.Height - 34, 15, Theme.TextDim);
    }

    /// <summary>Radbryter text så att den får plats i en viss bredd.</summary>
    public static void DrawWrapped(string text, float x, float y, float width, float size, Color color)
    {
        var line = "";
        foreach (var word in text.Split(' '))
        {
            string test = line.Length == 0 ? word : line + " " + word;
            if (Theme.Measure(test, size).X > width && line.Length > 0)
            {
                Theme.DrawText(line, x, y, size, color);
                y += size + 6;
                line = word;
            }
            else line = test;
        }
        if (line.Length > 0) Theme.DrawText(line, x, y, size, color);
    }
}
