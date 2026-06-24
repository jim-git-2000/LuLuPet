using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using LuluPet.Core.Desktop;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace LuluPet.App.Services;

public sealed class WpfDesktopBoundsProvider : IDesktopBoundsProvider
{
    private readonly Window _window;

    public WpfDesktopBoundsProvider(Window window)
    {
        _window = window;
    }

    public IReadOnlyList<DesktopBounds> GetScreenBounds()
    {
        var bounds = Forms.Screen.AllScreens
            .Select(screen => ToDesktopBounds(screen.Bounds))
            .Where(static bounds => !bounds.IsEmpty)
            .ToArray();

        if (bounds.Length > 0)
        {
            return bounds;
        }

        var fallback = SystemParameters.WorkArea;
        return new[]
        {
            DesktopBounds.FromEdges(fallback.Left, fallback.Top, fallback.Right, fallback.Bottom)
        };
    }

    public DesktopBounds GetVirtualBounds()
    {
        var virtualBounds = DesktopBounds.Union(GetScreenBounds());
        if (!virtualBounds.IsEmpty)
        {
            return virtualBounds;
        }

        var fallback = SystemParameters.WorkArea;
        return DesktopBounds.FromEdges(fallback.Left, fallback.Top, fallback.Right, fallback.Bottom);
    }

    public DesktopBounds GetBoundsForPoint(double x, double y)
    {
        var devicePoint = ToDevicePoint(x, y);
        var screen = Forms.Screen.FromPoint(new Drawing.Point(
            checked((int)Math.Round(devicePoint.X)),
            checked((int)Math.Round(devicePoint.Y))));

        return ToDesktopBounds(screen.Bounds);
    }

    private DesktopBounds ToDesktopBounds(Drawing.Rectangle rectangle)
    {
        var topLeft = FromDevicePoint(rectangle.Left, rectangle.Top);
        var bottomRight = FromDevicePoint(rectangle.Right, rectangle.Bottom);
        return DesktopBounds.FromEdges(
            topLeft.X,
            topLeft.Y,
            bottomRight.X,
            bottomRight.Y);
    }

    private System.Windows.Point ToDevicePoint(double x, double y)
    {
        return GetTransformToDevice().Transform(new System.Windows.Point(x, y));
    }

    private System.Windows.Point FromDevicePoint(double x, double y)
    {
        return GetTransformFromDevice().Transform(new System.Windows.Point(x, y));
    }

    private Matrix GetTransformToDevice()
    {
        return PresentationSource.FromVisual(_window)?.CompositionTarget?.TransformToDevice
            ?? Matrix.Identity;
    }

    private Matrix GetTransformFromDevice()
    {
        return PresentationSource.FromVisual(_window)?.CompositionTarget?.TransformFromDevice
            ?? Matrix.Identity;
    }
}
