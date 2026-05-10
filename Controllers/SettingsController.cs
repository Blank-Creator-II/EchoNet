using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Models;
using EchoNet.ViewModels;

namespace EchoNet.Controllers;

public class SettingsController : Controller
{
    private readonly ILogger<SettingsController> _logger;
    private readonly IThemeService _theme;

    public SettingsController(ILogger<SettingsController> logger, IThemeService theme)
    {
        _logger = logger;
        _theme = theme;
    }

    public IActionResult Index()
    {
        return View("Index");
    }

    [HttpPost("Settings/ChangeTheme")]
    public IActionResult ChangeTheme(string themeName)
    {
        _theme.SetTheme(themeName);
        return Ok();
    }
}