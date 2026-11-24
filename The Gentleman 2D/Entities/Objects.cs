using System.Numerics;
using Raylib_cs;
using Gentleman2D.Core;
using Gentleman2D.Rendering;
using Gentleman2D.UI;

namespace Gentleman2D.Entities;

/// <summary>En monter med byte i en butik eller bank. Fylls på varje morgon.</summary>
public class LootSpot : Entity, IInteractable
{
    public LootSpot(Vector2 position, ItemType type, int amount)
    {
        Position = position;
        Type = type;
        Amount = amount;
    }

    public ItemType Type { get; }
    public int Amount { get; }
    public bool Available { get; set; } = true;
    public float HoldTime => 1.4f;

    public string? GetPrompt(GameWorld world)
    {
        if (!Available) return "Tom monter. Fylls på kl 06:00";
        if (world.State.Inventory.FreeSpace == 0) return "Väskan är full!";
        return $"Stjäl {Amount} {ItemCatalog.Get(Type).Name.ToLower()}";
    }

    public bool CanUse(GameWorld world) => Available && world.State.Inventory.FreeSpace > 0;

    public void Interact(GameWorld world)
    {
        int added = world.State.Inventory.Add(Type, Amount);
        Available = false;
        world.Notify($"+{added} {ItemCatalog.Get(Type).Name}", ItemCatalog.Get(Type).Color);

        // Stjäl man mitt framför en kamera går larmet.
        if (world.Cameras.Any(c => c.SeesPlayer))
        {
            world.Heat.Spike(18);
            world.Notify("Larmet går!", Theme.Red);
        }
    }

    public override void Draw(GameWorld world)
    {
        var r = new Rectangle(Position.X - 20, Position.Y - 16, 40, 32);
        Paint.Shadow(Position + new Vector2(0, 10), 22, 7);
        Raylib.DrawRectangleRounded(r, 0.2f, 6, new Color(70, 52, 40, 255));
        var glass = new Rectangle(r.X + 3, r.Y + 3, r.Width - 6, r.Height - 8);
        Raylib.DrawRectangleRounded(glass, 0.2f, 6, new Color(200, 235, 245, 120));

        if (Available)
        {
            Paint.ItemIcon(Type, Position + new Vector2(-8, -2), 6);
            Paint.ItemIcon(Type, Position + new Vector2(8, 0), 6);
            float sparkle = MathF.Sin(world.Time * 3 + Position.X) * 0.5f + 0.5f;
            Raylib.DrawCircleV(Position + new Vector2(10, -8), 2 * sparkle, new Color(255, 255, 255, 220));
        }
        Raylib.DrawLineEx(new Vector2(glass.X + 5, glass.Y + 4), new Vector2(glass.X + 14, glass.Y + 4), 2, new Color(255, 255, 255, 120));
    }
}

/// <summary>Sängen i gömstället: sov till morgonen och spara spelet.</summary>
public class Bed : Entity, IInteractable
{
    public Bed(Vector2 position) => Position = position;

    public string? GetPrompt(GameWorld world) =>
        world.Heat.IsWanted ? "Du kan inte sova när polisen letar efter dig" : "Sov till morgonen (sparar spelet)";

    public bool CanUse(GameWorld world) => !world.Heat.IsWanted;

    public void Interact(GameWorld world) => world.Sleep();

    public override void Draw(GameWorld world)
    {
        var r = new Rectangle(Position.X - 22, Position.Y - 34, 44, 68);
        Paint.Shadow(Position + new Vector2(0, 26), 24, 8);
        Raylib.DrawRectangleRounded(r, 0.2f, 8, new Color(90, 60, 44, 255));
        Raylib.DrawRectangleRounded(new Rectangle(r.X + 3, r.Y + 3, r.Width - 6, r.Height - 6), 0.2f, 8, new Color(150, 40, 55, 255));
        Raylib.DrawRectangleRounded(new Rectangle(r.X + 6, r.Y + 6, r.Width - 12, 16), 0.5f, 8, new Color(235, 232, 225, 255));
        Raylib.DrawLineEx(new Vector2(r.X + 3, r.Y + 30), new Vector2(r.X + r.Width - 3, r.Y + 30), 2, new Color(120, 30, 45, 255));
    }

    public override float SortY => Position.Y + 30;
}

/// <summary>Laptopen: här köper man maskiner och prylar.</summary>
public class Laptop : Entity, IInteractable
{
    public Laptop(Vector2 position) => Position = position;

    public string? GetPrompt(GameWorld world) => "Öppna laptopen (butik)";
    public bool CanUse(GameWorld world) => true;
    public void Interact(GameWorld world) => world.OpenShop();

    public override void Draw(GameWorld world)
    {
        // Skrivbord
        var desk = new Rectangle(Position.X - 30, Position.Y - 18, 60, 36);
        Paint.Shadow(Position + new Vector2(0, 12), 30, 8);
        Raylib.DrawRectangleRounded(desk, 0.15f, 6, new Color(110, 78, 52, 255));
        Raylib.DrawRectangleRounded(new Rectangle(desk.X + 2, desk.Y + 2, desk.Width - 4, desk.Height - 6), 0.15f, 6, new Color(140, 100, 66, 255));
        // Laptop med lysande skärm
        Raylib.DrawRectangleRounded(new Rectangle(Position.X - 14, Position.Y - 12, 28, 18), 0.15f, 4, new Color(40, 42, 50, 255));
        float pulse = 0.75f + 0.25f * MathF.Sin(world.Time * 2);
        Raylib.DrawRectangleRounded(new Rectangle(Position.X - 12, Position.Y - 10, 24, 13), 0.1f, 4, Theme.WithAlpha(new Color(110, 190, 255, 255), pulse));
        Raylib.DrawRectangleRounded(new Rectangle(Position.X - 15, Position.Y + 5, 30, 6), 0.4f, 4, new Color(70, 72, 82, 255));
    }
}

/// <summary>Kassaskåpet: pengar här inne försvinner inte när man blir gripen.</summary>
public class Safe : Entity, IInteractable
{
    public Safe(Vector2 position) => Position = position;

    public string? GetPrompt(GameWorld world)
    {
        var s = world.State;
        if (!s.Has(Upgrade.Safe)) return "Tom plats: köp Kassaskåp på laptopen";
        if (s.Money > 0) return $"Lägg in ${s.Money} i kassaskåpet";
        if (s.SafeMoney > 0) return $"Ta ut ${s.SafeMoney}";
        return "Kassaskåpet är tomt";
    }

    public bool CanUse(GameWorld world) =>
        world.State.Has(Upgrade.Safe) && (world.State.Money > 0 || world.State.SafeMoney > 0);

    public void Interact(GameWorld world)
    {
        var s = world.State;
        if (s.Money > 0)
        {
            world.Notify($"${s.Money} inlagt i kassaskåpet", Theme.Gold);
            s.SafeMoney += s.Money;
            s.Money = 0;
        }
        else
        {
            world.Notify($"${s.SafeMoney} uttaget", Theme.Gold);
            s.Money += s.SafeMoney;
            s.SafeMoney = 0;
        }
    }

    public override void Draw(GameWorld world)
    {
        var r = new Rectangle(Position.X - 20, Position.Y - 20, 40, 40);
        if (!world.State.Has(Upgrade.Safe))
        {
            Raylib.DrawRectangleRounded(r, 0.2f, 8, new Color(255, 255, 255, 18));
            Raylib.DrawRectangleRoundedLinesEx(r, 0.2f, 8, 2, new Color(255, 255, 255, 70));
            Raylib.DrawLineEx(Position - new Vector2(7, 0), Position + new Vector2(7, 0), 3, new Color(255, 255, 255, 90));
            Raylib.DrawLineEx(Position - new Vector2(0, 7), Position + new Vector2(0, 7), 3, new Color(255, 255, 255, 90));
            return;
        }
        Paint.Shadow(Position + new Vector2(0, 14), 22, 7);
        Raylib.DrawRectangleRounded(r, 0.15f, 8, new Color(70, 76, 88, 255));
        Raylib.DrawRectangleRounded(new Rectangle(r.X + 4, r.Y + 4, r.Width - 8, r.Height - 8), 0.15f, 8, new Color(98, 106, 120, 255));
        Raylib.DrawCircleV(Position, 7, new Color(50, 54, 62, 255));
        Raylib.DrawCircleV(Position, 3, Theme.Gold);
    }
}

/// <summary>Pantbankens disk: köper rå varor billigt. Öppet 08–20.</summary>
public class PawnCounter : Entity, IInteractable
{
    public PawnCounter(Vector2 position) => Position = position;

    public static bool IsOpen(GameWorld world) => world.Clock.Hour >= 8 && world.Clock.Hour < 20;

    private static int RawCount(GameWorld world) =>
        ItemCatalog.All.Where(t => ItemCatalog.Get(t).IsRaw).Sum(t => world.State.Inventory.Count(t));

    private static int RawValue(GameWorld world) =>
        ItemCatalog.All.Where(t => ItemCatalog.Get(t).IsRaw)
            .Sum(t => world.State.Inventory.Count(t) * (int)(ItemCatalog.Get(t).PawnPrice * world.Market));

    public string? GetPrompt(GameWorld world)
    {
        if (!IsOpen(world)) return "Pantbanken är stängd (öppet 08–20)";
        int n = RawCount(world);
        return n > 0 ? $"Sälj {n} råvaror för ${RawValue(world)}" : "Pantbanken köper rå ädelstenar och guldskrot";
    }

    public bool CanUse(GameWorld world) => IsOpen(world) && RawCount(world) > 0;

    public void Interact(GameWorld world)
    {
        int value = RawValue(world);
        foreach (var t in ItemCatalog.All.Where(t => ItemCatalog.Get(t).IsRaw))
            world.State.Inventory.Remove(t, world.State.Inventory.Count(t));
        world.State.Earn(value);
        world.Notify($"Sålt till pantbanken: +${value}", Theme.Gold);
    }

    public override void Draw(GameWorld world)
    {
        // Disken
        var r = new Rectangle(Position.X - 60, Position.Y - 12, 120, 24);
        Paint.Shadow(Position + new Vector2(0, 10), 60, 8);
        Raylib.DrawRectangleRounded(r, 0.3f, 8, new Color(92, 64, 46, 255));
        Raylib.DrawRectangleRounded(new Rectangle(r.X + 3, r.Y + 3, r.Width - 6, r.Height - 8), 0.3f, 8, new Color(130, 92, 62, 255));
        Raylib.DrawRectangleRounded(new Rectangle(Position.X + 20, Position.Y - 8, 22, 12), 0.3f, 4, new Color(60, 64, 74, 255));

        // Expediten står bakom disken när det är öppet
        if (IsOpen(world))
            Paint.Person(Position + new Vector2(0, -34), new Vector2(0, 1), new Color(110, 80, 60, 255),
                new Color(210, 170, 135, 255), HeadWear.Hair, new Color(80, 80, 86, 255), 0);
    }

    public override float SortY => Position.Y + 10;
}

/// <summary>Soptunna/container: göm dig här inne så hittar polisen dig inte.</summary>
public class Dumpster : Entity, IInteractable
{
    public Dumpster(Vector2 position) => Position = position;

    public string? GetPrompt(GameWorld world)
    {
        if (world.Player.HidingIn == this) return "Kliv ut ur containern";
        if (world.Officers.Any(o => o.SeesPlayer)) return "Polisen ser dig! Bryt sikten först";
        return "Göm dig i containern";
    }

    public bool CanUse(GameWorld world) =>
        world.Player.HidingIn == this || !world.Officers.Any(o => o.SeesPlayer);

    public void Interact(GameWorld world)
    {
        var player = world.Player;
        if (player.HidingIn == this)
        {
            player.HidingIn = null;
        }
        else
        {
            player.HidingIn = this;
            world.Notify("Du gömmer dig...", Theme.TextDim);
        }
    }

    public override void Draw(GameWorld world)
    {
        var r = new Rectangle(Position.X - 22, Position.Y - 16, 44, 32);
        Paint.Shadow(Position + new Vector2(0, 12), 24, 7);
        Raylib.DrawRectangleRounded(r, 0.2f, 6, new Color(46, 96, 70, 255));
        Raylib.DrawRectangleRounded(new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, 14), 0.3f, 6, new Color(62, 124, 90, 255));
        Raylib.DrawLineEx(new Vector2(r.X + 4, r.Y + 17), new Vector2(r.X + r.Width - 4, r.Y + 17), 2, new Color(30, 60, 44, 255));

        if (world.Player.HidingIn == this)
        {
            // Ett par ögon kikar ut under locket
            Raylib.DrawCircleV(Position + new Vector2(-4, -2), 2, Color.White);
            Raylib.DrawCircleV(Position + new Vector2(4, -2), 2, Color.White);
        }
    }
}

/// <summary>Kund som väntar på en leverans. Skapas av OrderSystem.</summary>
public class Customer : Entity, IInteractable
{
    private readonly Color coat;
    private readonly Color hair;

    public Customer(Vector2 position, Systems.Order order, Random rng)
    {
        Position = position;
        Order = order;
        Color[] coats = { new(120, 60, 80, 255), new(60, 100, 120, 255), new(140, 110, 60, 255), new(80, 80, 90, 255), new(70, 110, 70, 255) };
        Color[] hairs = { new(50, 36, 30, 255), new(200, 170, 110, 255), new(30, 30, 30, 255), new(150, 70, 40, 255) };
        coat = coats[rng.Next(coats.Length)];
        hair = hairs[rng.Next(hairs.Length)];
    }

    public Systems.Order Order { get; }

    public string? GetPrompt(GameWorld world)
    {
        string item = ItemCatalog.Get(Order.Product).Name.ToLower();
        return world.State.Inventory.Count(Order.Product) >= Order.Quantity
            ? $"Leverera {Order.Quantity} {item} till {Order.CustomerName} (${Order.Price})"
            : $"{Order.CustomerName} väntar på {Order.Quantity} {item}";
    }

    public bool CanUse(GameWorld world) => world.State.Inventory.Count(Order.Product) >= Order.Quantity;

    public void Interact(GameWorld world) => world.Orders.Complete(world, Order);

    public override void Draw(GameWorld world)
    {
        var facing = Vector2.Normalize(world.Player.Position - Position + new Vector2(0.01f, 0.01f));
        Paint.Person(Position, facing, coat, new Color(225, 185, 150, 255), HeadWear.Hair, hair, 0);

        // Svävande markering ovanför kunden
        float bob = MathF.Sin(world.Time * 3) * 4;
        var m = Position + new Vector2(0, -44 + bob);
        Raylib.DrawCircleV(m, 13, new Color(0, 0, 0, 90));
        Raylib.DrawCircleV(m, 11, Theme.Gold);
        Paint.ItemIcon(Order.Product, m, 6);
    }
}

public enum PropKind { Lamp, Crate, Desk, Bench, Plant, Shelf, Car, Fountain }

/// <summary>Dekoration: lyktstolpar, lådor, skrivbord, bänkar, krukväxter, hyllor och bilar.</summary>
public class Prop : Entity
{
    private readonly Color tint;

    public Prop(Vector2 position, PropKind kind, Color? tint = null)
    {
        Position = position;
        Kind = kind;
        this.tint = tint ?? new Color(180, 60, 60, 255);
    }

    public PropKind Kind { get; }

    // Fontänen ska ligga under allt annat.
    public override float SortY => Kind == PropKind.Fountain ? Position.Y - 1000 : Position.Y;

    public override void Draw(GameWorld world)
    {
        var p = Position;
        switch (Kind)
        {
            case PropKind.Lamp:
                Paint.Shadow(p + new Vector2(0, 4), 8, 4);
                Raylib.DrawCircleV(p, 6, new Color(40, 42, 48, 255));
                Raylib.DrawCircleV(p, 4, world.Clock.Darkness > 0.3f ? new Color(255, 226, 160, 255) : new Color(110, 112, 120, 255));
                break;
            case PropKind.Crate:
                Paint.Shadow(p + new Vector2(0, 12), 20, 6);
                Raylib.DrawRectangleRounded(new Rectangle(p.X - 18, p.Y - 18, 36, 36), 0.1f, 4, new Color(150, 110, 64, 255));
                Raylib.DrawRectangleLinesEx(new Rectangle(p.X - 14, p.Y - 14, 28, 28), 2, new Color(110, 78, 44, 255));
                Raylib.DrawLineEx(p - new Vector2(14, 14), p + new Vector2(14, 14), 2, new Color(110, 78, 44, 255));
                break;
            case PropKind.Desk:
                Paint.Shadow(p + new Vector2(0, 12), 30, 7);
                Raylib.DrawRectangleRounded(new Rectangle(p.X - 30, p.Y - 16, 60, 32), 0.15f, 6, new Color(100, 104, 116, 255));
                Raylib.DrawRectangleRounded(new Rectangle(p.X - 10, p.Y - 10, 20, 14), 0.2f, 4, new Color(220, 224, 230, 255));
                break;
            case PropKind.Bench:
                Paint.Shadow(p + new Vector2(0, 8), 30, 6);
                Raylib.DrawRectangleRounded(new Rectangle(p.X - 30, p.Y - 9, 60, 18), 0.3f, 6, new Color(120, 82, 52, 255));
                for (int i = -1; i <= 1; i++)
                    Raylib.DrawLineEx(new Vector2(p.X - 27, p.Y + i * 5), new Vector2(p.X + 27, p.Y + i * 5), 1.5f, new Color(90, 60, 38, 255));
                break;
            case PropKind.Plant:
                Paint.Shadow(p + new Vector2(0, 8), 12, 5);
                Raylib.DrawCircleV(p, 11, new Color(150, 90, 60, 255));
                Raylib.DrawCircleV(p + new Vector2(-4, -4), 9, new Color(70, 140, 70, 255));
                Raylib.DrawCircleV(p + new Vector2(5, -2), 8, new Color(90, 160, 80, 255));
                break;
            case PropKind.Shelf:
                Paint.Shadow(p + new Vector2(0, 10), 40, 6);
                Raylib.DrawRectangleRounded(new Rectangle(p.X - 44, p.Y - 12, 88, 24), 0.15f, 6, new Color(84, 62, 48, 255));
                for (int i = 0; i < 6; i++)
                    Raylib.DrawRectangleRounded(new Rectangle(p.X - 40 + i * 14, p.Y - 8, 10, 14), 0.2f, 4,
                        i % 2 == 0 ? new Color(170, 140, 90, 255) : new Color(120, 140, 160, 255));
                break;
            case PropKind.Car:
                Paint.Shadow(p + new Vector2(0, 16), 50, 14);
                Raylib.DrawRectangleRounded(new Rectangle(p.X - 46, p.Y - 22, 92, 44), 0.45f, 10, Paint.Darken(tint, 0.75f));
                Raylib.DrawRectangleRounded(new Rectangle(p.X - 44, p.Y - 20, 88, 38), 0.45f, 10, tint);
                Raylib.DrawRectangleRounded(new Rectangle(p.X - 18, p.Y - 16, 40, 30), 0.3f, 8, new Color(40, 50, 64, 255));
                Raylib.DrawRectangleRounded(new Rectangle(p.X - 12, p.Y - 12, 28, 22), 0.3f, 8, Paint.Lighten(tint, 1.1f));
                break;
            case PropKind.Fountain:
                Raylib.DrawRing(p, 66, 76, 0, 360, 48, new Color(176, 170, 160, 255));
                Raylib.DrawRingLines(p, 66, 76, 0, 360, 48, new Color(150, 144, 134, 255));
                Raylib.DrawCircleV(p, 16, new Color(176, 170, 160, 255));
                Raylib.DrawCircleV(p, 9 + MathF.Sin(world.Time * 5) * 2, new Color(210, 240, 252, 220));
                for (int i = 0; i < 6; i++)
                {
                    float a = world.Time * 0.8f + i * MathF.Tau / 6;
                    Raylib.DrawCircleV(p + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 30, 2.5f, new Color(210, 240, 252, 160));
                }
                break;
        }
    }
}
