using System.Text.Json;
using EchoNet.Models;
using EchoNet.Services;
using EchoNet.ViewModels;

namespace EchoNet.Utils;

public class AppDataJsonReader
{
    private readonly string _appDataPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<AppDataJsonReader> _logger;
    private readonly IAudioService _audio;
    private readonly IThemeService _themeService;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    // "In-Memory" app data for the UI to use
    private AppData _currentData = new();
    public AppData Current => _currentData;

    public AppDataJsonReader(IWebHostEnvironment env, ILogger<AppDataJsonReader> logger, IAudioService audio, IThemeService themeService)
    {
        _logger = logger;
        _audio = audio;
        _themeService = themeService;
        _appDataPath = Path.Combine(env.WebRootPath, "data", "AppData.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        EnsureFileExists();
        InitializeInMemoryData();
    }

    private void EnsureFileExists()
    {
        var directory = Path.GetDirectoryName(_appDataPath);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory!);

        if (!File.Exists(_appDataPath))
        {
            var json = JsonSerializer.Serialize(new AppData(), _jsonOptions);
            File.WriteAllText(_appDataPath, json);
        }
    }

    private void InitializeInMemoryData()
    {
        try
        {
            var json = File.ReadAllText(_appDataPath);
            _currentData = JsonSerializer.Deserialize<AppData>(json) ?? new AppData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AppData in memory.");
            _currentData = new AppData();
        }
    }

    // Updates the in-memory state without writing on disk.
    public void UpdateInMemory()
    {
        AppData newData = new AppData
        {
            Theme = _themeService.GetTheme().Name,
            songMetadata = _audio.GetSongMetadata(),
            Position = _audio.CurrentTime,
            Volume = _audio.Volume
        };
        _currentData = newData;
        
        _logger.LogInformation("AppData on memory updated");
    }

    // Saves the current in-memory state (or provided state) to the disk.
    public async Task SaveAsync(AppData? dataToSave = null)
    {
        var data = dataToSave ?? _currentData;
        
        await _fileLock.WaitAsync();
        try
        {
            _currentData = data; 
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(_appDataPath, json);
            _logger.LogInformation("AppData saved to disk.");
        }
        finally
        {
            _fileLock.Release();
        }
    }
}