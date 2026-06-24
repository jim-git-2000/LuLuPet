using System;
using System.Collections.Generic;
using System.Linq;

namespace LuluPet.Core.Desktop;

public readonly record struct DesktopBounds(double Left, double Top, double Right, double Bottom)
{
    public double Width => Math.Max(0, Right - Left);

    public double Height => Math.Max(0, Bottom - Top);

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool ContainsPoint(double x, double y)
    {
        return x >= Left && x <= Right && y >= Top && y <= Bottom;
    }

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

    public static MovementBounds GetNearestMovementBounds(
        IEnumerable<DesktopBounds> bounds,
        double windowLeft,
        double windowTop,
        double windowWidth,
        double windowHeight)
    {
        var normalizedWidth = Math.Max(0, windowWidth);
        var normalizedHeight = Math.Max(0, windowHeight);
        var candidates = bounds
            .Where(static item => !item.IsEmpty)
            .Select(item => new
            {
                Bounds = item,
                MovementBounds = item.GetMovementBounds(normalizedWidth, normalizedHeight),
                OverlapArea = item.GetIntersectionArea(
                    windowLeft,
                    windowTop,
                    windowLeft + normalizedWidth,
                    windowTop + normalizedHeight),
                DistanceSquared = item.GetDistanceSquaredToPoint(
                    windowLeft + normalizedWidth / 2,
                    windowTop + normalizedHeight / 2)
            })
            .ToArray();

        if (candidates.Length == 0)
        {
            return FromEdges(0, 0, normalizedWidth, normalizedHeight)
                .GetMovementBounds(normalizedWidth, normalizedHeight);
        }

        var selected = candidates
            .OrderByDescending(item => item.MovementBounds.Contains(windowLeft, windowTop))
            .ThenByDescending(item => item.Bounds.ContainsPoint(
                windowLeft + normalizedWidth / 2,
                windowTop + normalizedHeight / 2))
            .ThenByDescending(item => item.OverlapArea)
            .ThenBy(item => item.DistanceSquared)
            .First();

        return selected.MovementBounds;
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

    private double GetIntersectionArea(double left, double top, double right, double bottom)
    {
        var intersectionWidth = Math.Max(0, Math.Min(Right, right) - Math.Max(Left, left));
        var intersectionHeight = Math.Max(0, Math.Min(Bottom, bottom) - Math.Max(Top, top));
        return intersectionWidth * intersectionHeight;
    }

    private double GetDistanceSquaredToPoint(double x, double y)
    {
        var nearestX = Math.Clamp(x, Left, Right);
        var nearestY = Math.Clamp(y, Top, Bottom);
        var deltaX = x - nearestX;
        var deltaY = y - nearestY;
        return deltaX * deltaX + deltaY * deltaY;
    }
}
