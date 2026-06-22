using LuluPet.Core;
using Xunit;

namespace LuluPet.Core.Tests;

public sealed class ProjectInfoTests
{
    [Fact]
    public void ProductName_IsLuluPet()
    {
        Assert.Equal("LuluPet", ProjectInfo.ProductName);
    }
}
