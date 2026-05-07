using System.Text.Json;
using EchoNet.Models;

namespace EchoNet.Services;

public class ThemeService : IThemeService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ThemeService> _logger;
    private string _currentTheme = "default";

    public ThemeService(IWebHostEnvironment env, ILogger<ThemeService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public void SetTheme(string themeName)
    {
        _logger.LogInformation($"Theme Set to {themeName}");
        _currentTheme = themeName;
    }

    public Theme GetTheme()
    {
        var path = Path.Combine(_env.WebRootPath, "theme", $"{_currentTheme}.json");

        if (!File.Exists(path))
        {
            path = Path.Combine(_env.WebRootPath, "theme", "default.json");
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Theme>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new Theme();
    }
}