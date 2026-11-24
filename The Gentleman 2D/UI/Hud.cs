using System.Numerics;
using Raylib_cs;
using Gentleman2D.Core;
using Gentleman2D.Rendering;
using Gentleman2D.World;

namespace Gentleman2D.UI;

/// <summary>
/// Allt som ligger ovanpå spelet: pengar, klocka, mål, efterlysning, minikarta,
/// väskan, vad E gör just nu och notiser.
/// </summary>
public class Hud : IDisposable
{
    private const int MiniScale = 3;
    private readonly RenderTexture2D minimap;
    private readonly int screenW;
    private readonly int screenH;

    public Hud(GameWorld world, int screenW, int screenH)
    {
        this.screenW = screenW;
        this.screenH = screenH;
        minimap = Raylib.LoadRenderTexture(world.Map.Width * MiniScale, world.Map.Height * MiniScale);
        RenderMinimap(world);
    }

    /// <summary>Minikartan ritas en gång i förväg, sedan läggs bara prickar ovanpå.</summary>
    private void RenderMinimap(GameWorld world)
    {
        var map = world.Map;
        Raylib.BeginTextureMode(minimap);
        Raylib.ClearBackground(Color.Black);
        for (int x = 0; x < map.Width; x++)
            for (int y = 0; y < map.Height; y++)
            {
                Color c = map[x, y] switch
                {
                    Tile.Road => new(70, 74, 84, 255),
                    Tile.Sidewalk => new(130, 134, 140, 255),
                    Tile.Path or Tile.Plaza => new(170, 160, 140, 255),
                    Tile.Wall => new(40, 42, 54, 255),
                    Tile.Roof => new(150, 80, 66, 255),
                    Tile.RoofSlate => new(80, 94, 112, 255),
                    Tile.Tree => new(50, 100, 60, 255),
                    Tile.Bush => new(64, 124, 70, 255),
                    Tile.Water => new(52, 100, 150, 255),
                    Tile.Pier => new(130, 96, 64, 255),
                    Tile.Grass => new(90, 136, 76, 255),
                    _ => new(200, 196, 186, 255)
                };
                Raylib.DrawRectangle(x * MiniScale, y * MiniScale, MiniScale, MiniScale, c);
            }
        Raylib.EndTextureMode();
    }

    public void Draw(GameWorld world)
    {
        DrawTopLeft(world);
        DrawTopRight(world);
        DrawInventory(world);
        DrawPrompt(world);
        DrawJammer(world);
        DrawNotifications(world);

        Theme.DrawTextRight("TAB Telefon   Q Störsändare   ESC Meny", screenW - 20, screenH - 30, 16, Theme.WithAlpha(Theme.Text, 0.55f));

        if (world.SleepFade > 0)
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, (int)(255 * Math.Min(1, world.SleepFade))));
    }

    private void DrawTopLeft(GameWorld world)
    {
        var s = world.State;
        var panel = new Rectangle(18, 18, 300, s.Has(Upgrade.Safe) ? 100 : 80);
        Theme.DrawPanel(panel);
        Theme.DrawText($"${s.Money:N0}".Replace(",", " "), 36, 28, 36, Theme.Gold);
        if (s.Has(Upgrade.Safe))
            Theme.DrawText($"Kassaskåp: ${s.SafeMoney:N0}".Replace(",", " "), 38, 74, 16, Theme.TextDim);

        // Klockan till höger, med sol eller måne
        var clock = world.Clock;
        float right = panel.X + panel.Width - 18;
        Theme.DrawTextRight(clock.TimeText, right, 26, 28, Theme.Text);
        string day = $"Dag {clock.Day}";
        Theme.DrawTextRight(day, right, 58, 16, Theme.TextDim);
        var iconPos = new Vector2(right - Theme.Measure(day, 16).X - 16, 67);
        if (clock.Darkness > 0.5f)
        {
            Raylib.DrawCircleV(iconPos, 7, new Color(230, 230, 210, 255));
            Raylib.DrawCircleV(iconPos + new Vector2(3, -2), 6, Theme.Panel);
        }
        else
        {
            Raylib.DrawCircleV(iconPos, 5, new Color(255, 200, 80, 255));
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8;
                var d = new Vector2(MathF.Cos(a), MathF.Sin(a));
                Raylib.DrawLineEx(iconPos + d * 7, iconPos + d * 9.5f, 1.5f, new Color(255, 200, 80, 255));
            }
        }

        // Nästa mål
        string objective = world.Objective;
        var size = Theme.Measure(objective, 17);
        var objPanel = new Rectangle(18, panel.Y + panel.Height + 10, Math.Max(220, size.X + 36), 52);
        Theme.DrawPanel(objPanel, Theme.WithAlpha(Theme.Panel, 0.75f));
        Theme.DrawText("MÅL", objPanel.X + 16, objPanel.Y + 7, 13, Theme.Gold);
        Theme.DrawText(objective, objPanel.X + 16, objPanel.Y + 24, 17, Theme.Text);
    }

    private void DrawTopRight(GameWorld world)
    {
        var heat = world.Heat;
        float right = screenW - 18;

        // Efterlysningsstjärnor
        var panel = new Rectangle(right - 250, 18, 250, 64);
        bool flash = heat.IsWanted && (int)(world.Time * 3) % 2 == 0;
        Theme.DrawPanel(panel, flash ? new Color(70, 20, 24, 230) : Theme.Panel);
        for (int i = 0; i < 3; i++)
        {
            var c = new Vector2(panel.X + 34 + i * 36, panel.Y + 26);
            Theme.DrawStar(c, 13, i < heat.Stars ? Theme.Gold : new Color(255, 255, 255, 40));
        }
        Theme.DrawText(heat.IsWanted ? "EFTERLYST" : "Ingen jagar dig", panel.X + 130, panel.Y + 18, 14,
            heat.IsWanted ? Theme.Red : Theme.TextDim);
        Theme.DrawBar(new Rectangle(panel.X + 16, panel.Y + 48, panel.Width - 32, 6), heat.Heat / 100f,
            heat.IsWanted ? Theme.Red : new Color(240, 170, 80, 255));

        // Minikarta
        float mw = world.Map.Width * MiniScale, mh = world.Map.Height * MiniScale;
        var mapRect = new Rectangle(right - mw - 10, panel.Y + panel.Height + 10, mw + 10, mh + 10);
        Theme.DrawPanel(mapRect, Theme.Panel, 0.06f);
        var origin = new Vector2(mapRect.X + 5, mapRect.Y + 5);
        Raylib.DrawTextureRec(minimap.Texture, new Rectangle(0, 0, mw, -mh), origin, Color.White);

        Vector2 M(Vector2 worldPos) => origin + worldPos / TileMap.TileSize * MiniScale;

        Raylib.DrawCircleV(M(world.PlayerStart), 4, Theme.Gold);
        foreach (var cam in world.Cameras)
            Raylib.DrawCircleV(M(cam.Position), 2, cam.IsJammed ? new Color(170, 120, 255, 255) : Theme.Red);
        foreach (var order in world.Orders.Active)
        {
            Raylib.DrawCircleV(M(order.Place.Position), 5, new Color(0, 0, 0, 120));
            Raylib.DrawCircleV(M(order.Place.Position), 3.5f, Theme.Gold);
        }
        foreach (var o in world.Officers)
            Raylib.DrawCircleV(M(o.Position), 3, (int)(world.Time * 4) % 2 == 0 ? Theme.Blue : Theme.Red);
        Raylib.DrawCircleV(M(world.Player.Position), 4, Color.White);
        Raylib.DrawCircleV(M(world.Player.Position), 2.5f, new Color(30, 30, 40, 255));
    }

    private void DrawInventory(GameWorld world)
    {
        var inv = world.State.Inventory;
        const int slot = 62, gap = 10;
        int count = ItemCatalog.All.Length;
        float totalW = count * slot + (count - 1) * gap;
        float x0 = screenW / 2f - totalW / 2, y = screenH - slot - 22;

        for (int i = 0; i < count; i++)
        {
            var type = ItemCatalog.All[i];
            var r = new Rectangle(x0 + i * (slot + gap), y, slot, slot);
            int n = inv.Count(type);
            Theme.DrawPanel(r, n > 0 ? Theme.PanelLight : Theme.WithAlpha(Theme.Panel, 0.6f), 0.25f);
            if (n > 0)
            {
                Paint.ItemIcon(type, new Vector2(r.X + slot / 2f, r.Y + slot / 2f - 4), 13);
                Theme.DrawTextRight(n.ToString(), r.X + slot - 8, r.Y + slot - 24, 18, Theme.Text);
            }
            else
            {
                Raylib.DrawCircleV(new Vector2(r.X + slot / 2f, r.Y + slot / 2f), 4, new Color(255, 255, 255, 25));
            }
        }

        string bag = $"Väska {inv.Total}/{inv.Capacity}";
        Theme.DrawTextCentered(bag, screenW / 2f, y - 22, 15, inv.FreeSpace == 0 ? Theme.Red : Theme.TextDim);
    }

    private void DrawPrompt(GameWorld world)
    {
        float y = screenH - 150;

        if (world.Player.IsHidden(world))
        {
            const string hidden = "GÖMD";
            var m = Theme.Measure(hidden, 16);
            Theme.DrawPill(screenW / 2f - m.X / 2 - 12, y - 40, hidden, 16, new Color(40, 90, 60, 220), Theme.Text);
        }

        var focus = world.Focused;
        string? prompt = focus?.GetPrompt(world);
        if (focus is null || prompt is null) return;

        bool usable = focus.CanUse(world);
        string key = usable ? (focus.HoldTime > 0 ? "Håll E" : "E") : "";
        var textSize = Theme.Measure(prompt, 20);
        float keyW = key.Length > 0 ? Theme.Measure(key, 18).X + 22 : 0;
        float w = textSize.X + keyW + 40;
        var r = new Rectangle(screenW / 2f - w / 2, y, w, 44);
        Theme.DrawPanel(r, Theme.Panel, 0.5f);

        float x = r.X + 20;
        if (key.Length > 0)
        {
            var keyRect = new Rectangle(x, r.Y + 9, keyW - 8, 26);
            Raylib.DrawRectangleRounded(keyRect, 0.35f, 8, Theme.Gold);
            Theme.DrawTextCentered(key, keyRect.X + keyRect.Width / 2, keyRect.Y + 4, 18, new Color(30, 24, 10, 255));
            x += keyW + 4;
        }
        Theme.DrawText(prompt, x, r.Y + 11, 20, usable ? Theme.Text : Theme.TextDim);

        if (world.HoldProgress > 0 && focus.HoldTime > 0)
            Theme.DrawBar(new Rectangle(r.X + 14, r.Y + r.Height - 6, r.Width - 28, 4), world.HoldProgress / focus.HoldTime, Theme.Gold);
    }

    private void DrawJammer(GameWorld world)
    {
        if (!world.State.Has(Upgrade.Jammer)) return;

        var c = new Vector2(52, screenH - 56);
        float ready = 1 - world.JamCooldown / GameWorld.JamRecharge;
        Raylib.DrawCircleV(c + new Vector2(2, 4), 32, new Color(0, 0, 0, 70));
        Raylib.DrawCircleV(c, 32, Theme.Panel);
        var purple = new Color(170, 110, 240, 255);
        Raylib.DrawRing(c, 26, 31, -90, -90 + 360 * ready, 36, ready >= 1 ? purple : Theme.WithAlpha(purple, 0.5f));
        Theme.DrawTextCentered("Q", c.X, c.Y - 12, 24, ready >= 1 ? Theme.Text : Theme.TextDim);
        Theme.DrawText(ready >= 1 ? "Störsändare redo" : $"Laddar {world.JamCooldown:0} s", c.X + 44, c.Y - 9, 16,
            ready >= 1 ? purple : Theme.TextDim);
    }

    private void DrawNotifications(GameWorld world)
    {
        float y = 18 + 64 + 10 + world.Map.Height * MiniScale + 10 + 16;
        foreach (var n in world.Notifications.AsEnumerable().Reverse())
        {
            float alpha = Math.Min(1, n.TimeLeft / 0.5f);
            var size = Theme.Measure(n.Text, 17);
            var r = new Rectangle(screenW - 18 - size.X - 30, y, size.X + 30, 34);
            Theme.DrawPanel(r, Theme.WithAlpha(Theme.Panel, 0.85f * alpha), 0.5f);
            Raylib.DrawCircleV(new Vector2(r.X + 12, r.Y + 17), 3.5f, Theme.WithAlpha(n.Color, alpha));
            Theme.DrawText(n.Text, r.X + 20, r.Y + 8, 17, Theme.WithAlpha(n.Color, alpha));
            y += 40;
        }
    }

    public void Dispose() => Raylib.UnloadRenderTexture(minimap);
}
