using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Models;
using EchoNet.Utils;
using EchoNet.ViewModels;

namespace EchoNet.Controllers;

public class PlayerController : Controller
{
    private readonly IAudioService _audio;
    private readonly ILibScannerService _libScannerService;
    private readonly AppDataJsonReader _appDataReader;
    private readonly IThemeService _themeService;
    private readonly ILogger<PlayerController> _logger;

    public PlayerController(IAudioService audio, ILibScannerService libScanner, AppDataJsonReader appDataReader, IThemeService themeService, ILogger<PlayerController> logger)
    {
        _audio = audio;
        _libScannerService = libScanner;
        _appDataReader = appDataReader;
        _themeService = themeService;
        _logger = logger;
    }

    [HttpPost("Player/Play")]
    public async Task<IActionResult> Play([FromBody] SongMetadata _song)
    {
        if (_song == null)
        {
            return NotFound();
        }
        
        _logger.LogInformation("Play request received with ID: {song.Id}", _song.Id);

        _audio.CurrentSongID = _song.Id;

        var filePath = _song.FilePath;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("Play request failed: path was null or empty");
            return BadRequest("No path provided");
        }

        await _audio.LoadAsync(filePath);
        await _audio.PlayAsync();

        _logger.LogInformation("Now playing: {FilePath}", filePath);

        return Ok(new
        {
            success = true,
            isPlaying = true,

            song = _song
        });
    }

    [HttpPost("Player/TogglePlay")]
    public async Task<IActionResult> TogglePlay()
    {
        if (_audio.IsPlaying)
            _audio.Pause();
        else
           await _audio.PlayAsync();
        
        return Ok(new
        {
            isPlaying = !_audio.IsPlaying,
            isSeekable = _audio.IsSeekable
        });
    }

    [HttpGet("Player/Status")]
    public IActionResult Status()
    {
        return Json(new
        {
            id = _audio.CurrentSongID,
            currentTime = _audio.CurrentTime.TotalSeconds,
            duration = _audio.Duration.TotalSeconds,
            isPlaying = _audio.IsPlaying,
            isSeekable = _audio.IsSeekable,
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

    [HttpPost("Player/FolderPicker")]
    public async Task<IActionResult> FolderPicker([FromBody] List<string> paths, CancellationToken cancellationToken)
    {
        if (paths == null || paths.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "No folders selected."
            });
        }

        var success = await _libScannerService.ScanFoldersAsync(paths, cancellationToken);

        if (!success)
        {
            return BadRequest(new
            {
                success = false,
                message = "Library setup failed."
            });
        }

        return Ok(new
        {
            success = true,
            message = "Library scan completed."
        });
    }

    [HttpPost("Player/SaveState")]
    public ActionResult SaveState([FromBody] SongMetadata song)
    {   
        _audio.SetSongMetadata(song);
        _appDataReader.UpdateInMemory();
        return Ok();
    }
}