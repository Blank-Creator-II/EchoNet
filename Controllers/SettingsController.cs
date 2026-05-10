using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Models;
using EchoNet.ViewModels;

namespace EchoNet.Controllers;

public class SettingsController : Controller
{
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ILogger<SettingsController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View("Index");
    }   
}