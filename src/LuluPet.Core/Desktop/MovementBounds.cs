using System;

namespace LuluPet.Core.Desktop;

public readonly record struct MovementBounds(double Left, double Top, double Right, double Bottom)
{
    public double ClampLeft(double value)
    {
        return Math.Clamp(value, Left, Right);
    }

    public double ClampTop(double value)
    {
        return Math.Clamp(value, Top, Bottom);
    }
}
