using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Models;
using EchoNet.ViewModels;

namespace EchoNet.Controllers;

public class SongsController : Controller
{
    private readonly ILogger<SongsController> _logger;

    public SongsController(ILogger<SongsController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View("Index");
    }   
}