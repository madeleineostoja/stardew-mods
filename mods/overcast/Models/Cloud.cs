using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Overcast.Models;

internal sealed class Cloud
{
    private readonly float baseWidth;
    private readonly float baseHeight;

    public Cloud(Texture2D texture, Vector2 position, float variation, bool flipHorizontally, Vector2 velocity, bool isSecondary)
    {
        Texture = texture;
        baseWidth = texture.Width * variation;
        baseHeight = texture.Height * variation;
        Position = position;
        Variation = variation;
        FlipHorizontally = flipHorizontally;
        Velocity = velocity;
        IsSecondary = isSecondary;
    }

    public Texture2D Texture { get; }
    public Vector2 Position { get; private set; }
    public float Variation { get; }
    public bool FlipHorizontally { get; }
    public Vector2 Velocity { get; }
    public bool IsSecondary { get; }

    public void Move(float elapsedSeconds, float speed)
    {
        Position += Velocity * elapsedSeconds * speed;
    }

    public WorldBounds GetBounds(float globalScale)
    {
        return new WorldBounds(Position.X, Position.Y, baseWidth * globalScale, baseHeight * globalScale);
    }
}
