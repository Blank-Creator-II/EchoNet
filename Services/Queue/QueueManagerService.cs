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

    private Dictionary<Guid, SongMetadata> LookUpIndex = new Dictionary<Guid, SongMetadata>();
    private List<Guid> _Queue = new List<Guid>();

    private PlayerState playerState { get; set; }

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

    private SongMetadata? FindSongById(Guid id)
    {
        return LookUpIndex.TryGetValue(id, out var song) ? song : null;
    }

    public async Task<bool> GenerateQueue()
    {
        _logger.LogInformation("Generating playback queue from song service.");
        try
        {
            List<Song> songs;
            using (var scope = _serviceProvider.CreateScope())
            {
                var songService = scope.ServiceProvider.GetRequiredService<ISongService>();
                songs = await songService.GetAllSongsAsync();
            }

            LookUpIndex.Clear();
            foreach (Song song in songs)
            {
                LookUpIndex[song.Id] = MetadataHelper.ReadSongMetadata(song);
            }
            
            _Queue = LookUpIndex.Keys.ToList();
            
            _logger.LogInformation("Successfully generated queue. Total items: {Count}", _Queue.Count);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to generate queue.");
            return false;
        }
        return true;
    }

    public List<SongMetadata> GetQueue()
    {
        return LookUpIndex.Values.ToList();
    }

    public void SortQueue(QueueState orderMethod, int? seed = null)
    {
        List<SongMetadata> orderedList = LookUpIndex.Values.ToList();
        
        _logger.LogDebug("Sorting queue. SortMethod: {OrderMethod}, HasSeed: {HasSeed}", orderMethod, seed.HasValue);

        switch (orderMethod)
        {
            case QueueState.Random:
                Random ran = seed.HasValue ? new Random(seed.Value) : new Random();
                int n = orderedList.Count;
                while (n > 1)
                {
                    n--;
                    int k = ran.Next(n + 1);
                    SongMetadata value = orderedList[k];
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

        _Queue = orderedList.Select(s => s.Id).ToList();
        
        _logger.LogInformation("Successfully sorted queue by: {OrderMethod}", orderMethod);
    }
    
    public void OnSongEnd(object? sender, EventArgs e)
    {
        _logger.LogDebug("LibVLC event recived: Executing queue manger for next task.");
        // Safe offloading strictly for the LibVLC native thread deadlock
        Task.Run(async () => await PlayTrackAsync(PlaybackDirection.AutoEvent));
    }

    public async Task PlayNextAsync()
    {
        _logger.LogDebug("Manual request received: Skipping to next track.");
        await PlayTrackAsync(PlaybackDirection.Forward);
    }

    public async Task PlayPreviousAsync()
    {
        _logger.LogDebug("Manual request received: Skipping to previous track.");
        await PlayTrackAsync(PlaybackDirection.Backward);
    }

    private async Task PlayTrackAsync(PlaybackDirection direction)
    {
        try
        {
            var audio = _serviceProvider.GetRequiredService<IAudioService>();
            Guid currentId = audio.CurrentSongID;

            int currentIndex = _Queue.IndexOf(currentId);
            if (currentIndex == -1)
            {
                _logger.LogWarning("Playback navigation halted: Current song ID {SongId} was not found in active queue.", currentId);
                return;
            }

            int targetIndex = currentIndex;

            // Handle UI Forced Skips vs Automated Ending Rules
            if (direction == PlaybackDirection.AutoEvent && playerState.changeState == ChangeState.LoopOnce)
            {
                _logger.LogInformation("Playback loop condition matched: [LoopOnce]. Re-playing song of index: {CurrentIndex}", currentIndex);
                targetIndex = currentIndex;
            }
            else if (direction == PlaybackDirection.Backward)
            {
                targetIndex = currentIndex - 1;
                
                if (targetIndex < 0)
                {
                    targetIndex = _Queue.Count - 1;
                    _logger.LogDebug("Navigation index wrapped around to end of song queue: Index {TargetIndex}", targetIndex);
                }
            }
            else // PlaybackDirection.Forward OR (AutoEvent with NoLoop/Loop)
            {
                targetIndex = currentIndex + 1;

                if (targetIndex >= _Queue.Count)
                {
                    if (playerState.changeState == ChangeState.Loop || direction != PlaybackDirection.AutoEvent) 
                    {
                        targetIndex = 0; 
                        _logger.LogDebug("Navigation index reset back to start: Index 0. LoopState: {LoopState}", playerState.changeState);
                    }
                    else 
                    {
                        _logger.LogInformation("Reached the end of the playback queue");
                        return; 
                    }
                }
            }
            
            Guid nextSongId = _Queue[targetIndex];
            SongMetadata? nextSong = FindSongById(nextSongId);

            if (nextSong != null)
            {
                _logger.LogInformation("Transitioning to next song. Direction: {Direction}, Index: {OldIndex} -> {NewIndex}, Title: {SongTitle}", direction, currentIndex, targetIndex, nextSong.Title);

                audio.CurrentSongID = nextSongId; 
                await audio.LoadAsync(nextSong.FilePath); 
                await audio.PlayAsync(nextSong);
            }
            else
            {
                _logger.LogError("Expected song at index {TargetIndex} but object was null.", targetIndex);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogCritical(ex, "Playback navigation failed.");
        }
    }
}