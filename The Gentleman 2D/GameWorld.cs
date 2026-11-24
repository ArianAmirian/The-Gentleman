using System.Numerics;
using Raylib_cs;
using Gentleman2D.Core;
using Gentleman2D.Entities;
using Gentleman2D.Systems;
using Gentleman2D.UI;
using Gentleman2D.World;

namespace Gentleman2D;

/// <summary>En ljuskälla: gatlyktor, lampor inomhus, polisens blåljus.</summary>
public readonly record struct Light(Vector2 Position, float Radius, Color Color, bool NightOnly = true);

public record Notification(string Text, Color Color)
{
    public float TimeLeft { get; set; } = 4.5f;
}

/// <summary>Vad som hände när man blev gripen, visas på "Gripen"-skärmen.</summary>
public record ArrestReport(int LostMoney, int LostItems);

/// <summary>
/// Hela spelvärlden: kartan, alla entiteter och systemen (klocka, efterlysning, beställningar).
/// GameApp sköter fönster och menyer, GameWorld sköter själva spelet.
/// </summary>
public class GameWorld
{
    public const float InteractRange = 58;
    public const float JamRecharge = 40;
    public const float JamRadiusTiles = 6;

    private readonly List<Entity> entities = new();
    private readonly List<Entity> spawnQueue = new();
    private bool holdLatch;

    public GameWorld()
    {
        Clock.Morning += OnMorning;
        WorldBuilder.Build(this);
        Player = new Player { Position = PlayerStart };
        entities.Add(Player);
        Market = 1f;
    }

    // ---------- Innehåll ----------
    public TileMap Map { get; set; } = new(1, 1);
    public GameClock Clock { get; } = new();
    public GameState State { get; } = new();
    public HeatSystem Heat { get; } = new();
    public OrderSystem Orders { get; } = new();
    public Player Player { get; }
    public Random Rng { get; } = new();

    public IReadOnlyList<Entity> Entities => entities;
    public List<SecurityCamera> Cameras { get; } = new();
    public List<LootSpot> LootSpots { get; } = new();
    public List<ProductionStation> Stations { get; } = new();
    public List<Light> Lights { get; } = new();
    public IEnumerable<PoliceOfficer> Officers => entities.OfType<PoliceOfficer>();

    public Vector2 PlayerStart { get; set; }
    public Vector2 PoliceSpawn { get; set; }

    // ---------- Tillstånd ----------
    public float Time { get; private set; }
    public InputState Input { get; set; } = new();

    /// <summary>Sant när telefonen eller butiken är öppen: spelaren står still.</summary>
    public bool InputLocked { get; set; }

    /// <summary>Dagens marknadspris (0,85–1,25). Ändras varje morgon.</summary>
    public float Market { get; private set; }

    public List<Notification> Notifications { get; } = new();
    public float JamCooldown { get; private set; }
    public IInteractable? Focused { get; private set; }
    public float HoldProgress { get; private set; }

    public bool ShopRequested { get; set; }
    public ArrestReport? PendingArrest { get; set; }
    public float SleepFade { get; private set; }
    public event Action? Saved;

    // ---------- Uppdatering ----------

    public void Update(float dt)
    {
        Time += dt;
        Clock.Update(dt);

        foreach (var e in entities.ToList())
            e.Update(this, dt);
        if (PendingArrest is not null) return;

        Heat.Update(this, dt);
        Orders.Update(this);

        entities.AddRange(spawnQueue);
        spawnQueue.Clear();
        entities.RemoveAll(e => e.Removed);

        UpdateInteraction(dt);
        UpdateJammer(dt);

        foreach (var n in Notifications) n.TimeLeft -= dt;
        Notifications.RemoveAll(n => n.TimeLeft <= 0);
        SleepFade = Math.Max(0, SleepFade - dt);
    }

    private void UpdateInteraction(float dt)
    {
        Focused = null;
        if (InputLocked) { HoldProgress = 0; return; }

        // Gömd i en container kan man bara kliva ut.
        if (Player.HidingIn is not null)
            Focused = Player.HidingIn;
        else
            Focused = entities.OfType<IInteractable>()
                .Where(i => Vector2.Distance(i.Position, Player.Position) < InteractRange && i.GetPrompt(this) is not null)
                .OrderBy(i => Vector2.Distance(i.Position, Player.Position))
                .FirstOrDefault();

        if (!Input.InteractHeld) holdLatch = false;

        if (Focused is null || !Focused.CanUse(this))
        {
            HoldProgress = 0;
            return;
        }

        if (Focused.HoldTime > 0)
        {
            if (Input.InteractHeld && !holdLatch)
            {
                HoldProgress += dt;
                if (HoldProgress >= Focused.HoldTime)
                {
                    Focused.Interact(this);
                    HoldProgress = 0;
                    holdLatch = true; // måste släppa E innan nästa
                }
            }
            else HoldProgress = 0;
        }
        else if (Input.Interact)
        {
            Focused.Interact(this);
        }
    }

    private void UpdateJammer(float dt)
    {
        JamCooldown = Math.Max(0, JamCooldown - dt);
        if (!Input.Jam || InputLocked) return;

        if (!State.Has(Upgrade.Jammer))
        {
            Notify("Du har ingen störsändare. Köp en på laptopen.", Theme.TextDim);
            return;
        }
        if (JamCooldown > 0)
        {
            Notify($"Störsändaren laddar... {JamCooldown:0} s", Theme.TextDim);
            return;
        }

        var near = Cameras.Where(c => Vector2.Distance(c.Position, Player.Position) < JamRadiusTiles * TileMap.TileSize).ToList();
        foreach (var c in near) c.Jam();
        JamCooldown = JamRecharge;
        Notify(near.Count > 0 ? $"Störsändare: {near.Count} kameror utslagna i 20 s" : "Störsändare: inga kameror i närheten", new Color(190, 140, 255, 255));
    }

    // ---------- Händelser ----------

    public void Spawn(Entity entity) => spawnQueue.Add(entity);

    public void AddStatic(Entity entity) => entities.Add(entity);

    public void Notify(string text, Color color)
    {
        Notifications.Add(new Notification(text, color));
        if (Notifications.Count > 5) Notifications.RemoveAt(0);
    }

    public void OpenShop() => ShopRequested = true;

    /// <summary>Står man i ljuset (dagsljus eller nära en lampa)?</summary>
    public bool IsLit(Vector2 p)
    {
        if (Clock.Darkness < 0.5f) return true;
        return Lights.Any(l => Vector2.Distance(l.Position, p) < l.Radius * 0.55f);
    }

    private void OnMorning()
    {
        foreach (var loot in LootSpots) loot.Available = true;
        Market = 0.85f + (float)Rng.NextDouble() * 0.4f;

        // Hyran dras från fickan först, sedan från kassaskåpet.
        int rent = GameState.DailyRent;
        int fromCash = Math.Min(rent, State.Money);
        State.Money -= fromCash;
        State.SafeMoney -= Math.Min(rent - fromCash, State.SafeMoney);

        string trend = Market > 1.1f ? "Priserna är höga idag!" : Market < 0.95f ? "Priserna är låga idag." : "Normala priser idag.";
        Notify($"Dag {Clock.Day}. Hyra -${rent}. {trend}", Theme.Gold);
    }

    public void Sleep()
    {
        Clock.SkipToNextDay(7);
        SleepFade = 1.5f;
        Notify("God morgon, Gentleman.", Theme.Text);
        Saved?.Invoke();
    }

    /// <summary>Gripen: man förlorar allt man bär på (men inte det i kassaskåpet) och vaknar hemma.</summary>
    public void Arrest()
    {
        if (PendingArrest is not null) return;

        int lostItems = State.Inventory.Total;
        PendingArrest = new ArrestReport(State.Money, lostItems);
        State.Money = 0;
        State.Inventory.Clear();
        State.TimesArrested++;

        Heat.Clear();
        foreach (var o in Officers) o.Removed = true;
        Player.HidingIn = null;
        Player.Position = PlayerStart;
        Clock.SkipToNextDay(8);
    }

    // ---------- Hjälp till spelaren ----------

    /// <summary>Nästa mål, visas i hörnet så att man alltid vet vad man ska göra.</summary>
    public string Objective
    {
        get
        {
            var s = State;
            var inv = s.Inventory;
            var cutter = Stations.OfType<GemCutter>().FirstOrDefault();

            if (!s.Has(Upgrade.GemCutter))
                return s.Money >= Shop.Get(Upgrade.GemCutter).Price
                    ? "Köp en Slipbänk på laptopen i gömstället"
                    : "Skaffa $250: stjäl byte och sälj på Pantbanken";
            if (cutter is { Done: > 0 }) return "Hämta dina diamanter från slipbänken";
            if (inv.Count(ItemType.CutDiamond) > 0 && Orders.Active.Count > 0) return "Leverera till en kund (öppna telefonen med TAB)";
            if (inv.Count(ItemType.RawGem) > 0) return "Lägg ädelstenarna i slipbänken hemma";
            if (cutter is { Queued: > 0 }) return "Slipbänken arbetar. Vänta eller gör en ny kupp";
            if (s.OrdersDelivered == 0) return "Stjäl rå ädelstenar hos Juvelerare Kronan (natten är säkrast)";
            if (!s.Has(Upgrade.Jammer)) return $"Spara ihop till en Störsändare (${Shop.Get(Upgrade.Jammer).Price})";
            if (!s.Has(Upgrade.Smelter)) return "Köp en Smältugn och prova Guldbanken";
            return "Bygg ditt imperium. Guldbanken väntar...";
        }
    }
}
