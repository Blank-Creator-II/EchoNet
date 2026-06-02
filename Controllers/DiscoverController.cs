using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Models;
using EchoNet.ViewModels;

namespace EchoNet.Controllers;

public class DiscoverController : Controller
{
    private readonly ILogger<DiscoverController> _logger;

    public DiscoverController(ILogger<DiscoverController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        _logger.LogDebug("Navigating to Discover Index view.");
        return View("Index");
    }   
}