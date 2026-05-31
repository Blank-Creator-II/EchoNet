using System.Text.Json;
using EchoNet.Models;

namespace EchoNet.Services;

public class ThemeService : IThemeService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ThemeService> _logger;
    private string? _currentTheme;

    public ThemeService(IWebHostEnvironment env, ILogger<ThemeService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public void SetTheme(string themeName)
    {
        if (_currentTheme == themeName) {return;}
        _logger.LogInformation($"Theme Set to {themeName}");
        _currentTheme = themeName;
    }

    public Theme GetTheme()
    {
        var path = Path.Combine(_env.WebRootPath, "theme", $"{_currentTheme}.json");

        if (!File.Exists(path))
        {
            path = Path.Combine(_env.WebRootPath, "theme", "Crimson Shadow.json");
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Theme>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new Theme();
    }

    public List<Theme> GetAllThemes()
    {
        var themes = new List<Theme>();

        var themeFolder = Path.Combine(_env.WebRootPath, "theme");

        if (!Directory.Exists(themeFolder))
        {
            _logger.LogWarning("Theme folder does not exist at {Path}", themeFolder);
            return themes;
        }

        var jsonFiles = Directory.GetFiles(themeFolder, "*.json");

        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file);

                var theme = JsonSerializer.Deserialize<Theme>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (theme != null)
                {
                    themes.Add(theme);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load theme from {File}", file);
            }
        }

        return themes;
    }
}