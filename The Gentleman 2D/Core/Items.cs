using Raylib_cs;

namespace Gentleman2D.Core;

/// <summary>Allt man kan bära i väskan.</summary>
public enum ItemType
{
    RawGem,      // stulen, oslipad ädelsten
    GoldScrap,   // stulet guldskrot
    CutDiamond,  // tillverkad i slipbänken
    GoldBar      // tillverkad i smältugnen
}

/// <summary>Fast information om en sorts föremål.</summary>
public record ItemInfo(string Name, Color Color, int PawnPrice, bool IsRaw);

public static class ItemCatalog
{
    public static ItemInfo Get(ItemType type) => type switch
    {
        ItemType.RawGem => new("Rå ädelsten", new Color(110, 190, 210, 255), 45, true),
        ItemType.GoldScrap => new("Guldskrot", new Color(186, 146, 74, 255), 35, true),
        ItemType.CutDiamond => new("Slipad diamant", new Color(190, 245, 255, 255), 0, false),
        _ => new("Guldtacka", new Color(244, 204, 86, 255), 0, false)
    };

    public static readonly ItemType[] All = Enum.GetValues<ItemType>();
}

/// <summary>Spelarens väska. Har ett tak för hur mycket som får plats.</summary>
public class Inventory
{
    private readonly Dictionary<ItemType, int> counts = new();

    public int Capacity { get; set; } = 8;
    public int Total => counts.Values.Sum();
    public int FreeSpace => Math.Max(0, Capacity - Total);

    public int Count(ItemType type) => counts.GetValueOrDefault(type);

    /// <summary>Lägger till så många som får plats och returnerar hur många det blev.</summary>
    public int Add(ItemType type, int amount)
    {
        int added = Math.Min(amount, FreeSpace);
        if (added > 0) counts[type] = Count(type) + added;
        return added;
    }

    public bool Remove(ItemType type, int amount)
    {
        if (Count(type) < amount) return false;
        counts[type] = Count(type) - amount;
        return true;
    }

    public void Clear() => counts.Clear();

    public Dictionary<string, int> ToSave() => counts.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);

    public void LoadFrom(Dictionary<string, int>? data)
    {
        counts.Clear();
        if (data is null) return;
        foreach (var (key, value) in data)
            if (Enum.TryParse<ItemType>(key, out var type))
                counts[type] = value;
    }
}
