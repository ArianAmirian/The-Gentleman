using System.Numerics;
using Gentleman2D.Core;
using Gentleman2D.Entities;
using Gentleman2D.World;

namespace Gentleman2D;

/// <summary>
/// Automatiska tester och skärmbilder. Körs bara om miljövariabeln GENTLEMAN_AUTOPILOT är satt.
/// Först kontrolleras spelreglerna utan att rita något, sedan spelas en kort sekvens
/// upp i fönstret och skärmbilder sparas i den angivna mappen.
/// </summary>
public static class AutoPilot
{
    public static void Attach(GameApp app, string outDir)
    {
        Directory.CreateDirectory(outDir);
        SaveSystem.FilePath = Path.Combine(outDir, "save.json"); // rör inte spelarens riktiga sparfil
        if (File.Exists(SaveSystem.FilePath)) File.Delete(SaveSystem.FilePath);

        bool ok = RunLogicTests();
        Console.WriteLine(ok ? "LOGIKTESTER: ALLA OK" : "LOGIKTESTER: FEL FINNS");

        app.Script = frame => Step(app, frame);
        app.AfterFrame = frame => Shoot(app, frame, outDir);
    }

    // ---------- Logiktester ----------

    private static int failures;

    private static void Check(string name, bool condition)
    {
        Console.WriteLine($"  [{(condition ? "OK" : "FEL")}] {name}");
        if (!condition) failures++;
    }

    private static void Tick(GameWorld w, float seconds, InputState? input = null)
    {
        for (float t = 0; t < seconds; t += 0.05f)
        {
            w.Input = input ?? new InputState();
            w.Update(0.05f);
            input = null; // knapptryck gäller bara första bildrutan
        }
    }

    private static bool RunLogicTests()
    {
        failures = 0;
        var w = new GameWorld();
        var s = w.State;

        // Allt viktigt går att nå från gömstället
        var reachable = Reachable(w.Map, TileMap.ToTile(w.PlayerStart));
        bool Near(Vector2 p)
        {
            var (x, y) = TileMap.ToTile(p);
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (reachable.Contains((x + dx, y + dy))) return true;
            return false;
        }
        Check("Alla montrar går att nå", w.LootSpots.All(l => Near(l.Position)));
        Check("Alla maskiner går att nå", w.Stations.All(st => Near(st.Position)));
        Check("Alla mötesplatser går att nå", w.Orders.MeetPoints.All(m => reachable.Contains(TileMap.ToTile(m.Position))));
        Check("Pantbanken går att nå", w.Entities.OfType<PawnCounter>().All(p => Near(p.Position)));
        Check("Polisen hittar väg till gömstället", w.Map.FindPath(TileMap.ToTile(w.PoliceSpawn), TileMap.ToTile(w.PlayerStart)).Count > 0);

        // Stjäla: håll in E vid en monter
        var loot = w.LootSpots.First();
        w.Player.Position = loot.Position + new Vector2(0, 46);
        var hold = new InputState { InteractHeld = true };
        for (int i = 0; i < 20 && loot.Available; i++) { w.Input = hold; w.Update(0.1f); }
        Check("Stjäla från en monter (håll E)", !loot.Available && s.Inventory.Count(loot.Type) == loot.Amount);
        s.Inventory.Clear();

        // Köpa och tillverka
        Check("Köpa slipbänk för startpengarna", s.TryBuy(Shop.Get(Upgrade.GemCutter)) && s.Money == GameState.StartMoney - 250);
        Check("Kan inte köpa störsändare utan pengar", !s.TryBuy(Shop.Get(Upgrade.Jammer)));
        var cutter = w.Stations.OfType<GemCutter>().First();
        s.Inventory.Add(ItemType.RawGem, 3);
        w.Player.Position = cutter.Position + new Vector2(0, 48);
        Tick(w, 0.05f, new InputState { Interact = true });
        Check("Ädelstenar läggs i slipbänken", cutter.Queued == 3 && s.Inventory.Count(ItemType.RawGem) == 0);
        Tick(w, 3 * 30 / GameClock.GameMinutesPerSecond + 1);
        Check("Slipbänken blir klar", cutter.Done == 3 && cutter.Queued == 0);
        Tick(w, 0.05f, new InputState { Interact = true });
        Check("Diamanterna hamnar i väskan", s.Inventory.Count(ItemType.CutDiamond) == 3);

        // Beställning och leverans
        w.Orders.ForceOrder(w);
        var order = w.Orders.Active.FirstOrDefault();
        Check("En kund beställer", order is not null);
        if (order is not null)
        {
            s.Inventory.Add(ItemType.CutDiamond, 3);
            int before = s.Money;
            Tick(w, 0.1f);
            w.Player.Position = order.Place.Position + new Vector2(36, 0);
            Tick(w, 0.05f, new InputState { Interact = true });
            Check("Leverans ger betalt", s.Money == before + order.Price && s.OrdersDelivered == 1);
        }

        // Kameror, buskar och störsändare
        var cam = w.Cameras.First();
        float rad = cam.Angle * MathF.PI / 180;
        w.Player.Position = cam.Position + new Vector2(MathF.Cos(rad), MathF.Sin(rad)) * 110;
        Check("Kameran ser spelaren framför sig", cam.CanSee(w));
        float heatBefore = w.Heat.Heat;
        for (int i = 0; i < 40; i++)
        {
            rad = cam.Angle * MathF.PI / 180;
            w.Player.Position = cam.Position + new Vector2(MathF.Cos(rad), MathF.Sin(rad)) * 110;
            Tick(w, 0.05f);
        }
        Check("Efterlysningen stiger när kameran ser en", w.Heat.Heat > heatBefore + 15);

        s.Earn(2000);
        Check("Köpa störsändare", s.TryBuy(Shop.Get(Upgrade.Jammer)));
        Tick(w, 0.05f, new InputState { Jam = true });
        Check("Störsändaren slår ut kameran", cam.IsJammed && !cam.CanSee(w));

        w.Player.Position = TileMap.TileCenter(24, 18);
        Check("Man är gömd i en buske", w.Player.IsHidden(w));

        // Polisen griper
        w.Heat.Spike(70);
        Tick(w, 0.5f);
        var cop = w.Officers.FirstOrDefault();
        Check("Polisen rycker ut vid efterlysning", cop is not null);
        s.Earn(100);
        s.Inventory.Add(ItemType.RawGem, 2);
        if (cop is not null)
        {
            w.Player.Position = cop.Position + new Vector2(20, 0);
            Tick(w, 0.1f);
        }
        Check("Polisen griper spelaren", w.PendingArrest is not null && s.Money == 0 && s.Inventory.Total == 0);
        Check("Efterlysningen nollställs efter gripandet", !w.Heat.IsWanted);

        // Spara och ladda
        s.SafeMoney = 777;
        SaveSystem.Save(w);
        var w2 = new GameWorld();
        Check("Ladda sparat spel", SaveSystem.TryLoad(w2) && w2.State.SafeMoney == 777 &&
                                     w2.State.Has(Upgrade.Jammer) && w2.Clock.Day == w.Clock.Day);
        File.Delete(SaveSystem.FilePath);

        return failures == 0;
    }

    private static HashSet<(int, int)> Reachable(TileMap map, (int X, int Y) start)
    {
        var seen = new HashSet<(int, int)> { start };
        var q = new Queue<(int X, int Y)>();
        q.Enqueue(start);
        while (q.Count > 0)
        {
            var (x, y) = q.Dequeue();
            foreach (var n in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                if (!map.IsSolid(n.Item1, n.Item2) && seen.Add(n)) q.Enqueue(n);
        }
        return seen;
    }

    // ---------- Skärmbilder ----------

    private static readonly Dictionary<int, string> Shots = new()
    {
        [45] = "01_meny",
        [110] = "02_gomstalle",
        [170] = "03_slipbank",
        [200] = "04_butik",
        [222] = "05_juvelerare_natt",
        [330] = "06_upptackt",
        [440] = "07_polis",
        [470] = "08_gripen",
        [520] = "09_telefon",
        [565] = "10_torget_dag",
        [575] = "11_karta_dag",
        [585] = "12_karta_natt",
    };

    private static InputState Step(GameApp app, int f)
    {
        var w = app.World;
        var input = new InputState();

        switch (f)
        {
            case 46:
                app.StartNewGame();
                break;
            case 111:
                w.State.Earn(3000);
                foreach (var u in new[] { Upgrade.GemCutter, Upgrade.Smelter, Upgrade.Safe, Upgrade.Jammer })
                    w.State.TryBuy(Shop.Get(u));
                w.State.Inventory.Add(ItemType.RawGem, 4);
                w.State.Inventory.Add(ItemType.GoldScrap, 2);
                w.Clock.SetState(2, 14 * 60);
                w.Player.Position = w.Stations.OfType<GemCutter>().First().Position + new Vector2(-46, 4);
                break;
            case 120:
                input.Interact = true;
                break;
            case 171:
                w.OpenShop();
                break;
            case 201:
                input.Back = true;
                break;
            case 210:
                w.Clock.SetState(2, 23 * 60 + 30);
                w.Player.Position = TileMap.TileCenter(29, 20);
                app.FollowPlayer(0, true);
                break;
            case 331:
                w.Player.Position = TileMap.TileCenter(22, 23);
                w.Heat.Spike(70);
                app.FollowPlayer(0, true);
                break;
            case 441:
                w.Arrest();
                break;
            case 471:
                input.Confirm = true;
                break;
            case 480:
                w.Clock.SetState(3, 12 * 60);
                for (int i = 0; i < 3; i++) w.Orders.ForceOrder(w);
                w.State.Inventory.Add(ItemType.CutDiamond, 3);
                w.State.Inventory.Add(ItemType.GoldBar, 2);
                break;
            case 485:
            case 521:
                input.Phone = true;
                break;
            case 530:
                w.Player.Position = TileMap.TileCenter(52, 18);
                app.FollowPlayer(0, true);
                break;
            case 566:
                app.CameraLocked = true;
                app.SetCamera(new Vector2(w.Map.PixelWidth / 2, w.Map.PixelHeight / 2), 0.42f);
                break;
            case 576:
                w.Clock.SetState(3, 23 * 60);
                break;
            case 590:
                app.Quit();
                break;
        }

        if (f >= 46 && f < 60) input.Move = new Vector2(0, 1); // gå lite i början
        return input;
    }

    private static void Shoot(GameApp app, int frame, string dir)
    {
        if (Shots.TryGetValue(frame, out var name))
        {
            app.SaveScreenshot(Path.Combine(dir, name + ".png"));
            var w = app.World;
            Console.WriteLine($"  bild {name}: heat={w.Heat.Heat:0} stjärnor={w.Heat.Stars} poliser={w.Officers.Count()} " +
                              $"pengar={w.State.Money} gripen={app.IsArrested} tid={w.Clock.TimeText}");
        }
    }
}
