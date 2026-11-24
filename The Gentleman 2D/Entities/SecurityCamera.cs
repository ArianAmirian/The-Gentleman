using System.Numerics;
using Raylib_cs;
using Gentleman2D.UI;
using Gentleman2D.World;

namespace Gentleman2D.Entities;

/// <summary>
/// Övervakningskamera som sveper fram och tillbaka. Ser den dig fylls en misstankemätare,
/// och när den är full stiger din efterlysning. Kan slås ut med störsändaren.
/// </summary>
public class SecurityCamera : Entity
{
    public const float FieldOfView = 56f;
    public const float RangeTiles = 6.5f;
    public const float JamDuration = 20f;

    private readonly float baseAngle;
    private readonly float sweep;
    private readonly float period;
    private readonly float phase;
    private bool alarmRaised;

    public SecurityCamera(Vector2 position, float baseAngle, float sweep = 40, float period = 8)
    {
        Position = position;
        this.baseAngle = baseAngle;
        this.sweep = sweep;
        this.period = period;
        phase = (position.X * 7 + position.Y * 13) % period;
        Angle = baseAngle;
    }

    /// <summary>Vinkeln kameran tittar åt, i grader (0 = höger, 90 = ner).</summary>
    public float Angle { get; private set; }

    /// <summary>0 = lugn, 1 = har sett dig.</summary>
    public float Suspicion { get; private set; }

    public float JammedTimer { get; private set; }
    public bool IsJammed => JammedTimer > 0;
    public bool SeesPlayer { get; private set; }

    public void Jam() => JammedTimer = JamDuration;

    /// <summary>På natten ser kameran kortare, om man inte står i ljuset från en lampa.</summary>
    public float Range(GameWorld world) =>
        RangeTiles * TileMap.TileSize * (world.Clock.Darkness > 0.5f && !world.IsLit(world.Player.Position) ? 0.7f : 1f);

    public override void Update(GameWorld world, float dt)
    {
        if (IsJammed)
        {
            JammedTimer -= dt;
            Suspicion = 0;
            SeesPlayer = false;
            alarmRaised = false;
            return;
        }

        Angle = baseAngle + MathF.Sin((world.Time + phase) * MathF.Tau / period) * sweep;
        SeesPlayer = CanSee(world);

        Suspicion = Math.Clamp(Suspicion + (SeesPlayer ? dt * 1.7f : -dt * 0.6f), 0, 1);

        if (SeesPlayer && Suspicion >= 1)
        {
            if (!alarmRaised)
            {
                alarmRaised = true;
                world.Heat.Spike(22);
                world.Notify("En kamera har sett dig!", Theme.Red);
            }
            world.Heat.Spotted(dt, 14);
        }
        else if (Suspicion <= 0)
        {
            alarmRaised = false;
        }
    }

    public bool CanSee(GameWorld world)
    {
        var player = world.Player;
        if (IsJammed || player.IsHidden(world)) return false;

        var toPlayer = player.Position - Position;
        float dist = toPlayer.Length();
        if (dist > Range(world)) return false;

        float angleToPlayer = MathF.Atan2(toPlayer.Y, toPlayer.X) * 180 / MathF.PI;
        if (MathF.Abs(DeltaAngle(angleToPlayer, Angle)) > FieldOfView / 2) return false;

        return world.Map.HasLineOfSight(Position, player.Position);
    }

    private static float DeltaAngle(float a, float b)
    {
        float d = (a - b) % 360;
        if (d > 180) d -= 360;
        if (d < -180) d += 360;
        return d;
    }

    /// <summary>
    /// Synkonen ritas ovanpå mörkret så att man alltid ser den. Vi skickar ut strålar
    /// åt olika håll och stoppar varje stråle vid första väggen, så att konen inte går igenom väggar.
    /// </summary>
    public void DrawCone(GameWorld world)
    {
        if (IsJammed) return;

        var calm = new Color(255, 214, 90, 255);
        var alarm = new Color(255, 70, 60, 255);
        var color = Theme.Lerp(calm, alarm, Suspicion);
        float range = Range(world);

        const int rays = 30;
        var far = new Vector2[rays + 1];
        var near = new Vector2[rays + 1];
        for (int i = 0; i <= rays; i++)
        {
            float a = (Angle - FieldOfView / 2 + FieldOfView * i / rays) * MathF.PI / 180;
            var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
            float dist = CastRay(world.Map, Position, dir, range);
            far[i] = Position + dir * dist;
            near[i] = Position + dir * MathF.Min(dist, range * 0.35f);
        }

        var fill = Theme.WithAlpha(color, 0.16f + Suspicion * 0.14f);
        var core = Theme.WithAlpha(color, 0.12f);
        var edge = Theme.WithAlpha(color, 0.5f);
        for (int i = 0; i < rays; i++)
        {
            Raylib.DrawTriangle(Position, far[i + 1], far[i], fill);
            Raylib.DrawTriangle(Position, near[i + 1], near[i], core);
            Raylib.DrawLineEx(far[i], far[i + 1], 1.2f, Theme.WithAlpha(color, 0.3f));
        }
        Raylib.DrawLineEx(Position, far[0], 1.5f, edge);
        Raylib.DrawLineEx(Position, far[rays], 1.5f, edge);
    }

    private static float CastRay(TileMap map, Vector2 origin, Vector2 dir, float max)
    {
        for (float d = 6; d < max; d += 6)
        {
            var (x, y) = TileMap.ToTile(origin + dir * d);
            if (map.BlocksSight(x, y)) return d;
        }
        return max;
    }

    public override void Draw(GameWorld world)
    {
        float rad = Angle * MathF.PI / 180;
        var dir = new Vector2(MathF.Cos(rad), MathF.Sin(rad));

        Raylib.DrawCircleV(Position, 9, new Color(40, 44, 54, 255));
        Raylib.DrawLineEx(Position, Position + dir * 13, 9, new Color(70, 76, 90, 255));
        Raylib.DrawCircleV(Position + dir * 13, 4.5f, new Color(20, 22, 28, 255));
        Raylib.DrawCircleV(Position + dir * 13, 2f, new Color(120, 180, 255, 255));

        if (IsJammed)
        {
            // Gnistor och blått sken när kameran är utslagen
            bool spark = (int)(world.Time * 8) % 3 == 0;
            Raylib.DrawCircleV(Position, 3.5f, spark ? new Color(170, 120, 255, 255) : new Color(60, 60, 80, 255));
        }
        else
        {
            bool blink = (int)(world.Time * 2) % 2 == 0;
            Raylib.DrawCircleV(Position - dir * 3, 2.5f, blink || Suspicion > 0.5f ? new Color(255, 50, 50, 255) : new Color(90, 20, 20, 255));
        }
    }
}
