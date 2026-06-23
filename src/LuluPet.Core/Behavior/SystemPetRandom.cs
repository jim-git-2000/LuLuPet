namespace LuluPet.Core.Behavior;

public sealed class SystemPetRandom : IPetRandom
{
    private readonly Random _random;

    public SystemPetRandom()
        : this(Random.Shared)
    {
    }

    public SystemPetRandom(Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public double NextDouble()
    {
        return _random.NextDouble();
    }

    public int NextInt(int minValue, int maxValue)
    {
        return _random.Next(minValue, maxValue);
    }
}
