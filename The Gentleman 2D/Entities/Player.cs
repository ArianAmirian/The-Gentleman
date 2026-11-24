using System.Numerics;
using Raylib_cs;
using Gentleman2D.Core;
using Gentleman2D.Rendering;
using Gentleman2D.World;

namespace Gentleman2D.Entities;

/// <summary>The Gentleman: svart kostym, hög hatt med rött band.</summary>
public class Player : Entity
{
    public const float Radius = 13;
    public const float BaseSpeed = 170;

    private float walkPhase;
    private bool moving;

    public Vector2 Facing { get; private set; } = new(0, 1);

    /// <summary>Containern man gömmer sig i, eller null.</summary>
    public Dumpster? HidingIn { get; set; }

    public float Speed(GameWorld world) => world.State.Has(Upgrade.Shoes) ? BaseSpeed * 1.25f : BaseSpeed;

    public (int X, int Y) Tile => TileMap.ToTile(Position);

    /// <summary>Gömd i en container eller en buske.</summary>
    public bool IsHidden(GameWorld world) => HidingIn is not null || world.Map.IsHidingSpot(Tile.X, Tile.Y);

    public override void Update(GameWorld world, float dt)
    {
        moving = false;
        if (HidingIn is not null || world.InputLocked) return;

        var move = world.Input.Move;
        if (move == Vector2.Zero) return;

        move = Vector2.Normalize(move);
        Facing = move;
        var delta = move * Speed(world) * dt;

        // Flytta en axel i taget, då kan man glida längs väggar.
        TryMove(world, new Vector2(delta.X, 0));
        TryMove(world, new Vector2(0, delta.Y));

        walkPhase += dt * 11;
        moving = true;
    }

    private void TryMove(GameWorld world, Vector2 delta)
    {
        var next = Position + delta;
        if (!world.Map.CollidesAt(next, Radius))
            Position = next;
    }

    public override void Draw(GameWorld world)
    {
        if (HidingIn is not null) return;

        bool inBush = world.Map.IsHidingSpot(Tile.X, Tile.Y);
        Paint.Person(Position, Facing, new Color(38, 40, 52, 255), new Color(236, 196, 160, 255),
            HeadWear.TopHat, new Color(190, 40, 50, 255), moving ? walkPhase : 0, inBush ? 0.6f : 1f);
    }
}
