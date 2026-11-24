using System.Numerics;
using Raylib_cs;
using Gentleman2D.Core;

namespace Gentleman2D.UI;

/// <summary>En enkel lista med val som styrs med pilarna och Enter.</summary>
public class MenuList
{
    private float bounce;

    public MenuList(params string[] options) => Options = options.ToList();

    public List<string> Options { get; }
    public int Selected { get; set; }

    /// <summary>Returnerar valet som bekräftades, eller null.</summary>
    public string? Update(InputState input, float dt)
    {
        bounce = Math.Max(0, bounce - dt * 4);
        if (input.Up) { Selected = (Selected + Options.Count - 1) % Options.Count; bounce = 1; }
        if (input.Down) { Selected = (Selected + 1) % Options.Count; bounce = 1; }
        return input.Confirm ? Options[Selected] : null;
    }

    public void Draw(float centerX, float y, float size = 26)
    {
        for (int i = 0; i < Options.Count; i++)
        {
            bool sel = i == Selected;
            float yy = y + i * (size + 26);
            if (sel)
            {
                var m = Theme.Measure(Options[i], size);
                var r = new Rectangle(centerX - m.X / 2 - 30 - bounce * 6, yy - 10, m.X + 60 + bounce * 12, size + 20);
                Raylib.DrawRectangleRounded(r, 0.5f, 12, Theme.WithAlpha(Theme.Gold, 0.16f));
                Raylib.DrawRectangleRoundedLinesEx(r, 0.5f, 12, 1.5f, Theme.WithAlpha(Theme.Gold, 0.7f));
            }
            Theme.DrawTextCentered(Options[i], centerX, yy, size, sel ? Theme.Gold : Theme.Text, true);
        }
    }
}

/// <summary>Huvudmenyn, ritas ovanpå staden som långsamt glider förbi.</summary>
public static class TitleScreen
{
    public static void Draw(MenuList menu, int w, int h, float time)
    {
        // Mörk toning så att texten syns mot staden bakom
        Raylib.DrawRectangleGradientV(0, 0, w, h, new Color(8, 10, 18, 120), new Color(8, 10, 18, 230));

        // Den höga hatten
        var c = new Vector2(w / 2f, 150 + MathF.Sin(time * 1.5f) * 4);
        Raylib.DrawEllipse((int)c.X, (int)c.Y + 34, 74, 12, new Color(20, 20, 24, 255));
        Raylib.DrawRectangleRounded(new Rectangle(c.X - 42, c.Y - 36, 84, 70), 0.15f, 8, new Color(26, 26, 32, 255));
        Raylib.DrawRectangle((int)c.X - 42, (int)c.Y + 14, 84, 14, new Color(176, 36, 48, 255));
        Raylib.DrawRectangle((int)c.X - 30, (int)c.Y - 30, 6, 40, new Color(255, 255, 255, 25));

        Theme.DrawTextCentered("THE GENTLEMAN", w / 2f, 214, 78, Theme.Gold, true);
        Theme.DrawTextCentered("Ett imperium byggt i det tysta", w / 2f, 300, 22, Theme.TextDim);

        menu.Draw(w / 2f, 380);

        Theme.DrawTextCentered("W/S eller pilarna: välj    Enter: bekräfta", w / 2f, h - 46, 16, Theme.WithAlpha(Theme.Text, 0.5f));
    }
}

public static class PauseScreen
{
    public static void Draw(MenuList menu, int w, int h)
    {
        Raylib.DrawRectangle(0, 0, w, h, new Color(6, 8, 14, 170));
        var r = new Rectangle(w / 2f - 230, h / 2f - 170, 460, 330);
        Theme.DrawPanel(r, Theme.Panel, 0.08f);
        Theme.DrawTextCentered("PAUS", w / 2f, r.Y + 30, 36, Theme.Gold);
        menu.Draw(w / 2f, r.Y + 110, 24);

        // Kontroller
        Theme.DrawTextCentered("WASD gå · E använd · Q störsändare · TAB telefon", w / 2f, r.Y + r.Height + 20, 16, Theme.TextDim);
    }
}

public static class ArrestScreen
{
    public static void Draw(ArrestReport report, int w, int h, float time)
    {
        Raylib.DrawRectangle(0, 0, w, h, new Color(20, 6, 10, 215));
        float blink = (int)(time * 4) % 2 == 0 ? 1 : 0.4f;
        Raylib.DrawRectangleGradientH(0, 0, w / 2, h, Theme.WithAlpha(new Color(255, 40, 40, 255), 0.12f * blink), new Color(0, 0, 0, 0));
        Raylib.DrawRectangleGradientH(w / 2, 0, w / 2, h, new Color(0, 0, 0, 0), Theme.WithAlpha(new Color(60, 100, 255, 255), 0.12f * (1.4f - blink)));

        Theme.DrawTextCentered("GRIPEN", w / 2f, h / 2f - 130, 84, Theme.Red, true);
        Theme.DrawTextCentered("Polisen fick tag på dig.", w / 2f, h / 2f - 30, 24, Theme.Text);
        Theme.DrawTextCentered($"Du förlorade ${report.LostMoney} i kontanter och {report.LostItems} föremål.", w / 2f, h / 2f + 10, 20, Theme.TextDim);
        Theme.DrawTextCentered("Pengar i kassaskåpet och maskinerna hemma är kvar.", w / 2f, h / 2f + 40, 20, Theme.TextDim);
        Theme.DrawTextCentered("Tryck Enter för att släppas nästa morgon", w / 2f, h / 2f + 120, 20, Theme.Gold);
    }
}
