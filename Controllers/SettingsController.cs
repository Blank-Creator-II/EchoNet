using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Utils;
using EchoNet.Models;
using System.Threading.Tasks;

namespace EchoNet.Controllers;

public class SettingsController : Controller
{
    private readonly ILogger<SettingsController> _logger;
    private readonly IThemeService _theme;
    private readonly SettingsJsonReader _settingsReader;

    public SettingsController(ILogger<SettingsController> logger, IThemeService theme, SettingsJsonReader settingsReader)
    {
        _logger = logger;
        _theme = theme;
        _settingsReader = settingsReader;
    }

    public IActionResult Index()
    {
        return View("Index");
    }

    [HttpPost("Settings/ChangeTheme")]
    public async Task<IActionResult> ChangeTheme(string themeName)
    {
        _theme.SetTheme(themeName);
        await _settingsReader.SaveAsync(new AppSettings{Theme = themeName});

        return Ok();
    }
}