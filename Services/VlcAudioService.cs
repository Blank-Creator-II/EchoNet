using EchoNet.ViewModels;
using LibVLCSharp.Shared;

namespace EchoNet.Services;

public class VlcAudioService : IAudioService, IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;
    private readonly ILogger<VlcAudioService> _logger;

    private Media? _currentMedia;

    public Guid? CurrentSongID { get; set; }
    private SongMetadata _currentSongMetadata = new SongMetadata{};

    public TimeSpan Duration => _player.Length > 0 ? TimeSpan.FromMilliseconds(_player.Length) : TimeSpan.Zero;

    public TimeSpan CurrentTime => _player.Time > 0 ? TimeSpan.FromMilliseconds(_player.Time) : TimeSpan.Zero;
    public int Volume => _player.Volume;
    public bool IsPlaying => _player.IsPlaying;
    public bool IsSeekable => _player.IsSeekable;

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

    public void SetSongMetadata(SongMetadata song)
    {
        if (_currentSongMetadata == song) {return;}
        _currentSongMetadata = song;
    }

    public SongMetadata GetSongMetadata()
    {
        return _currentSongMetadata;
    }

    public Task LoadAsync(string filePath, TimeSpan? startTime = null)
    {
        _logger.LogInformation("Loading song: {FilePath}", filePath);

        _currentMedia?.Dispose();

        _currentMedia = new Media(_libVlc, filePath, FromType.FromPath);

        // If we have a start time, add it as a media option (in seconds)
        if (startTime.HasValue && startTime.Value.TotalSeconds > 0)
        {
            var seconds = (int)startTime.Value.TotalSeconds;
            _currentMedia.AddOption($":start-time={seconds}");
        }

        _player.Media = _currentMedia;

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