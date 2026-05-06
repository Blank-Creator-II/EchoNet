using LibVLCSharp.Shared;

namespace EchoNet.Services;

public class VlcAudioService : IAudioService, IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;
    private readonly object _lock = new();
    private readonly ILogger<VlcAudioService> _logger;

    public string? CurrentSong { get; private set; }
    public bool IsPlaying => _player.IsPlaying;

    public VlcAudioService(ILogger<VlcAudioService> logger)
    {
        _logger = logger;

        _libVlc = new LibVLC();
        _player = new MediaPlayer(_libVlc);

        // Defaults
        _player.Volume = 100;

        _logger.LogInformation("VLC Audio Service initialized");
    }

    public Task LoadAsync(string filePath)
    {
        lock (_lock)
        {
            _logger.LogInformation($"Loading track: {filePath}");

            CurrentSong = filePath;
            using var media = new Media(_libVlc, filePath, FromType.FromPath);
            _player.Media = media;
        }

        return Task.CompletedTask;
    }
    public Task PlayAsync()
    {
        lock (_lock)
        {
            _logger.LogInformation("Play Triggered");
            _player.Play();
        }

        return Task.CompletedTask;
    }

    public void Pause()
    {
        lock (_lock)
        {
            _logger.LogInformation($"Song {(_player.IsPlaying ? "Paused" : "Playing")}");
            _player.Pause();
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _logger.LogInformation($"Song Stoped");
            _player.Stop();
        }
    }

    public void SetVolume(int volume)
    {
        lock (_lock)
        {
            _player.Volume = Math.Clamp(volume, 0, 100);
        }
    }

    public void Seek(TimeSpan position)
    {
        lock (_lock)
        {
            if (_player.Length > 0)
            {
                var ratio = (double)position.TotalMilliseconds / _player.Length;
                _player.Position = (float)Math.Clamp(ratio, 0.0, 1.0);
            }
        }
    }

    public void Dispose()
    {
        _player.Dispose();
        _libVlc.Dispose();
    }
}