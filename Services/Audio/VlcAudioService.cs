using EchoNet.Hubs;
using EchoNet.Models;
using LibVLCSharp.Shared;
using Microsoft.AspNetCore.SignalR;

namespace EchoNet.Services;

public class VlcAudioService : IAudioService, IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;
    private readonly ILogger<VlcAudioService> _logger;
    private readonly IHubContext<AudioHub> _hubContext; // Inject SignalR Hub Context
    private readonly IQueueManagerService _queueManager;

    private Media? _currentMedia;

    public Guid CurrentSongID { get; set; }
    private Song _currentSong = new Song{};

    public TimeSpan Duration => _player.Length > 0 ? TimeSpan.FromMilliseconds(_player.Length) : TimeSpan.Zero;

    public TimeSpan CurrentTime => _player.Time > 0 ? TimeSpan.FromMilliseconds(_player.Time) : TimeSpan.Zero;
    public int Volume => _player.Volume;
    public bool IsPlaying => _player.IsPlaying;
    public bool IsSeekable => _player.IsSeekable;

    public VlcAudioService(ILogger<VlcAudioService> logger, IHubContext<AudioHub> hubContext, IQueueManagerService queueManager)
    {
        _logger = logger;
        _hubContext = hubContext;
        _queueManager = queueManager;

        Core.Initialize();

        _libVlc = new LibVLC();

        _player = new MediaPlayer(_libVlc)
        {
            Volume = 100
        };

        // Bind LibVLC events directly to SignalR Broadcasts
        _player.TimeChanged += OnPlayerTimeChanged;
        _player.Paused += OnPlayerStateChanged;
        _player.Playing += OnPlayerStateChanged;
        _player.Stopped += OnPlayerStateChanged;

        // Bind LibVLC song ending event to Queue Manager
        _player.EndReached += _queueManager.OnSongEnd;

        _logger.LogInformation("VLC Audio Service initialized with SignalR links");
    }

    private async void OnPlayerMediaChanged(Song _song)
    {
        try
        {
            // Broadcast whenever new media get's loaded
            await _hubContext.Clients.All.SendAsync("ReceiveMediaChange", new{song = _song});
            _logger.LogDebug("SignalR sent media data, ID: {ID}", _song.Id);            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send media change update via SignalR.");
        }

    }

    private async void OnPlayerTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        // Don't spam the network infinitely if nothing is playing or length is invalid
        if (_player.Length <= 0) return;

        try
        {
            // Broadcast a status payload down to ALL connected scripts over web sockets instantly
            await _hubContext.Clients.All.SendAsync("ReceiveStatus", new
            {
                id = CurrentSongID,
                currentTime = TimeSpan.FromMilliseconds(e.Time).TotalSeconds,
                duration = Duration.TotalSeconds,
                isPlaying = _player.IsPlaying,
                isSeekable = _player.IsSeekable,
                volume = _player.Volume
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send player status update via SignalR.");
        }
    }

    private async void OnPlayerStateChanged(object? sender, EventArgs e)
    {
        try
        {
            // Broadcast whenever state toggles (Play/Pause/Stop)
            await _hubContext.Clients.All.SendAsync("ReceiveStateChange", new
            {
                id = CurrentSongID,
                isPlaying = _player.IsPlaying,
                isSeekable = _player.IsSeekable
            });

            _logger.LogDebug("SignalR sent state change. IsPlaying: {IsPlaying}", _player.IsPlaying);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send state change update via SignalR.");
        }
    }

    public void SetSong(Song song)
    {
        if (_currentSong == song) {return;}
        _logger.LogInformation("Stored song metadata with ID: {SongID} Title: {Title}", song.Id, song.Title);
        _currentSong = song;
    }

    public Song GetSong()
    {
        return _currentSong;
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

    public Task PlayAsync(Song? song = null)
    {
        _logger.LogInformation("Playback play triggered.");

        _player.Play();
        OnPlayerMediaChanged(song == null ? _currentSong : song);

        return Task.CompletedTask;
    }

    public void Pause()
    {
        _logger.LogInformation("Playback pause toggled.");

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
        _player.TimeChanged -= OnPlayerTimeChanged;
        _player.Paused -= OnPlayerStateChanged;
        _player.Playing -= OnPlayerStateChanged;
        _player.Stopped -= OnPlayerStateChanged;
        _player.EndReached -= _queueManager.OnSongEnd;
        
        _currentMedia?.Dispose();
        _player.Dispose();
        _libVlc.Dispose();
    }
}