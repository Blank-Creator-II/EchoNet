using Microsoft.Extensions.Hosting;
using EchoNet.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EchoNet.Services;

public class AppInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAudioService _audio;
    private readonly IQueueManagerService _queueManager;
    private readonly AppDataJsonReader _appDataReader;
    private readonly ILogger<AppInitializationService> _logger;
    private readonly IThemeService _themeService;
    private readonly AppStateContainer _stateContainer;

    public AppInitializationService(IServiceProvider serviceProvider, IAudioService audio, AppDataJsonReader appDataReader, ILogger<AppInitializationService> logger, IQueueManagerService queueManager, IThemeService themeService, AppStateContainer stateContainer)
    {
        _serviceProvider = serviceProvider;
        _audio = audio;
        _queueManager = queueManager;
        _appDataReader = appDataReader;
        _logger = logger;
        _themeService = themeService;
        _stateContainer = stateContainer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting EchoNet initialization sequence.");

        try
        {
            _logger.LogInformation("Running background Theme configuration");
            _themeService.SetTheme(_appDataReader.Current.Theme);

            _logger.LogInformation("Running background VLC configuration");
            var song = _appDataReader.Current.song;
            var songPath = song.FilePath;
            var position = _appDataReader.Current.Position;
            var volume = _appDataReader.Current.Volume;
            var songId = song.Id;
            var playerState = _appDataReader.Current.playerState;
            var seed = _appDataReader.Current.ShuffleSeed;

            if (!string.IsNullOrEmpty(songPath))
            {
                _logger.LogInformation("Restoring previous playback session. SongId: {SongId}, Position: {Position}s, Volume: {Volume}%", songId, position, volume);
                
                await _audio.LoadAsync(songPath, position);
                _audio.SetSong(song);
                _audio.CurrentSongID = songId;
                _audio.SetVolume(volume);
            }
            else
            {
                _logger.LogWarning("No previous song found to pre-load.");
            }

            _logger.LogInformation("Running background Queue Manager configuration with state: Sort By {QueueState} (Seed: {Seed})", playerState.queueState, seed);

            _queueManager.SetPlayerState(playerState);
            // fetch and build the collection baseline first
            await _queueManager.GenerateQueue();
            // now sort or seed-shuffle the freshly loaded items
            _queueManager.SortQueue(playerState.queueState, seed);

            // -- loading finished! Mark as ready --
            _logger.LogInformation("Previous session successfully restored.");
            _stateContainer.MarkAsReady();
            _logger.LogInformation("Application is fully initialized and READY.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Application failed to complete initialization sequence.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EchoNet background initialization service is shutting down.");
        return Task.CompletedTask;
    }
}