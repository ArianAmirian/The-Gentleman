using System.Numerics;
using Raylib_cs;
using Gentleman2D.Rendering;
using Gentleman2D.UI;
using Gentleman2D.World;

namespace Gentleman2D.Entities;

/// <summary>
/// En polis som dyker upp när du är efterlyst. Jagar dig när den ser dig,
/// letar runt där du senast syntes och går tillbaka till stationen när efterlysningen är över.
/// </summary>
public class PoliceOfficer : Entity
{
    public const float VisionTiles = 6.5f;
    public const float ChaseSpeed = 158;
    public const float WalkSpeed = 105;

    private enum State { Chase, Search, Leave }

    private State state = State.Search;
    private Vector2 target;
    private Vector2 lastKnown;
    private List<(int X, int Y)> path = new();
    private float repathTimer;
    private Vector2 facing = new(0, 1);
    private float walkPhase;

    public PoliceOfficer(Vector2 spawn, Vector2 lastKnownPlayerPosition)
    {
        Position = spawn;
        lastKnown = lastKnownPlayerPosition;
        target = lastKnownPlayerPosition;
    }

    public bool SeesPlayer { get; private set; }
    public bool IsLeaving => state == State.Leave;

    public override void Update(GameWorld world, float dt)
    {
        var player = world.Player;
        SeesPlayer = false;

        if (!world.Heat.IsWanted)
        {
            SetState(State.Leave, world.PoliceSpawn);
        }
        else
        {
            SeesPlayer = CanSee(world);
            if (SeesPlayer)
            {
                lastKnown = player.Position;
                world.Heat.PoliceSees(dt);
                SetState(State.Chase, player.Position);
            }
            else if (state == State.Leave)
            {
                SetState(State.Search, lastKnown);
            }
        }

        bool arrived = Vector2.Distance(Position, target) < 10;

        switch (state)
        {
            case State.Chase when arrived && !SeesPlayer:
                SetState(State.Search, RandomSpotNear(world, lastKnown));
                break;
            case State.Search when arrived:
                SetState(State.Search, RandomSpotNear(world, lastKnown));
                break;
            case State.Leave when Vector2.Distance(Position, world.PoliceSpawn) < 24:
                Removed = true;
                return;
        }

        Move(world, dt);

        // Fångad! (I en container är man säker.)
        if (world.Heat.IsWanted && player.HidingIn is null &&
            Vector2.Distance(Position, player.Position) < Player.Radius + 14)
        {
            world.Arrest();
        }
    }

    private void SetState(State newState, Vector2 newTarget)
    {
        bool targetMoved = Vector2.Distance(newTarget, target) > TileMap.TileSize * 0.75f;
        if (newState != state || targetMoved)
            repathTimer = 0;
        state = newState;
        target = newTarget;
    }

    private bool CanSee(GameWorld world)
    {
        var player = world.Player;
        if (player.HidingIn is not null) return false;

        float dist = Vector2.Distance(Position, player.Position);

        // I en buske syns man bara om polisen står precis bredvid.
        if (player.IsHidden(world) && dist > TileMap.TileSize * 1.1f) return false;

        float range = VisionTiles * TileMap.TileSize;
        if (world.Clock.Darkness > 0.5f && !world.IsLit(player.Position)) range *= 0.75f;

        return dist <= range && world.Map.HasLineOfSight(Position, player.Position);
    }

    private void Move(GameWorld world, float dt)
    {
        float speed = state == State.Chase ? ChaseSpeed : WalkSpeed;
        var map = world.Map;

        // Ser polisen dig springer den rakt mot dig, annars följer den en väg runt hindren.
        if (SeesPlayer && Vector2.Distance(Position, target) < TileMap.TileSize * 3)
        {
            Step(map, target, speed * dt);
            return;
        }

        repathTimer -= dt;
        if (repathTimer <= 0)
        {
            repathTimer = 0.4f;
            path = map.FindPath(TileMap.ToTile(Position), TileMap.ToTile(target));
        }

        if (path.Count == 0)
        {
            Step(map, target, speed * dt);
            return;
        }

        var next = TileMap.TileCenter(path[0].X, path[0].Y);
        if (path.Count == 1) next = target;
        Step(map, next, speed * dt);
        if (Vector2.Distance(Position, next) < 6) path.RemoveAt(0);
    }

    private void Step(TileMap map, Vector2 goal, float distance)
    {
        var diff = goal - Position;
        if (diff.Length() < 0.5f) return;

        var dir = Vector2.Normalize(diff);
        var delta = dir * MathF.Min(distance, diff.Length());
        facing = dir;
        walkPhase += distance * 0.07f;

        var nx = Position + new Vector2(delta.X, 0);
        if (!map.CollidesAt(nx, 11)) Position = nx;
        var ny = Position + new Vector2(0, delta.Y);
        if (!map.CollidesAt(ny, 11)) Position = ny;
    }

    private static Vector2 RandomSpotNear(GameWorld world, Vector2 around)
    {
        var (cx, cy) = TileMap.ToTile(around);
        for (int i = 0; i < 25; i++)
        {
            int x = cx + world.Rng.Next(-5, 6);
            int y = cy + world.Rng.Next(-5, 6);
            if (!world.Map.IsSolid(x, y)) return TileMap.TileCenter(x, y);
        }
        return around;
    }

    public override void Draw(GameWorld world)
    {
        Paint.Person(Position, facing, new Color(40, 70, 140, 255), new Color(222, 182, 150, 255),
            HeadWear.PoliceCap, new Color(28, 40, 80, 255), walkPhase);

        if (SeesPlayer)
        {
            // Ett "!" ovanför huvudet när polisen ser dig
            Theme.DrawTextCentered("!", Position.X, Position.Y - 46, 26, Theme.Red, true);
        }
    }

    /// <summary>Blinkande blått och rött ljus, används av ljussystemet.</summary>
    public Color SirenColor(float time) =>
        (int)(time * 5) % 2 == 0 ? new Color(70, 110, 255, 255) : new Color(255, 60, 60, 255);
}
