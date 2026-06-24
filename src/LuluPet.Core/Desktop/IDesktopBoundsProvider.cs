namespace LuluPet.Core.Desktop;

public interface IDesktopBoundsProvider
{
    DesktopBounds GetVirtualBounds();

    DesktopBounds GetBoundsForPoint(double x, double y);
}
