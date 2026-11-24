using Gentleman2D.Entities;
using Gentleman2D.UI;

namespace Gentleman2D.Systems;

/// <summary>
/// Efterlysning ("heat"), 0–100. Kameror och poliser som ser dig höjer den.
/// Över 25/55/85 får du 1/2/3 stjärnor och lika många poliser letar efter dig.
/// Håller du dig undan i 6 sekunder börjar den sjunka.
/// </summary>
public class HeatSystem
{
    public const float CooldownDelay = 6f;
    public const float DecayPerSecond = 7f;
    private const float SpawnInterval = 2.5f;

    private float unseenTimer = 99;
    private float spawnTimer;
    private int lastStars;

    public float Heat { get; private set; }

    public int Stars => Heat >= 85 ? 3 : Heat >= 55 ? 2 : Heat >= 25 ? 1 : 0;
    public bool IsWanted => Stars > 0;

    /// <summary>Sant en kort stund efter att någon sett dig, används av gränssnittet.</summary>
    public bool RecentlySeen => unseenTimer < 1f;

    public void Spike(float amount)
    {
        Heat = Math.Min(100, Heat + amount);
        unseenTimer = 0;
    }

    public void Spotted(float dt, float perSecond)
    {
        Heat = Math.Min(100, Heat + perSecond * dt);
        unseenTimer = 0;
    }

    /// <summary>En polis som ser dig håller efterlysningen kvar (men höjer den bara lite).</summary>
    public void PoliceSees(float dt)
    {
        Heat = Math.Min(100, Math.Max(Heat, 30) + 3 * dt);
        unseenTimer = 0;
    }

    public void Clear()
    {
        Heat = 0;
        unseenTimer = 99;
        lastStars = 0;
    }

    public void Update(GameWorld world, float dt)
    {
        unseenTimer += dt;
        if (unseenTimer > CooldownDelay)
            Heat = Math.Max(0, Heat - DecayPerSecond * dt);

        if (Stars > lastStars)
            world.Notify(Stars == 1 ? "Du är efterlyst! Polisen är på väg." : $"Efterlysningen ökar: {Stars} stjärnor!", Theme.Red);
        else if (Stars == 0 && lastStars > 0)
            world.Notify("Du skakade av dig polisen.", Theme.Green);
        lastStars = Stars;

        // Skicka ut poliser tills det finns lika många som stjärnor.
        int active = world.Officers.Count(o => !o.IsLeaving);
        spawnTimer -= dt;
        if (active < Stars && spawnTimer <= 0)
        {
            spawnTimer = SpawnInterval;
            world.Spawn(new PoliceOfficer(world.PoliceSpawn, world.Player.Position));
        }
    }
}
