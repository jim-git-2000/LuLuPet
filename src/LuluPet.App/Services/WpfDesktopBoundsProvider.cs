using System.Linq;
using System.Windows;
using LuluPet.Core.Desktop;
using Forms = System.Windows.Forms;

namespace LuluPet.App.Services;

public sealed class WpfDesktopBoundsProvider : IDesktopBoundsProvider
{
    public DesktopBounds GetVirtualBounds()
    {
        var bounds = Forms.Screen.AllScreens
            .Select(static screen => ToDesktopBounds(screen.Bounds))
            .ToArray();

        var virtualBounds = DesktopBounds.Union(bounds);
        if (!virtualBounds.IsEmpty)
        {
            return virtualBounds;
        }

        var fallback = SystemParameters.WorkArea;
        return DesktopBounds.FromEdges(fallback.Left, fallback.Top, fallback.Right, fallback.Bottom);
    }

    public DesktopBounds GetBoundsForPoint(double x, double y)
    {
        var screen = Forms.Screen.FromPoint(new System.Drawing.Point(
            checked((int)Math.Round(x)),
            checked((int)Math.Round(y))));

        return ToDesktopBounds(screen.Bounds);
    }

    private static DesktopBounds ToDesktopBounds(System.Drawing.Rectangle rectangle)
    {
        return DesktopBounds.FromEdges(
            rectangle.Left,
            rectangle.Top,
            rectangle.Right,
            rectangle.Bottom);
    }
}
