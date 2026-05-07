using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Models;
using EchoNet.ViewModels;

namespace EchoNet.Controllers;

public class PlayerController : Controller
{
    private readonly IAudioService _audio;
    private readonly IThemeService _theme;
    private readonly ILogger<PlayerController> _logger;

    public PlayerController(IAudioService audio, IThemeService theme, ILogger<PlayerController> logger)
    {
        _audio = audio;
        _theme = theme;
        _logger = logger;
    }

    public IActionResult Index()
    {
        _theme.SetTheme("Crimson Cream"); //currently hard coded
        var vm = new PlayerViewModel
        {
            Songs = new List<Song>
            {
                new Song { Title="LOVE", FilePath="Test/LOVE. FEAT. ZACARI..mp3" },
                new Song { Title="Kyouran", FilePath="Test/Kyouran Hey Kids!!.mp3" }
            },
            CurrentSongTitle = TempData["CurrentSong"]?.ToString() ?? "Nothing Loaded",
            IsPlaying = _audio.IsPlaying
        }; 

        return View(vm);
    }

    [HttpPost("Player/Play")]
    public async Task<IActionResult> Play(string filePath, string title)
    {
        _logger.LogInformation($"Play request received with path: {filePath}");

        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("Play request failed: path was null or empty");
            return BadRequest("No path provided");
        }

        await _audio.LoadAsync(filePath);
        await _audio.PlayAsync();

        _logger.LogInformation($"Now playing: {filePath}");

        TempData["CurrentSong"] = title;
        return RedirectToAction("Index");
    }

    [HttpPost("Player/Pause")]
    public IActionResult Pause()
    {
        _audio.Pause();
        TempData["CurrentSong"] =  _audio.IsPlaying ? null : "title";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Player/Stop")]
    public IActionResult Stop()
    {
        _audio.Stop();
        TempData["CurrentSong"] = null;
        return RedirectToAction("Index");
    }
}