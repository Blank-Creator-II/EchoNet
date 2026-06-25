using EchoNet.Models;
using EchoNet.ViewModels;
using EchoNet.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoNet.Services;

public class QueueManagerService : IQueueManagerService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueueManagerService> _logger;

    private Dictionary<Guid, Song> localLookUpIndex = new Dictionary<Guid, Song>();
    private Dictionary<Guid, Song> remoteLookUpIndex = new Dictionary<Guid, Song>();
    private List<Guid> localQueue = new List<Guid>();
    private List<Guid> remoteQueue = new List<Guid>();

    private PlayerState playerState { get; set; }
    private PlaybackDirection _lastDirection = PlaybackDirection.Forward;

    public QueueManagerService(IServiceProvider serviceProvider, ILogger<QueueManagerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        playerState = new PlayerState{changeState = ChangeState.NoLoop, queueState = QueueState.AZ};
    }

    public PlayerState GetPlayerState()
    {
        return playerState;
    }

    public void SetPlayerState(PlayerState _playerState)
    {
        playerState = _playerState; 
    }

    public Song? FindSongByIdFromQueue(Guid id, QueueType type)
    {
        if (type == QueueType.Local) {return localLookUpIndex.TryGetValue(id, out var song) ? song : null;}
        else {return remoteLookUpIndex.TryGetValue(id, out var song) ? song : null;}
    }

    public async Task<bool> GenerateQueue(QueueType type, List<Song>? songs = null)
    {
        _logger.LogInformation("Generating playback [{QueueType}] queue from song service.", type);
        try
        {
            if (type == QueueType.Local)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var songService = scope.ServiceProvider.GetRequiredService<ISongService>();
                    songs = await songService.GetAllSongsAsync();
                }

                localLookUpIndex.Clear();
                foreach (Song song in songs)
                {
                    localLookUpIndex[song.Id] = song;
                }
                
                localQueue = localLookUpIndex.Keys.ToList();
                
                _logger.LogInformation("Successfully generated [Local] queue. Total items: {Count}", localQueue.Count);   
            }
            else if (songs is not null)
            {
                remoteLookUpIndex.Clear();
                foreach (Song song in songs)
                {
                    remoteLookUpIndex[song.Id] = song;
                }
                
                remoteQueue = remoteLookUpIndex.Keys.ToList();
                
                _logger.LogInformation("Successfully generated [Remote] queue. Total items: {Count}", remoteQueue.Count);
            }
            else
            {
                _logger.LogError("Failed to generate queue: Incorrect queue type requested");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to generate queue.");
            return false;
        }
        return true;
    }

    public List<Song> GetQueue(QueueType type)
    {
        if (type == QueueType.Local) {return localQueue.Where(id => localLookUpIndex.ContainsKey(id)).Select(id => localLookUpIndex[id]).ToList();}
        else {return remoteQueue.Where(id => remoteLookUpIndex.ContainsKey(id)).Select(id => remoteLookUpIndex[id]).ToList();}
    }

    public void SortQueue(QueueType type, QueueState orderMethod, int? seed = null)
    {
        List<Song> orderedList = type == QueueType.Local ? localLookUpIndex.Values.ToList() : remoteLookUpIndex.Values.ToList();
        
        _logger.LogDebug("Sorting [{QueueType}] queue. SortMethod: {OrderMethod}, HasSeed: {HasSeed}", type, orderMethod, seed.HasValue);

        switch (orderMethod)
        {
            case QueueState.Random:
                Random ran = seed.HasValue ? new Random(seed.Value) : new Random();
                int n = orderedList.Count;
                while (n > 1)
                {
                    n--;
                    int k = ran.Next(n + 1);
                    Song value = orderedList[k];
                    orderedList[k] = orderedList[n];
                    orderedList[n] = value;
                }
                break;
            case QueueState.Newest:
                orderedList = orderedList.OrderByDescending(s => s.CreatedAt).ToList();
                break;
            case QueueState.Oldest:
                orderedList = orderedList.OrderBy(s => s.CreatedAt).ToList();
                break;
            case QueueState.AZ:
                orderedList = orderedList.OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase).ToList();
                break;
            case QueueState.ZA:
                orderedList = orderedList.OrderByDescending(s => s.Title, StringComparer.OrdinalIgnoreCase).ToList();
                break;
            default:
                orderedList = orderedList.OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase).ToList();
                break;
        }

        if (type == QueueType.Local) {localQueue = orderedList.Select(s => s.Id).ToList();}
        else {remoteQueue = orderedList.Select(s => s.Id).ToList();}
        
        _logger.LogInformation("Successfully sorted queue by: {OrderMethod}", orderMethod);
    }
    
    public void OnSongEnd(object? sender, EventArgs e)
    {
        var audio = _serviceProvider.GetRequiredService<IAudioService>();
        _logger.LogDebug("LibVLC event recived: Executing queue manger for next task.");
        // Safe offloading strictly for the LibVLC native thread deadlock
        Task.Run(async () => await PlayTrackAsync(PlaybackDirection.AutoEvent, audio.IsPlayingLocalSong ? QueueType.Local : QueueType.Remote));
    }

    public void OnSongError(object? sender, EventArgs e)
    {
        var audio = _serviceProvider.GetRequiredService<IAudioService>();
        _logger.LogError("LibVLC native playback error encountered. Auto-recovering queue...");
        
        // If the user was trying to go backward, maintain that momentum past the broken song (it always go back on other apps T-T)
        PlaybackDirection recoveryDirection = (_lastDirection == PlaybackDirection.Backward) ? PlaybackDirection.Backward : PlaybackDirection.Forward;

        _logger.LogInformation("Error recovery directing queue: {Direction}", recoveryDirection);

        Task.Run(async () => await PlayTrackAsync(recoveryDirection, audio.IsPlayingLocalSong ? QueueType.Local : QueueType.Remote));
    }

    public async Task PlayNextAsync()
    {
        var audio = _serviceProvider.GetRequiredService<IAudioService>();
        _logger.LogDebug("Manual request received: Skipping to next track.");
        await PlayTrackAsync(PlaybackDirection.Forward, audio.IsPlayingLocalSong ? QueueType.Local : QueueType.Remote);
    }

    public async Task PlayPreviousAsync()
    {
        var audio = _serviceProvider.GetRequiredService<IAudioService>();
        _logger.LogDebug("Manual request received: Skipping to previous track.");
        await PlayTrackAsync(PlaybackDirection.Backward, audio.IsPlayingLocalSong ? QueueType.Local : QueueType.Remote);
    }

    private async Task PlayTrackAsync(PlaybackDirection direction, QueueType type)
    {
        _lastDirection = direction;
        
        var _Queue = type == QueueType.Local ? localQueue : remoteQueue;
        
        if (_Queue.Count == 0)
        {
            _logger.LogWarning("Playback navigation halted: The [{QueueType}] queue is empty.", type);
            return;
        }

        var audio = _serviceProvider.GetRequiredService<IAudioService>();
        Guid currentId = audio.CurrentSongID;
        int currentIndex = _Queue.IndexOf(currentId);

        // If current song isn't found, default to start of the queue
        if (currentIndex == -1)
        {
            currentIndex = 0;
        }

        int targetIndex = currentIndex;

        // Calculate initial target index based on intended direction
        if (direction == PlaybackDirection.AutoEvent && playerState.changeState == ChangeState.LoopOnce)
        {
            _logger.LogInformation("Playback loop condition matched: [LoopOnce]. Re-playing current index.");
        }
        else if (direction == PlaybackDirection.Backward)
        {
            targetIndex = currentIndex - 1;
            if (targetIndex < 0) targetIndex = _Queue.Count - 1;
        }
        else // Forward or AutoEvent
        {
            targetIndex = currentIndex + 1;
            if (targetIndex >= _Queue.Count)
            {
                if (playerState.changeState == ChangeState.Loop || direction != PlaybackDirection.AutoEvent)
                {
                    targetIndex = 0;
                }
                else
                {
                    _logger.LogInformation("Reached the end of the playback queue.");
                    return;
                }
            }
        }

        int step = (direction == PlaybackDirection.Backward) ? -1 : 1;
        int attempts = 0;
        int maxAttempts = _Queue.Count;
        bool playbackSuccessful = false;

        // Loop through the queue until a track plays successfully or we run out of tracks 
        while (!playbackSuccessful && attempts < maxAttempts)
        {
            Guid nextSongId = _Queue[targetIndex];
            Song? nextSong = FindSongByIdFromQueue(nextSongId, type);

            if (nextSong != null)
            {
                try
                {
                    _logger.LogInformation("Attempting to play track. Index: {Index}, Title: {Title}", targetIndex, nextSong.Title);
                    
                    if (type == QueueType.Local) 
                    {
                        await audio.LoadAsync(nextSong.FilePath);
                    }
                    else 
                    {
                        await audio.LoadRemoteAsync($"{nextSong.FilePath}/stream/{nextSong.Id}");
                    }
                    
                    await audio.PlayAsync(nextSong);

                    audio.CurrentSongID = nextSongId;
                    playbackSuccessful = true; // Exits the loop safely
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load track '{Title}' at index {Index}. Host might be offline. Skipping...", nextSong.Title, targetIndex);
                    
                    // Maintain original navigation direction when skipping bad tracks
                    targetIndex = (targetIndex + step + _Queue.Count) % _Queue.Count;
                    attempts++;
                }
            }
            else
            {
                _logger.LogWarning("Expected song at index {Index} was null. Skipping...", targetIndex);
                targetIndex = (targetIndex + step + _Queue.Count) % _Queue.Count;
                attempts++;
            }
        }

        if (!playbackSuccessful)
        {
            _logger.LogCritical("Playback stopped: All tracks in the queue failed to load or devices are completely unreachable.");
        }
    }
}