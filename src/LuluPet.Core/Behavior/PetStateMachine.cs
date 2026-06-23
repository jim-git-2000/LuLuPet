namespace LuluPet.Core.Behavior;

public sealed class PetStateMachine
{
    private readonly PetStateMachineOptions _options;
    private readonly IPetRandom _random;
    private TimeSpan _awakeElapsed;
    private TimeSpan _idleDecisionElapsed;
    private TimeSpan _stateDurationLimit;

    public PetStateMachine(PetStateMachineOptions? options = null, IPetRandom? random = null)
    {
        _options = options ?? new PetStateMachineOptions();
        _options.Validate();
        _random = random ?? new SystemPetRandom();
    }

    public PetState CurrentState { get; private set; } = PetState.Idle;

    public TimeSpan StateElapsed { get; private set; }

    public event EventHandler<PetStateChangedEventArgs>? StateChanged;

    public void Tick(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return;
        }

        StateElapsed += elapsed;
        if (CurrentState != PetState.Sleep)
        {
            _awakeElapsed += elapsed;
        }

        switch (CurrentState)
        {
            case PetState.Idle:
                TickIdle(elapsed);
                break;
            case PetState.Walk:
                if (StateElapsed >= _stateDurationLimit)
                {
                    ChangeState(PetState.Idle);
                }

                break;
            case PetState.Sleep:
                if (StateElapsed >= _stateDurationLimit)
                {
                    ChangeState(PetState.Idle);
                }

                break;
        }
    }

    public void Wake()
    {
        if (CurrentState == PetState.Sleep)
        {
            ChangeState(PetState.Idle);
        }
    }

    public void ForceState(PetState state)
    {
        if (CurrentState == state)
        {
            StateElapsed = TimeSpan.Zero;
            _idleDecisionElapsed = TimeSpan.Zero;
            _stateDurationLimit = CreateStateDurationLimit(state);
            return;
        }

        ChangeState(state);
    }

    private void TickIdle(TimeSpan elapsed)
    {
        if (StateElapsed < _options.MinIdleBeforeWalk)
        {
            return;
        }

        _idleDecisionElapsed += elapsed;
        if (_idleDecisionElapsed < _options.IdleDecisionInterval)
        {
            return;
        }

        _idleDecisionElapsed = TimeSpan.Zero;

        if (_awakeElapsed >= _options.LongIdleBeforeSleep
            && _random.NextDouble() < _options.SleepProbability)
        {
            ChangeState(PetState.Sleep);
            return;
        }

        if (_random.NextDouble() < _options.WalkProbability)
        {
            ChangeState(PetState.Walk);
        }
    }

    private void ChangeState(PetState nextState)
    {
        if (CurrentState == nextState)
        {
            return;
        }

        var previousState = CurrentState;
        CurrentState = nextState;
        StateElapsed = TimeSpan.Zero;
        _idleDecisionElapsed = TimeSpan.Zero;
        _stateDurationLimit = CreateStateDurationLimit(nextState);

        if (previousState == PetState.Sleep && CurrentState == PetState.Idle)
        {
            _awakeElapsed = TimeSpan.Zero;
        }

        StateChanged?.Invoke(this, new PetStateChangedEventArgs(previousState, CurrentState));
    }

    private TimeSpan CreateStateDurationLimit(PetState state)
    {
        return state switch
        {
            PetState.Walk => NextDuration(_options.MinWalkDuration, _options.MaxWalkDuration),
            PetState.Sleep => NextDuration(_options.MinSleepDuration, _options.MaxSleepDuration),
            _ => TimeSpan.Zero
        };
    }

    private TimeSpan NextDuration(TimeSpan minDuration, TimeSpan maxDuration)
    {
        if (minDuration == maxDuration)
        {
            return minDuration;
        }

        var minMilliseconds = checked((int)Math.Ceiling(minDuration.TotalMilliseconds));
        var maxMilliseconds = checked((int)Math.Ceiling(maxDuration.TotalMilliseconds));
        return TimeSpan.FromMilliseconds(_random.NextInt(minMilliseconds, maxMilliseconds + 1));
    }
}
