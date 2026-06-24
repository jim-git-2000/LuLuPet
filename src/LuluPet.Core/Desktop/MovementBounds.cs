using System;

namespace LuluPet.Core.Desktop;

public readonly record struct MovementBounds(double Left, double Top, double Right, double Bottom)
{
    public bool Contains(double left, double top)
    {
        return left >= Left && left <= Right && top >= Top && top <= Bottom;
    }

    public double ClampLeft(double value)
    {
        return Math.Clamp(value, Left, Right);
    }

    public double ClampTop(double value)
    {
        return Math.Clamp(value, Top, Bottom);
    }
}
