using LuluPet.Core.Desktop;
using Xunit;

namespace LuluPet.Core.Tests;

public sealed class P15DesktopBoundsTests
{
    [Fact]
    public void DesktopBounds_Union_AllowsNegativeMonitorCoordinates()
    {
        var bounds = DesktopBounds.Union(new[]
        {
            DesktopBounds.FromEdges(0, 0, 1920, 1080),
            DesktopBounds.FromEdges(-1280, 0, 0, 1024),
            DesktopBounds.FromEdges(0, -900, 1600, 0)
        });

        Assert.Equal(-1280, bounds.Left);
        Assert.Equal(-900, bounds.Top);
        Assert.Equal(1920, bounds.Right);
        Assert.Equal(1080, bounds.Bottom);
    }

    [Fact]
    public void DesktopBounds_GetMovementBounds_KeepsWindowInsideVirtualDesktop()
    {
        var bounds = DesktopBounds.FromEdges(-1280, 0, 1920, 1080);

        var movementBounds = bounds.GetMovementBounds(windowWidth: 340, windowHeight: 390);

        Assert.Equal(-1280, movementBounds.Left);
        Assert.Equal(0, movementBounds.Top);
        Assert.Equal(1580, movementBounds.Right);
        Assert.Equal(690, movementBounds.Bottom);
    }

    [Fact]
    public void MovementBounds_ClampsNegativeAndPositiveCoordinates()
    {
        var movementBounds = new MovementBounds(-1280, -900, 1580, 690);

        Assert.Equal(-1280, movementBounds.ClampLeft(-2000));
        Assert.Equal(1580, movementBounds.ClampLeft(2000));
        Assert.Equal(-900, movementBounds.ClampTop(-1200));
        Assert.Equal(690, movementBounds.ClampTop(1200));
    }
}
