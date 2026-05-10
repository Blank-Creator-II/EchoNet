using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Models;
using EchoNet.ViewModels;

namespace EchoNet.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View("Index");
    }   
}