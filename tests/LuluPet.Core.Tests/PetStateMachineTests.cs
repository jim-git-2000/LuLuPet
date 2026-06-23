using LuluPet.Core.Behavior;
using Xunit;

namespace LuluPet.Core.Tests;

public sealed class PetStateMachineTests
{
    [Fact]
    public void Tick_TransitionsFromIdleToWalk_WhenWalkRollSucceeds()
    {
        var stateMachine = new PetStateMachine(
            new PetStateMachineOptions
            {
                MinIdleBeforeWalk = TimeSpan.FromSeconds(1),
                IdleDecisionInterval = TimeSpan.FromSeconds(1),
                LongIdleBeforeSleep = TimeSpan.FromMinutes(1),
                WalkProbability = 1,
                SleepProbability = 0,
                MinWalkDuration = TimeSpan.FromSeconds(2),
                MaxWalkDuration = TimeSpan.FromSeconds(2)
            },
            new SequencePetRandom(doubles: new[] { 0d }));

        stateMachine.Tick(TimeSpan.FromMilliseconds(999));

        Assert.Equal(PetState.Idle, stateMachine.CurrentState);

        stateMachine.Tick(TimeSpan.FromMilliseconds(1001));

        Assert.Equal(PetState.Walk, stateMachine.CurrentState);
        Assert.Equal(TimeSpan.Zero, stateMachine.StateElapsed);
    }

    [Fact]
    public void Tick_ReturnsFromWalkToIdle_WhenWalkDurationEnds()
    {
        var stateMachine = new PetStateMachine(
            new PetStateMachineOptions
            {
                MinIdleBeforeWalk = TimeSpan.Zero,
                IdleDecisionInterval = TimeSpan.FromSeconds(1),
                LongIdleBeforeSleep = TimeSpan.FromMinutes(1),
                WalkProbability = 1,
                SleepProbability = 0,
                MinWalkDuration = TimeSpan.FromSeconds(2),
                MaxWalkDuration = TimeSpan.FromSeconds(2)
            },
            new SequencePetRandom(doubles: new[] { 0d }));

        stateMachine.Tick(TimeSpan.FromSeconds(1));
        Assert.Equal(PetState.Walk, stateMachine.CurrentState);

        stateMachine.Tick(TimeSpan.FromMilliseconds(1999));
        Assert.Equal(PetState.Walk, stateMachine.CurrentState);

        stateMachine.Tick(TimeSpan.FromMilliseconds(1));
        Assert.Equal(PetState.Idle, stateMachine.CurrentState);
    }

    [Fact]
    public void Tick_TransitionsFromLongIdleToSleep_ThenWakesToIdle()
    {
        var stateMachine = new PetStateMachine(
            new PetStateMachineOptions
            {
                MinIdleBeforeWalk = TimeSpan.Zero,
                IdleDecisionInterval = TimeSpan.FromSeconds(1),
                LongIdleBeforeSleep = TimeSpan.FromSeconds(2),
                WalkProbability = 0,
                SleepProbability = 1,
                MinSleepDuration = TimeSpan.FromSeconds(3),
                MaxSleepDuration = TimeSpan.FromSeconds(3)
            },
            new SequencePetRandom(doubles: new[] { 0.5, 0 }));

        stateMachine.Tick(TimeSpan.FromSeconds(1));
        Assert.Equal(PetState.Idle, stateMachine.CurrentState);

        stateMachine.Tick(TimeSpan.FromSeconds(1));
        Assert.Equal(PetState.Sleep, stateMachine.CurrentState);

        stateMachine.Tick(TimeSpan.FromSeconds(3));
        Assert.Equal(PetState.Idle, stateMachine.CurrentState);
    }

    [Fact]
    public void Tick_CanSleepAfterWalking_WhenAwakeTimeExceedsSleepThreshold()
    {
        var stateMachine = new PetStateMachine(
            new PetStateMachineOptions
            {
                MinIdleBeforeWalk = TimeSpan.Zero,
                IdleDecisionInterval = TimeSpan.FromSeconds(1),
                LongIdleBeforeSleep = TimeSpan.FromSeconds(3),
                WalkProbability = 1,
                SleepProbability = 1,
                MinWalkDuration = TimeSpan.FromSeconds(1),
                MaxWalkDuration = TimeSpan.FromSeconds(1),
                MinSleepDuration = TimeSpan.FromSeconds(2),
                MaxSleepDuration = TimeSpan.FromSeconds(2)
            },
            new SequencePetRandom(doubles: new[] { 0d, 0d }));

        stateMachine.Tick(TimeSpan.FromSeconds(1));
        Assert.Equal(PetState.Walk, stateMachine.CurrentState);

        stateMachine.Tick(TimeSpan.FromSeconds(1));
        Assert.Equal(PetState.Idle, stateMachine.CurrentState);

        stateMachine.Tick(TimeSpan.FromSeconds(1));
        Assert.Equal(PetState.Sleep, stateMachine.CurrentState);
    }

    [Fact]
    public void StateChanged_RaisesTransitionSequence()
    {
        var stateMachine = new PetStateMachine(
            new PetStateMachineOptions
            {
                MinIdleBeforeWalk = TimeSpan.Zero,
                IdleDecisionInterval = TimeSpan.FromSeconds(1),
                LongIdleBeforeSleep = TimeSpan.FromMinutes(1),
                WalkProbability = 1,
                SleepProbability = 0,
                MinWalkDuration = TimeSpan.FromSeconds(1),
                MaxWalkDuration = TimeSpan.FromSeconds(1)
            },
            new SequencePetRandom(doubles: new[] { 0d }));
        var transitions = new List<(PetState Previous, PetState Current)>();
        stateMachine.StateChanged += (_, args) => transitions.Add((args.PreviousState, args.CurrentState));

        stateMachine.Tick(TimeSpan.FromSeconds(1));
        stateMachine.Tick(TimeSpan.FromSeconds(1));

        var expected = new List<(PetState Previous, PetState Current)>
        {
            (PetState.Idle, PetState.Walk),
            (PetState.Walk, PetState.Idle)
        };

        Assert.Equal(expected, transitions);
    }

    private sealed class SequencePetRandom : IPetRandom
    {
        private readonly Queue<double> _doubles;
        private readonly Queue<int> _ints;

        public SequencePetRandom(IEnumerable<double>? doubles = null, IEnumerable<int>? ints = null)
        {
            _doubles = new Queue<double>(doubles ?? Array.Empty<double>());
            _ints = new Queue<int>(ints ?? Array.Empty<int>());
        }

        public double NextDouble()
        {
            return _doubles.Count == 0 ? 0.5 : _doubles.Dequeue();
        }

        public int NextInt(int minValue, int maxValue)
        {
            if (_ints.Count == 0)
            {
                return minValue;
            }

            var value = _ints.Dequeue();
            return Math.Clamp(value, minValue, maxValue - 1);
        }
    }
}
