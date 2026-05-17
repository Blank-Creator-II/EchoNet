using LibVLCSharp.Shared;

namespace EchoNet.Services;

public interface IAudioService
{
    Task LoadAsync(string filePath);
    Task PlayAsync();
    void Pause();
    void Stop();
    void SetVolume(int volume);
    void Seek(TimeSpan position);
    string? CurrentSong { get; }
    TimeSpan CurrentTime { get; }
    TimeSpan Duration { get; }
    int Volume { get; }
    bool IsPlaying { get; }
}