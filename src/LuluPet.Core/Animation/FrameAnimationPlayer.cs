namespace LuluPet.Core.Animation;

public sealed class FrameAnimationPlayer
{
    private readonly Dictionary<string, AnimationAction> _actions = new(StringComparer.OrdinalIgnoreCase);
    private TimeSpan _accumulated;
    private AnimationAction? _currentAction;

    public string CurrentAction => _currentAction?.Name ?? string.Empty;

    public int CurrentFrameIndex { get; private set; }

    public string CurrentFramePath => _currentAction?.FramePaths[CurrentFrameIndex] ?? string.Empty;

    public bool IsPlaying { get; private set; }

    public event EventHandler<AnimationFrameChangedEventArgs>? FrameChanged;

    public void LoadAction(string actionName, IReadOnlyList<string> framePaths, int fps, bool loop = true)
    {
        if (string.IsNullOrWhiteSpace(actionName))
        {
            throw new ArgumentException("Action name is required.", nameof(actionName));
        }

        if (framePaths.Count == 0)
        {
            throw new ArgumentException("At least one frame is required.", nameof(framePaths));
        }

        if (fps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fps), "FPS must be greater than zero.");
        }

        _actions[actionName] = new AnimationAction(actionName, framePaths.ToArray(), fps, loop);
    }

    public bool TryLoadActionFromDirectory(string actionName, string directoryPath, int fps, bool loop = true)
    {
        var frames = AnimationFrameLoader.LoadPngFrames(directoryPath);

        if (frames.Count == 0)
        {
            return false;
        }

        LoadAction(actionName, frames, fps, loop);
        return true;
    }

    public bool CanPlay(string actionName)
    {
        return _actions.ContainsKey(actionName);
    }

    public void Play(string actionName)
    {
        if (!_actions.TryGetValue(actionName, out var action))
        {
            throw new InvalidOperationException($"Animation action '{actionName}' is not loaded.");
        }

        _currentAction = action;
        CurrentFrameIndex = 0;
        _accumulated = TimeSpan.Zero;
        IsPlaying = true;
        RaiseFrameChanged();
    }

    public void Stop()
    {
        IsPlaying = false;
        _accumulated = TimeSpan.Zero;
    }

    public void Tick(TimeSpan elapsed)
    {
        if (!IsPlaying || _currentAction is null || elapsed <= TimeSpan.Zero)
        {
            return;
        }

        var frameDuration = TimeSpan.FromSeconds(1d / _currentAction.Fps);
        _accumulated += elapsed;

        while (_accumulated >= frameDuration && IsPlaying)
        {
            _accumulated -= frameDuration;
            AdvanceFrame();
        }
    }

    private void AdvanceFrame()
    {
        if (_currentAction is null)
        {
            return;
        }

        var nextFrame = CurrentFrameIndex + 1;

        if (nextFrame >= _currentAction.FramePaths.Count)
        {
            if (!_currentAction.Loop)
            {
                IsPlaying = false;
                return;
            }

            nextFrame = 0;
        }

        CurrentFrameIndex = nextFrame;
        RaiseFrameChanged();
    }

    private void RaiseFrameChanged()
    {
        if (_currentAction is null)
        {
            return;
        }

        FrameChanged?.Invoke(
            this,
            new AnimationFrameChangedEventArgs(CurrentAction, CurrentFrameIndex, CurrentFramePath));
    }

    private sealed record AnimationAction(
        string Name,
        IReadOnlyList<string> FramePaths,
        int Fps,
        bool Loop);
}

