using System.Numerics;
using Gentleman2D.Core;

namespace Gentleman2D.Entities;

/// <summary>Basklass för allt som finns i världen och ritas: spelaren, poliser, kameror, möbler...</summary>
public abstract class Entity
{
    public Vector2 Position { get; set; }

    /// <summary>Sätts till true när entiteten ska tas bort ur världen.</summary>
    public bool Removed { get; set; }

    /// <summary>Saker längre ner på skärmen ritas sist, så att de hamnar "framför".</summary>
    public virtual float SortY => Position.Y;

    public virtual void Update(GameWorld world, float dt) { }

    public abstract void Draw(GameWorld world);
}

/// <summary>Något man kan använda med E-tangenten.</summary>
public interface IInteractable
{
    Vector2 Position { get; }

    /// <summary>Texten som visas när man står nära. Null betyder "inget att visa".</summary>
    string? GetPrompt(GameWorld world);

    /// <summary>Kan man trycka E just nu?</summary>
    bool CanUse(GameWorld world);

    /// <summary>Hur länge man måste hålla in E (0 = ett tryck räcker).</summary>
    float HoldTime => 0;

    void Interact(GameWorld world);
}
