using System.Numerics;
using Raylib_cs;
using Gentleman2D.Core;
using Gentleman2D.Rendering;
using Gentleman2D.UI;
using Gentleman2D.World;

namespace Gentleman2D;

/// <summary>
/// Fönstret och spelets "scener": huvudmeny, spel, paus och gripen.
/// Allt ritas först i en bild på 1280 × 720 som sedan skalas till fönstret,
/// så att spelet ser likadant ut oavsett fönsterstorlek och skärm.
/// </summary>
public class GameApp : IDisposable
{
    public const int ScreenW = 1280;
    public const int ScreenH = 720;

    private enum Scene { Menu, Playing, Paused, Arrested }

    private readonly RenderTexture2D target;
    private readonly Lighting lighting;
    private readonly PhoneUi phone = new();
    private readonly ShopUi shop = new();
    private MenuList mainMenu = new();
    private readonly MenuList pauseMenu = new("Fortsätt", "Spara och gå till menyn", "Spara och avsluta");
    private Scene scene = Scene.Menu;
    private Hud hud;
    private Camera2D camera;
    private bool quit;
    private float menuTime;

    public GameApp()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
        Raylib.SetTraceLogLevel(TraceLogLevel.Error);
        Raylib.InitWindow(ScreenW, ScreenH, "The Gentleman");
        Raylib.SetWindowMinSize(640, 360);
        Raylib.SetExitKey(KeyboardKey.Null); // Esc öppnar pausmenyn i stället för att stänga
        Raylib.SetTargetFPS(60);

        Theme.Load();
        target = Raylib.LoadRenderTexture(ScreenW, ScreenH);
        Raylib.SetTextureFilter(target.Texture, TextureFilter.Bilinear);
        lighting = new Lighting(ScreenW, ScreenH);

        World = CreateMenuWorld();
        hud = new Hud(World, ScreenW, ScreenH);
        camera = new Camera2D(new Vector2(ScreenW / 2f, ScreenH / 2f), World.PlayerStart, 0, 1);
        BuildMainMenu();
    }

    public GameWorld World { get; private set; }

    /// <summary>Används av autopiloten (skärmbilder och tester) för att styra spelet.</summary>
    public Func<int, InputState?>? Script { get; set; }
    public Action<int>? AfterFrame { get; set; }

    public bool IsPlaying => scene == Scene.Playing;
    public bool IsArrested => scene == Scene.Arrested;

    /// <summary>Låser kameran (autopiloten använder det för översiktsbilder).</summary>
    public bool CameraLocked { get; set; }
    public PhoneUi Phone => phone;

    private static GameWorld CreateMenuWorld()
    {
        var w = new GameWorld();
        w.Clock.SetState(1, 23 * 60);
        return w;
    }

    private void BuildMainMenu()
    {
        mainMenu = SaveSystem.Exists
            ? new MenuList("Fortsätt", "Nytt spel", "Avsluta")
            : new MenuList("Nytt spel", "Avsluta");
    }

    // ---------- Spelflöde ----------

    public void StartNewGame()
    {
        SetWorld(new GameWorld());
        World.Notify("Välkommen hem, Gentleman.", Theme.Gold);
        World.Notify("Gå till laptopen och köp en Slipbänk.", Theme.Text);
        scene = Scene.Playing;
    }

    private void ContinueGame()
    {
        SetWorld(new GameWorld());
        if (!SaveSystem.TryLoad(World))
            World.Notify("Sparfilen gick inte att läsa. Nytt spel startat.", Theme.Red);
        else
            World.Notify($"Välkommen tillbaka. Dag {World.Clock.Day}.", Theme.Gold);
        scene = Scene.Playing;
    }

    private void SetWorld(GameWorld world)
    {
        hud.Dispose();
        World = world;
        World.Saved += () => SaveSystem.Save(World);
        hud = new Hud(World, ScreenW, ScreenH);
        phone.Close();
        shop.Close();
        camera.Target = World.Player.Position;
    }

    public void Run()
    {
        int frame = 0;
        while (!quit && !Raylib.WindowShouldClose())
        {
            float dt = Math.Min(Raylib.GetFrameTime(), 1 / 20f);
            var input = Script?.Invoke(frame) ?? InputState.FromKeyboard();
            Update(input, dt);
            Render();
            AfterFrame?.Invoke(frame);
            frame++;
        }

        if (scene is Scene.Playing or Scene.Paused) SaveSystem.Save(World);
    }

    public void Quit() => quit = true;

    private void Update(InputState input, float dt)
    {
        switch (scene)
        {
            case Scene.Menu:
                menuTime += dt;
                switch (mainMenu.Update(input, dt))
                {
                    case "Nytt spel": StartNewGame(); break;
                    case "Fortsätt": ContinueGame(); break;
                    case "Avsluta": quit = true; break;
                }
                // Staden glider långsamt förbi bakom menyn
                camera.Zoom = 1;
                camera.Target = new Vector2(
                    World.Map.PixelWidth / 2 + MathF.Sin(menuTime * 0.05f) * 850,
                    World.Map.PixelHeight / 2 + MathF.Cos(menuTime * 0.07f) * 350);
                break;

            case Scene.Playing:
                if (input.Pause && !phone.IsOpen && !shop.IsOpen)
                {
                    pauseMenu.Selected = 0;
                    scene = Scene.Paused;
                    return;
                }
                if (input.Phone && !shop.IsOpen) phone.Toggle();

                World.Input = input;
                World.InputLocked = phone.IsOpen || shop.IsOpen;
                phone.Update(World, dt);
                shop.Update(World, dt);
                World.Update(dt);

                if (World.ShopRequested)
                {
                    World.ShopRequested = false;
                    phone.Close();
                    shop.Open();
                }
                if (World.PendingArrest is not null)
                {
                    phone.Close();
                    shop.Close();
                    scene = Scene.Arrested;
                }
                if (!CameraLocked) FollowPlayer(dt);
                break;

            case Scene.Paused:
                phone.Update(World, dt);
                if (input.Pause) { scene = Scene.Playing; break; }
                switch (pauseMenu.Update(input, dt))
                {
                    case "Fortsätt": scene = Scene.Playing; break;
                    case "Spara och gå till menyn":
                        SaveSystem.Save(World);
                        BuildMainMenu();
                        SetWorld(CreateMenuWorld());
                        scene = Scene.Menu;
                        break;
                    case "Spara och avsluta":
                        SaveSystem.Save(World);
                        quit = true;
                        break;
                }
                break;

            case Scene.Arrested:
                if (input.Confirm)
                {
                    World.PendingArrest = null;
                    SaveSystem.Save(World);
                    World.Notify("Du är fri igen. Var försiktigare den här gången.", Theme.Text);
                    scene = Scene.Playing;
                    camera.Target = World.Player.Position;
                }
                break;
        }
    }

    /// <summary>Kameran följer mjukt efter spelaren men visar aldrig något utanför kartan.</summary>
    public void FollowPlayer(float dt, bool instant = false)
    {
        var goal = World.Player.Position;
        float halfW = ScreenW / 2f / camera.Zoom, halfH = ScreenH / 2f / camera.Zoom;
        goal.X = Math.Clamp(goal.X, halfW, World.Map.PixelWidth - halfW);
        goal.Y = Math.Clamp(goal.Y, halfH, World.Map.PixelHeight - halfH);
        camera.Target = instant ? goal : Vector2.Lerp(camera.Target, goal, 1 - MathF.Exp(-dt * 8));
    }

    public void SetCamera(Vector2 target, float zoom)
    {
        camera.Target = target;
        camera.Zoom = zoom;
    }

    // ---------- Ritning ----------

    private void Render()
    {
        // Ljuskartan måste ritas innan vi börjar rita i huvudbilden.
        lighting.Render(World, camera);

        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(new Color(20, 22, 28, 255));
        DrawWorld();

        switch (scene)
        {
            case Scene.Menu:
                TitleScreen.Draw(mainMenu, ScreenW, ScreenH, menuTime);
                break;
            case Scene.Playing:
                hud.Draw(World);
                phone.Draw(World, ScreenW, ScreenH);
                shop.Draw(World, ScreenW, ScreenH);
                break;
            case Scene.Paused:
                hud.Draw(World);
                PauseScreen.Draw(pauseMenu, ScreenW, ScreenH);
                break;
            case Scene.Arrested:
                ArrestScreen.Draw(World.PendingArrest!, ScreenW, ScreenH, World.Time);
                break;
        }

        // Halvgenomskinliga paneler sänker bildens alfavärde. Här sätts alfa till 100 %
        // (svart som adderas ändrar inte färgen), annars blir bilden för mörk i fönstret.
        Raylib.BeginBlendMode(BlendMode.AddColors);
        Raylib.DrawRectangle(0, 0, ScreenW, ScreenH, new Color(0, 0, 0, 255));
        Raylib.EndBlendMode();
        Raylib.EndTextureMode();

        // Skala bilden till fönstret och behåll proportionerna (svarta kanter vid behov).
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        float scale = Math.Min(Raylib.GetScreenWidth() / (float)ScreenW, Raylib.GetScreenHeight() / (float)ScreenH);
        var dest = new Rectangle((Raylib.GetScreenWidth() - ScreenW * scale) / 2, (Raylib.GetScreenHeight() - ScreenH * scale) / 2,
            ScreenW * scale, ScreenH * scale);
        Raylib.DrawTexturePro(target.Texture, new Rectangle(0, 0, ScreenW, -ScreenH), dest, Vector2.Zero, 0, Color.White);
        Raylib.EndDrawing();
    }

    private void DrawWorld()
    {
        var view = new Rectangle(
            camera.Target.X - ScreenW / 2f / camera.Zoom, camera.Target.Y - ScreenH / 2f / camera.Zoom,
            ScreenW / camera.Zoom, ScreenH / camera.Zoom);
        var cull = new Rectangle(view.X - 100, view.Y - 100, view.Width + 200, view.Height + 200);

        Raylib.BeginMode2D(camera);
        WorldRenderer.DrawGround(World, view);
        foreach (var e in World.Entities.Where(e => Raylib.CheckCollisionPointRec(e.Position, cull)).OrderBy(e => e.SortY))
            e.Draw(World);
        WorldRenderer.DrawDecor(World, view);
        Raylib.EndMode2D();

        lighting.Apply();

        // Kamerornas synfält ritas ovanpå mörkret så att man alltid ser dem.
        Raylib.BeginMode2D(camera);
        foreach (var cam in World.Cameras.Where(c => Raylib.CheckCollisionPointRec(c.Position, cull)))
            cam.DrawCone(World);
        Raylib.EndMode2D();

        DrawVignette();
    }

    private static void DrawVignette()
    {
        var edge = new Color(0, 0, 0, 110);
        var clear = new Color(0, 0, 0, 0);
        Raylib.DrawRectangleGradientV(0, 0, ScreenW, 110, edge, clear);
        Raylib.DrawRectangleGradientV(0, ScreenH - 130, ScreenW, 130, clear, edge);
        Raylib.DrawRectangleGradientH(0, 0, 140, ScreenH, edge, clear);
        Raylib.DrawRectangleGradientH(ScreenW - 140, 0, 140, ScreenH, clear, edge);
    }

    /// <summary>Sparar det som just ritats som en PNG (används av autopiloten).</summary>
    public void SaveScreenshot(string path)
    {
        var image = Raylib.LoadImageFromTexture(target.Texture);
        Raylib.ImageFlipVertical(ref image);
        Raylib.ExportImage(image, path);
        Raylib.UnloadImage(image);
    }

    public void Dispose()
    {
        hud.Dispose();
        lighting.Dispose();
        Raylib.UnloadRenderTexture(target);
        Theme.Unload();
        Raylib.CloseWindow();
    }
}
