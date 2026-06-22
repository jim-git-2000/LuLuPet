namespace LuluPet.Core.Animation;

public sealed class AnimationFrameChangedEventArgs : EventArgs
{
    public AnimationFrameChangedEventArgs(string actionName, int frameIndex, string framePath)
    {
        ActionName = actionName;
        FrameIndex = frameIndex;
        FramePath = framePath;
    }

    public string ActionName { get; }

    public int FrameIndex { get; }

    public string FramePath { get; }
}

