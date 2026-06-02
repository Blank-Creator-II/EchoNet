using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Models;
using EchoNet.Utils;
using EchoNet.ViewModels;

namespace EchoNet.Controllers;

public class HomeController : Controller
{
    private readonly IAudioService _audio;
    private readonly AppDataJsonReader _appDataReader;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IAudioService audio, ILogger<HomeController> logger, AppDataJsonReader appDataReader)
    {
        _audio = audio;
        _appDataReader = appDataReader;
        _logger = logger;
    }

    public IActionResult Index()
    {   
        _logger.LogDebug("Navigating to Home Index view.");
        return View("Index");
    }

    [HttpGet("Home/FolderPicker")]
    public IActionResult FolderPicker()
    {
        return PartialView("_FolderPicker");
    }
}