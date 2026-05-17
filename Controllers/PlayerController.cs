using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Models;
using EchoNet.ViewModels;

namespace EchoNet.Controllers;

public class PlayerController : Controller
{
    private readonly IAudioService _audio;
    private readonly ILogger<PlayerController> _logger;

    public PlayerController(IAudioService audio, IThemeService theme, ILogger<PlayerController> logger)
    {
        _audio = audio;
        _logger = logger;
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

        return RedirectToAction("Index");
    }

    [HttpPost("Player/TogglePlay")]
    public IActionResult TogglePlay()
    {
        if (_audio.IsPlaying)
            _audio.Pause();
        else
            _audio.PlayAsync();
        
        return Ok(new
        {
            isPlaying = !_audio.IsPlaying
        });
    }

    [HttpGet("Player/Status")]
    public IActionResult Status()
    {
        return Json(new
        {
            currentTime = _audio.CurrentTime.TotalSeconds,
            duration = _audio.Duration.TotalSeconds,
            isPlaying = _audio.IsPlaying,
            volume = _audio.Volume
        });
    }

    [HttpPost("Player/Seek")]
    public IActionResult Seek([FromBody] SeekRequest request)
    {
        _audio.Seek(TimeSpan.FromSeconds(request.Position));

        return Ok();
    }

    [HttpPost("Player/Volume")]
    public IActionResult Volume([FromBody] VolumeRequest request)
    {
        _audio.SetVolume(request.Volume);
        return Ok();
    }
}