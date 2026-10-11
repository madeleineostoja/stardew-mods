namespace Overcast.Models;

internal readonly record struct WorldBounds(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;
    public float Bottom => Y + Height;

    public bool Intersects(WorldBounds other)
    {
        return X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;
    }

    public WorldBounds? GetExposedBand(WorldBounds previous)
    {
        var left = previous.X - X;
        var right = Right - previous.Right;
        var top = previous.Y - Y;
        var bottom = Bottom - previous.Bottom;
        var horizontal = Math.Max(left, right);
        var vertical = Math.Max(top, bottom);
        if (horizontal >= vertical && horizontal > 1f)
            return right >= left
                ? new WorldBounds(previous.Right, Y, right, Height)
                : new WorldBounds(X, Y, left, Height);
        if (vertical > 1f)
            return bottom >= top
                ? new WorldBounds(X, previous.Bottom, Width, bottom)
                : new WorldBounds(X, Y, Width, top);
        return null;
    }

    public WorldBounds Inflate(float amount)
    {
        return new WorldBounds(X - amount, Y - amount, Width + (amount * 2), Height + (amount * 2));
    }
}
