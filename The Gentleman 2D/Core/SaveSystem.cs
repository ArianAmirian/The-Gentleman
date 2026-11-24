using System.Text.Json;
using Gentleman2D.Entities;

namespace Gentleman2D.Core;

/// <summary>Det som skrivs till sparfilen.</summary>
public class SaveData
{
    public int Money { get; set; }
    public int SafeMoney { get; set; }
    public int Day { get; set; }
    public float Minutes { get; set; }
    public Dictionary<string, int> Inventory { get; set; } = new();
    public List<string> Owned { get; set; } = new();
    public int OrdersDelivered { get; set; }
    public int TotalEarned { get; set; }
    public int TimesArrested { get; set; }
    public List<int[]> Stations { get; set; } = new(); // [i kö, klara] per maskin
}

/// <summary>Sparar och laddar spelet som JSON.</summary>
public static class SaveSystem
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string FilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TheGentleman2D", "save.json");

    public static bool Exists => File.Exists(FilePath);

    public static void Save(GameWorld world)
    {
        var s = world.State;
        var data = new SaveData
        {
            Money = s.Money,
            SafeMoney = s.SafeMoney,
            Day = world.Clock.Day,
            Minutes = world.Clock.Minutes,
            Inventory = s.Inventory.ToSave(),
            Owned = s.Owned.Select(o => o.ToString()).ToList(),
            OrdersDelivered = s.OrdersDelivered,
            TotalEarned = s.TotalEarned,
            TimesArrested = s.TimesArrested,
            Stations = world.Stations.Select(st => new[] { st.Queued, st.Done }).ToList()
        };

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(data, Options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            world.Notify("Kunde inte spara spelet.", UI.Theme.Red);
        }
    }

    public static bool TryLoad(GameWorld world)
    {
        try
        {
            var data = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(FilePath));
            if (data is null) return false;

            var s = world.State;
            s.Money = data.Money;
            s.SafeMoney = data.SafeMoney;
            s.OrdersDelivered = data.OrdersDelivered;
            s.TotalEarned = data.TotalEarned;
            s.TimesArrested = data.TimesArrested;
            s.Owned.Clear();
            foreach (var name in data.Owned)
                if (Enum.TryParse<Upgrade>(name, out var u)) s.Owned.Add(u);
            s.Inventory.Capacity = s.Has(Upgrade.BigBag) ? 16 : 8;
            s.Inventory.LoadFrom(data.Inventory);

            world.Clock.SetState(Math.Max(1, data.Day), data.Minutes);
            for (int i = 0; i < Math.Min(data.Stations.Count, world.Stations.Count); i++)
            {
                world.Stations[i].Queued = data.Stations[i][0];
                world.Stations[i].Done = data.Stations[i][1];
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException or IndexOutOfRangeException)
        {
            return false;
        }
    }
}
