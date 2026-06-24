using System.Collections.Generic;

namespace LuluPet.Core.Desktop;

public interface IDesktopBoundsProvider
{
    IReadOnlyList<DesktopBounds> GetScreenBounds();

    DesktopBounds GetVirtualBounds();

    DesktopBounds GetBoundsForPoint(double x, double y);
}
