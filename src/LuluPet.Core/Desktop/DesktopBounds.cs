using System;
using System.Collections.Generic;
using System.Linq;

namespace LuluPet.Core.Desktop;

public readonly record struct DesktopBounds(double Left, double Top, double Right, double Bottom)
{
    public double Width => Math.Max(0, Right - Left);

    public double Height => Math.Max(0, Bottom - Top);

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public static DesktopBounds FromEdges(double left, double top, double right, double bottom)
    {
        return new DesktopBounds(
            Math.Min(left, right),
            Math.Min(top, bottom),
            Math.Max(left, right),
            Math.Max(top, bottom));
    }

    public static DesktopBounds Union(IEnumerable<DesktopBounds> bounds)
    {
        using var enumerator = bounds.Where(static item => !item.IsEmpty).GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return new DesktopBounds(0, 0, 0, 0);
        }

        var left = enumerator.Current.Left;
        var top = enumerator.Current.Top;
        var right = enumerator.Current.Right;
        var bottom = enumerator.Current.Bottom;

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            left = Math.Min(left, current.Left);
            top = Math.Min(top, current.Top);
            right = Math.Max(right, current.Right);
            bottom = Math.Max(bottom, current.Bottom);
        }

        return new DesktopBounds(left, top, right, bottom);
    }

    public MovementBounds GetMovementBounds(double windowWidth, double windowHeight)
    {
        var normalizedWidth = Math.Max(0, windowWidth);
        var normalizedHeight = Math.Max(0, windowHeight);
        return new MovementBounds(
            Left,
            Top,
            Math.Max(Left, Right - normalizedWidth),
            Math.Max(Top, Bottom - normalizedHeight));
    }
}
