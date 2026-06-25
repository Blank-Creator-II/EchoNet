using EchoNet.Models;
using LibVLCSharp.Shared;

namespace EchoNet.Services;

public interface IAudioService
{
    public void SetSong(Song song);
    public Song GetSong();
    Task LoadAsync(string filePath, TimeSpan? startTime = null);
    Task LoadRemoteAsync(string streamUrl, TimeSpan? startTime = null);
    Task PlayAsync(Song? song = null);
    void Pause();
    void Stop();
    void SetVolume(int volume);
    void Seek(TimeSpan position);
    Guid CurrentSongID { get; set; }
    bool IsPlayingLocalSong { get; }
    TimeSpan CurrentTime { get; }
    TimeSpan Duration { get; }
    int Volume { get; }
    bool IsPlaying { get; }
    bool IsSeekable { get; }
}