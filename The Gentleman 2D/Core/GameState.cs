using Raylib_cs;

namespace Gentleman2D.Core;

/// <summary>Saker man kan köpa på laptopen i gömstället.</summary>
public enum Upgrade
{
    GemCutter,
    Smelter,
    Safe,
    BigBag,
    Shoes,
    Jammer
}

public record ShopItem(Upgrade Id, string Name, string Description, int Price, Color Color);

public static class Shop
{
    public static readonly IReadOnlyList<ShopItem> Items = new List<ShopItem>
    {
        new(Upgrade.GemCutter, "Slipbänk",
            "Slipar rå ädelstenar till diamanter som kunderna betalar bra för.", 250, new Color(120, 200, 230, 255)),
        new(Upgrade.Smelter, "Smältugn",
            "Smälter guldskrot till guldtackor. Låser upp guldkunder.", 450, new Color(240, 170, 70, 255)),
        new(Upgrade.Safe, "Kassaskåp",
            "Pengar i kassaskåpet är säkra om polisen griper dig.", 300, new Color(150, 160, 175, 255)),
        new(Upgrade.BigBag, "Större väska",
            "Bär 16 föremål i stället för 8.", 220, new Color(170, 120, 80, 255)),
        new(Upgrade.Shoes, "Tysta skor",
            "Du springer 25 % snabbare. Lättare att skaka av sig polisen.", 350, new Color(110, 110, 125, 255)),
        new(Upgrade.Jammer, "Störsändare",
            "Tryck Q för att slå ut alla kameror i närheten i 20 sekunder.", 900, new Color(170, 110, 240, 255)),
    };

    public static ShopItem Get(Upgrade id) => Items.First(i => i.Id == id);
}

/// <summary>Spelarens framsteg: pengar, väska, köpta saker och statistik. Det är detta som sparas.</summary>
public class GameState
{
    public const int StartMoney = 300;
    public const int DailyRent = 40;

    public int Money { get; set; } = StartMoney;
    public int SafeMoney { get; set; }
    public Inventory Inventory { get; } = new();
    public HashSet<Upgrade> Owned { get; } = new();
    public int OrdersDelivered { get; set; }
    public int TotalEarned { get; set; }
    public int TimesArrested { get; set; }

    public bool Has(Upgrade upgrade) => Owned.Contains(upgrade);

    public void Earn(int amount)
    {
        Money += amount;
        TotalEarned += amount;
    }

    /// <summary>Försöker köpa något. Returnerar false om man inte har råd eller redan äger det.</summary>
    public bool TryBuy(ShopItem item)
    {
        if (Has(item.Id) || Money < item.Price) return false;
        Money -= item.Price;
        Owned.Add(item.Id);
        if (item.Id == Upgrade.BigBag) Inventory.Capacity = 16;
        return true;
    }
}
