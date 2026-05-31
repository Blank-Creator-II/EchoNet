using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Models;
using EchoNet.ViewModels;
using EchoNet.Utils;

namespace EchoNet.Controllers;

public class SongsController : Controller
{
    private readonly ILogger<SongsController> _logger;
    private readonly ISongService _songService;

    public SongsController(ILogger<SongsController> logger, ISongService songService)
    {
        _logger = logger;
        _songService = songService;
    }

    public async Task<IActionResult> Index()
    {
        var songs = await _songService.GetAllSongsAsync();
        List<SongMetadata> metadataSongs = new List<SongMetadata>{};

        foreach (Song song in songs)
        {
            metadataSongs.Add(MetadataHelper.ReadSongMetadata(song));
        }

        return View(metadataSongs);
    }
}