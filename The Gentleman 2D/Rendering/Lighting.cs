using System.Numerics;
using Raylib_cs;
using Gentleman2D.Entities;

namespace Gentleman2D.Rendering;

/// <summary>
/// Dag och natt. Vi ritar en "ljuskarta": först fylls den med omgivningsljuset
/// (vitt på dagen, mörkblått på natten), sedan läggs lampornas sken till.
/// Till sist multipliceras ljuskartan med bilden, så att allt utanför lamporna blir mörkt.
/// </summary>
public class Lighting : IDisposable
{
    private readonly RenderTexture2D lightMap;
    private readonly int width;
    private readonly int height;

    public Lighting(int width, int height)
    {
        this.width = width;
        this.height = height;
        lightMap = Raylib.LoadRenderTexture(width, height);
    }

    public void Render(GameWorld world, Camera2D camera)
    {
        float darkness = world.Clock.Darkness;

        Raylib.BeginTextureMode(lightMap);
        Raylib.ClearBackground(world.Clock.Ambient);

        if (darkness > 0.01f)
        {
            Raylib.BeginMode2D(camera);
            Raylib.BeginBlendMode(BlendMode.Additive);

            foreach (var light in world.Lights)
            {
                float k = light.NightOnly ? darkness : Math.Max(0.35f, darkness);
                Glow(light.Position, light.Radius, light.Color, k * 0.95f);
            }

            // Lite ljus runt spelaren så att man alltid ser sig själv
            Glow(world.Player.Position, 90, new Color(120, 120, 140, 255), darkness * 0.6f);

            // Polisens blåljus
            foreach (var officer in world.Officers)
                Glow(officer.Position, 150, officer.SirenColor(world.Time), Math.Max(0.35f, darkness) * 0.8f);

            // Lysdioderna på kamerorna
            foreach (var cam in world.Cameras.Where(c => !c.IsJammed))
                Glow(cam.Position, 34, new Color(255, 60, 60, 255), darkness * 0.7f);

            Raylib.EndBlendMode();
            Raylib.EndMode2D();
        }

        Raylib.EndTextureMode();
    }

    private static void Glow(Vector2 p, float radius, Color c, float intensity)
    {
        if (intensity <= 0) return;
        var inner = new Color((int)(c.R * intensity), (int)(c.G * intensity), (int)(c.B * intensity), 255);
        Raylib.DrawCircleGradient(p, radius, inner, new Color(0, 0, 0, 0));
    }

    /// <summary>Lägger ljuskartan över bilden som just ritats.</summary>
    public void Apply()
    {
        Raylib.BeginBlendMode(BlendMode.Multiplied);
        Raylib.DrawTextureRec(lightMap.Texture, new Rectangle(0, 0, width, -height), Vector2.Zero, Color.White);
        Raylib.EndBlendMode();
    }

    public void Dispose() => Raylib.UnloadRenderTexture(lightMap);
}
