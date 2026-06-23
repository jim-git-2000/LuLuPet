namespace LuluPet.Core.Behavior;

public sealed class PetStateChangedEventArgs : EventArgs
{
    public PetStateChangedEventArgs(PetState previousState, PetState currentState)
    {
        PreviousState = previousState;
        CurrentState = currentState;
    }

    public PetState PreviousState { get; }

    public PetState CurrentState { get; }
}
