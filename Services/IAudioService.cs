using EchoNet.ViewModels;
using LibVLCSharp.Shared;

namespace EchoNet.Services;

public interface IAudioService
{
    public void SetSongMetadata(SongMetadata song);
    public SongMetadata GetSongMetadata();
    Task LoadAsync(string filePath, TimeSpan? startTime = null);
    Task PlayAsync();
    void Pause();
    void Stop();
    void SetVolume(int volume);
    void Seek(TimeSpan position);
    Guid? CurrentSongID { get; set; }
    TimeSpan CurrentTime { get; }
    TimeSpan Duration { get; }
    int Volume { get; }
    bool IsPlaying { get; }
    bool IsSeekable { get; }
}