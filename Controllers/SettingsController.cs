using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Utils;
using EchoNet.Models;
using System.Threading.Tasks;

namespace EchoNet.Controllers;

public class SettingsController : Controller
{
    private readonly ILogger<SettingsController> _logger;
    private readonly IThemeService _themeService;
    private readonly AppDataJsonReader _appDataReader;

    public SettingsController(ILogger<SettingsController> logger, IThemeService themeService, AppDataJsonReader appDataReader)
    {
        _logger = logger;
        _themeService = themeService;
        _appDataReader = appDataReader;
    }

    public IActionResult Index()
    {
        _logger.LogDebug("Navigating to Settings Index view.");
        return View("Index");
    }

    [HttpPost("Settings/ChangeTheme")]
    public IActionResult ChangeTheme(string themeName)
    {
        _appDataReader.UpdateInMemory([(AppDataTarget.Theme, themeName)]);
        _logger.LogInformation("Application theme successfully updated to: {ThemeName}", themeName);
        return Ok();
    }
}