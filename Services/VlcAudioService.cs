using LibVLCSharp.Shared;

namespace EchoNet.Services;

public class VlcAudioService : IAudioService, IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;
    private readonly ILogger<VlcAudioService> _logger;

    private Media? _currentMedia;

    public string? CurrentSong { get; private set; }

    public TimeSpan Duration => _player.Length > 0 ? TimeSpan.FromMilliseconds(_player.Length) : TimeSpan.Zero;

    public TimeSpan CurrentTime => _player.Time > 0 ? TimeSpan.FromMilliseconds(_player.Time) : TimeSpan.Zero;
    public int Volume => _player.Volume;
    public bool IsPlaying => _player.IsPlaying;

    public VlcAudioService(ILogger<VlcAudioService> logger)
    {
        _logger = logger;

        Core.Initialize();

        _libVlc = new LibVLC();

        _player = new MediaPlayer(_libVlc)
        {
            Volume = 100
        };

        _logger.LogInformation("VLC Audio Service initialized");
    }

    public Task LoadAsync(string filePath)
    {
        _logger.LogInformation("Loading track: {FilePath}", filePath);

        _currentMedia?.Dispose();

        _currentMedia = new Media(_libVlc, filePath, FromType.FromPath);

        _player.Media = _currentMedia;

        CurrentSong = filePath;

        return Task.CompletedTask;
    }

    public Task PlayAsync()
    {
        _logger.LogInformation("Play triggered");

        _player.Play();

        return Task.CompletedTask;
    }

    public void Pause()
    {
        _logger.LogInformation("Toggling pause");

        _player.Pause();
    }

    public void Stop()
    {
        _logger.LogInformation("Playback stopped");

        _player.Stop();
    }

    public void SetVolume(int volume)
    {
        _player.Volume = Math.Clamp(volume, 0, 100);
    }

    public void Seek(TimeSpan position)
    {
        _player.Time = (long)position.TotalMilliseconds;
    }

    public void Dispose()
    {
        _currentMedia?.Dispose();
        _player.Dispose();
        _libVlc.Dispose();
    }
}