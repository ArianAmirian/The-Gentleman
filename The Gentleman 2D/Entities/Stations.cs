using System.Numerics;
using Raylib_cs;
using Gentleman2D.Core;
using Gentleman2D.UI;

namespace Gentleman2D.Entities;

/// <summary>
/// En maskin i gömstället som förvandlar stulna råvaror till något värdefullare.
/// Man lägger i upp till fem råvaror, väntar och hämtar sedan resultatet.
/// Abstrakt: GemCutter och Smelter bestämmer vad som tillverkas och hur det ser ut.
/// </summary>
public abstract class ProductionStation : Entity, IInteractable
{
    public const int Capacity = 5;

    private float progress; // spelminuter på den vara som tillverkas just nu

    protected ProductionStation(Vector2 position)
    {
        Position = position;
    }

    public abstract string Name { get; }
    public abstract Upgrade RequiredUpgrade { get; }
    public abstract ItemType Input { get; }
    public abstract ItemType Output { get; }
    public abstract float MinutesPerUnit { get; }

    public int Queued { get; set; }
    public int Done { get; set; }
    public float Progress01 => Queued > 0 ? progress / MinutesPerUnit : 0;

    public bool IsOwned(GameWorld world) => world.State.Has(RequiredUpgrade);

    public override void Update(GameWorld world, float dt)
    {
        if (!IsOwned(world) || Queued == 0)
        {
            progress = 0;
            return;
        }

        progress += world.Clock.DeltaMinutes;
        while (progress >= MinutesPerUnit && Queued > 0)
        {
            progress -= MinutesPerUnit;
            Queued--;
            Done++;
            if (Queued == 0)
                world.Notify($"{Name}: {Done} {ItemCatalog.Get(Output).Name.ToLower()} klara!", Theme.Green);
        }
    }

    public string? GetPrompt(GameWorld world)
    {
        var inv = world.State.Inventory;
        string input = ItemCatalog.Get(Input).Name.ToLower();
        string output = ItemCatalog.Get(Output).Name.ToLower();

        if (!IsOwned(world)) return $"Tom plats: köp {Name} på laptopen";
        if (Done > 0) return $"Hämta {Done} {output}";
        if (inv.Count(Input) > 0 && Queued < Capacity) return $"Lägg i {Math.Min(inv.Count(Input), Capacity - Queued)} {input}";
        if (Queued > 0) return $"{Name} arbetar... {Queued} kvar";
        return $"{Name}: behöver {input}";
    }

    public bool CanUse(GameWorld world)
    {
        if (!IsOwned(world)) return false;
        return Done > 0 || (world.State.Inventory.Count(Input) > 0 && Queued < Capacity);
    }

    public void Interact(GameWorld world)
    {
        var inv = world.State.Inventory;
        if (Done > 0)
        {
            int taken = inv.Add(Output, Done);
            Done -= taken;
            if (taken == 0) world.Notify("Väskan är full!", Theme.Red);
            else world.Notify($"+{taken} {ItemCatalog.Get(Output).Name}", ItemCatalog.Get(Output).Color);
            return;
        }

        int amount = Math.Min(inv.Count(Input), Capacity - Queued);
        if (amount <= 0) return;
        inv.Remove(Input, amount);
        Queued += amount;
        world.Notify($"{Name} startad ({amount} st)", Theme.Text);
    }

    public override void Draw(GameWorld world)
    {
        var r = new Rectangle(Position.X - 22, Position.Y - 22, 44, 44);
        if (!IsOwned(world))
        {
            // Tom plats: streckad ram med ett plustecken
            Raylib.DrawRectangleRounded(r, 0.2f, 8, new Color(255, 255, 255, 18));
            Raylib.DrawRectangleRoundedLinesEx(r, 0.2f, 8, 2, new Color(255, 255, 255, 70));
            Raylib.DrawLineEx(Position - new Vector2(7, 0), Position + new Vector2(7, 0), 3, new Color(255, 255, 255, 90));
            Raylib.DrawLineEx(Position - new Vector2(0, 7), Position + new Vector2(0, 7), 3, new Color(255, 255, 255, 90));
            return;
        }

        Rendering.Paint.Shadow(Position + new Vector2(0, 14), 24, 8);
        DrawMachine(world, r);

        if (Queued > 0)
        {
            Raylib.DrawRing(Position + new Vector2(0, -34), 7, 10, -90, -90 + 360 * Progress01, 24, Theme.Gold);
            Raylib.DrawRing(Position + new Vector2(0, -34), 7, 10, -90 + 360 * Progress01, 270, 24, new Color(0, 0, 0, 90));
        }
        if (Done > 0)
        {
            Raylib.DrawCircleV(Position + new Vector2(16, -18), 9, Theme.Green);
            Theme.DrawTextCentered(Done.ToString(), Position.X + 16, Position.Y - 27, 15, new Color(10, 40, 20, 255));
        }
    }

    protected abstract void DrawMachine(GameWorld world, Rectangle r);
}

public class GemCutter : ProductionStation
{
    public GemCutter(Vector2 position) : base(position) { }

    public override string Name => "Slipbänk";
    public override Upgrade RequiredUpgrade => Upgrade.GemCutter;
    public override ItemType Input => ItemType.RawGem;
    public override ItemType Output => ItemType.CutDiamond;
    public override float MinutesPerUnit => 30;

    protected override void DrawMachine(GameWorld world, Rectangle r)
    {
        Raylib.DrawRectangleRounded(r, 0.2f, 8, new Color(96, 106, 122, 255));
        Raylib.DrawRectangleRounded(new Rectangle(r.X + 4, r.Y + 4, r.Width - 8, r.Height - 14), 0.2f, 8, new Color(130, 142, 160, 255));
        // Slipskivan snurrar när den arbetar
        float spin = Queued > 0 ? world.Time * 600 : 0;
        Raylib.DrawCircleV(Position + new Vector2(0, -3), 12, new Color(60, 64, 74, 255));
        Raylib.DrawCircleSector(Position + new Vector2(0, -3), 11, spin, spin + 90, 8, new Color(190, 230, 245, 255));
        Raylib.DrawCircleSector(Position + new Vector2(0, -3), 11, spin + 180, spin + 270, 8, new Color(190, 230, 245, 255));
        Raylib.DrawCircleV(Position + new Vector2(0, -3), 3, new Color(30, 30, 36, 255));
    }
}

public class Smelter : ProductionStation
{
    public Smelter(Vector2 position) : base(position) { }

    public override string Name => "Smältugn";
    public override Upgrade RequiredUpgrade => Upgrade.Smelter;
    public override ItemType Input => ItemType.GoldScrap;
    public override ItemType Output => ItemType.GoldBar;
    public override float MinutesPerUnit => 40;

    protected override void DrawMachine(GameWorld world, Rectangle r)
    {
        Raylib.DrawRectangleRounded(r, 0.25f, 8, new Color(70, 62, 60, 255));
        Raylib.DrawRectangleRounded(new Rectangle(r.X + 5, r.Y + 5, r.Width - 10, r.Height - 10), 0.3f, 8, new Color(46, 40, 40, 255));
        float glow = Queued > 0 ? 0.6f + 0.4f * MathF.Sin(world.Time * 6) : 0.15f;
        Raylib.DrawCircleV(Position, 11, Theme.WithAlpha(new Color(255, 140, 40, 255), glow));
        Raylib.DrawCircleV(Position, 6, Theme.WithAlpha(new Color(255, 220, 120, 255), glow));
    }
}
