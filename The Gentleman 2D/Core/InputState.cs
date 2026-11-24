using System.Numerics;
using Raylib_cs;

namespace Gentleman2D.Core;

/// <summary>
/// Vad spelaren trycker på den här bildrutan. Samlat i en klass så att
/// spelet även kan styras av ett skript (används för automatiska skärmbilder).
/// </summary>
public class InputState
{
    public Vector2 Move;
    public bool Interact;
    public bool InteractHeld;
    public bool Jam;
    public bool Phone;
    public bool Pause;
    public bool Up;
    public bool Down;
    public bool Confirm;
    public bool Back;
    public bool Tab1;
    public bool Tab2;

    public static InputState FromKeyboard()
    {
        var move = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up)) move.Y -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down)) move.Y += 1;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) move.X -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) move.X += 1;

        return new InputState
        {
            Move = move,
            Interact = Raylib.IsKeyPressed(KeyboardKey.E),
            InteractHeld = Raylib.IsKeyDown(KeyboardKey.E),
            Jam = Raylib.IsKeyPressed(KeyboardKey.Q),
            Phone = Raylib.IsKeyPressed(KeyboardKey.Tab),
            Pause = Raylib.IsKeyPressed(KeyboardKey.Escape),
            Up = Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Up),
            Down = Raylib.IsKeyPressed(KeyboardKey.S) || Raylib.IsKeyPressed(KeyboardKey.Down),
            Confirm = Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.E),
            Back = Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Backspace),
            Tab1 = Raylib.IsKeyPressed(KeyboardKey.One),
            Tab2 = Raylib.IsKeyPressed(KeyboardKey.Two),
        };
    }
}
