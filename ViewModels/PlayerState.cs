namespace EchoNet.ViewModels;

public sealed class PlayerState
{
    public ChangeState changeState { get; set; }
    public QueueState queueState { get; set; }
};

public enum ChangeState
{
    Loop,
    LoopOnce,
    NoLoop,
}

public enum QueueState
{
    Random,
    Newest,
    Oldest,
    AZ,
    ZA
}

public enum PlaybackDirection
{
    Forward,
    Backward,
    AutoEvent // Specific to LibVLC's EndReached event
}

