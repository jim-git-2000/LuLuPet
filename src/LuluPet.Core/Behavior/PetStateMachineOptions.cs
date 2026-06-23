namespace LuluPet.Core.Behavior;

public sealed class PetStateMachineOptions
{
    public TimeSpan MinIdleBeforeWalk { get; init; } = TimeSpan.FromSeconds(8);

    public TimeSpan IdleDecisionInterval { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan LongIdleBeforeSleep { get; init; } = TimeSpan.FromSeconds(45);

    public double WalkProbability { get; init; } = 0.45;

    public double SleepProbability { get; init; } = 0.35;

    public TimeSpan MinWalkDuration { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan MaxWalkDuration { get; init; } = TimeSpan.FromSeconds(12);

    public TimeSpan MinSleepDuration { get; init; } = TimeSpan.FromSeconds(15);

    public TimeSpan MaxSleepDuration { get; init; } = TimeSpan.FromSeconds(30);

    public void Validate()
    {
        ThrowIfNegative(MinIdleBeforeWalk, nameof(MinIdleBeforeWalk));
        ThrowIfNotPositive(IdleDecisionInterval, nameof(IdleDecisionInterval));
        ThrowIfNegative(LongIdleBeforeSleep, nameof(LongIdleBeforeSleep));
        ThrowIfProbabilityOutOfRange(WalkProbability, nameof(WalkProbability));
        ThrowIfProbabilityOutOfRange(SleepProbability, nameof(SleepProbability));
        ThrowIfNotPositive(MinWalkDuration, nameof(MinWalkDuration));
        ThrowIfNotPositive(MaxWalkDuration, nameof(MaxWalkDuration));
        ThrowIfNotPositive(MinSleepDuration, nameof(MinSleepDuration));
        ThrowIfNotPositive(MaxSleepDuration, nameof(MaxSleepDuration));

        if (MaxWalkDuration < MinWalkDuration)
        {
            throw new ArgumentException("Max walk duration must be greater than or equal to min walk duration.");
        }

        if (MaxSleepDuration < MinSleepDuration)
        {
            throw new ArgumentException("Max sleep duration must be greater than or equal to min sleep duration.");
        }
    }

    private static void ThrowIfNegative(TimeSpan value, string parameterName)
    {
        if (value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Duration must not be negative.");
        }
    }

    private static void ThrowIfNotPositive(TimeSpan value, string parameterName)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Duration must be greater than zero.");
        }
    }

    private static void ThrowIfProbabilityOutOfRange(double value, string parameterName)
    {
        if (value < 0 || value > 1 || double.IsNaN(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Probability must be between 0 and 1.");
        }
    }
}
