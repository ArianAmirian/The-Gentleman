using System.Numerics;
using Raylib_cs;
using Gentleman2D.Core;
using Gentleman2D.Entities;
using Gentleman2D.Systems;

namespace Gentleman2D.World;

/// <summary>
/// Bygger staden: vägar, kvarter, byggnader, kameror och allt annat.
/// Kartan är 64 × 40 rutor och delas in i nio kvarter av två vägar på varje håll.
///
///   Gömstället   | Parken         | Pantbanken
///   -------------+----------------+-----------
///   Polisen      | Juvelerare     | Torget
///   -------------+----------------+-----------
///   Gränden      | Guldbanken     | Hamnen
/// </summary>
public static class WorldBuilder
{
    public const int Width = 64;
    public const int Height = 40;

    private static readonly Color LampLight = new(255, 214, 150, 255);
    private static readonly Color IndoorLight = new(255, 230, 190, 255);

    public static void Build(GameWorld w)
    {
        var map = new TileMap(Width, Height);
        w.Map = map;

        BuildRoads(map);
        BuildHideout(w, map);
        BuildPark(w, map);
        BuildPawnShop(w, map);
        BuildPoliceStation(w, map);
        BuildJeweler(w, map);
        BuildPlaza(w, map);
        BuildAlley(w, map);
        BuildBank(w, map);
        BuildHarbor(w, map);
        BuildStreetLamps(w, map);
    }

    // ---------- Byggstenar ----------

    private static void Fill(TileMap m, int x0, int y0, int x1, int y1, Tile t)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                m[x, y] = t;
    }

    /// <summary>En byggnad: väggar runtom och golv inuti.</summary>
    private static void Building(TileMap m, int x0, int y0, int x1, int y1, Tile floor)
    {
        Fill(m, x0, y0, x1, y1, Tile.Wall);
        Fill(m, x0 + 1, y0 + 1, x1 - 1, y1 - 1, floor);
    }

    private static Vector2 C(float x, float y) => TileMap.TileCenter(0, 0) + new Vector2(x, y) * TileMap.TileSize;

    /// <summary>Lägger till ett objekt och markerar dess rutor som blockerade.</summary>
    private static T Place<T>(GameWorld w, T e, params (int X, int Y)[] blocked) where T : Entity
    {
        w.AddStatic(e);
        foreach (var (x, y) in blocked) w.Map.SetBlocked(x, y);
        return e;
    }

    private static void Lamp(GameWorld w, int x, int y)
    {
        Place(w, new Prop(C(x, y), PropKind.Lamp), (x, y));
        w.Lights.Add(new Light(C(x, y), 3.8f * TileMap.TileSize, LampLight));
    }

    private static void Loot(GameWorld w, int x, int y, ItemType type, int amount) =>
        w.LootSpots.Add(Place(w, new LootSpot(C(x, y), type, amount), (x, y)));

    /// <summary>
    /// Kamera i ett hörn eller längs en vägg. (x, y) är golvrutan precis innanför väggen
    /// och "side" säger åt vilket håll väggen sitter.
    /// </summary>
    private static void Camera(GameWorld w, int x, int y, Vector2 side, float angle, float sweep = 38)
    {
        var pos = C(x, y) + side * 19;
        var cam = new SecurityCamera(pos, angle, sweep);
        w.Cameras.Add(cam);
        w.AddStatic(cam);
    }

    private static readonly Vector2 TL = new(-1, -1), TR = new(1, -1), BL = new(-1, 1), BR = new(1, 1);
    private static readonly Vector2 Top = new(0, -1), Bottom = new(0, 1);

    // ---------- Kvarteren ----------

    private static void BuildRoads(TileMap m)
    {
        int[] hRoads = { 12, 27 };
        int[] vRoads = { 20, 44 };

        // Trottoarer först, sedan asfalt ovanpå (så att korsningarna blir asfalt).
        foreach (int y in hRoads)
        {
            Fill(m, 0, y - 1, Width - 1, y - 1, Tile.Sidewalk);
            Fill(m, 0, y + 2, Width - 1, y + 2, Tile.Sidewalk);
        }
        foreach (int x in vRoads)
        {
            Fill(m, x - 1, 0, x - 1, Height - 1, Tile.Sidewalk);
            Fill(m, x + 2, 0, x + 2, Height - 1, Tile.Sidewalk);
        }
        foreach (int y in hRoads) Fill(m, 0, y, Width - 1, y + 1, Tile.Road);
        foreach (int x in vRoads) Fill(m, x, 0, x + 1, Height - 1, Tile.Road);
    }

    private static void BuildHideout(GameWorld w, TileMap m)
    {
        Building(m, 3, 2, 12, 9, Tile.WoodFloor);
        m[7, 9] = Tile.Door;
        m[7, 10] = Tile.Path;

        w.PlayerStart = C(7, 6);

        Place(w, new Bed(C(4, 3.5f)), (4, 3), (4, 4));
        Place(w, new Laptop(C(8, 3)), (8, 3));
        Place(w, new Safe(C(11, 3)), (11, 3));
        w.Stations.Add(Place(w, new GemCutter(C(11, 6)), (11, 6)));
        w.Stations.Add(Place(w, new Smelter(C(11, 8)), (11, 8)));
        Place(w, new Prop(C(4, 8), PropKind.Plant), (4, 8));
        Place(w, new Prop(C(5.5f, 8), PropKind.Crate), (5, 8));
        w.Lights.Add(new Light(C(7.5f, 5.5f), 5.5f * TileMap.TileSize, IndoorLight, NightOnly: false));

        // Trädgård
        foreach (var (x, y) in new[] { (15, 2), (17, 7), (1, 1), (1, 6) }) m[x, y] = Tile.Tree;
        foreach (var (x, y) in new[] { (14, 8), (15, 8), (2, 9), (13, 4) }) m[x, y] = Tile.Bush;
        Place(w, new Dumpster(C(10, 10)), (10, 10));
    }

    private static void BuildPark(GameWorld w, TileMap m)
    {
        Fill(m, 23, 5, 42, 5, Tile.Path);
        Fill(m, 32, 0, 32, 10, Tile.Path);
        Fill(m, 31, 4, 33, 6, Tile.Plaza);

        foreach (var (x, y) in new[] { (24, 1), (27, 2), (37, 1), (41, 3), (25, 8), (29, 9), (39, 8), (41, 9), (35, 2), (23, 10) })
            m[x, y] = Tile.Tree;
        foreach (var (x, y) in new[] { (28, 7), (29, 7), (36, 3), (37, 3), (40, 6), (24, 3), (35, 8), (36, 8) })
            m[x, y] = Tile.Bush;

        Place(w, new Prop(C(28, 4), PropKind.Bench), (28, 4));
        Place(w, new Prop(C(36, 6), PropKind.Bench), (36, 6));
        Lamp(w, 30, 3);
        Lamp(w, 34, 7);

        w.Orders.MeetPoints.Add(new MeetPoint("Parken", C(32, 5)));
    }

    private static void BuildPawnShop(GameWorld w, TileMap m)
    {
        Building(m, 50, 2, 58, 9, Tile.StoreFloor);
        m[54, 9] = Tile.Door;
        m[54, 10] = Tile.Path;

        Place(w, new PawnCounter(C(54, 5)), (53, 5), (54, 5), (55, 5));
        Place(w, new Prop(C(52.5f, 3), PropKind.Shelf), (52, 3), (53, 3));
        Place(w, new Prop(C(56.5f, 3), PropKind.Shelf), (56, 3), (57, 3));
        Place(w, new Prop(C(51, 8), PropKind.Plant), (51, 8));
        w.Lights.Add(new Light(C(54, 6), 4.5f * TileMap.TileSize, IndoorLight));

        foreach (var (x, y) in new[] { (48, 2), (62, 4), (61, 9), (47, 7) }) m[x, y] = Tile.Tree;
        foreach (var (x, y) in new[] { (60, 6), (48, 9) }) m[x, y] = Tile.Bush;
    }

    private static void BuildPoliceStation(GameWorld w, TileMap m)
    {
        Building(m, 3, 16, 14, 24, Tile.OfficeFloor);
        m[14, 20] = Tile.Door;
        Fill(m, 15, 20, 18, 20, Tile.Path);
        w.PoliceSpawn = C(12, 20);

        Place(w, new Prop(C(6, 18), PropKind.Desk), (6, 18));
        Place(w, new Prop(C(9, 18), PropKind.Desk), (9, 18));
        Place(w, new Prop(C(6, 22), PropKind.Desk), (6, 22));
        Place(w, new Prop(C(4, 23), PropKind.Plant), (4, 23));
        w.Lights.Add(new Light(C(8.5f, 20), 5f * TileMap.TileSize, new Color(200, 220, 255, 255)));

        var police = new Color(235, 238, 245, 255);
        Place(w, new Prop(C(16.5f, 17), PropKind.Car, police), (16, 17), (17, 17));
        Place(w, new Prop(C(16.5f, 23), PropKind.Car, police), (16, 23), (17, 23));
        foreach (var (x, y) in new[] { (1, 15), (1, 25), (17, 25) }) m[x, y] = Tile.Tree;
    }

    private static void BuildJeweler(GameWorld w, TileMap m)
    {
        // Butik (väst) och valv (öst) med en innervägg emellan
        Building(m, 25, 16, 40, 24, Tile.StoreFloor);
        Fill(m, 35, 17, 39, 23, Tile.VaultFloor);
        Fill(m, 34, 17, 34, 23, Tile.Wall);
        m[34, 20] = Tile.Door;
        m[32, 24] = Tile.Door;          // huvudentré
        m[32, 25] = Tile.Path;
        m[40, 20] = Tile.Door;          // bakdörr
        Fill(m, 41, 20, 42, 20, Tile.Path);

        Loot(w, 28, 18, ItemType.RawGem, 2);
        Loot(w, 31, 18, ItemType.RawGem, 2);
        Loot(w, 28, 22, ItemType.RawGem, 2);
        Loot(w, 37, 18, ItemType.RawGem, 3);
        Loot(w, 37, 22, ItemType.RawGem, 3);

        Place(w, new Prop(C(26, 23), PropKind.Plant), (26, 23));
        Place(w, new Prop(C(39, 20), PropKind.Plant), (39, 20));

        Camera(w, 26, 17, TL, 45);
        Camera(w, 33, 23, BR, 225);
        Camera(w, 39, 17, TR, 135, 30);
        Camera(w, 35, 23, BL, 315, 30);
        Camera(w, 32, 25, Top, 90, 55);    // utanför entrén

        w.Lights.Add(new Light(C(29.5f, 20), 4f * TileMap.TileSize, IndoorLight));
        w.Lights.Add(new Light(C(37, 20), 3.2f * TileMap.TileSize, new Color(190, 210, 255, 255)));

        foreach (var (x, y) in new[] { (24, 18), (24, 22), (41, 17), (42, 23), (23, 20) }) m[x, y] = Tile.Bush;
        Place(w, new Dumpster(C(42, 18)), (42, 18));
    }

    private static void BuildPlaza(GameWorld w, TileMap m)
    {
        Fill(m, 47, 15, 63, 25, Tile.Plaza);
        Fill(m, 54, 19, 56, 21, Tile.Water); // fontän
        w.AddStatic(new Prop(C(55, 20), PropKind.Fountain));

        foreach (var (x, y) in new[] { (48, 16), (62, 16), (48, 24), (62, 24) }) m[x, y] = Tile.Tree;
        Place(w, new Prop(C(51, 17), PropKind.Bench), (51, 17));
        Place(w, new Prop(C(59, 17), PropKind.Bench), (59, 17));
        Place(w, new Prop(C(51, 23), PropKind.Bench), (51, 23));
        Place(w, new Prop(C(59, 23), PropKind.Bench), (59, 23));
        Lamp(w, 50, 20);
        Lamp(w, 60, 20);
        Lamp(w, 55, 16);
        Lamp(w, 55, 24);

        w.Orders.MeetPoints.Add(new MeetPoint("Torget", C(52, 20)));
    }

    private static void BuildAlley(GameWorld w, TileMap m)
    {
        Fill(m, 1, 31, 6, 38, Tile.Roof);
        Fill(m, 12, 31, 18, 38, Tile.RoofSlate);
        Fill(m, 7, 30, 11, 39, Tile.Path);

        Place(w, new Dumpster(C(8, 33)), (8, 33));
        Place(w, new Dumpster(C(10, 37)), (10, 37));
        Place(w, new Prop(C(10, 31), PropKind.Crate), (10, 31));
        Place(w, new Prop(C(8, 38), PropKind.Crate), (8, 38));
        Lamp(w, 11, 34);

        w.Orders.MeetPoints.Add(new MeetPoint("Gränden", C(9, 35)));
    }

    private static void BuildBank(GameWorld w, TileMap m)
    {
        // Lobby (norr) och valv (söder)
        Building(m, 24, 31, 41, 38, Tile.StoreFloor);
        Fill(m, 25, 36, 40, 37, Tile.VaultFloor);
        Fill(m, 25, 35, 40, 35, Tile.Wall);
        m[32, 35] = Tile.Door;
        m[32, 31] = Tile.Door;
        m[32, 30] = Tile.Path;

        Loot(w, 37, 32, ItemType.GoldScrap, 2);
        Loot(w, 27, 36, ItemType.GoldScrap, 3);
        Loot(w, 30, 37, ItemType.GoldScrap, 3);
        Loot(w, 35, 36, ItemType.GoldScrap, 3);
        Loot(w, 38, 37, ItemType.GoldScrap, 3);

        Place(w, new Prop(C(27, 33), PropKind.Desk), (27, 33));
        Place(w, new Prop(C(25, 34), PropKind.Plant), (25, 34));
        Place(w, new Prop(C(40, 34), PropKind.Plant), (40, 34));

        Camera(w, 25, 32, TL, 30, 30);
        Camera(w, 40, 32, TR, 150, 30);
        Camera(w, 25, 37, BL, 330, 28);
        Camera(w, 40, 37, BR, 210, 28);
        Camera(w, 32, 36, Top, 90, 70);    // valvdörren

        w.Lights.Add(new Light(C(32, 33), 5f * TileMap.TileSize, IndoorLight));
        w.Lights.Add(new Light(C(32.5f, 36.5f), 4f * TileMap.TileSize, new Color(190, 210, 255, 255)));

        foreach (var (x, y) in new[] { (23, 33), (42, 36), (23, 37) }) m[x, y] = Tile.Bush;
        Place(w, new Dumpster(C(42, 32)), (42, 32));
    }

    private static void BuildHarbor(GameWorld w, TileMap m)
    {
        Fill(m, 47, 30, 63, 33, Tile.Pier);
        Fill(m, 47, 34, 63, 39, Tile.Water);
        Fill(m, 50, 34, 51, 38, Tile.Pier);
        Fill(m, 58, 34, 59, 38, Tile.Pier);

        Place(w, new Prop(C(48, 31), PropKind.Crate), (48, 31));
        Place(w, new Prop(C(49, 31), PropKind.Crate), (49, 31));
        Place(w, new Prop(C(62, 31), PropKind.Crate), (62, 31));
        Place(w, new Prop(C(62, 32), PropKind.Crate), (62, 32));
        Lamp(w, 52, 30);
        Lamp(w, 60, 33);

        w.Orders.MeetPoints.Add(new MeetPoint("Hamnen", C(55, 32)));
    }

    private static void BuildStreetLamps(GameWorld w, TileMap m)
    {
        var spots = new[]
        {
            (3, 11), (13, 11), (27, 11), (37, 11), (51, 11), (61, 11),
            (3, 26), (13, 26), (27, 26), (37, 26), (51, 26), (61, 26),
            (19, 4), (19, 33), (43, 4), (43, 36), (22, 17), (46, 33), (22, 36), (46, 8)
        };
        foreach (var (x, y) in spots) Lamp(w, x, y);

        var red = new Color(170, 60, 60, 255);
        var blue = new Color(60, 100, 160, 255);
        Place(w, new Prop(C(24.5f, 13), PropKind.Car, red), (24, 13), (25, 13));
        Place(w, new Prop(C(57.5f, 12), PropKind.Car, blue), (57, 12), (58, 12));
        Place(w, new Prop(C(8.5f, 28), PropKind.Car, new Color(70, 140, 90, 255)), (8, 28), (9, 28));
    }
}
