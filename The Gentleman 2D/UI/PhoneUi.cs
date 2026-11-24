using System.Numerics;
using Raylib_cs;
using Gentleman2D.Core;
using Gentleman2D.Rendering;

namespace Gentleman2D.UI;

/// <summary>Telefonen (TAB): beställningar från kunder och en översikt över imperiet.</summary>
public class PhoneUi
{
    private const float Width = 360;
    private const float Height = 560;

    private float slide; // 0 = gömd, 1 = framme
    private int tab;

    public bool IsOpen { get; private set; }

    public void Toggle() => IsOpen = !IsOpen;
    public void Close() => IsOpen = false;

    public void Update(GameWorld world, float dt)
    {
        slide = Math.Clamp(slide + (IsOpen ? dt : -dt) * 6, 0, 1);
        if (!IsOpen) return;
        if (world.Input.Tab1) tab = 0;
        if (world.Input.Tab2) tab = 1;
        if (world.Input.Back) IsOpen = false;
    }

    public void Draw(GameWorld world, int screenW, int screenH)
    {
        if (slide <= 0) return;

        float ease = 1 - MathF.Pow(1 - slide, 3);
        float x = screenW - Width - 40;
        float y = screenH - Height * ease - 20 + (1 - ease) * 40;

        // Telefonens skal och skärm
        var body = new Rectangle(x, y, Width, Height);
        Raylib.DrawRectangleRounded(new Rectangle(x + 6, y + 10, Width, Height), 0.12f, 16, new Color(0, 0, 0, 90));
        Raylib.DrawRectangleRounded(body, 0.12f, 16, new Color(24, 24, 30, 255));
        var screen = new Rectangle(x + 12, y + 12, Width - 24, Height - 24);
        Raylib.DrawRectangleRounded(screen, 0.1f, 16, new Color(244, 241, 234, 255));
        Raylib.DrawRectangleRounded(new Rectangle(x + Width / 2 - 40, y + 18, 80, 18), 1f, 8, new Color(24, 24, 30, 255));

        var dark = new Color(30, 32, 40, 255);
        var dim = new Color(120, 120, 130, 255);
        Theme.DrawText(world.Clock.TimeText, screen.X + 20, screen.Y + 8, 15, dark);
        Theme.DrawTextRight("Krypterad", screen.X + screen.Width - 20, screen.Y + 8, 14, dim);

        // Flikar
        string[] tabs = { "1  Uppdrag", "2  Översikt" };
        for (int i = 0; i < tabs.Length; i++)
        {
            var tr = new Rectangle(screen.X + 14 + i * (screen.Width - 28) / 2, screen.Y + 40, (screen.Width - 36) / 2, 34);
            Raylib.DrawRectangleRounded(tr, 0.5f, 10, i == tab ? dark : new Color(225, 221, 212, 255));
            Theme.DrawTextCentered(tabs[i], tr.X + tr.Width / 2, tr.Y + 8, 17, i == tab ? Theme.Gold : dim);
        }

        float cy = screen.Y + 90;
        if (tab == 0) DrawOrders(world, screen, cy);
        else DrawOverview(world, screen, cy);

        Theme.DrawTextCentered("TAB stäng   1/2 byt flik", screen.X + screen.Width / 2, screen.Y + screen.Height - 26, 14, dim);
    }

    private static void DrawOrders(GameWorld world, Rectangle screen, float y)
    {
        var dark = new Color(30, 32, 40, 255);
        var dim = new Color(120, 120, 130, 255);
        var orders = world.Orders.Active;

        if (orders.Count == 0)
        {
            Theme.DrawTextCentered("Inga beställningar just nu.", screen.X + screen.Width / 2, y + 40, 18, dim);
            string hint = world.State.Has(Upgrade.GemCutter) || world.State.Has(Upgrade.Smelter)
                ? "Kunderna hör av sig snart."
                : "Köp en Slipbänk så hör kunderna av sig.";
            Theme.DrawTextCentered(hint, screen.X + screen.Width / 2, y + 66, 16, dim);
            return;
        }

        foreach (var order in orders)
        {
            var card = new Rectangle(screen.X + 14, y, screen.Width - 28, 122);
            Raylib.DrawRectangleRounded(card, 0.12f, 12, Color.White);
            Raylib.DrawRectangleRoundedLinesEx(card, 0.12f, 12, 1, new Color(0, 0, 0, 25));

            var info = ItemCatalog.Get(order.Product);
            Raylib.DrawCircleV(new Vector2(card.X + 30, card.Y + 30), 18, dark);
            Paint.ItemIcon(order.Product, new Vector2(card.X + 30, card.Y + 30), 9);

            Theme.DrawText(order.CustomerName, card.X + 58, card.Y + 12, 19, dark);
            Theme.DrawText($"Vill köpa {order.Quantity} × {info.Name.ToLower()}", card.X + 58, card.Y + 36, 16, dim);

            int have = world.State.Inventory.Count(order.Product);
            bool ready = have >= order.Quantity;
            Theme.DrawText($"${order.Price}", card.X + 16, card.Y + 64, 24, new Color(170, 120, 30, 255));
            Theme.DrawTextRight($"Plats: {order.Place.Name}", card.X + card.Width - 16, card.Y + 68, 16, dark);

            float left = order.MinutesLeft(world.Clock);
            string time = $"{(int)(left / 60)} h {(int)(left % 60):00} min kvar";
            Theme.DrawText(time, card.X + 16, card.Y + 96, 15, left < 120 ? Theme.Red : dim);
            Theme.DrawTextRight(ready ? "Redo att leverera" : $"Du har {have}/{order.Quantity}", card.X + card.Width - 16, card.Y + 96, 15,
                ready ? new Color(40, 150, 70, 255) : dim);

            y += card.Height + 10;
        }
    }

    private static void DrawOverview(GameWorld world, Rectangle screen, float y)
    {
        var dark = new Color(30, 32, 40, 255);
        var dim = new Color(120, 120, 130, 255);
        var s = world.State;
        float x = screen.X + 24, right = screen.X + screen.Width - 24;

        void Row(string label, string value, Color? color = null)
        {
            Theme.DrawText(label, x, y, 17, dim);
            Theme.DrawTextRight(value, right, y, 17, color ?? dark);
            y += 28;
        }

        Row("Dag", world.Clock.Day.ToString());
        Row("Kontanter", $"${s.Money}");
        Row("Kassaskåp", s.Has(Upgrade.Safe) ? $"${s.SafeMoney}" : "–");
        Row("Marknadspris idag", $"{world.Market * 100:0} %", world.Market >= 1 ? new Color(40, 150, 70, 255) : Theme.Red);
        Row("Levererade order", s.OrdersDelivered.ToString());
        Row("Totalt intjänat", $"${s.TotalEarned}");
        Row("Gripen", $"{s.TimesArrested} gånger");

        y += 10;
        Theme.DrawText("UTRUSTNING", x, y, 15, new Color(170, 120, 30, 255));
        y += 26;
        foreach (var item in Shop.Items)
        {
            bool owned = s.Has(item.Id);
            Raylib.DrawCircleV(new Vector2(x + 6, y + 9), 6, owned ? new Color(40, 150, 70, 255) : new Color(210, 206, 198, 255));
            Theme.DrawText(item.Name, x + 20, y, 16, owned ? dark : dim);
            y += 24;
        }
    }
}
