using System.Numerics;
using Gentleman2D.Core;
using Gentleman2D.Entities;
using Gentleman2D.UI;

namespace Gentleman2D.Systems;

/// <summary>En plats på kartan där man möter kunder.</summary>
public record MeetPoint(string Name, Vector2 Position);

/// <summary>En beställning från en kund, visas i telefonen.</summary>
public class Order
{
    public required string CustomerName { get; init; }
    public required ItemType Product { get; init; }
    public required int Quantity { get; init; }
    public required int Price { get; init; }
    public required MeetPoint Place { get; init; }
    public required float Deadline { get; init; } // i absoluta spelminuter
    public Customer? Npc { get; set; }

    public float MinutesLeft(GameClock clock) => Deadline - clock.Absolute;
}

/// <summary>
/// Kunder skickar beställningar till telefonen. Man levererar genom att gå till
/// mötesplatsen med rätt varor och prata med kunden.
/// </summary>
public class OrderSystem
{
    public const int MaxActive = 3;

    private static readonly string[] Names =
    {
        "Fru Lindqvist", "Greven", "Doktor Berg", "Madame Rose", "Herr Holm", "Baronessan",
        "Kapten Ek", "Fröken Sjö", "Mäklaren", "Professorn", "Gamle Svensson", "Lady Blom"
    };

    private float nextOrderIn = 20; // spelminuter

    public List<Order> Active { get; } = new();
    public List<MeetPoint> MeetPoints { get; } = new();

    /// <summary>Varor man kan tillverka just nu, bara sådana beställs.</summary>
    private static List<ItemType> Products(GameState state)
    {
        var list = new List<ItemType>();
        if (state.Has(Upgrade.GemCutter)) list.Add(ItemType.CutDiamond);
        if (state.Has(Upgrade.Smelter)) list.Add(ItemType.GoldBar);
        return list;
    }

    public static int BasePrice(ItemType type) => type == ItemType.CutDiamond ? 210 : 170;

    public void Update(GameWorld world)
    {
        // Gamla beställningar går ut
        foreach (var order in Active.Where(o => o.MinutesLeft(world.Clock) <= 0).ToList())
        {
            Remove(order);
            world.Notify($"{order.CustomerName} tröttnade och gick.", Theme.TextDim);
        }

        var products = Products(world.State);
        if (products.Count == 0) return;

        nextOrderIn -= world.Clock.DeltaMinutes;
        if (nextOrderIn <= 0 && Active.Count < MaxActive)
        {
            CreateOrder(world, products);
            nextOrderIn = world.Rng.Next(90, 180);
        }
    }

    /// <summary>Skapar en beställning direkt (används av autopiloten för tester).</summary>
    public void ForceOrder(GameWorld world)
    {
        var products = Products(world.State);
        if (products.Count > 0 && Active.Count < MaxActive) CreateOrder(world, products);
    }

    /// <summary>Gör att en beställning kommer snart, t.ex. direkt efter att man köpt en maskin.</summary>
    public void RequestSoon() => nextOrderIn = Math.Min(nextOrderIn, 10);

    private void CreateOrder(GameWorld world, List<ItemType> products)
    {
        var rng = world.Rng;
        var product = products[rng.Next(products.Count)];
        int qty = rng.Next(1, 4);
        int unit = (int)(BasePrice(product) * world.Market * (0.9 + rng.NextDouble() * 0.35));

        var usedPlaces = Active.Select(o => o.Place).ToHashSet();
        var free = MeetPoints.Where(p => !usedPlaces.Contains(p)).ToList();
        if (free.Count == 0) return;

        var usedNames = Active.Select(o => o.CustomerName).ToHashSet();
        var name = Names.Where(n => !usedNames.Contains(n)).OrderBy(_ => rng.Next()).First();

        var order = new Order
        {
            CustomerName = name,
            Product = product,
            Quantity = qty,
            Price = unit * qty,
            Place = free[rng.Next(free.Count)],
            Deadline = world.Clock.Absolute + rng.Next(8, 14) * 60
        };
        order.Npc = new Customer(order.Place.Position, order, rng);
        world.Spawn(order.Npc);
        Active.Add(order);

        world.Notify($"Nytt meddelande från {name}! (TAB)", Theme.Gold);
    }

    public void Complete(GameWorld world, Order order)
    {
        if (!world.State.Inventory.Remove(order.Product, order.Quantity)) return;
        world.State.Earn(order.Price);
        world.State.OrdersDelivered++;
        Remove(order);
        world.Notify($"{order.CustomerName} betalade ${order.Price}. Ett nöje att göra affärer.", Theme.Gold);
        nextOrderIn = Math.Min(nextOrderIn, 45);
    }

    private void Remove(Order order)
    {
        Active.Remove(order);
        if (order.Npc is not null) order.Npc.Removed = true;
    }

    public void Clear()
    {
        foreach (var o in Active) if (o.Npc is not null) o.Npc.Removed = true;
        Active.Clear();
        nextOrderIn = 20;
    }
}
