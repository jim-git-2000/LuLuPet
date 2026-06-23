using LuluPet.Core.Behavior;
using LuluPet.Core.Dialogues;
using Xunit;

namespace LuluPet.Core.Tests;

public sealed class DialogueLineProviderTests
{
    [Fact]
    public void LoadsLines_FromObjectJson()
    {
        var path = CreateTempFile("""
            {
              "lines": [
                "hello",
                "  ",
                "hello",
                "world"
              ]
            }
            """);

        var provider = new DialogueLineProvider(path, fallbackLines: new[] { "fallback" });

        Assert.Equal(new[] { "hello", "world" }, provider.Lines);
    }

    [Fact]
    public void LoadsLines_FromArrayJson()
    {
        var path = CreateTempFile("""
            [
              "first",
              "second"
            ]
            """);

        var provider = new DialogueLineProvider(path, fallbackLines: new[] { "fallback" });

        Assert.Equal(new[] { "first", "second" }, provider.Lines);
    }

    [Fact]
    public void UsesFallback_WhenJsonIsInvalid()
    {
        var path = CreateTempFile("{ invalid json");

        var provider = new DialogueLineProvider(path, fallbackLines: new[] { "fallback" });

        Assert.Equal(new[] { "fallback" }, provider.Lines);
    }

    [Fact]
    public void GetRandomLine_UsesInjectedRandomIndex()
    {
        var path = CreateTempFile("""
            {
              "lines": [
                "first",
                "second"
              ]
            }
            """);
        var provider = new DialogueLineProvider(
            path,
            random: new FixedIndexRandom(1),
            fallbackLines: new[] { "fallback" });

        var line = provider.GetRandomLine();

        Assert.Equal("second", line);
    }

    private static string CreateTempFile(string contents)
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "lines.json");
        File.WriteAllText(path, contents);
        return path;
    }

    private sealed class FixedIndexRandom : IPetRandom
    {
        private readonly int _index;

        public FixedIndexRandom(int index)
        {
            _index = index;
        }

        public double NextDouble()
        {
            return 0;
        }

        public int NextInt(int minValue, int maxValue)
        {
            return Math.Clamp(_index, minValue, maxValue - 1);
        }
    }
}
