namespace LuluPet.Core.Behavior;

public interface IPetRandom
{
    double NextDouble();

    int NextInt(int minValue, int maxValue);
}
