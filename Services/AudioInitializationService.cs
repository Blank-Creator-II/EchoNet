using Microsoft.Extensions.Hosting;
using EchoNet.Utils;

namespace EchoNet.Services;

public class AudioInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAudioService _audio;
    private readonly AppDataJsonReader _appDataReader;
    private readonly ILogger<AudioInitializationService> _logger;

    public AudioInitializationService(IServiceProvider serviceProvider,IAudioService audio, AppDataJsonReader appDataReader, ILogger<AudioInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _audio = audio;
        _appDataReader = appDataReader;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running background VLC configuration");

        var song = _appDataReader.Current.songMetadata;
        var songPath = song.FilePath;
        var position = _appDataReader.Current.Position;
        var volume = _appDataReader.Current.Volume;
        var songId = song.Id;

        if (!string.IsNullOrEmpty(songPath))
        {
            await _audio.LoadAsync(songPath, position);
            _audio.SetSongMetadata(song);
            _audio.CurrentSongID = songId;
            _audio.SetVolume(volume);
            _logger.LogInformation("Previous session successfully restored.");
        }
        else
        {
            _logger.LogInformation("No previous song found to pre-load.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}