using System.Text.Json;
using EchoNet.Models;

namespace EchoNet.Utils;

public class SettingsJsonReader
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public SettingsJsonReader(IWebHostEnvironment env)
    {
        _settingsPath = Path.Combine(env.WebRootPath, "data", "settings.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        EnsureFileExists();
    }

    private void EnsureFileExists()
    {
        var directory = Path.GetDirectoryName(_settingsPath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        if (!File.Exists(_settingsPath))
        {
            var defaultSettings = new AppSettings();

            var json = JsonSerializer.Serialize(defaultSettings, _jsonOptions);

            File.WriteAllText(_settingsPath, json);
        }
    }

    public async Task<AppSettings> LoadAsync()
    {
        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath);

            var settings = JsonSerializer.Deserialize<AppSettings>(json);

            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, _jsonOptions);

        await File.WriteAllTextAsync(_settingsPath, json);
    }
}