namespace EchoNet.Services;

public class AppStateContainer
{
    public bool IsReady { get; private set; } = false;

    public void MarkAsReady()
    {
        IsReady = true;
    }
}