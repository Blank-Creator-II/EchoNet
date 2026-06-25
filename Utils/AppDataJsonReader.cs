using System.Text.Json;
using EchoNet.Models;
using EchoNet.Services;
using EchoNet.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace EchoNet.Utils;

public class AppDataJsonReader
{
    private readonly string _appDataPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<AppDataJsonReader> _logger;
    private readonly IAudioService _audio;
    private readonly IThemeService _themeService;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private AppData _currentData = new();
    public AppData Current => _currentData;

    public AppDataJsonReader(IWebHostEnvironment env, ILogger<AppDataJsonReader> logger, IAudioService audio, IThemeService themeService)
    {
        _logger = logger;
        _audio = audio;
        _themeService = themeService;
        _appDataPath = Path.Combine(env.WebRootPath, "data", "AppData.json");
        _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(allowIntegerValues: true) }
        };

        EnsureFileExists();
        InitializeInMemoryData();
    }

    private void EnsureFileExists()
    {
        try
        {
            var directory = Path.GetDirectoryName(_appDataPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory!);

            if (!File.Exists(_appDataPath))
            {
                var json = JsonSerializer.Serialize(new AppData(), _jsonOptions);
                File.WriteAllText(_appDataPath, json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure AppData file structure exists.");
        }
    }

    private void InitializeInMemoryData()
    {
        try
        {
            var json = File.ReadAllText(_appDataPath);
            _currentData = JsonSerializer.Deserialize<AppData>(json, _jsonOptions) ?? new AppData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AppData in memory.");
            _currentData = new AppData();
        }
    }

    public void UpdateInMemory(params (AppDataTarget Target, object DataValue)[] items)
    {
        var appId = _currentData.AppId; // never get's updated unless app data is cleared on disk
        // Default to current states rather than re-querying active services every time
        var theme = _currentData.Theme;
        var _song = _currentData.song;
        var position = _currentData.Position;
        var volume = _currentData.Volume;
        var viewType = _currentData.ViewType;
        var sortType = _currentData.SortType;
        var playerState = _currentData.playerState;
        var shuffleSeed = _currentData.ShuffleSeed;

        foreach (var (target, dataValue) in items)
        {
            switch (target)
            {
                case AppDataTarget.Theme when dataValue is string newTheme:
                    theme = newTheme;
                    break;
                case AppDataTarget.Song when dataValue is Song newSong:
                    _song = newSong;
                    break;
                case AppDataTarget.Position when dataValue is TimeSpan newPosition:
                    position = newPosition;
                    break;
                case AppDataTarget.Volume when dataValue is int newVolume:
                    volume = newVolume;
                    break;
                case AppDataTarget.ViewType when dataValue is string newViewType:
                    viewType = newViewType;
                    break;
                case AppDataTarget.SortType when dataValue is string newSortType:
                    sortType = newSortType;
                    break;
                case AppDataTarget.PlayerState when dataValue is PlayerState newPlayerState:
                    playerState = newPlayerState;
                    break;
                case AppDataTarget.ShuffleSeed when dataValue is int newShuffleSeed:
                    shuffleSeed = newShuffleSeed;
                    break;
            }
        }

        _currentData = new AppData 
        {
            AppId = appId,
            Theme = theme,
            song = _song,
            Position = position,
            Volume = volume,
            ViewType = viewType,
            SortType = sortType,
            playerState = playerState,
            ShuffleSeed = shuffleSeed,
        };
        
        _themeService.SetTheme(theme);
        _audio.SetSong(_song);
        _logger.LogDebug("AppData in memory updated successfully.");
    }

    public async Task SaveAsync(AppData? dataToSave = null)
    {
        var data = dataToSave ?? _currentData;

        // if the saved song data was a remote one replace it with a default data
        if(!data.song.IsLocal) {data.song = new Song{};}
        
        await _fileLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(_appDataPath, json);
            
            // Only overwrite local cache once disk operation succeeds
            _currentData = data; 
            _logger.LogInformation("AppData saved to disk successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save AppData to disk.");
        }
        finally
        {
            _fileLock.Release();
        }
    }
}